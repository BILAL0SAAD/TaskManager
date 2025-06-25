// Services/Business/CachedDashboardService.cs
using TaskManager.Web.Data;
using TaskManager.Web.Models;
using TaskManager.Web.Models.Cache;
using TaskManager.Web.Services.Interfaces;
using TaskManager.Web.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;
using TaskStatus = TaskManager.Web.Models.TaskStatus; // Alias to avoid ambiguity if 'TaskStatus' also exists in another namespace

namespace TaskManager.Web.Services.Business
{
    public interface ICachedDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(string userId);
        Task InvalidateDashboardCacheAsync(string userId);
        Task<UserStatsCacheData> GetUserStatsAsync(string userId);
    }

    public class CachedDashboardService : ICachedDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CachedDashboardService> _logger;

        public CachedDashboardService(
            ApplicationDbContext context,
            ICacheService cacheService,
            ILogger<CachedDashboardService> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(string userId)
        {
            // Try to get from cache first
            var cachedData = await _cacheService.GetDashboardCacheAsync(userId);
            
            // Check for valid cache
            if (cachedData != null && IsValidCache(cachedData.CachedAt))
            {
                _logger.LogDebug("Dashboard cache hit for user: {UserId}", userId);
                return MapToViewModel(cachedData);
            }

            _logger.LogDebug("Dashboard cache miss for user: {UserId}, fetching from database", userId);
            
            // Fetch fresh data from database
            var dashboardData = await FetchDashboardDataFromDatabase(userId);
            
            // Cache the data
            await _cacheService.SetDashboardCacheAsync(userId, dashboardData);
            
            return MapToViewModel(dashboardData);
        }

        public async Task InvalidateDashboardCacheAsync(string userId)
        {
            await _cacheService.InvalidateDashboardCacheAsync(userId);
            _logger.LogInformation("Dashboard cache invalidated for user: {UserId}", userId);
        }

        public async Task<UserStatsCacheData> GetUserStatsAsync(string userId)
        {
            var cacheKey = "user_stats"; // More descriptive cache key
            return await _cacheService.GetOrSetAsync(
                $"user:{userId}:{cacheKey}",
                () => FetchUserStatsFromDatabase(userId),
                TimeSpan.FromHours(1)
            );
        }

        private async Task<DashboardCacheData> FetchDashboardDataFromDatabase(string userId)
        {
            var now = DateTime.UtcNow; // Consistent UTC
            
            // Get tasks using anonymous type first to avoid ambiguity and pull only necessary data
            var taskData = await _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .AsNoTracking()
                .Select(t => new
                {
                    TaskId = t.Id,
                    TaskTitle = t.Title,
                    TaskDescription = t.Description,
                    TaskPriority = t.Priority,
                    TaskStatus = t.Status,
                    TaskDueDate = t.DueDate,
                    TaskCreatedAt = t.CreatedAt,
                    TaskProjectId = t.ProjectId,
                    TaskIsOverdue = t.DueDate.HasValue && t.DueDate < now && t.Status != TaskStatus.Done // Calculation on server
                })
                .ToListAsync();

            // Convert to TaskSummaryDto list
            var tasks = taskData.Select(t => new TaskSummaryDto
            {
                Id = t.TaskId,
                Title = t.TaskTitle,
                Description = t.TaskDescription,
                Priority = t.TaskPriority,
                Status = t.TaskStatus,
                DueDate = t.TaskDueDate,
                CreatedAt = t.TaskCreatedAt,
                IsOverdue = t.TaskIsOverdue,
                ProjectId = t.TaskProjectId
                // ProjectName and ProjectColor are not directly populated here
                // If needed, they'd require a join or separate fetch for each task
            }).ToList();

            // Get projects with computed task counts
            var projectsQuery = _context.Projects
                .Where(p => p.UserId == userId)
                .AsNoTracking()
                .Select(p => new
                {
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    ProjectDescription = p.Description,
                    ProjectColor = p.Color,
                    ProjectCreatedAt = p.CreatedAt,
                    // Use subqueries for counts, EF Core translates this efficiently
                    TaskCount = _context.Tasks.Count(t => t.ProjectId == p.Id && !t.IsDeleted),
                    CompletedTaskCount = _context.Tasks.Count(t => t.ProjectId == p.Id && !t.IsDeleted && t.Status == TaskStatus.Done)
                });

            var projectData = await projectsQuery.ToListAsync();

            var projects = projectData.Select(p => new ProjectSummaryDto
            {
                Id = p.ProjectId,
                Name = p.ProjectName,
                Description = p.ProjectDescription,
                Color = p.ProjectColor,
                CreatedAt = p.ProjectCreatedAt,
                TaskCount = p.TaskCount,
                CompletedTaskCount = p.CompletedTaskCount,
                CompletionPercentage = p.TaskCount > 0 ? (double)p.CompletedTaskCount / p.TaskCount * 100 : 0
            }).ToList();

            return new DashboardCacheData
            {
                TotalTasks = tasks.Count,
                CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Done),
                OverdueTasks = tasks.Count(t => t.IsOverdue),
                TodayTasks = tasks.Count(t => t.DueDate?.Date == now.Date), // Compare with today's date in UTC
                TotalProjects = projects.Count,
                RecentTasks = tasks.OrderByDescending(t => t.CreatedAt).Take(5).ToList(),
                UpcomingTasks = tasks
                    .Where(t => t.DueDate.HasValue && t.DueDate > now && t.Status != TaskStatus.Done)
                    .OrderBy(t => t.DueDate)
                    .Take(5)
                    .ToList(),
                RecentProjects = projects.OrderByDescending(p => p.CreatedAt).Take(3).ToList(),
                TasksByStatus = tasks.GroupBy(t => t.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
                TasksByPriority = tasks.GroupBy(t => t.Priority.ToString()).ToDictionary(g => g.Key, g => g.Count()),
                TasksThisWeek = GetTasksThisWeek(tasks) // Counts tasks created this week
            };
        }

        private async Task<UserStatsCacheData> FetchUserStatsFromDatabase(string userId)
        {
            var now = DateTime.UtcNow;
            // Adjust week/month start based on consistent UTC and desired cultural week start if necessary.
            // For general purpose, DayOfWeek from DateTime.UtcNow is often sufficient.
            var weekStart = now.AddDays(-(int)now.DayOfWeek).Date; // Start of current week (Sunday for en-US)
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc); // Start of current month

            var totalTasks = await _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .CountAsync();

            var tasksCompletedThisWeek = await _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted && 
                            t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= weekStart && t.Status == TaskStatus.Done)
                .CountAsync();

            var tasksCompletedThisMonth = await _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted && 
                            t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= monthStart && t.Status == TaskStatus.Done)
                .CountAsync();

            return new UserStatsCacheData
            {
                TotalTasksCreated = totalTasks,
                TasksCompletedThisWeek = tasksCompletedThisWeek,
                TasksCompletedThisMonth = tasksCompletedThisMonth,
                LastUpdated = DateTime.UtcNow
                // AverageCompletionTime and CurrentStreak are in the DTO but not calculated here.
                // You'd need more complex logic to calculate them if desired.
            };
        }

        private static DashboardViewModel MapToViewModel(DashboardCacheData data)
        {
            // Direct mapping is clean and efficient
            return new DashboardViewModel
            {
                TotalTasks = data.TotalTasks,
                CompletedTasks = data.CompletedTasks,
                OverdueTasks = data.OverdueTasks,
                TodayTasks = data.TodayTasks,
                TotalProjects = data.TotalProjects,
                RecentTasks = data.RecentTasks,
                UpcomingTasks = data.UpcomingTasks,
                RecentProjects = data.RecentProjects,
                TasksByStatus = data.TasksByStatus,
                TasksByPriority = data.TasksByPriority,
                TasksThisWeek = data.TasksThisWeek
            };
        }

        private static bool IsValidCache(DateTime cachedAt)
        {
            // Cache valid for 10 minutes from UTC Now
            return DateTime.UtcNow - cachedAt < TimeSpan.FromMinutes(10);
        }

        private static Dictionary<string, int> GetTasksThisWeek(IEnumerable<TaskSummaryDto> tasks)
        {
            // Adjust startOfWeek for local time if you want it relative to the user's current day,
            // otherwise keep it based on UTC or a fixed week start (e.g., Monday).
            // This example uses DateTime.Today which is local time. For consistency with UTC in DB,
            // consider converting task.CreatedAt to local or comparing everything in UTC.
            // Using DateTime.UtcNow.Date for consistency with other parts of the service that use UTC.
            var startOfWeek = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek).Date; // Start of week based on UTC

            var result = new Dictionary<string, int>();

            for (int i = 0; i < 7; i++)
            {
                var day = startOfWeek.AddDays(i);
                var dayName = day.ToString("ddd"); // e.g., "Mon", "Tue"
                var count = tasks.Count(t => t.CreatedAt.Date == day.Date); // Counts tasks created on this specific day (UTC date part)
                result[dayName] = count;
            }

            return result;
        }
    }
}