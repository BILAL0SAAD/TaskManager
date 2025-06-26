using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Business;
using TaskManager.Web.Services.Interfaces;
using TaskManager.Web.ViewModels.Projects;

namespace TaskManager.Web.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProjectsController> _logger;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly INotificationService _notificationService;

        public ProjectsController(
            IProjectService projectService,
            UserManager<ApplicationUser> userManager,
            ILogger<ProjectsController> logger,
            ICacheInvalidationService cacheInvalidation,
            INotificationService notificationService)
        {
            _projectService = projectService;
            _userManager = userManager;
            _logger = logger;
            _cacheInvalidation = cacheInvalidation;
            _notificationService = notificationService;
        }

        // GET: Projects
        public async Task<IActionResult> Index(string search = "")
        {
            var userId = _userManager.GetUserId(User)!;
            var allProjects = await _projectService.GetUserProjectsAsync(userId);
            
            // Apply search filter if provided
            var filteredProjects = allProjects.AsEnumerable();
            if (!string.IsNullOrEmpty(search))
            {
                filteredProjects = filteredProjects.Where(p => 
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description != null && p.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            var projects = filteredProjects.ToList();

            var viewModel = new ProjectListViewModel
            {
                Projects = projects,
                SearchTerm = search,
                TotalProjects = projects.Count,
                ActiveProjects = projects.Count(p => p.CompletionPercentage < 100),
                CompletedProjects = projects.Count(p => p.CompletionPercentage >= 100)
            };

            return View(viewModel);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            return View(new CreateProjectViewModel());
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var project = await _projectService.GetProjectByIdAsync(id, userId);

            if (project == null)
            {
                _logger.LogWarning("Project with ID {ProjectId} not found for user {UserId}", id, userId);
                return NotFound();
            }

            var viewModel = new CreateProjectViewModel
            {
                Name = project.Name,
                Description = project.Description,
                Color = project.Color
            };

            return View(viewModel);
        }

 // Updated Details action - pass Project model directly
public async Task<IActionResult> Details(int id)
{
    var userId = _userManager.GetUserId(User)!;
    var project = await _projectService.GetProjectByIdAsync(id, userId);

    if (project == null)
    {
        _logger.LogWarning("Project {ProjectId} not found for user {UserId}", id, userId);
        return NotFound();
    }

    // Pass the project model directly instead of creating a ViewModel
    return View(project);
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateProjectViewModel viewModel)
{
    var userId = _userManager.GetUserId(User)!;
    
    if (!ModelState.IsValid)
    {
        _logger.LogWarning("Invalid model state for project creation");
        return View(viewModel);
    }

    var project = new Project
    {
        Name = viewModel.Name,
        Description = viewModel.Description,
        Color = viewModel.Color,
        UserId = userId
    };

    // Use a transaction to ensure project is saved before notification
    using (var transaction = await _projectService.GetDbContext().Database.BeginTransactionAsync())
    {
        try
        {
            // Use the returned project from CreateProjectAsync
            var savedProject = await _projectService.CreateProjectAsync(project);
            if (savedProject.Id == 0)
            {
                throw new InvalidOperationException("Project creation failed; no ID assigned.");
            }
            await _notificationService.NotifyProjectCreatedAsync(userId, savedProject);
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create project and notification for user {UserId}", userId);
            ModelState.AddModelError("", "An error occurred while creating the project.");
            return View(viewModel);
        }
    }

    // Invalidate cache after creating project
    await _cacheInvalidation.InvalidateProjectCacheAsync(userId);

    _logger.LogInformation("Project {ProjectName} created for user {UserId}", project.Name, userId);
    TempData["SuccessMessage"] = "Project created successfully!";
    return RedirectToAction(nameof(Index));
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateProjectViewModel viewModel)
        {
            var userId = _userManager.GetUserId(User)!;
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for project edit, ID {ProjectId}", id);
                return View(viewModel);
            }

            var project = await _projectService.GetProjectByIdAsync(id, userId);

            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found for user {UserId}", id, userId);
                return NotFound();
            }

            project.Name = viewModel.Name;
            project.Description = viewModel.Description;
            project.Color = viewModel.Color;

            await _projectService.UpdateProjectAsync(project);

            // Invalidate cache after updating project
            await _cacheInvalidation.InvalidateProjectCacheAsync(userId);

            _logger.LogInformation("Project {ProjectName} updated for user {UserId}", project.Name, userId);
            TempData["SuccessMessage"] = "Project updated successfully!";
            return RedirectToAction(nameof(Details), new { id = project.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var project = await _projectService.GetProjectByIdAsync(id, userId);

            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found for user {UserId}", id, userId);
                return NotFound();
            }

            await _projectService.DeleteProjectAsync(id, userId);

            // Invalidate cache after deleting project
            await _cacheInvalidation.InvalidateProjectCacheAsync(userId);

            _logger.LogInformation("Project ID {ProjectId} deleted for user {UserId}", id, userId);
            TempData["SuccessMessage"] = "Project deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}