// Models/Notification.cs
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; }
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReadAt { get; set; }
        
        // Related entity info (optional)
        public int? TaskId { get; set; }
        public int? ProjectId { get; set; }
        
        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public TaskItem? Task { get; set; }
        public Project? Project { get; set; }
    }
    
    public enum NotificationType
    {
        TaskDue,           // Task is due today/tomorrow
        TaskOverdue,       // Task is overdue
        TaskCompleted,     // Task was completed
        ProjectCreated,    // New project created
        TaskAssigned,      // Task assigned (future feature)
        Reminder,          // General reminder
        System             // System announcements
    }
}
