// Services/IEmailService.cs
namespace TaskManager.Web.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string toEmail, string firstName);
        Task SendEmailConfirmationAsync(string toEmail, string confirmationLink);
        Task SendTaskReminderAsync(string toEmail, string taskTitle, DateTime dueDate);
        Task SendPasswordResetAsync(string toEmail, string resetLink);
    }
}