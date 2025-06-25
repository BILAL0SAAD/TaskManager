using TaskManager.Web.Models;
using TaskStatus = TaskManager.Web.Models.TaskStatus;

namespace TaskManager.Web.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        // Statistics
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TodayTasks { get; set; }
        public int TotalProjects { get; set; }

        // Data collections
        #pragma warning disable CS8618
        public IEnumerable<TaskSummaryDto> RecentTasks { get; set; } = new List<TaskSummaryDto>();
        public IEnumerable<TaskSummaryDto> UpcomingTasks { get; set; } = new List<TaskSummaryDto>();
        public IEnumerable<ProjectSummaryDto> RecentProjects { get; set; } = new List<ProjectSummaryDto>();
        public Dictionary<string, int> TasksByStatus { get; set; } = new();
        public Dictionary<string, int> TasksByPriority { get; set; } = new();
        public Dictionary<string, int> TasksThisWeek { get; set; } = new();
        #pragma warning restore CS8618
    }

    public class TaskSummaryDto
    {
        public int Id { get; set; }

        #pragma warning disable CS8618
        public string Title { get; set; } = string.Empty;
        #pragma warning restore CS8618

        public string? Description { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOverdue { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectColor { get; set; }

        public string PriorityBadgeClass => Priority switch
        {
            TaskPriority.Low => "badge bg-success",
            TaskPriority.Medium => "badge bg-warning",
            TaskPriority.High => "badge bg-danger",
            TaskPriority.Critical => "badge bg-dark",
            _ => "badge bg-secondary"
        };

        public string StatusBadgeClass => Status switch
        {
            TaskStatus.Todo => "badge bg-secondary",
            TaskStatus.InProgress => "badge bg-primary",
            TaskStatus.Review => "badge bg-warning",
            TaskStatus.Done => "badge bg-success",
            TaskStatus.Cancelled => "badge bg-danger",
            _ => "badge bg-secondary"
        };

        public string DueDateDisplay => DueDate?.ToString("MMM dd, yyyy") ?? "No due date";

        public string TimeUntilDue
        {
            get
            {
                if (!DueDate.HasValue) return "No due date";
                var timeSpan = DueDate.Value - DateTime.UtcNow;
                if (timeSpan.TotalDays < 0) return "Overdue";
                if (timeSpan.TotalDays < 1) return "Due today";
                if (timeSpan.TotalDays < 2) return "Due tomorrow";
                return $"Due in {(int)timeSpan.TotalDays} days";
            }
        }
    }

    public class ProjectSummaryDto
    {
        public int Id { get; set; }

        #pragma warning disable CS8618
        public string Name { get; set; } = string.Empty;
        #pragma warning restore CS8618

        public string? Description { get; set; }

        #pragma warning disable CS8618
        public string Color { get; set; } = "#3B82F6";
        #pragma warning restore CS8618

        public DateTime CreatedAt { get; set; }
        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
        public double CompletionPercentage { get; set; }

        public string ProgressBarClass => CompletionPercentage switch
        {
            >= 100 => "progress-bar bg-success",
            >= 75 => "progress-bar bg-info",
            >= 50 => "progress-bar bg-warning",
            _ => "progress-bar bg-danger"
        };

        public string StatusText => CompletionPercentage switch
        {
            >= 100 => "Completed",
            >= 75 => "Nearly Done",
            >= 50 => "In Progress",
            >= 25 => "Getting Started",
            _ => "Just Started"
        };

        public int RemainingTasks => TaskCount - CompletedTaskCount;
    }
}