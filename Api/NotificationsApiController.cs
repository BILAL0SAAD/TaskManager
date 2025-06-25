// Controllers/Api/NotificationsApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Interfaces;

namespace TaskManager.Web.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsApiController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationsApiController> _logger;

        public NotificationsApiController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationsApiController> logger)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/notifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int take = 10)
        {
            var userId = _userManager.GetUserId(User)!;
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, take);
            
            return Ok(notifications.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type.ToString(),
                isRead = n.IsRead,
                createdAt = n.CreatedAt,
                taskId = n.TaskId,
                projectId = n.ProjectId,
                taskTitle = n.Task?.Title,
                projectName = n.Project?.Name
            }));
        }

        // GET: api/notifications/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User)!;
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        // POST: api/notifications/{id}/mark-read
        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            await _notificationService.MarkAsReadAsync(id, userId);
            return Ok(new { success = true });
        }

        // POST: api/notifications/mark-all-read
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User)!;
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { success = true });
        }

        // DELETE: api/notifications/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            await _notificationService.DeleteNotificationAsync(id, userId);
            return Ok(new { success = true });
        }
    }
}