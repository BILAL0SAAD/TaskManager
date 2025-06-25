// Services/Business/NotificationService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using TaskManager.Web.Data;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Interfaces;
using TaskManager.Web.Hubs;

namespace TaskManager.Web.Services.Business
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, int? taskId = null, int? projectId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                TaskId = taskId,
                ProjectId = projectId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send real-time notification using User groups (fixed)
            await SendRealTimeNotificationAsync(userId, new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type.ToString(),
                createdAt = notification.CreatedAt,
                taskId = notification.TaskId,
                projectId = notification.ProjectId
            });

            _logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, int take = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .Include(n => n.Task)
                .Include(n => n.Project)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Update real-time counter - FIXED to use groups
                var unreadCount = await GetUnreadCountAsync(userId);
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("UnreadCountUpdated", unreadCount);
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Update real-time counter - FIXED to use groups
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("UnreadCountUpdated", 0);
        }

        public async Task DeleteNotificationAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                // Update unread count after deletion
                var unreadCount = await GetUnreadCountAsync(userId);
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("UnreadCountUpdated", unreadCount);
            }
        }

        public async Task SendRealTimeNotificationAsync(string userId, object notification)
        {
            try
            {
                // FIXED: Use groups instead of User
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send real-time notification to user {UserId}", userId);
            }
        }

        // Specific notification methods
        public async Task NotifyTaskDueAsync(string userId, TaskItem task)
        {
            var dueText = task.DueDate?.Date == DateTime.Today ? "today" : "tomorrow";
            await CreateNotificationAsync(
                userId,
                "Task Due Soon ⏰",
                $"Task '{task.Title}' is due {dueText}",
                NotificationType.TaskDue,
                taskId: task.Id
            );
        }

        public async Task NotifyTaskOverdueAsync(string userId, TaskItem task)
        {
            var daysPast = (DateTime.Now.Date - task.DueDate!.Value.Date).Days;
            await CreateNotificationAsync(
                userId,
                "Task Overdue 🚨",
                $"Task '{task.Title}' is {daysPast} day(s) overdue",
                NotificationType.TaskOverdue,
                taskId: task.Id
            );
        }

        public async Task NotifyTaskCompletedAsync(string userId, TaskItem task)
        {
            await CreateNotificationAsync(
                userId,
                "Task Completed 🎉",
                $"Great job! You completed '{task.Title}'",
                NotificationType.TaskCompleted,
                taskId: task.Id
            );
        }

        public async Task NotifyProjectCreatedAsync(string userId, Project project)
        {
            await CreateNotificationAsync(
                userId,
                "Project Created 📁",
                $"New project '{project.Name}' has been created",
                NotificationType.ProjectCreated,
                projectId: project.Id
            );
        }
    }
}