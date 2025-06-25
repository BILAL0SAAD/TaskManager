using System.Net.Mail;
using System.Net;
using TaskManager.Web.Services.Interfaces;

namespace TaskManager.Web.Services.infrastructure
{
    public class SimpleEmailService : IEmailService
    {
        private readonly ILogger<SimpleEmailService> _logger;
        private const string GMAIL_SMTP = "smtp.gmail.com";
        private const int GMAIL_PORT = 587;
        private const string GMAIL_USERNAME = "belalsaad2001@gmail.com";
        private const string GMAIL_APP_PASSWORD = "knvv uwwf pugq ejbx"; // Replace this!

        public SimpleEmailService(ILogger<SimpleEmailService> logger)
        {
            _logger = logger;
        }

        public async Task SendEmailConfirmationAsync(string toEmail, string confirmationLink)
        {
            try
            {
                _logger.LogInformation("🚀 Attempting to send email confirmation to {Email}", toEmail);

                using var client = new SmtpClient(GMAIL_SMTP, GMAIL_PORT);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(GMAIL_USERNAME, GMAIL_APP_PASSWORD);

                var message = new MailMessage();
                message.From = new MailAddress(GMAIL_USERNAME, "Task Manager");
                message.To.Add(toEmail);
                message.Subject = "Confirm your email - Task Manager";
                message.Body = CreateConfirmationEmailBody(confirmationLink);
                message.IsBodyHtml = true;

                await client.SendMailAsync(message);
                _logger.LogInformation("✅ Email confirmation sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Email confirmation failed for {Email}: {Error}", toEmail, ex.Message);
                throw;
            }
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string firstName)
        {
            try
            {
                _logger.LogInformation("🚀 Attempting to send welcome email to {Email}", toEmail);

                using var client = new SmtpClient(GMAIL_SMTP, GMAIL_PORT);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(GMAIL_USERNAME, GMAIL_APP_PASSWORD);

                var message = new MailMessage();
                message.From = new MailAddress(GMAIL_USERNAME, "Task Manager");
                message.To.Add(toEmail);
                message.Subject = "Welcome to Task Manager!";
                message.Body = CreateWelcomeEmailBody(firstName);
                message.IsBodyHtml = true;

                await client.SendMailAsync(message);
                _logger.LogInformation("✅ Welcome email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Welcome email failed for {Email}: {Error}", toEmail, ex.Message);
                throw;
            }
        }

        public async Task SendTaskReminderAsync(string toEmail, string taskTitle, DateTime dueDate)
        {
            try
            {
                _logger.LogInformation("🚀 Attempting to send task reminder to {Email}", toEmail);

                using var client = new SmtpClient(GMAIL_SMTP, GMAIL_PORT);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(GMAIL_USERNAME, GMAIL_APP_PASSWORD);

                var message = new MailMessage();
                message.From = new MailAddress(GMAIL_USERNAME, "Task Manager");
                message.To.Add(toEmail);
                message.Subject = $"Task Reminder: {taskTitle}";
                message.Body = CreateTaskReminderEmailBody(taskTitle, dueDate);
                message.IsBodyHtml = true;

                await client.SendMailAsync(message);
                _logger.LogInformation("✅ Task reminder sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Task reminder failed for {Email}: {Error}", toEmail, ex.Message);
                throw;
            }
        }

        public async Task SendPasswordResetAsync(string toEmail, string resetLink)
        {
            try
            {
                _logger.LogInformation("🚀 Attempting to send password reset to {Email}", toEmail);

                using var client = new SmtpClient(GMAIL_SMTP, GMAIL_PORT);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(GMAIL_USERNAME, GMAIL_APP_PASSWORD);

                var message = new MailMessage();
                message.From = new MailAddress(GMAIL_USERNAME, "Task Manager");
                message.To.Add(toEmail);
                message.Subject = "Reset your password - Task Manager";
                message.Body = CreatePasswordResetEmailBody(resetLink);
                message.IsBodyHtml = true;

                await client.SendMailAsync(message);
                _logger.LogInformation("✅ Password reset email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Password reset email failed for {Email}: {Error}", toEmail, ex.Message);
                throw;
            }
        }

        private string CreateConfirmationEmailBody(string confirmationLink)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='text-align: center; margin-bottom: 30px;'>
                        <h1 style='color: #333; margin-bottom: 10px;'>📋 Task Manager</h1>
                        <h2 style='color: #007bff; margin-top: 0;'>Confirm Your Email Address</h2>
                    </div>
                    
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                        <p style='margin: 0; font-size: 16px;'>Thank you for registering with Task Manager!</p>
                    </div>
                    
                    <p style='font-size: 16px; color: #333;'>To complete your registration and start organizing your tasks, please confirm your email address by clicking the button below:</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{confirmationLink}' 
                           style='background-color: #007bff; color: white; padding: 15px 30px; 
                                  text-decoration: none; border-radius: 8px; display: inline-block; 
                                  font-weight: bold; font-size: 16px;'>
                            ✅ Confirm Email Address
                        </a>
                    </div>
                    
