// Services/Interfaces/ICacheService.cs
using TaskManager.Web.Models.Cache;

namespace TaskManager.Web.Services.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class;
        
        // User-specific cache methods
        Task<T?> GetUserCacheAsync<T>(string userId, string key) where T : class;
        Task SetUserCacheAsync<T>(string userId, string key, T value, TimeSpan? expiration = null) where T : class;
        Task RemoveUserCacheAsync(string userId, string key);
        Task ClearUserCacheAsync(string userId);
        
        // Dashboard specific methods
        Task<DashboardCacheData?> GetDashboardCacheAsync(string userId);
        Task SetDashboardCacheAsync(string userId, DashboardCacheData data);
        Task InvalidateDashboardCacheAsync(string userId);
    }
}