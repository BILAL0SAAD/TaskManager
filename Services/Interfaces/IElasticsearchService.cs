using TaskManager.Web.Models.Elasticsearch;

namespace TaskManager.Web.Services.Interfaces
{
    public interface IElasticsearchService
    {
        // Index Management
        Task<bool> CreateIndexAsync();
        Task<bool> DeleteIndexAsync();
        Task<bool> IndexExistsAsync();
        Task<bool> ReindexAllDataAsync();

        // Document Operations
        Task<bool> IndexTaskAsync(TaskDocument taskDocument);
        Task<bool> UpdateTaskAsync(TaskDocument taskDocument);
        Task<bool> DeleteTaskAsync(int taskId, string userId);
        Task<TaskDocument?> GetTaskAsync(int taskId, string userId);

        // Bulk Operations
        Task<bool> BulkIndexTasksAsync(IEnumerable<TaskDocument> tasks);
        Task<bool> BulkDeleteTasksAsync(IEnumerable<int> taskIds, string userId);

        // Search Operations
        Task<ElasticsearchSearchResult<TaskDocument>> SearchTasksAsync(
            string query, 
            string userId, 
            int page = 1, 
            int pageSize = 20,
            TaskSearchFilters? filters = null);
            
        Task<List<string>> GetSearchSuggestionsAsync(string query, string userId);
        Task<ElasticsearchSearchResult<TaskDocument>> AdvancedSearchAsync(
            AdvancedSearchRequest request);

        // Analytics
        Task<Dictionary<string, long>> GetTaskStatusAggregationAsync(string userId);
        Task<Dictionary<string, long>> GetTaskPriorityAggregationAsync(string userId);
        Task<List<TaskDocument>> GetOverdueTasksAsync(string userId);

        // Debug Operations
        Task<bool> TestConnectionAsync();
        Task<List<TaskDocument>> GetAllTasksForDebuggingAsync(string userId);
        Task<object> TestDocumentExistsAsync(string userId);
        Task<object> GetIndexMappingAsync();
        Task<ElasticsearchSearchResult<TaskDocument>> SimpleTestSearchAsync(string query, string userId);
    }

    public class ElasticsearchSearchResult<T>
    {
        public List<T> Documents { get; set; } = new();
        public long Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public double MaxScore { get; set; }
        public long TookMilliseconds { get; set; }
        public Dictionary<string, object> Aggregations { get; set; } = new();
    }

    public class TaskSearchFilters
    {
        public List<string>? Status { get; set; }
        public List<string>? Priority { get; set; }
        public int? ProjectId { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public bool? IsOverdue { get; set; }
        public List<string>? Tags { get; set; }
    }

    public class AdvancedSearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public TaskSearchFilters? Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        public bool IncludeHighlights { get; set; } = true;
        public bool IncludeAggregations { get; set; } = false;
    }
}