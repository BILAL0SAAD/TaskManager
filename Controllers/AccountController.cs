using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.Models;
using TaskManager.Web.ViewModels.Auth;
using TaskManager.Web.Services;
using Microsoft.EntityFrameworkCore;
using TaskManager.Web.Data;
using System.Text.Encodings.Web;
using TaskManager.Web.Services.Interfaces;

namespace TaskManager.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _dbContext;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            ILogger<AccountController> logger,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
            _dbContext = dbContext;
        }

        // ==================== TEST EMAIL METHOD ====================
        [HttpGet]
        [AllowAnonymous] // Remove after testing
        public async Task<IActionResult> TestEmail(string email = "belalsaad2001@gmail.com")
        {
            try
            {
                _logger.LogInformation("Starting email test for {Email}", email);
                
                // Test the email service
                await _emailService.SendEmailConfirmationAsync(email, "https://localhost:5001/Account/ConfirmEmail?userId=test&token=test-token-123");
                
                _logger.LogInformation("Email test completed successfully for {Email}", email);
                
                return Ok(new { 
                    Success = true, 
                    Message = "Test email sent successfully! Check your inbox and spam folder.",
                    SentTo = email,
                    TestTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email test failed for {Email}", email);
                
                return BadRequest(new { 
                    Success = false, 
                    Message = "Email test failed",
                    Error = ex.Message,
                    InnerException = ex.InnerException?.Message,
                    EmailAddress = email,
                    TestTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    // Include some debugging info
                    SmtpServer = "smtp-relay.brevo.com",
                    Port = 587
                });
            }
        }

        // Alternative test method with different email templates
        [HttpGet]
        [AllowAnonymous] // Remove after testing
        public async Task<IActionResult> TestEmailTemplates(string email = "belalsaad2001@gmail.com")
        {
            var results = new List<object>();
            
            try
            {
                // Test 1: Email Confirmation
                await _emailService.SendEmailConfirmationAsync(email, "https://localhost:5001/test-confirmation");
                results.Add(new { Test = "Email Confirmation", Status = "Success" });
                
                // Test 2: Welcome Email  
                await _emailService.SendWelcomeEmailAsync(email, "Test User");
                results.Add(new { Test = "Welcome Email", Status = "Success" });
                
                // Test 3: Password Reset
                await _emailService.SendPasswordResetAsync(email, "https://localhost:5001/test-reset");
                results.Add(new { Test = "Password Reset", Status = "Success" });
                
                return Ok(new { 
                    Success = true, 
                    Message = "All email templates sent successfully!",
                    SentTo = email,
                    Results = results,
                    TestTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                });
            }
            catch (Exception ex)
            {
                results.Add(new { Test = "Failed", Status = "Error", Error = ex.Message });
                
                return BadRequest(new { 
                    Success = false, 
                    Message = "Email template test failed",
                    Error = ex.Message,
                    Results = results
                });
            }
        }

        // Test method to check email configuration
        [HttpGet]
        [AllowAnonymous] // Remove after testing
        public IActionResult TestEmailConfig()
        {
            try
            {
                // This will help debug configuration issues
                return Ok(new {
                    Success = true,
                    Message = "Email service is properly configured",
                    ConfigurationCheck = "✅ EmailService injected successfully",
                    TestTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    Instructions = new {
                        Step1 = "Visit /Account/TestEmail to send a test email",
                        Step2 = "Check your email inbox and spam folder",
                        Step3 = "Check console logs for detailed information",
                        Step4 = "If successful, try user registration"
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new {
                    Success = false,
                    Message = "Email service configuration error",
                    Error = ex.Message,
                    InnerException = ex.InnerException?.Message
                });
            }
        }

        // ==================== EXISTING METHODS ====================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                // If user exists but email is not confirmed, offer to resend or delete and recreate
                if (!existingUser.EmailConfirmed)
                {
                    // Check if account is older than 24 hours - if so, delete and recreate
                    if (existingUser.CreatedAt < DateTime.UtcNow.AddHours(-24))
                    {
                        try
                        {
                            // Delete the old unconfirmed account
                            var deleteResult = await _userManager.DeleteAsync(existingUser);
                            if (deleteResult.Succeeded)
                            {
                                _logger.LogInformation("Deleted expired unconfirmed account for {Email}", model.Email);
                                // Continue with new registration below
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, "Unable to process registration. Please contact support.");
                                return View(model);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error deleting expired unconfirmed account for {Email}", model.Email);
                            ModelState.AddModelError(string.Empty, "Unable to process registration. Please contact support.");
                            return View(model);
                        }
                    }
                    else
                    {
                        // Account is less than 24 hours old, offer to resend confirmation
                        ModelState.AddModelError(string.Empty, 
                            "An account with this email already exists but hasn't been confirmed. Please check your email for the confirmation link.");
                        ViewBag.ShowResendLink = true;
                        ViewBag.Email = model.Email;
                        return View(model);
                    }
                }
                else
                {
                    // Email is confirmed, account already exists
                    ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                    return View(model);
                }
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with email {Email}.", user.Email);
                
                // Add user to default role
                await _userManager.AddToRoleAsync(user, "User");

                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new { userId = user.Id, token = token },
                    Request.Scheme);

                if (!string.IsNullOrEmpty(confirmationLink))
                {
                    try
                    {
                        await _emailService.SendEmailConfirmationAsync(user.Email, confirmationLink);
                        _logger.LogInformation("Email confirmation sent to {Email}.", user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send confirmation email to {Email}.", user.Email);
                        // Don't fail registration if email fails - user can resend later
                    }
                }
                else
                {
                    _logger.LogError("Confirmation link could not be generated for user {Email}.", user.Email);
                }

                return View("RegisterConfirmation", model);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Email confirmation attempted with missing userId or token.");
                TempData["ErrorMessage"] = "Invalid confirmation link.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found during email confirmation.", userId);
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            // Check if email is already confirmed
            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["InfoMessage"] = "Your email is already confirmed. You can log in.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed for user {Email}.", user.Email);
                TempData["SuccessMessage"] = "Thank you for confirming your email. You can now log in.";
                return RedirectToAction(nameof(Login));
            }

            _logger.LogError("Error confirming email for user {Email}: {Errors}", 
                user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            TempData["ErrorMessage"] = "Error confirming email. The link may be expired or invalid.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmationEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email address is required.";
                return RedirectToAction(nameof(Register));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that user doesn't exist
                TempData["InfoMessage"] = "If an account with that email exists, a confirmation email has been sent.";
                return RedirectToAction(nameof(Register));
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["InfoMessage"] = "Your email is already confirmed.";
                return RedirectToAction(nameof(Login));
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = user.Id, token = token },
                Request.Scheme);

            if (!string.IsNullOrEmpty(confirmationLink))
            {
                try
                {
                    await _emailService.SendEmailConfirmationAsync(user.Email, confirmationLink);
                    TempData["SuccessMessage"] = "Confirmation email has been resent.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resend confirmation email to {Email}.", user.Email);
                    TempData["ErrorMessage"] = "Failed to send confirmation email. Please try again later.";
                }
            }

            return RedirectToAction(nameof(Register));
        }

        // TEMPORARY CLEANUP METHODS - Remove [AllowAnonymous] after use
        [HttpGet]
        [AllowAnonymous] // Remove this after cleanup
        public async Task<IActionResult> CleanupUnconfirmedAccounts()
        {
            try
            {
                // Get all unconfirmed accounts older than 1 hour
                var unconfirmedUsers = await _userManager.Users
                    .Where(u => !u.EmailConfirmed && u.CreatedAt < DateTime.UtcNow.AddHours(-1))
                    .ToListAsync();

                var deletedCount = 0;
                var errors = new List<string>();

                foreach (var user in unconfirmedUsers)
                {
                    try
                    {
                        // Delete related data first
                        var userTasks = await _dbContext.Tasks
                            .Where(t => t.UserId == user.Id)
                            .ToListAsync();
                        if (userTasks.Any())
                        {
                            _dbContext.Tasks.RemoveRange(userTasks);
                        }

                        var userProjects = await _dbContext.Projects
                            .Where(p => p.UserId == user.Id)
                            .ToListAsync();
                        if (userProjects.Any())
                        {
                            _dbContext.Projects.RemoveRange(userProjects);
                        }

                        await _dbContext.SaveChangesAsync();

                        // Delete the user
                        var result = await _userManager.DeleteAsync(user);
                        if (result.Succeeded)
                        {
                            deletedCount++;
                            _logger.LogInformation("Deleted unconfirmed account: {Email}", user.Email);
                        }
                        else
                        {
                            errors.Add($"Failed to delete {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting unconfirmed account {Email}", user.Email);
                        errors.Add($"Exception deleting {user.Email}: {ex.Message}");
                    }
                }

                var message = $"Cleanup completed. Deleted {deletedCount} unconfirmed accounts.";
                if (errors.Any())
                {
                    message += $" Errors: {string.Join("; ", errors)}";
                }

                return Ok(new { 
                    Success = true, 
                    Message = message,
                    DeletedCount = deletedCount,
                    Errors = errors 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup of unconfirmed accounts");
                return BadRequest(new { 
                    Success = false, 
                    Message = $"Cleanup failed: {ex.Message}" 
                });
            }
        }

        [HttpPost]
        [AllowAnonymous] // Remove this after cleanup
        public async Task<IActionResult> DeleteSpecificUnconfirmedAccount(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required");
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                if (user.EmailConfirmed)
                {
                    return BadRequest("Cannot delete confirmed accounts through this method");
                }

                // Delete related data first
                var userTasks = await _dbContext.Tasks
                    .Where(t => t.UserId == user.Id)
                    .ToListAsync();
                if (userTasks.Any())
                {
                    _dbContext.Tasks.RemoveRange(userTasks);
                }

                var userProjects = await _dbContext.Projects
                    .Where(p => p.UserId == user.Id)
                    .ToListAsync();
                if (userProjects.Any())
                {
                    _dbContext.Projects.RemoveRange(userProjects);
                }

                await _dbContext.SaveChangesAsync();

                // Delete the user
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Manually deleted unconfirmed account: {Email}", user.Email);
                    return Ok(new { Success = true, Message = $"Successfully deleted unconfirmed account: {email}" });
                }
                else
                {
                    return BadRequest(new { 
                        Success = false, 
                        Message = $"Failed to delete account: {string.Join(", ", result.Errors.Select(e => e.Description))}" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting specific unconfirmed account {Email}", email);
                return BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, 
                    "Your email has not been confirmed. Please check your inbox for a confirmation link or resend it.");
                ViewBag.ShowResendLink = true;
                ViewBag.Email = model.Email;
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in.", user.Email);
                
                // Update last login timestamp
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                
                return RedirectToLocal(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(VerifyAuthenticatorCode), new { returnUrl, model.RememberMe });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out for {Email}.", user.Email);
                return View("Lockout");
            }

            // Invalid password or other failure
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyAuthenticatorCode(string? returnUrl = null, bool rememberMe = false)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["RememberMe"] = rememberMe;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new DeleteAccountViewModel
            {
                Email = user.Email
            };
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found for deletion.");
                return NotFound("User not found.");
            }

            _logger.LogInformation("Attempting to delete user account for {Email}", user.Email);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Delete related Tasks first (due to foreign key constraints)
                var userTasks = await _dbContext.Tasks
                    .Where(t => t.UserId == user.Id)
                    .ToListAsync();
                
                if (userTasks.Any())
                {
                    _dbContext.Tasks.RemoveRange(userTasks);
                    _logger.LogInformation("Marked {Count} tasks for deletion for user {Email}", 
                        userTasks.Count, user.Email);
                }

                // Delete related Projects
                var userProjects = await _dbContext.Projects
                    .Where(p => p.UserId == user.Id)
                    .ToListAsync();
                
                if (userProjects.Any())
                {
                    _dbContext.Projects.RemoveRange(userProjects);
                    _logger.LogInformation("Marked {Count} projects for deletion for user {Email}", 
                        userProjects.Count, user.Email);
                }

                // Save changes to delete related data
                await _dbContext.SaveChangesAsync();

                // Delete the user account
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    await transaction.CommitAsync();
                    await _signInManager.SignOutAsync();
                    
                    _logger.LogInformation("User {Email} successfully deleted their account.", user.Email);
                    TempData["SuccessMessage"] = "Your account has been successfully deleted.";
                    return RedirectToAction("Index", "Home");
                }

                // If user deletion failed, rollback transaction
                await transaction.RollbackAsync();
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                _logger.LogError("Error deleting user account for {Email}: {Errors}", 
                    user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return View(model);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error deleting account for {Email}", user.Email);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting your account. Please try again or contact support.";
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return View("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { token, email = user.Email },
                Request.Scheme);

            if (!string.IsNullOrEmpty(resetLink))
            {
                try
                {
                    await _emailService.SendPasswordResetAsync(user.Email, resetLink);
                    _logger.LogInformation("Password reset email sent to {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                }
            }

            return View("ForgotPasswordConfirmation");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }
    }
}