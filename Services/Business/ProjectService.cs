using Microsoft.EntityFrameworkCore;
using TaskManager.Web.Data;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Infrastructure;
using TaskManager.Web.Services.Interfaces;
using TaskStatus = TaskManager.Web.Models.TaskStatus;

namespace TaskManager.Web.Services.Business
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(
            ApplicationDbContext context,
            ICacheService cacheService,
            ILogger<ProjectService> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        public ApplicationDbContext GetDbContext() => _context;

        public async Task<IEnumerable<Project>> GetUserProjectsAsync(string userId)
        {
            var cacheKey = $"user_projects_{userId}";
            var cachedProjects = await _cacheService.GetAsync<List<Project>>(cacheKey);
                     
            if (cachedProjects != null)
                return cachedProjects;

            var projects = await _context.Projects
                .Where(p => p.UserId == userId)
                .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            foreach (var project in projects)
            {
                project.User = null!;
                foreach (var task in project.Tasks)
                {
                    task.Project = null!;
                    task.User = null!;
                    task.Comments = new List<TaskComment>();
                    task.Attachments = new List<TaskAttachment>();
                }
            }

            await _cacheService.SetAsync(cacheKey, projects, TimeSpan.FromMinutes(15));
            return projects;
        }

        public async Task<Project?> GetProjectByIdAsync(int id, string userId)
        {
            var project = await _context.Projects
                .Where(p => p.Id == id && p.UserId == userId)
                .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (project != null)
            {
                project.User = null!;
                foreach (var task in project.Tasks)
                {
                    task.Project = null!;
                    task.User = null!;
                    task.Comments = new List<TaskComment>();
                    task.Attachments = new List<TaskAttachment>();
                }
            }

            return project;
        }

      

        public async Task<Project> CreateProjectAsync(Project project)
        {
            var newProject = new Project
            {
                Name = project.Name,
                Description = project.Description,
                Color = project.Color,
                UserId = project.UserId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            
            try
            {
                _context.Projects.Add(newProject);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Project created with ID {ProjectId} for user {UserId}", newProject.Id, newProject.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create project for user {UserId}", project.UserId);
                throw;
            }

            await _cacheService.RemoveAsync($"user_projects_{project.UserId}");
            
            return newProject;
        }

        public async Task<Project> UpdateProjectAsync(Project project)
        {
            var existingProject = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == project.Id && p.UserId == project.UserId);

            if (existingProject == null)
                throw new InvalidOperationException($"Project with ID {project.Id} not found for user {project.UserId}");

            existingProject.Name = project.Name;
            existingProject.Description = project.Description;
            existingProject.Color = project.Color;
            
            await _context.SaveChangesAsync();

            await _cacheService.RemoveAsync($"user_projects_{project.UserId}");

            _logger.LogInformation("Project updated: {ProjectId} by user {UserId}", project.Id, project.UserId);
            return existingProject;
        }

        public async Task DeleteProjectAsync(int id, string userId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (project != null)
            {
                await _context.Tasks
                    .Where(t => t.ProjectId == id && !t.IsDeleted)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ProjectId, (int?)null));

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();

                await _cacheService.RemoveAsync($"user_projects_{userId}");

                _logger.LogInformation("Project deleted: {ProjectId} by user {UserId}", id, userId);
            }
        }

        public async Task<bool> ProjectExistsAsync(int id, string userId)
        {
            return await _context.Projects
                .AnyAsync(p => p.Id == id && p.UserId == userId);
        }
    }
}