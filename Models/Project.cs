// Models/Project.cs
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.Models
{
    public class Project
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(7)]
        public string Color { get; set; } = "#3B82F6"; // Default blue
        
        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsDeleted { get; set; } = false;
        
        // Foreign keys
        public string UserId { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        
        // UI Helper Properties
        public int TaskCount => Tasks?.Count(t => !t.IsDeleted) ?? 0;
        public int CompletedTaskCount => Tasks?.Count(t => !t.IsDeleted && t.Status == TaskStatus.Done) ?? 0;
        public double CompletionPercentage => TaskCount > 0 ? (double)CompletedTaskCount / TaskCount * 100 : 0;
    }
}
