// Services/Business/CacheInvalidationService.cs
using TaskManager.Web.Services.Interfaces;

namespace TaskManager.Web.Services.Business
{
    public interface ICacheInvalidationService
    {
        Task InvalidateUserCacheAsync(string userId);
        Task InvalidateTaskCacheAsync(string userId);
        Task InvalidateProjectCacheAsync(string userId);
        Task InvalidateDashboardCacheAsync(string userId);
    }

    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheInvalidationService> _logger;

        public CacheInvalidationService(ICacheService cacheService, ILogger<CacheInvalidationService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task InvalidateUserCacheAsync(string userId)
        {
            await _cacheService.ClearUserCacheAsync(userId);
            _logger.LogDebug("Invalidated all cache for user: {UserId}", userId);
        }

        public async Task InvalidateTaskCacheAsync(string userId)
        {
            await _cacheService.RemoveUserCacheAsync(userId, "tasks");
            await InvalidateDashboardCacheAsync(userId);
        }

        public async Task InvalidateProjectCacheAsync(string userId)
        {
            await _cacheService.RemoveUserCacheAsync(userId, "projects");
            await InvalidateDashboardCacheAsync(userId);
        }

        public async Task InvalidateDashboardCacheAsync(string userId)
        {
            await _cacheService.InvalidateDashboardCacheAsync(userId);
        }
    }
}