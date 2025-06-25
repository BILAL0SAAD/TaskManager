using TaskManager.Web.Models;

namespace TaskManager.Web.ViewModels.Tasks
{
    public class TaskListViewModel
    {
        public IEnumerable<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public IEnumerable<Project> Projects { get; set; } = new List<Project>();
        public string? SelectedStatus { get; set; }
        public string? SelectedPriority { get; set; }
        public int? SelectedProjectId { get; set; }
        public string? SearchTerm { get; set; }
        
        // Statistics
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TodayTasks { get; set; }
    }
}