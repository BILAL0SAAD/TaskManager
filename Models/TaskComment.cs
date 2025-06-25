// Models/TaskComment.cs
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.Models
{
    public class TaskComment
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Foreign keys
        public int TaskId { get; set; }
        public string UserId { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual TaskItem Task { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}

