// ViewModels/Projects/CreateProjectViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.ViewModels.Projects
{
    public class CreateProjectViewModel
    {
        [Required(ErrorMessage = "Project name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Project Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Color is required")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Invalid color format")]
        public string Color { get; set; } = "#3B82F6";
    }
}