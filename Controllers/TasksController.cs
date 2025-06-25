using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Business;
using TaskManager.Web.Services.Interfaces;
using TaskManager.Web.ViewModels.Tasks;
using TaskStatus = TaskManager.Web.Models.TaskStatus;

namespace TaskManager.Web.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TasksController> _logger;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly INotificationService _notificationService;

        public TasksController(
            ITaskService taskService,
            IProjectService projectService,
            UserManager<ApplicationUser> userManager,
            ILogger<TasksController> logger,
            ICacheInvalidationService cacheInvalidation,
            INotificationService notificationService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _userManager = userManager;
            _logger = logger;
            _cacheInvalidation = cacheInvalidation;
            _notificationService = notificationService;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(
            string search = "",
            string status = "",
            string priority = "",
            int? projectId = null)
        {
            var userId = _userManager.GetUserId(User)!;
            
            // Get all tasks for the user
            var allTasks = await _taskService.GetUserTasksAsync(userId);
            
            // Apply filters
            var filteredTasks = allTasks.AsEnumerable();
            
            if (!string.IsNullOrEmpty(search))
            {
                filteredTasks = filteredTasks.Where(t => 
                    t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.Description != null && t.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }
            
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TaskStatus>(status, out var statusEnum))
            {
                filteredTasks = filteredTasks.Where(t => t.Status == statusEnum);
            }
            
            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TaskPriority>(priority, out var priorityEnum))
            {
                filteredTasks = filteredTasks.Where(t => t.Priority == priorityEnum);
            }
            
            if (projectId.HasValue)
            {
                filteredTasks = filteredTasks.Where(t => t.ProjectId == projectId.Value);
            }

            var tasks = filteredTasks.ToList();
            
            // Get projects for filter dropdown
            var projects = await _projectService.GetUserProjectsAsync(userId);

            var viewModel = new TaskListViewModel
            {
                Tasks = tasks,
                Projects = projects,
                SearchTerm = search,
                SelectedStatus = status,
                SelectedPriority = priority,
                SelectedProjectId = projectId
            };

            return View(viewModel);
        }

        // GET: Tasks/Create
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User)!;
            var projects = await _projectService.GetUserProjectsAsync(userId);

            var viewModel = new CreateTaskViewModel
            {
                Projects = projects.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
            };

            return View(viewModel);
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var task = await _taskService.GetTaskByIdAsync(id, userId);

            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}", id, userId);
                return NotFound();
            }

            var projects = await _projectService.GetUserProjectsAsync(userId);

            var viewModel = new EditTaskViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                Status = task.Status,
                DueDate = task.DueDate,
                ProjectId = task.ProjectId,
                Projects = projects.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name,
                    Selected = p.Id == task.ProjectId
                })
            };

            return View(viewModel);
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var task = await _taskService.GetTaskByIdAsync(id, userId);

            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}", id, userId);
                return NotFound();
            }

            var viewModel = new TaskDetailsViewModel
            {
                Task = task,
                Comments = task.Comments?.Where(c => !c.Task.IsDeleted) ?? new List<TaskComment>(),
                Attachments = task.Attachments?.Where(a => !a.Task.IsDeleted) ?? new List<TaskAttachment>(),
                NewComment = string.Empty,
                AttachmentFile = null
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTaskViewModel model)
        {
            var userId = _userManager.GetUserId(User)!;
            
            if (!ModelState.IsValid)
            {
                var projects = await _projectService.GetUserProjectsAsync(userId);
                model.Projects = projects.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                });
                _logger.LogWarning("Invalid model state for task creation");
                return View(model);
            }

            var task = new TaskItem
            {
                Title = model.Title,
                Description = model.Description,
                Priority = model.Priority,
                DueDate = model.DueDate,
                ProjectId = model.ProjectId,
                UserId = userId
            };

            await _taskService.CreateTaskAsync(task);

            // Invalidate cache after creating task
            await _cacheInvalidation.InvalidateTaskCacheAsync(userId);

            // 🎉 ADDED: Send notification when task is created
            await _notificationService.CreateNotificationAsync(
                userId,
                "Task Created ✅",
                $"New task '{task.Title}' has been created successfully!",
                NotificationType.System,
                taskId: task.Id
            );

            _logger.LogInformation("Task {TaskTitle} created for user {UserId}", task.Title, userId);
            TempData["SuccessMessage"] = "Task created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditTaskViewModel model)
        {
            var userId = _userManager.GetUserId(User)!;
            
            if (!ModelState.IsValid)
            {
                var projects = await _projectService.GetUserProjectsAsync(userId);
                model.Projects = projects.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name,
                    Selected = p.Id == model.ProjectId
                });
                _logger.LogWarning("Invalid model state for task edit, ID {TaskId}", id);
                return View(model);
            }

            var task = await _taskService.GetTaskByIdAsync(id, userId);

            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}", id, userId);
                return NotFound();
            }

            var originalStatus = task.Status;

            task.Title = model.Title;
            task.Description = model.Description;
            task.Priority = model.Priority;
            task.Status = model.Status;
            task.DueDate = model.DueDate;
            task.ProjectId = model.ProjectId;

            if (model.Status == TaskStatus.Done && task.CompletedAt == null)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else if (model.Status != TaskStatus.Done)
            {
                task.CompletedAt = null;
            }

            await _taskService.UpdateTaskAsync(task);

            // Invalidate cache after updating task
            await _cacheInvalidation.InvalidateTaskCacheAsync(userId);

            // Send notification if task was completed
            if (originalStatus != TaskStatus.Done && model.Status == TaskStatus.Done)
            {
                await _notificationService.NotifyTaskCompletedAsync(userId, task);
            }

            _logger.LogInformation("Task {TaskTitle} updated for user {UserId}", task.Title, userId);
            TempData["SuccessMessage"] = "Task updated successfully!";
            return RedirectToAction(nameof(Details), new { id = task.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var task = await _taskService.GetTaskByIdAsync(id, userId);

            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}", id, userId);
                return NotFound();
            }

            await _taskService.DeleteTaskAsync(id, userId);

            // Invalidate cache after deleting task
            await _cacheInvalidation.InvalidateTaskCacheAsync(userId);

            _logger.LogInformation("Task ID {TaskId} deleted for user {UserId}", id, userId);
            TempData["SuccessMessage"] = "Task deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}