// BackgroundJobs/TaskReminderJob.cs
using TaskManager.Web.Data;
using TaskManager.Web.Services;
using Microsoft.EntityFrameworkCore;
using TaskManager.Web.Services.Interfaces;

namespace TaskManager.Web.BackgroundJobs
{
    public class TaskReminderJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<TaskReminderJob> _logger;

        public TaskReminderJob(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<TaskReminderJob> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendTaskReminders()
        {
            var tomorrow = DateTime.Today.AddDays(1);
            var tasksToRemind = await _context.Tasks
                .Include(t => t.User)
                .Where(t => t.DueDate.HasValue &&
                           t.DueDate.Value.Date == tomorrow &&
                           t.Status != Models.TaskStatus.Done &&
                           !t.IsDeleted)
                .ToListAsync();

            foreach (var task in tasksToRemind)
            {
                try
                {
                    await _emailService.SendTaskReminderAsync(
                        task.User.Email,
                        task.Title,
                        task.DueDate.Value);

                    _logger.LogInformation("Reminder sent for task {TaskId} to {Email}", 
                        task.Id, task.User.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reminder for task {TaskId}", task.Id);
                }
            }
        }
    }
}
