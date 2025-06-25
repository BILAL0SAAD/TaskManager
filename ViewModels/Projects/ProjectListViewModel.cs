using TaskManager.Web.Models;

namespace TaskManager.Web.ViewModels.Projects
{
    public class ProjectListViewModel
    {
        public IEnumerable<Project> Projects { get; set; } = new List<Project>();
        public string? SearchTerm { get; set; }
        
        // Statistics
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
    }
}