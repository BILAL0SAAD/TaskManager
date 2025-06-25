// BackgroundJobs/ElasticsearchSyncJob.cs
using TaskManager.Web.Data;
using TaskManager.Web.Extensions;
using TaskManager.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.Web.BackgroundJobs
{
    public class ElasticsearchSyncJob
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IElasticsearchService _elasticsearchService;
        private readonly ILogger<ElasticsearchSyncJob> _logger;

        public ElasticsearchSyncJob(
            ApplicationDbContext dbContext,
            IElasticsearchService elasticsearchService,
            ILogger<ElasticsearchSyncJob> logger)
        {
            _dbContext = dbContext;
            _elasticsearchService = elasticsearchService;
            _logger = logger;
        }

        public async Task SyncAllTasksAsync()
        {
            try
            {
                _logger.LogInformation("Starting Elasticsearch sync job");

                // Ensure index exists
                var indexExists = await _elasticsearchService.IndexExistsAsync();
                if (!indexExists)
                {
                    await _elasticsearchService.CreateIndexAsync();
                }

                // Get all tasks with related data
                var tasks = await _dbContext.Tasks
                    .Include(t => t.Project)
                    .Include(t => t.Comments.Where(c => !c.Task.IsDeleted))
                    .Where(t => !t.IsDeleted)
                    .ToListAsync();

                // Convert to Elasticsearch documents
                var taskDocuments = tasks.Select(t => t.ToElasticsearchDocument()).ToList();

                // Bulk index
                var success = await _elasticsearchService.BulkIndexTasksAsync(taskDocuments);

                if (success)
                {
                    _logger.LogInformation("Successfully synced {Count} tasks to Elasticsearch", taskDocuments.Count);
                }
                else
                {
                    _logger.LogError("Failed to sync tasks to Elasticsearch");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Elasticsearch sync");
            }
        }

        public async Task SyncTaskAsync(int taskId)
        {
            try
            {
                var task = await _dbContext.Tasks
                    .Include(t => t.Project)
                    .Include(t => t.Comments.Where(c => !c.Task.IsDeleted))
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for Elasticsearch sync", taskId);
                    return;
                }

                if (task.IsDeleted)
                {
                    await _elasticsearchService.DeleteTaskAsync(taskId, task.UserId);
                }
                else
                {
                    var taskDocument = task.ToElasticsearchDocument();
                    await _elasticsearchService.IndexTaskAsync(taskDocument);
                }

                _logger.LogDebug("Successfully synced task {TaskId} to Elasticsearch", taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing task {TaskId} to Elasticsearch", taskId);
            }
        }
    }
}