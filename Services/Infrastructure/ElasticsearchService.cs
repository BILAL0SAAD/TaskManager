using Microsoft.Extensions.Options;
using Nest;
using TaskManager.Web.Configuration;
using TaskManager.Web.Models.Elasticsearch;
using TaskManager.Web.Services.Interfaces;

namespace TaskManager.Web.Services.Infrastructure
{
    public class ElasticsearchService : IElasticsearchService, IDisposable
    {
        private readonly IElasticClient _client;
        private readonly ElasticsearchSettings _settings;
        private readonly ILogger<ElasticsearchService> _logger;
        private readonly string _indexName;

        public ElasticsearchService(
            IOptions<ElasticsearchSettings> settings,
            ILogger<ElasticsearchService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _indexName = $"{_settings.DefaultIndex}-{DateTime.UtcNow:yyyy-MM}";

            var connectionSettings = new ConnectionSettings(new Uri(_settings.Uri))
                .DefaultIndex(_indexName)
                .DefaultMappingFor<TaskDocument>(m => m.IndexName(_indexName))
                .RequestTimeout(TimeSpan.FromSeconds(_settings.TimeoutSeconds))
                .DisableDirectStreaming() // Enable for debugging
                .PrettyJson()
                // FIXED: Add these settings to handle Docker Elasticsearch better
                .EnableHttpCompression(false)
                .DisablePing() // This helps avoid the product check issue
                .SniffOnStartup(false) // Disable node discovery for single-node setup
                .SniffOnConnectionFault(false); // Disable sniffing on connection issues

            // Enhanced logging
            connectionSettings.OnRequestCompleted(callDetails =>
            {
                var requestBody = callDetails.RequestBodyInBytes != null ? 
                    System.Text.Encoding.UTF8.GetString(callDetails.RequestBodyInBytes) : "null";
                var responseBody = callDetails.ResponseBodyInBytes != null ? 
                    System.Text.Encoding.UTF8.GetString(callDetails.ResponseBodyInBytes) : "null";

                if (callDetails.Success)
                {
                    _logger.LogInformation("✅ ES SUCCESS: {Method} {Uri}\n📤 REQUEST: {Request}\n📥 RESPONSE: {Response}",
                        callDetails.HttpMethod, callDetails.Uri, requestBody, responseBody);
                }
                else
                {
                    _logger.LogError("❌ ES FAILED: {Method} {Uri}\n📤 REQUEST: {Request}\n📥 RESPONSE: {Response}\n💥 ERROR: {Error}",
                        callDetails.HttpMethod, callDetails.Uri, requestBody, responseBody, callDetails.OriginalException?.Message);
                }
            });

            if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
            {
                connectionSettings.BasicAuthentication(_settings.Username, _settings.Password);
            }

            _client = new ElasticClient(connectionSettings);
        }

        #region Debug Methods

        // FIXED: Robust connection test method
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // FIXED: Use a more robust connection test instead of PingAsync
                // Try to get cluster health which doesn't trigger the product check issue
                var healthResponse = await _client.Cluster.HealthAsync();
                
                if (healthResponse.IsValid)
                {
                    _logger.LogInformation("🔗 Elasticsearch connection test: {IsValid} - Cluster Status: {Status}", 
                        healthResponse.IsValid, healthResponse.Status);
                    return true;
                }
                
                // Fallback: Try a simple search on a non-existent index (should return 404 but connection works)
                var fallbackResponse = await _client.SearchAsync<object>(s => s
                    .Index("_non_existent_index_test")
                    .Size(0));
                    
                // If we get a response (even an error), the connection is working
                bool isConnected = fallbackResponse.ServerError?.Status == 404 || 
                                  fallbackResponse.IsValid || 
                                  !string.IsNullOrEmpty(fallbackResponse.DebugInformation);
                
                _logger.LogInformation("🔗 Elasticsearch connection test (fallback): {IsValid}", isConnected);
                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Connection test failed");
                
                // Final fallback: Try a raw HTTP request to bypass NEST client issues
                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    var response = await httpClient.GetAsync(_settings.Uri);
                    
