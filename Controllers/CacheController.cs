// Controllers/CacheController.cs - For monitoring and testing cache performance
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Interfaces;
using TaskManager.Web.Services.Business;
using System.Diagnostics;

namespace TaskManager.Web.Controllers
{
    [Authorize]
    public class CacheController : Controller
    {
        private readonly ICacheService _cacheService;
        private readonly ICachedDashboardService _dashboardService;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CacheController> _logger;

        public CacheController(
            ICacheService cacheService,
            ICachedDashboardService dashboardService,
            ICacheInvalidationService cacheInvalidation,
            UserManager<ApplicationUser> userManager,
            ILogger<CacheController> logger)
        {
            _cacheService = cacheService;
            _dashboardService = dashboardService;
            _cacheInvalidation = cacheInvalidation;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestCachePerformance()
        {
            var userId = _userManager.GetUserId(User)!;
            var results = new List<object>();

            // Test 1: Dashboard without cache
            await _cacheInvalidation.InvalidateUserCacheAsync(userId);
            var stopwatch = Stopwatch.StartNew();
            await _dashboardService.GetDashboardDataAsync(userId);
            stopwatch.Stop();
            var noCacheTime = stopwatch.ElapsedMilliseconds;

            // Test 2: Dashboard with cache
            stopwatch.Restart();
            await _dashboardService.GetDashboardDataAsync(userId);
            stopwatch.Stop();
            var cacheTime = stopwatch.ElapsedMilliseconds;

            results.Add(new
            {
                Test = "Dashboard Load (No Cache)",
                Time = $"{noCacheTime}ms",
                Status = noCacheTime < 500 ? "Good" : "Slow"
            });

            results.Add(new
            {
                Test = "Dashboard Load (With Cache)",
                Time = $"{cacheTime}ms",
                Status = cacheTime < 100 ? "Excellent" : "Good"
            });

            results.Add(new
            {
                Test = "Performance Improvement",
                Time = $"{Math.Round((double)(noCacheTime - cacheTime) / noCacheTime * 100, 1)}%",
                Status = cacheTime < noCacheTime / 2 ? "Significant" : "Moderate"
            });

            return Json(new { success = true, results });
        }

        [HttpPost]
        public async Task<IActionResult> ClearUserCache()
        {
            var userId = _userManager.GetUserId(User)!;
            await _cacheInvalidation.InvalidateUserCacheAsync(userId);
            
            TempData["SuccessMessage"] = "Your cache has been cleared successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> WarmupCache()
        {
            var userId = _userManager.GetUserId(User)!;
            
            var stopwatch = Stopwatch.StartNew();
            
            // Pre-load dashboard data
            await _dashboardService.GetDashboardDataAsync(userId);
            
            // Pre-load user stats
            await _dashboardService.GetUserStatsAsync(userId);
            
            stopwatch.Stop();
            
            TempData["SuccessMessage"] = $"Cache warmed up successfully in {stopwatch.ElapsedMilliseconds}ms!";
            return RedirectToAction(nameof(Index));
        }
    }
}