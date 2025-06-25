// Services/Infrastructure/CacheService.cs
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TaskManager.Web.Services.Interfaces;
using TaskManager.Web.Models.Cache;

namespace TaskManager.Web.Services.Infrastructure
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // Cache key prefixes
        private const string USER_PREFIX = "user";
        private const string DASHBOARD_PREFIX = "dashboard";
        private const string TASKS_PREFIX = "tasks";
        private const string PROJECTS_PREFIX = "projects";
        private const string STATS_PREFIX = "stats";

        // Default expiration times
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan DashboardExpiration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan StatsExpiration = TimeSpan.FromHours(1);

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var value = await _cache.GetStringAsync(key);
                if (value == null) return null;

                return JsonSerializer.Deserialize<T>(value, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                };
                await _cache.SetStringAsync(key, json, options);
                
                _logger.LogDebug("Set cache key: {Key} with expiration: {Expiration}", key, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug("Removed cache key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            _logger.LogDebug("Pattern removal requested for: {Pattern}", pattern);
            await Task.CompletedTask;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}, fetching data", key);
            var value = await getItem();
            await SetAsync(key, value, expiration ?? DefaultExpiration);
            return value;
        }

        // User-specific cache methods
        public async Task<T?> GetUserCacheAsync<T>(string userId, string key) where T : class
        {
            var fullKey = GetUserCacheKey(userId, key);
            return await GetAsync<T>(fullKey);
        }

        public async Task SetUserCacheAsync<T>(string userId, string key, T value, TimeSpan? expiration = null) where T : class
        {
            var fullKey = GetUserCacheKey(userId, key);
            await SetAsync(fullKey, value, expiration ?? DefaultExpiration);
        }

        public async Task RemoveUserCacheAsync(string userId, string key)
        {
            var fullKey = GetUserCacheKey(userId, key);
            await RemoveAsync(fullKey);
        }

        public async Task ClearUserCacheAsync(string userId)
        {
            var keys = new[]
            {
                GetUserCacheKey(userId, DASHBOARD_PREFIX),
                GetUserCacheKey(userId, TASKS_PREFIX),
                GetUserCacheKey(userId, PROJECTS_PREFIX),
                GetUserCacheKey(userId, STATS_PREFIX)
            };

            foreach (var key in keys)
            {
                await RemoveAsync(key);
            }

            _logger.LogDebug("Cleared all cache for user: {UserId}", userId);
        }

        // Dashboard specific methods
        public async Task<DashboardCacheData?> GetDashboardCacheAsync(string userId)
        {
            return await GetUserCacheAsync<DashboardCacheData>(userId, DASHBOARD_PREFIX);
        }

        public async Task SetDashboardCacheAsync(string userId, DashboardCacheData data)
        {
            await SetUserCacheAsync(userId, DASHBOARD_PREFIX, data, DashboardExpiration);
        }

        public async Task InvalidateDashboardCacheAsync(string userId)
        {
            await RemoveUserCacheAsync(userId, DASHBOARD_PREFIX);
            _logger.LogDebug("Invalidated dashboard cache for user: {UserId}", userId);
        }

        // Helper methods
        private static string GetUserCacheKey(string userId, string key)
        {
            return $"{USER_PREFIX}:{userId}:{key}";
        }
    }
}