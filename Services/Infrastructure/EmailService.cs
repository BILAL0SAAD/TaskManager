// Services/EmailService.cs
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using TaskManager.Web.Configuration;
using TaskManager.Web.Services.Interfaces;

namespace TaskManager.Web.Services.infrastructure
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string firstName)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = "Welcome to Task Manager!";

                message.Body = new TextPart("html")
                {
                    Text = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #333;'>Welcome to Task Manager, {firstName}!</h2>
                        <p>Thank you for joining us. We're excited to help you organize your tasks and boost your productivity!</p>
                        <p>Get started by logging into your account and creating your first project.</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='#' style='background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Get Started
                            </a>
                        </div>
                        <p>If you have any questions, feel free to contact our support team.</p>
                        <p>Best regards,<br>The Task Manager Team</p>
                    </div>"
                };

                await SendEmailAsync(message);
                _logger.LogInformation("Welcome email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendEmailConfirmationAsync(string toEmail, string confirmationLink)
        {
            try
            {
                _logger.LogInformation("Attempting to send email confirmation to {Email}", toEmail);
                _logger.LogDebug("Using SMTP settings - Server: {Server}, Port: {Port}, Username: {Username}, SenderEmail: {SenderEmail}", 
                    _emailSettings.SmtpServer, _emailSettings.Port, _emailSettings.Username, _emailSettings.SenderEmail);
                
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = "Confirm your email - Task Manager";

                message.Body = new TextPart("html")
                {
                    Text = $@"
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
                    </div>"
                };

                await SendEmailAsync(message);
                _logger.LogInformation("Email confirmation sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email confirmation to {Email}. Error: {Error}", toEmail, ex.Message);
                throw;
            }
        }

        public async Task SendTaskReminderAsync(string toEmail, string taskTitle, DateTime dueDate)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = $"Task Reminder: {taskTitle}";

                message.Body = new TextPart("html")
                {
                    Text = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #333;'>📅 Task Reminder</h2>
                        <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0;'><strong>Task:</strong> {taskTitle}</p>
                            <p style='margin: 10px 0 0 0;'><strong>Due Date:</strong> {dueDate:MMMM dd, yyyy 'at' HH:mm}</p>
                        </div>
                        <p>Don't forget to complete this task before the deadline!</p>
                        <p>Best regards,<br>Task Manager</p>
                    </div>"
                };

                await SendEmailAsync(message);
                _logger.LogInformation("Task reminder sent successfully to {Email} for task: {TaskTitle}", toEmail, taskTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send task reminder to {Email} for task: {TaskTitle}", toEmail, taskTitle);
                throw;
            }
        }

        public async Task SendPasswordResetAsync(string toEmail, string resetLink)
        {
            try
            {
                _logger.LogInformation("Attempting to send password reset email to {Email}", toEmail);
                
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = "Reset your password - Task Manager";

                message.Body = new TextPart("html")
                {
                    Text = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <h1 style='color: #333; margin-bottom: 10px;'>📋 Task Manager</h1>
                            <h2 style='color: #dc3545; margin-top: 0;'>🔐 Password Reset Request</h2>
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
                        
                        <p style='font-size: 14px; color: #666;'>If the button doesn't work, copy and paste this link into your browser:</p>
                        <p style='word-break: break-all; color: #dc3545; font-size: 12px; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{resetLink}</p>
                        
                        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                        
                        <div style='background-color: #d1ecf1; border: 1px solid #bee5eb; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0; font-size: 14px; color: #0c5460;'>
                                <strong>Security Tip:</strong> If you didn't request a password reset, please ignore this email. 
                                Your password will remain unchanged and your account is secure.
                            </p>
                        </div>
                        
                        <p style='font-size: 12px; color: #999; text-align: center;'>
                            This email was sent from an automated system, please do not reply.<br>
                            If you continue to have issues, please contact our support team.
                        </p>
                    </div>"
                };

                await SendEmailAsync(message);
                _logger.LogInformation("Password reset email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
                throw;
            }
        }

        private async Task SendEmailAsync(MimeMessage message)
        {
            try
            {
                _logger.LogInformation("Connecting to SMTP server: {Server}:{Port}", _emailSettings.SmtpServer, _emailSettings.Port);
                
                using var client = new SmtpClient();
                
                // Connect to SMTP server
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
                _logger.LogInformation("Connected to SMTP server successfully");
                
                // Authenticate
                _logger.LogInformation("Authenticating with username: {Username}", _emailSettings.Username);
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                _logger.LogInformation("SMTP authentication successful");
                
                // Send email
                _logger.LogInformation("Sending email from {From} to {To} with subject: {Subject}", 
                    string.Join(", ", message.From), 
                    string.Join(", ", message.To),
                    message.Subject);
                    
                await client.SendAsync(message);
                _logger.LogInformation("Email sent successfully");
                
                // Disconnect
                await client.DisconnectAsync(true);
                _logger.LogDebug("Disconnected from SMTP server");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email. SMTP Details - Server: {Server}:{Port}, Username: {Username}, SenderEmail: {SenderEmail}", 
                    _emailSettings.SmtpServer, _emailSettings.Port, _emailSettings.Username, _emailSettings.SenderEmail);
                throw;
            }
        }
    }
}