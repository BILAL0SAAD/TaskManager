// Models/TaskItem.cs
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.Models
{
    public enum TaskPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum TaskStatus
    {
        Todo = 1,
        InProgress = 2,
        Review = 3,
        Done = 4,
        Cancelled = 5
    }

    public class TaskItem
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public TaskStatus Status { get; set; } = TaskStatus.Todo;
        
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }
        
        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Completed")]
        public DateTime? CompletedAt { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        
        // Foreign keys
        public string UserId { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Project? Project { get; set; }
        public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public virtual ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
        
        // UI Helper Properties
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
        
        // FIX: Use DateTime.UtcNow for consistent date comparison
        public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.UtcNow && Status != TaskStatus.Done;
    }
}