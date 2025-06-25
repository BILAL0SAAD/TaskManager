// ViewModels/Tasks/CreateTaskViewModel.cs
using System.ComponentModel.DataAnnotations;
using TaskManager.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskManager.Web.ViewModels.Tasks
{
    public class CreateTaskViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Display(Name = "Priority")]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [Display(Name = "Due Date")]
        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Project")]
        public int? ProjectId { get; set; }

        // For dropdown
        public IEnumerable<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
    }
}
