// ViewModels/Projects/ProjectDetailsViewModel.cs
namespace TaskManager.Web.ViewModels.Projects
{
    public class ProjectDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Color { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
        public double CompletionPercentage { get; set; }
    }
}