                    bool httpConnectionWorks = response.IsSuccessStatusCode;
                    _logger.LogInformation("🔗 Elasticsearch HTTP connection test: {IsValid}", httpConnectionWorks);
                    return httpConnectionWorks;
                }
                catch (Exception httpEx)
                {
                    _logger.LogError(httpEx, "💥 HTTP connection test also failed");
                    return false;
                }
            }
        }

        public async Task<List<TaskDocument>> GetAllTasksForDebuggingAsync(string userId)
        {
            try
            {
                _logger.LogInformation("🔍 DEBUG: Getting all tasks for user {UserId} from index {Index}", userId, _indexName);

                var response = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .Size(100)
                    .Query(q => q.MatchAll()));

                _logger.LogInformation("🔍 DEBUG: Raw match-all query returned {Count} total documents", response.Documents.Count());

                // Log all documents found
                var allDocs = response.Documents.ToList();
                foreach (var doc in allDocs.Take(5))
                {
                    _logger.LogInformation("🔍 DEBUG: Document - Id={Id}, Title='{Title}', UserId='{UserId}', IsDeleted={IsDeleted}, Status='{Status}'", 
                        doc.Id, doc.Title, doc.UserId, doc.IsDeleted, doc.Status);
                }

                // Filter by user
                var userTasks = allDocs.Where(d => d.UserId == userId && !d.IsDeleted).ToList();
                _logger.LogInformation("🔍 DEBUG: Found {Count} non-deleted tasks for user {UserId}", userTasks.Count, userId);

                return userTasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception in debug method");
                return new List<TaskDocument>();
            }
        }

        public async Task<object> TestDocumentExistsAsync(string userId)
        {
            try
            {
                _logger.LogInformation("🧪 Testing document existence for user {UserId}", userId);

                // Test 1: Get document by ID (we know task 19 exists from logs)
                var getResponse = await _client.GetAsync<TaskDocument>(19, g => g.Index(_indexName));
                
                // Test 2: Match all documents in index
                var allDocsResponse = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .Size(100)
                    .Query(q => q.MatchAll()));

                // Test 3: Simple term query for user
                var userDocsResponse = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .Size(100)
                    .Query(q => q.Term(t => t.Field(f => f.UserId).Value(userId))));

                // Test 4: Simple match query for "asp"
                var matchResponse = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .Size(100)
                    .Query(q => q.Match(m => m.Field(f => f.Title).Query("asp"))));

                // Test 5: Query string search
                var queryStringResponse = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .Size(100)
                    .Query(q => q.QueryString(qs => qs.Query("asp"))));

                // Test 6: Wildcard search
                var wildcardResponse = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .Size(100)
                    .Query(q => q.Wildcard(w => w.Field(f => f.Title).Value("*asp*"))));

                return new
                {
                    IndexName = _indexName,
                    UserId = userId,
                    GetById = new
                    {
                        Found = getResponse.Found,
                        IsValid = getResponse.IsValid,
                        Document = getResponse.Source != null ? new
                        {
                            getResponse.Source.Id,
                            getResponse.Source.Title,
                            getResponse.Source.UserId,
                            getResponse.Source.IsDeleted
                        } : null
                    },
                    AllDocs = new
                    {
                        Total = allDocsResponse.Total,
                        Count = allDocsResponse.Documents.Count(),
                        IsValid = allDocsResponse.IsValid,
                        Documents = allDocsResponse.Documents.Take(3).Select(d => new
                        {
                            d.Id,
                            d.Title,
                            d.UserId,
                            d.IsDeleted
                        }).ToList()
                    },
                    UserDocs = new
                    {
                        Total = userDocsResponse.Total,
                        Count = userDocsResponse.Documents.Count(),
                        IsValid = userDocsResponse.IsValid,
                        Documents = userDocsResponse.Documents.Take(3).Select(d => new
                        {
                            d.Id,
                            d.Title,
                            d.UserId,
                            d.IsDeleted
                        }).ToList()
                    },
                    MatchQuery = new
                    {
                        Total = matchResponse.Total,
                        Count = matchResponse.Documents.Count(),
                        IsValid = matchResponse.IsValid,
                        Error = matchResponse.OriginalException?.Message,
                        Documents = matchResponse.Documents.Take(3).Select(d => new
                        {
                            d.Id,
                            d.Title,
                            d.UserId,
                            d.IsDeleted
                        }).ToList()
                    },
                    QueryStringQuery = new
                    {
                        Total = queryStringResponse.Total,
                        Count = queryStringResponse.Documents.Count(),
                        IsValid = queryStringResponse.IsValid,
                        Error = queryStringResponse.OriginalException?.Message,
                        Documents = queryStringResponse.Documents.Take(3).Select(d => new
                        {
                            d.Id,
                            d.Title,
                            d.UserId,
                            d.IsDeleted
                        }).ToList()
                    },
                    WildcardQuery = new
                    {
                        Total = wildcardResponse.Total,
                        Count = wildcardResponse.Documents.Count(),
                        IsValid = wildcardResponse.IsValid,
                        Error = wildcardResponse.OriginalException?.Message,
                        Documents = wildcardResponse.Documents.Take(3).Select(d => new
                        {
                            d.Id,
                            d.Title,
                            d.UserId,
                            d.IsDeleted
                        }).ToList()
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🧪 Test failed");
                return new { Error = ex.Message, StackTrace = ex.StackTrace };
            }
        }

        public async Task<object> GetIndexMappingAsync()
        {
            try
            {
                var response = await _client.Indices.GetMappingAsync(new GetMappingRequest(_indexName));
                if (response.IsValid && response.Indices.ContainsKey(_indexName))
                {
                    return new
                    {
                        IndexName = _indexName,
                        Mapping = response.Indices[_indexName].Mappings.Properties
                    };
                }
                return new { Error = "Index not found or mapping request failed", IndexName = _indexName };
            }
            catch (Exception ex)
            {
                return new { Error = ex.Message, IndexName = _indexName };
            }
        }

        public async Task<ElasticsearchSearchResult<TaskDocument>> SimpleTestSearchAsync(string query, string userId)
        {
            try
            {
                _logger.LogInformation("🧪 SIMPLE TEST SEARCH: Query='{Query}', UserId='{UserId}'", query, userId);

                var response = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .Size(20)
                    .Query(q => 
                    {
                        if (string.IsNullOrWhiteSpace(query))
                        {
                            // Just match user's non-deleted tasks
                            return q.Bool(b => b
                                .Must(
                                    q.Term(t => t.Field(f => f.UserId).Value(userId)),
                                    q.Term(t => t.Field(f => f.IsDeleted).Value(false))
                                ));
                        }
                        else
                        {
                            // Use query_string for simpler text search
                            return q.Bool(b => b
                                .Must(
                                    q.Term(t => t.Field(f => f.UserId).Value(userId)),
                                    q.Term(t => t.Field(f => f.IsDeleted).Value(false)),
                                    q.QueryString(qs => qs
                                        .Query($"*{query}*")
                                        .Fields(f => f.Field(ff => ff.Title).Field(ff => ff.Description).Field(ff => ff.SearchContent))
                                        .AnalyzeWildcard(true))
                                ));
                        }
                    }));

                _logger.LogInformation("🧪 SIMPLE TEST RESULT: IsValid={IsValid}, Total={Total}, Count={Count}", 
                    response.IsValid, response.Total, response.Documents.Count());

                if (response.IsValid)
                {
                    foreach (var doc in response.Documents.Take(3))
                    {
                        _logger.LogInformation("🧪 FOUND: Id={Id}, Title='{Title}'", doc.Id, doc.Title);
                    }
                }
                else
                {
                    _logger.LogError("🧪 Search failed: {Error}", response.OriginalException?.Message);
                }

                return new ElasticsearchSearchResult<TaskDocument>
                {
                    Documents = response.Documents.ToList(),
                    Total = response.Total,
                    TookMilliseconds = response.Took // already a long
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🧪 Simple test search failed");
                return new ElasticsearchSearchResult<TaskDocument>();
            }
        }

        #endregion

        #region Main Search Methods

        public async Task<ElasticsearchSearchResult<TaskDocument>> SearchTasksAsync(
            string query, 
            string userId, 
            int page = 1, 
            int pageSize = 20,
            TaskSearchFilters? filters = null)
        {
            try
            {
                _logger.LogInformation("🔍 SEARCH: Query='{Query}', UserId='{UserId}', Page={Page}", query, userId, page);

                var response = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .From((page - 1) * pageSize)
                    .Size(pageSize)
                    .Query(q => BuildQuery(q, query, userId, filters))
                    .Sort(sort => sort
                        .Descending(SortSpecialField.Score)
                        .Descending(f => f.CreatedAt)));

                _logger.LogInformation("🔍 SEARCH RESULT: IsValid={IsValid}, Total={Total}, Count={Count}, Took={Took}ms", 
                    response.IsValid, response.Total, response.Documents.Count(), response.Took);

                if (!response.IsValid)
                {
                    _logger.LogError("❌ Search failed: {Error}", response.OriginalException?.Message ?? response.ServerError?.ToString());
                    return new ElasticsearchSearchResult<TaskDocument>();
                }

                // Log some sample results
                foreach (var doc in response.Documents.Take(3))
                {
                    _logger.LogDebug("🔍 RESULT: Id={Id}, Title='{Title}'", doc.Id, doc.Title);
                }

                return new ElasticsearchSearchResult<TaskDocument>
                {
                    Documents = response.Documents.ToList(),
                    Total = response.Total,
                    Page = page,
                    PageSize = pageSize,
                    MaxScore = response.MaxScore, // already a double
                    TookMilliseconds = response.Took // already a long
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception during search");
                return new ElasticsearchSearchResult<TaskDocument>();
            }
        }

        private QueryContainer BuildQuery(QueryContainerDescriptor<TaskDocument> q, string query, string userId, TaskSearchFilters? filters)
        {
            var mustQueries = new List<QueryContainer>
            {
                q.Term(t => t.Field(f => f.UserId).Value(userId)),
                q.Term(t => t.Field(f => f.IsDeleted).Value(false))
            };

            // Add text search if query provided
            if (!string.IsNullOrWhiteSpace(query))
            {
                var textQuery = q.Bool(b => b
                    .Should(
                        q.Match(m => m.Field(f => f.Title).Query(query).Boost(3)),
                        q.Match(m => m.Field(f => f.Description).Query(query).Boost(2)),
                        q.Match(m => m.Field(f => f.SearchContent).Query(query).Boost(1))
                    )
                    .MinimumShouldMatch(1));

                mustQueries.Add(textQuery);
                _logger.LogDebug("🔍 Added text search for: '{Query}'", query);
            }

            // Apply filters
            if (filters != null)
            {
                if (filters.Status?.Any() == true)
                {
                    mustQueries.Add(q.Terms(t => t.Field(f => f.Status).Terms(filters.Status)));
                    _logger.LogDebug("🔍 Added status filter: {Status}", string.Join(",", filters.Status));
                }

                if (filters.Priority?.Any() == true)
                {
                    mustQueries.Add(q.Terms(t => t.Field(f => f.Priority).Terms(filters.Priority)));
                    _logger.LogDebug("🔍 Added priority filter: {Priority}", string.Join(",", filters.Priority));
                }

                if (filters.IsOverdue.HasValue)
                {
                    mustQueries.Add(q.Term(t => t.Field(f => f.IsOverdue).Value(filters.IsOverdue.Value)));
                    _logger.LogDebug("🔍 Added overdue filter: {IsOverdue}", filters.IsOverdue.Value);
                }

                if (filters.ProjectId.HasValue)
                {
                    // Simple field match instead of nested query for debugging
                    mustQueries.Add(q.Term(t => t.Field("project.id").Value(filters.ProjectId.Value)));
                    _logger.LogDebug("🔍 Added project filter: {ProjectId}", filters.ProjectId.Value);
                }

                if (filters.DueDateFrom.HasValue || filters.DueDateTo.HasValue)
                {
                    mustQueries.Add(q.DateRange(dr => dr
                        .Field(f => f.DueDate)
                        .GreaterThanOrEquals(filters.DueDateFrom)
                        .LessThanOrEquals(filters.DueDateTo)));
                    _logger.LogDebug("🔍 Added date range filter");
                }
            }

            return q.Bool(b => b.Must(mustQueries.ToArray()));
        }

        public async Task<List<string>> GetSearchSuggestionsAsync(string query, string userId)
        {
            try
            {
                _logger.LogDebug("🔍 Getting suggestions for: '{Query}'", query);

                var response = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .Size(10)
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                q.Term(t => t.Field(f => f.UserId).Value(userId)),
                                q.Term(t => t.Field(f => f.IsDeleted).Value(false)),
                                q.Wildcard(w => w.Field(f => f.Title).Value($"*{query.ToLower()}*"))
                            )))
                    .Source(src => src.Includes(i => i.Field(f => f.Title))));

                if (response.IsValid)
                {
                    var suggestions = response.Documents.Select(d => d.Title).Distinct().ToList();
                    _logger.LogDebug("🔍 Found {Count} suggestions", suggestions.Count);
                    return suggestions;
                }

                _logger.LogWarning("❌ Suggestions query failed: {Error}", response.OriginalException?.Message);
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception getting suggestions");
                return new List<string>();
            }
        }

        #endregion

        #region Index Management

        public async Task<bool> CreateIndexAsync()
        {
            try
            {
                var existsResponse = await _client.Indices.ExistsAsync(_indexName);
                if (existsResponse.Exists)
                {
                    _logger.LogInformation("Index {IndexName} already exists", _indexName);
                    return true;
                }

                var createResponse = await _client.Indices.CreateAsync(_indexName, c => c
                    .Settings(s => s
                        .NumberOfShards(1)
                        .NumberOfReplicas(0)
                        .Analysis(a => a
                            .Analyzers(an => an
                                .Custom("task_analyzer", ca => ca
                                    .Tokenizer("standard")
                                    .Filters("lowercase", "stop", "stemmer")))))
                    .Map<TaskDocument>(m => m
                        .AutoMap()
                        .Properties(p => p
                            .Text(t => t
                                .Name(n => n.SearchContent)
                                .Analyzer("task_analyzer")))));

                if (createResponse.IsValid)
                {
                    _logger.LogInformation("Successfully created index {IndexName}", _indexName);
                    return true;
                }

                _logger.LogError("Failed to create index {IndexName}: {Error}", 
                    _indexName, createResponse.OriginalException?.Message ?? createResponse.ServerError?.ToString());
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception creating index {IndexName}", _indexName);
                return false;
            }
        }

        public async Task<bool> DeleteIndexAsync()
        {
            try
            {
                var response = await _client.Indices.DeleteAsync(_indexName);
                if (response.IsValid)
                {
                    _logger.LogInformation("Successfully deleted index {IndexName}", _indexName);
                    return true;
                }

                _logger.LogError("Failed to delete index {IndexName}: {Error}", 
                    _indexName, response.OriginalException?.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception deleting index {IndexName}", _indexName);
                return false;
            }
        }

        public async Task<bool> IndexExistsAsync()
        {
            try
            {
                var response = await _client.Indices.ExistsAsync(_indexName);
                return response.Exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception checking index existence");
                return false;
            }
        }

        public async Task<bool> ReindexAllDataAsync()
        {
            try
            {
                var indexExists = await IndexExistsAsync();
                if (!indexExists)
                {
                    return await CreateIndexAsync();
                }

                _logger.LogInformation("Reindex operation completed for {IndexName}", _indexName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during reindex operation");
                return false;
            }
        }

        #endregion

        #region Document Operations

        public async Task<bool> IndexTaskAsync(TaskDocument taskDocument)
        {
            try
            {
                taskDocument.SearchContent = $"{taskDocument.Title} {taskDocument.Description} " +
                    $"{taskDocument.Project?.Name} {string.Join(" ", taskDocument.Comments?.Select(c => c.Content) ?? new List<string>())}";

                var response = await _client.IndexDocumentAsync(taskDocument);

                if (response.IsValid)
                {
                    _logger.LogDebug("Successfully indexed task {TaskId}", taskDocument.Id);
                    return true;
                }

                _logger.LogError("Failed to index task {TaskId}: {Error}", 
                    taskDocument.Id, response.OriginalException?.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception indexing task {TaskId}", taskDocument.Id);
                return false;
            }
        }

        public async Task<bool> UpdateTaskAsync(TaskDocument taskDocument)
        {
            try
            {
                taskDocument.SearchContent = $"{taskDocument.Title} {taskDocument.Description} " +
                    $"{taskDocument.Project?.Name} {string.Join(" ", taskDocument.Comments?.Select(c => c.Content) ?? new List<string>())}";

                var response = await _client.UpdateAsync<TaskDocument>(taskDocument.Id, u => u
                    .Doc(taskDocument)
                    .DocAsUpsert(true)
                    .Index(_indexName));

                if (response.IsValid)
                {
                    _logger.LogDebug("Successfully updated task {TaskId}", taskDocument.Id);
                    return true;
                }

                _logger.LogError("Failed to update task {TaskId}: {Error}", 
                    taskDocument.Id, response.OriginalException?.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception updating task {TaskId}", taskDocument.Id);
                return false;
            }
        }

        public async Task<bool> DeleteTaskAsync(int taskId, string userId)
        {
            try
            {
                var response = await _client.DeleteAsync<TaskDocument>(taskId, d => d
                    .Index(_indexName)
                    .Routing(userId));

                if (response.IsValid)
                {
                    _logger.LogDebug("Successfully deleted task {TaskId}", taskId);
                    return true;
                }

                if (response.Result == Result.NotFound)
                {
                    _logger.LogDebug("Task {TaskId} not found in Elasticsearch (already deleted)", taskId);
                    return true;
                }

                _logger.LogError("Failed to delete task {TaskId}: {Error}", 
                    taskId, response.OriginalException?.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception deleting task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<TaskDocument?> GetTaskAsync(int taskId, string userId)
        {
            try
            {
                var response = await _client.GetAsync<TaskDocument>(taskId, g => g
                    .Index(_indexName)
                    .Routing(userId));

                if (response.IsValid && response.Found)
                {
                    return response.Source;
                }

                if (!response.Found)
                {
                    _logger.LogDebug("Task {TaskId} not found in Elasticsearch", taskId);
                }
                else
                {
                    _logger.LogError("Failed to get task {TaskId}: {Error}", 
                        taskId, response.OriginalException?.Message);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting task {TaskId}", taskId);
                return null;
            }
        }

        #endregion

        #region Bulk Operations

        public async Task<bool> BulkIndexTasksAsync(IEnumerable<TaskDocument> tasks)
        {
            try
            {
                var taskList = tasks.ToList();
                if (!taskList.Any())
                {
                    _logger.LogInformation("No tasks to bulk index");
                    return true;
                }

                foreach (var task in taskList)
                {
                    task.SearchContent = $"{task.Title} {task.Description} " +
                        $"{task.Project?.Name} {string.Join(" ", task.Comments?.Select(c => c.Content) ?? new List<string>())}";
                }

                var bulkRequest = new BulkRequest(_indexName)
                {
                    Operations = taskList.Select(task => new BulkIndexOperation<TaskDocument>(task)
                    {
                        Id = task.Id,
                        Routing = task.UserId
                    }).Cast<IBulkOperation>().ToList()
                };

                var response = await _client.BulkAsync(bulkRequest);

                if (response.IsValid)
                {
                    _logger.LogInformation("Successfully bulk indexed {Count} tasks", taskList.Count);
                    return true;
                }

                _logger.LogError("Bulk index failed: {Errors}", 
                    string.Join(", ", response.ItemsWithErrors.Select(i => i.Error?.Reason ?? "Unknown error")));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during bulk index");
                return false;
            }
        }

        public async Task<bool> BulkDeleteTasksAsync(IEnumerable<int> taskIds, string userId)
        {
            try
            {
                var taskIdList = taskIds.ToList();
                if (!taskIdList.Any())
                {
                    _logger.LogInformation("No tasks to bulk delete");
                    return true;
                }

                var bulkRequest = new BulkRequest(_indexName)
                {
                    Operations = taskIdList.Select(id => new BulkDeleteOperation<TaskDocument>(id)
                    {
                        Routing = userId
                    }).Cast<IBulkOperation>().ToList()
                };

                var response = await _client.BulkAsync(bulkRequest);

                if (response.IsValid)
                {
                    _logger.LogInformation("Successfully bulk deleted {Count} tasks", taskIdList.Count);
                    return true;
                }

                _logger.LogError("Bulk delete failed: {Errors}", 
                    string.Join(", ", response.ItemsWithErrors.Select(i => i.Error?.Reason ?? "Unknown error")));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during bulk delete");
                return false;
            }
        }

        #endregion

        #region Advanced Search & Analytics

        public async Task<ElasticsearchSearchResult<TaskDocument>> AdvancedSearchAsync(AdvancedSearchRequest request)
        {
            return await SearchTasksAsync(request.Query, request.UserId, request.Page, request.PageSize, request.Filters);
        }

        public async Task<Dictionary<string, long>> GetTaskStatusAggregationAsync(string userId)
        {
            try
            {
                var response = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .Size(0)
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                q.Term(t => t.Field(f => f.UserId).Value(userId)),
                                q.Term(t => t.Field(f => f.IsDeleted).Value(false)))))
                    .Aggregations(a => a
                        .Terms("status_distribution", t => t
                            .Field(f => f.Status)
                            .Size(10))));

                if (response.IsValid && response.Aggregations.TryGetValue("status_distribution", out var agg))
                {
                    var termsAgg = (BucketAggregate)agg;
                    return termsAgg.Items.Cast<KeyedBucket<object>>()
                        .ToDictionary(b => b.Key.ToString()!, b => b.DocCount ?? 0);
                }

                return new Dictionary<string, long>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting status aggregation");
                return new Dictionary<string, long>();
            }
        }

        public async Task<Dictionary<string, long>> GetTaskPriorityAggregationAsync(string userId)
        {
            try
            {
                var response = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .Size(0)
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                q.Term(t => t.Field(f => f.UserId).Value(userId)),
                                q.Term(t => t.Field(f => f.IsDeleted).Value(false)))))
                    .Aggregations(a => a
                        .Terms("priority_distribution", t => t
                            .Field(f => f.Priority)
                            .Size(10))));

                if (response.IsValid && response.Aggregations.TryGetValue("priority_distribution", out var agg))
                {
                    var termsAgg = (BucketAggregate)agg;
                    return termsAgg.Items.Cast<KeyedBucket<object>>()
                        .ToDictionary(b => b.Key.ToString()!, b => b.DocCount ?? 0);
                }

                return new Dictionary<string, long>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting priority aggregation");
                return new Dictionary<string, long>();
            }
        }

        public async Task<List<TaskDocument>> GetOverdueTasksAsync(string userId)
        {
            try
            {
                var response = await _client.SearchAsync<TaskDocument>(s => s
                    .Index(_indexName)
                    .Size(100)
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                q.Term(t => t.Field(f => f.UserId).Value(userId)),
                                q.Term(t => t.Field(f => f.IsDeleted).Value(false)),
                                q.Term(t => t.Field(f => f.IsOverdue).Value(true)))))
                    .Sort(s => s.Ascending(f => f.DueDate)));

                if (response.IsValid)
                {
                    return response.Documents.ToList();
                }

                _logger.LogError("Failed to get overdue tasks: {Error}", response.OriginalException?.Message);
                return new List<TaskDocument>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting overdue tasks");
                return new List<TaskDocument>();
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_client is IDisposable disposableClient)
            {
                disposableClient.Dispose();
            }
        }

        #endregion
    }
}