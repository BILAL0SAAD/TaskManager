// Services/Interfaces/INotificationService.cs
using TaskManager.Web.Models;

namespace TaskManager.Web.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, int? taskId = null, int? projectId = null);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, int take = 10);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(int notificationId, string userId);
        Task SendRealTimeNotificationAsync(string userId, object notification);
        
        // Specific notification types
        Task NotifyTaskDueAsync(string userId, TaskItem task);
        Task NotifyTaskOverdueAsync(string userId, TaskItem task);
        Task NotifyTaskCompletedAsync(string userId, TaskItem task);
        Task NotifyProjectCreatedAsync(string userId, Project project);
    }
}