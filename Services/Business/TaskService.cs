using Microsoft.EntityFrameworkCore;
using TaskManager.Web.Data;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Infrastructure; // FIXED: Capital I
using TaskManager.Web.Services.Interfaces;
using TaskManager.Web.BackgroundJobs; // ADD THIS
using TaskManager.Web.Extensions; // ADD THIS

namespace TaskManager.Web.Services.Business
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<TaskService> _logger;
        private readonly ElasticsearchSyncJob _elasticsearchSync; // ADD THIS

        public TaskService(
            ApplicationDbContext context, 
            IFileService fileService,
            ICacheService cacheService,
            ILogger<TaskService> logger,
            ElasticsearchSyncJob elasticsearchSync) // ADD THIS PARAMETER
        {
            _context = context;
            _fileService = fileService;
            _cacheService = cacheService;
            _logger = logger;
            _elasticsearchSync = elasticsearchSync; // ADD THIS
        }

        public async Task<IEnumerable<TaskItem>> GetUserTasksAsync(string userId)
        {
            var cacheKey = $"user_tasks_{userId}";
            var cachedTasks = await _cacheService.GetAsync<List<TaskItem>>(cacheKey);
            
            if (cachedTasks != null)
                return cachedTasks;

            // ✅ FIXED: No .Include() to avoid circular references
            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .AsNoTracking() // ✅ ADDED: Prevents tracking
                .Select(t => new TaskItem
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt,
                    ProjectId = t.ProjectId,
                    UserId = t.UserId,
                    IsDeleted = t.IsDeleted
                    // ✅ FIXED: No navigation properties loaded
                })
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            await _cacheService.SetAsync(cacheKey, tasks, TimeSpan.FromMinutes(10));
            return tasks;
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id, string userId)
        {
            // ✅ FIXED: No .Include() to avoid circular references
            return await _context.Tasks
                .Where(t => t.Id == id && t.UserId == userId && !t.IsDeleted)
                .AsNoTracking()
                .Select(t => new TaskItem
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt,
                    ProjectId = t.ProjectId,
                    UserId = t.UserId,
                    IsDeleted = t.IsDeleted
                    // ✅ FIXED: No navigation properties
                })
                .FirstOrDefaultAsync();
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem task)
        {
            // ✅ FIXED: Clear navigation properties before saving
            task.Project = null;
            task.User = null;
            task.Comments = new List<TaskComment>();
            task.Attachments = new List<TaskAttachment>();
            
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Clear cache
            await _cacheService.RemoveAsync($"user_tasks_{task.UserId}");

            // ✅ ADD: Sync to Elasticsearch
            try
            {
                await _elasticsearchSync.SyncTaskAsync(task.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync task {TaskId} to Elasticsearch", task.Id);
                // Don't fail the operation if Elasticsearch sync fails
            }

            _logger.LogInformation("Task created: {TaskId} by user {UserId}", task.Id, task.UserId);
            return task;
        }

        public async Task<TaskItem> UpdateTaskAsync(TaskItem task)
        {
            // ✅ FIXED: Clear navigation properties before saving
            task.Project = null;
            task.User = null;
            task.Comments = new List<TaskComment>();
            task.Attachments = new List<TaskAttachment>();
            
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();

            // Clear cache
            await _cacheService.RemoveAsync($"user_tasks_{task.UserId}");

            // ✅ ADD: Sync to Elasticsearch
            try
            {
                await _elasticsearchSync.SyncTaskAsync(task.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync task {TaskId} to Elasticsearch", task.Id);
                // Don't fail the operation if Elasticsearch sync fails
            }

            _logger.LogInformation("Task updated: {TaskId} by user {UserId}", task.Id, task.UserId);
            return task;
        }

        public async Task DeleteTaskAsync(int id, string userId)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task != null)
            {
                task.IsDeleted = true;
                await _context.SaveChangesAsync();

                // Clear cache
                await _cacheService.RemoveAsync($"user_tasks_{userId}");

                // ✅ ADD: Sync to Elasticsearch (will remove from index)
                try
                {
                    await _elasticsearchSync.SyncTaskAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync task {TaskId} deletion to Elasticsearch", id);
                    // Don't fail the operation if Elasticsearch sync fails
                }

                _logger.LogInformation("Task deleted: {TaskId} by user {UserId}", id, userId);
            }
        }

        public async Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId, string userId)
        {
            // ✅ FIXED: No .Include() to avoid circular references
            return await _context.Tasks
                .Where(t => t.ProjectId == projectId && t.UserId == userId && !t.IsDeleted)
                .AsNoTracking()
                .Select(t => new TaskItem
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt,
                    ProjectId = t.ProjectId,
                    UserId = t.UserId,
                    IsDeleted = t.IsDeleted
                })
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task AddCommentAsync(int taskId, string userId, string content)
        {
            var comment = new TaskComment
            {
                TaskId = taskId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.TaskComments.Add(comment);
            await _context.SaveChangesAsync();

            // Clear cache
            await _cacheService.RemoveAsync($"user_tasks_{userId}");

            // ✅ ADD: Sync to Elasticsearch (comments affect search content)
            try
            {
                await _elasticsearchSync.SyncTaskAsync(taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync task {TaskId} after comment to Elasticsearch", taskId);
            }

            _logger.LogInformation("Comment added to task {TaskId} by user {UserId}", taskId, userId);
        }

        public async Task AddAttachmentAsync(int taskId, string userId, IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var filePath = await _fileService.UploadFileAsync(stream, file.FileName, file.ContentType);

            var attachment = new TaskAttachment
            {
                TaskId = taskId,
                UserId = userId,
                FileName = file.FileName,
                FilePath = filePath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            _context.TaskAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            // Clear cache
            await _cacheService.RemoveAsync($"user_tasks_{userId}");

            _logger.LogInformation("Attachment added to task {TaskId} by user {UserId}", taskId, userId);
        }

        public async Task<bool> TaskExistsAsync(int id, string userId)
        {
            return await _context.Tasks
                .AnyAsync(t => t.Id == id && t.UserId == userId && !t.IsDeleted);
        }
    }
}