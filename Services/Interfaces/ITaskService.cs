// Services/ITaskService.cs
using TaskManager.Web.Models;

namespace TaskManager.Web.Services.Interfaces
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskItem>> GetUserTasksAsync(string userId);
        Task<TaskItem?> GetTaskByIdAsync(int id, string userId);
        Task<TaskItem> CreateTaskAsync(TaskItem task);
        Task<TaskItem> UpdateTaskAsync(TaskItem task);
        Task DeleteTaskAsync(int id, string userId);
        Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId, string userId);
        Task AddCommentAsync(int taskId, string userId, string content);
        Task AddAttachmentAsync(int taskId, string userId, IFormFile file);
        Task<bool> TaskExistsAsync(int id, string userId);
    }
}
