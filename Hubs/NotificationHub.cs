// Hubs/NotificationHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using TaskManager.Web.Models;

namespace TaskManager.Web.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationHub> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = _userManager.GetUserId(Context.User);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation("User {UserId} connected to notifications hub", userId);
            }
            else
            {
                _logger.LogWarning("Anonymous user attempted to connect to notifications hub");
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = _userManager.GetUserId(Context.User);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation("User {UserId} disconnected from notifications hub", userId);
            }
            
            if (exception != null)
            {
                _logger.LogError(exception, "User disconnected with error");
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        // Optional: Method to join a specific user group (called from client)
        public async Task JoinUserGroup(string userId)
        {
            var currentUserId = _userManager.GetUserId(Context.User);
            
            // Security check: users can only join their own group
            if (currentUserId == userId)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation("User {UserId} manually joined their notification group", userId);
            }
            else
            {
                _logger.LogWarning("User {CurrentUserId} attempted to join group for user {RequestedUserId}", 
                    currentUserId, userId);
            }
        }

        // Optional: Method to leave a user group
        public async Task LeaveUserGroup(string userId)
        {
            var currentUserId = _userManager.GetUserId(Context.User);
            
            if (currentUserId == userId)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation("User {UserId} left their notification group", userId);
            }
        }

        // Optional: Ping method for connection testing
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
        }
    }
}