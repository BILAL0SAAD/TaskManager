// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace TaskManager.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
        
        // Display name for UI
        public string FullName => $"{FirstName} {LastName}";
    }
}