                    <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 14px; color: #856404;'>
                            <strong>Important:</strong> This confirmation link will expire in 24 hours for security purposes.
                        </p>
                    </div>
                    
                    <p style='font-size: 14px; color: #666;'>If the button doesn't work, you can copy and paste this link into your browser:</p>
                    <p style='word-break: break-all; color: #007bff; font-size: 12px; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{confirmationLink}</p>
                    
                    <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                    
                    <p style='font-size: 12px; color: #999; text-align: center;'>
                        If you didn't create an account with Task Manager, please ignore this email.<br>
                        This email was sent from an automated system, please do not reply.
                    </p>
                </div>";
        }

        private string CreateWelcomeEmailBody(string firstName)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='text-align: center; margin-bottom: 30px;'>
                        <h1 style='color: #333;'>📋 Task Manager</h1>
                        <h2 style='color: #28a745;'>Welcome, {firstName}!</h2>
                    </div>
                    
                    <p style='font-size: 16px; color: #333;'>Thank you for joining Task Manager! We're excited to help you organize your tasks and boost your productivity.</p>
                    
                    <div style='background-color: #d1ecf1; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #0c5460; margin-top: 0;'>🚀 Get Started:</h3>
                        <ul style='color: #0c5460;'>
                            <li>Create your first project</li>
                            <li>Add tasks to organize your work</li>
                            <li>Set due dates and priorities</li>
                            <li>Track your progress</li>
                        </ul>
                    </div>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='#' style='background-color: #28a745; color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; display: inline-block; font-weight: bold;'>
                            🎯 Start Managing Tasks
                        </a>
                    </div>
                    
                    <p style='font-size: 14px; color: #666;'>If you have any questions, feel free to contact our support team.</p>
                    <p style='font-size: 14px; color: #666;'>Best regards,<br>The Task Manager Team</p>
                </div>";
        }

        private string CreateTaskReminderEmailBody(string taskTitle, DateTime dueDate)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='text-align: center; margin-bottom: 30px;'>
                        <h1 style='color: #333;'>📋 Task Manager</h1>
                        <h2 style='color: #ffc107;'>📅 Task Reminder</h2>
                    </div>
                    
                    <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #856404; margin-top: 0;'>⏰ Upcoming Task</h3>
                        <p style='margin: 0; font-size: 16px; color: #856404;'><strong>Task:</strong> {taskTitle}</p>
                        <p style='margin: 10px 0 0 0; font-size: 16px; color: #856404;'><strong>Due Date:</strong> {dueDate:MMMM dd, yyyy 'at' HH:mm}</p>
                    </div>
                    
                    <p style='font-size: 16px; color: #333;'>Don't forget to complete this task before the deadline!</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='#' style='background-color: #ffc107; color: #212529; padding: 15px 30px; text-decoration: none; border-radius: 8px; display: inline-block; font-weight: bold;'>
                            📋 View Task
                        </a>
                    </div>
                    
                    <p style='font-size: 14px; color: #666;'>Best regards,<br>Task Manager</p>
                </div>";
        }

        private string CreatePasswordResetEmailBody(string resetLink)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='text-align: center; margin-bottom: 30px;'>
                        <h1 style='color: #333;'>📋 Task Manager</h1>
                        <h2 style='color: #dc3545;'>🔐 Password Reset Request</h2>
                    </div>
                    
                    <div style='background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 0; color: #721c24;'>
                            <strong>Security Notice:</strong> We received a request to reset your password.
                        </p>
                    </div>
                    
                    <p style='font-size: 16px; color: #333;'>If you requested a password reset for your Task Manager account, click the button below to create a new password:</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}' 
                           style='background-color: #dc3545; color: white; padding: 15px 30px; 
                                  text-decoration: none; border-radius: 8px; display: inline-block; 
                                  font-weight: bold; font-size: 16px;'>
                            🔑 Reset Password
                        </a>
                    </div>
                    
                    <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 14px; color: #856404;'>
                            <strong>Important:</strong> This password reset link will expire in 1 hour for security purposes.
                        </p>
                    </div>
                    
                    <p style='font-size: 14px; color: #666;'>If the button doesn't work, copy and paste this link:</p>
                    <p style='word-break: break-all; color: #dc3545; font-size: 12px; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{resetLink}</p>
                    
                    <div style='background-color: #d1ecf1; border: 1px solid #bee5eb; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 14px; color: #0c5460;'>
                            <strong>Security Tip:</strong> If you didn't request a password reset, please ignore this email. 
                            Your password will remain unchanged and your account is secure.
                        </p>
                    </div>
                    
                    <p style='font-size: 12px; color: #999; text-align: center;'>
                        This email was sent from an automated system, please do not reply.
                    </p>
                </div>";
        }
    }
}