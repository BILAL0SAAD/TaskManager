// ViewModels/Tasks/TaskDetailsViewModel.cs
using TaskManager.Web.Models;

namespace TaskManager.Web.ViewModels.Tasks
{
    public class TaskDetailsViewModel
    {
        public TaskItem Task { get; set; } = null!;
        public IEnumerable<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public IEnumerable<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
        
        // For adding new comment
        public string NewComment { get; set; } = string.Empty;
        
        // For file upload
        public IFormFile? AttachmentFile { get; set; }
    }
}