using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Business;
using System.Diagnostics;

namespace TaskManager.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ICachedDashboardService _dashboardService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ICachedDashboardService dashboardService,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            
            // Add timing to measure cache performance
            var stopwatch = Stopwatch.StartNew();
            var dashboardData = await _dashboardService.GetDashboardDataAsync(userId);
            stopwatch.Stop();
            
            _logger.LogInformation("Dashboard loaded for user {UserId} in {ElapsedMs}ms", 
                userId, stopwatch.ElapsedMilliseconds);
            
            // Add cache status to ViewBag for debugging
            ViewBag.LoadTime = stopwatch.ElapsedMilliseconds;
            
            return View(dashboardData);
        }

        // Action to manually clear cache for testing
        [HttpPost]
        public async Task<IActionResult> ClearCache()
        {
            var userId = _userManager.GetUserId(User)!;
            await _dashboardService.InvalidateDashboardCacheAsync(userId);
            
            _logger.LogInformation("Cache cleared for user {UserId}", userId);
            TempData["SuccessMessage"] = "Cache cleared successfully!";
            
            return RedirectToAction(nameof(Index));
        }

        // Action to get cache stats for debugging
        public async Task<IActionResult> CacheStats()
        {
            var userId = _userManager.GetUserId(User)!;
            var userStats = await _dashboardService.GetUserStatsAsync(userId);
            
            return Json(new
            {
                userId = userId,
                userStats = userStats,
                timestamp = DateTime.UtcNow
            });
        }
    }
}