// Models/TaskAttachment.cs
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.Models
{
    public class TaskAttachment
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        [Display(Name = "Uploaded")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign keys
        public int TaskId { get; set; }
        public string UserId { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual TaskItem Task { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
        
        // UI Helper Properties
        public string FormattedFileSize
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = FileSize;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }
    }
}