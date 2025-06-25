// Controllers/NotificationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Interfaces;

namespace TaskManager.Web.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Notifications
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 50);
            return View(notifications);
        }

        // GET: Notifications/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User)!;
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { count });
        }

        // GET: Notifications/GetRecent
        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var userId = _userManager.GetUserId(User)!;
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 10);
            
            return Json(notifications.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type.ToString(),
                isRead = n.IsRead,
                createdAt = n.CreatedAt,
                taskId = n.TaskId,
                projectId = n.ProjectId
            }));
        }

        // POST: Notifications/MarkAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            await _notificationService.MarkAsReadAsync(id, userId);
            return Json(new { success = true });
        }

        // POST: Notifications/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User)!;
            await _notificationService.MarkAllAsReadAsync(userId);
            return Json(new { success = true });
        }

        // POST: Notifications/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            await _notificationService.DeleteNotificationAsync(id, userId);
            return Json(new { success = true });
        }
    }
}