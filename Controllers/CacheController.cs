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

    // Step 1: Clear existing cache for user
    await _cacheInvalidation.InvalidateUserCacheAsync(userId);

    // Step 2: Measure time without cache
    var stopwatch = Stopwatch.StartNew();
    await _dashboardService.GetDashboardDataAsync(userId);
    stopwatch.Stop();
    var noCacheTime = stopwatch.ElapsedMilliseconds;

    // Step 3: Measure time with cache
    stopwatch.Restart();
    await _dashboardService.GetDashboardDataAsync(userId);
    stopwatch.Stop();
    var cacheTime = stopwatch.ElapsedMilliseconds;

    // Step 4: Package results with correct property names
    results.Add(new Dictionary<string, string>
    {
        ["test"] = "Dashboard Load (No Cache)",
        ["time"] = $"{noCacheTime}ms",
        ["status"] = noCacheTime < 500 ? "Good" : "Slow"
    });

    results.Add(new Dictionary<string, string>
    {
        ["test"] = "Dashboard Load (With Cache)",
        ["time"] = $"{cacheTime}ms",
        ["status"] = cacheTime < 100 ? "Excellent" : "Good"
    });

    var improvement = Math.Round((double)(noCacheTime - cacheTime) / noCacheTime * 100, 1);
    results.Add(new Dictionary<string, string>
    {
        ["test"] = "Performance Improvement",
        ["time"] = $"{improvement}%",
        ["status"] = cacheTime < noCacheTime / 2 ? "Significant" : "Moderate"
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