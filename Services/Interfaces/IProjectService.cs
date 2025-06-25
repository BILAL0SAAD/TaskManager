// Services/IProjectService.cs
using TaskManager.Web.Models;

namespace TaskManager.Web.Services.Interfaces
{
    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetUserProjectsAsync(string userId);
        Task<Project?> GetProjectByIdAsync(int id, string userId);
        Task<Project> CreateProjectAsync(Project project);
        Task<Project> UpdateProjectAsync(Project project);
        Task DeleteProjectAsync(int id, string userId);
        Task<bool> ProjectExistsAsync(int id, string userId);
    }
}

