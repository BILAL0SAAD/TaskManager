// BackgroundJobs/NotificationJob.cs
using Microsoft.EntityFrameworkCore;
using TaskManager.Web.Data;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Interfaces;
using TaskStatus = TaskManager.Web.Models.TaskStatus; // FIXED: Explicit alias to resolve ambiguity

namespace TaskManager.Web.BackgroundJobs
{
    public class NotificationJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationJob> _logger;

        public NotificationJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<NotificationJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Send notifications for tasks due today or tomorrow
        /// </summary>
        public async System.Threading.Tasks.Task SendDueTaskNotifications() // FIXED: Explicit Task reference
        {
            try
            {
                _logger.LogInformation("Starting due task notifications job");

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Get tasks due today or tomorrow that haven't been notified recently
                var dueTasks = await _context.Tasks
                    .Include(t => t.User)
                    .Where(t => !t.IsDeleted && 
                               t.Status != TaskStatus.Done && 
                               t.DueDate.HasValue &&
                               (t.DueDate.Value.Date == today || t.DueDate.Value.Date == tomorrow))
                    .ToListAsync();

                var notificationsSent = 0;

                foreach (var task in dueTasks)
                {
                    // Check if we already sent a notification for this task recently
                    var recentNotification = await _context.Notifications
                        .Where(n => n.UserId == task.UserId && 
                                   n.TaskId == task.Id && 
                                   n.Type == NotificationType.TaskDue &&
                                   n.CreatedAt >= DateTime.UtcNow.AddHours(-12)) // Don't spam - only send once per 12 hours
                        .AnyAsync();

                    if (!recentNotification)
                    {
                        await _notificationService.NotifyTaskDueAsync(task.UserId, task);
                        notificationsSent++;
                        
                        // Small delay to prevent overwhelming the system
                        await System.Threading.Tasks.Task.Delay(100); // FIXED: Explicit Task reference
                    }
                }

                _logger.LogInformation("Due task notifications job completed. Sent {Count} notifications", notificationsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in due task notifications job");
                throw;
            }
        }

        /// <summary>
        /// Send notifications for overdue tasks
        /// </summary>
        public async System.Threading.Tasks.Task SendOverdueTaskNotifications() // FIXED: Explicit Task reference
        {
            try
            {
                _logger.LogInformation("Starting overdue task notifications job");

                var today = DateTime.Today;

                // Get tasks that are overdue and not completed
                var overdueTasks = await _context.Tasks
                    .Include(t => t.User)
                    .Where(t => !t.IsDeleted && 
                               t.Status != TaskStatus.Done && 
                               t.DueDate.HasValue &&
                               t.DueDate.Value.Date < today)
                    .ToListAsync();

                var notificationsSent = 0;

                foreach (var task in overdueTasks)
                {
                    // Check if we already sent an overdue notification recently (once per day)
                    var recentNotification = await _context.Notifications
                        .Where(n => n.UserId == task.UserId && 
                                   n.TaskId == task.Id && 
                                   n.Type == NotificationType.TaskOverdue &&
                                   n.CreatedAt >= DateTime.UtcNow.AddDays(-1))
                        .AnyAsync();

                    if (!recentNotification)
                    {
                        await _notificationService.NotifyTaskOverdueAsync(task.UserId, task);
                        notificationsSent++;
                        
                        // Small delay to prevent overwhelming the system
                        await System.Threading.Tasks.Task.Delay(100); // FIXED: Explicit Task reference
                    }
                }

                _logger.LogInformation("Overdue task notifications job completed. Sent {Count} notifications", notificationsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in overdue task notifications job");
                throw;
            }
        }

        /// <summary>
        /// Clean up old notifications (older than 30 days)
        /// </summary>
        public async System.Threading.Tasks.Task CleanupOldNotifications() // FIXED: Explicit Task reference
        {
            try
            {
                _logger.LogInformation("Starting notification cleanup job");

                var cutoffDate = DateTime.UtcNow.AddDays(-30);

                // Delete notifications older than 30 days
                var oldNotifications = await _context.Notifications
                    .Where(n => n.CreatedAt < cutoffDate)
                    .ToListAsync();

                if (oldNotifications.Any())
                {
                    _context.Notifications.RemoveRange(oldNotifications);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Notification cleanup completed. Deleted {Count} old notifications", oldNotifications.Count);
                }
                else
                {
                    _logger.LogInformation("Notification cleanup completed. No old notifications to delete");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification cleanup job");
                throw;
            }
        }

        /// <summary>
        /// Send daily summary notifications (optional - can be called manually or scheduled)
        /// </summary>
        public async System.Threading.Tasks.Task SendDailySummaryNotifications() // FIXED: Explicit Task reference
        {
            try
            {
                _logger.LogInformation("Starting daily summary notifications job");

                // Get all active users
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .ToListAsync();

                var summariesSent = 0;

                foreach (var user in users)
                {
                    // Check if user has any pending tasks
                    var pendingTasks = await _context.Tasks
                        .Where(t => t.UserId == user.Id && 
                                   !t.IsDeleted && 
                                   t.Status != TaskStatus.Done)
                        .CountAsync();

                    var completedToday = await _context.Tasks
                        .Where(t => t.UserId == user.Id && 
                                   !t.IsDeleted && 
                                   t.Status == TaskStatus.Done &&
                                   t.CompletedAt.HasValue &&
                                   t.CompletedAt.Value.Date == DateTime.Today)
                        .CountAsync();

                    // Only send summary if user has activity
                    if (pendingTasks > 0 || completedToday > 0)
                    {
                        var message = $"You have {pendingTasks} pending tasks";
                        if (completedToday > 0)
                        {
                            message += $" and completed {completedToday} tasks today";
                        }

                        await _notificationService.CreateNotificationAsync(
                            user.Id,
                            "Daily Task Summary 📊",
                            message,
                            NotificationType.Reminder);

                        summariesSent++;
                    }

                    // Small delay to prevent overwhelm the system
                    await System.Threading.Tasks.Task.Delay(500); // FIXED: Explicit Task reference
                }

                _logger.LogInformation("Daily summary notifications job completed. Sent {Count} summaries", summariesSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in daily summary notifications job");
                throw;
            }
        }
    }
}