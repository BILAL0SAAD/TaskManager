// Controllers/NotificationTestController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Interfaces;

namespace TaskManager.Web.Controllers
{
    [Authorize]
    public class NotificationTestController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationTestController> _logger;

        public NotificationTestController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationTestController> logger)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestNotification(string type = "System")
        {
            var userId = _userManager.GetUserId(User)!;
            
            try
            {
                switch (type.ToLower())
                {
                    case "taskdue":
                        await _notificationService.CreateNotificationAsync(
                            userId,
                            "Task Due Soon ⏰",
                            "Your task 'Complete Project Report' is due tomorrow!",
                            NotificationType.TaskDue);
                        break;

                    case "taskoverdue":
                        await _notificationService.CreateNotificationAsync(
                            userId,
                            "Task Overdue 🚨",
                            "Your task 'Review Documents' is 2 days overdue",
                            NotificationType.TaskOverdue);
                        break;

                    case "taskcompleted":
                        await _notificationService.CreateNotificationAsync(
                            userId,
                            "Task Completed 🎉",
                            "Congratulations! You completed 'Setup Development Environment'",
                            NotificationType.TaskCompleted);
                        break;

                    case "projectcreated":
                        await _notificationService.CreateNotificationAsync(
                            userId,
                            "Project Created 📁",
                            "New project 'Website Redesign' has been created successfully",
                            NotificationType.ProjectCreated);
                        break;

                    case "reminder":
                        await _notificationService.CreateNotificationAsync(
                            userId,
                            "Daily Reminder 🔔",
                            "Don't forget to review your tasks for today!",
                            NotificationType.Reminder);
                        break;

                    default:
                        await _notificationService.CreateNotificationAsync(
                            userId,
                            "Test Notification 📱",
                            "This is a test notification to verify the system is working!",
                            NotificationType.System);
                        break;
                }

                TempData["SuccessMessage"] = $"✅ Test notification sent successfully: {type}";
                _logger.LogInformation("Test notification sent: {Type} for user {UserId}", type, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test notification: {Type} for user {UserId}", type, userId);
                TempData["ErrorMessage"] = "❌ Failed to send test notification. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendBulkTest()
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                // Send multiple notifications for testing
                await _notificationService.CreateNotificationAsync(
                    userId, "Welcome! 👋", "Welcome to the enhanced notification system!", NotificationType.System);

                await Task.Delay(500); // Small delay between notifications

                await _notificationService.CreateNotificationAsync(
                    userId, "Task Due Soon ⏰", "Your project deadline is approaching", NotificationType.TaskDue);

                await Task.Delay(500);

                await _notificationService.CreateNotificationAsync(
                    userId, "Great Job! 🎉", "You've completed 5 tasks today!", NotificationType.TaskCompleted);

                await Task.Delay(500);

                await _notificationService.CreateNotificationAsync(
                    userId, "New Project 📁", "Website redesign project has been created", NotificationType.ProjectCreated);

                TempData["SuccessMessage"] = "✅ Bulk test notifications sent successfully! (4 notifications)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk test notifications for user {UserId}", userId);
                TempData["ErrorMessage"] = "❌ Failed to send test notifications.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}