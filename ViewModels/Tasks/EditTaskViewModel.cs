// ViewModels/Tasks/EditTaskViewModel.cs
using System.ComponentModel.DataAnnotations;
using TaskManager.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskStatus = TaskManager.Web.Models.TaskStatus; // Fixed: Use your custom TaskStatus

namespace TaskManager.Web.ViewModels.Tasks
{
    public class EditTaskViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Display(Name = "Priority")]
        public TaskPriority Priority { get; set; }

        [Display(Name = "Status")]
        public TaskStatus Status { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Project")]
        public int? ProjectId { get; set; }

        // For dropdowns
        public IEnumerable<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> PriorityOptions { get; set; } = new List<SelectListItem>();
    }
}