// Models/Cache/DashboardCacheData.cs
using TaskManager.Web.ViewModels.Dashboard;

namespace TaskManager.Web.Models.Cache
{
    public class DashboardCacheData
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TodayTasks { get; set; }
        public int TotalProjects { get; set; }
        public List<TaskSummaryDto> RecentTasks { get; set; } = new();
        public List<TaskSummaryDto> UpcomingTasks { get; set; } = new();
        public List<ProjectSummaryDto> RecentProjects { get; set; } = new();
        public Dictionary<string, int> TasksByStatus { get; set; } = new();
        public Dictionary<string, int> TasksByPriority { get; set; } = new();
        public Dictionary<string, int> TasksThisWeek { get; set; } = new();
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserStatsCacheData
    {
        public int TotalTasksCreated { get; set; }
        public int TasksCompletedThisWeek { get; set; }
        public int TasksCompletedThisMonth { get; set; }
        public double AverageCompletionTime { get; set; }
        public int CurrentStreak { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class ProjectCacheData
    {
        public List<ProjectSummaryDto> Projects { get; set; } = new();
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    }
}