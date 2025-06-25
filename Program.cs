using Microsoft.AspNetCore.Identity;
using Hangfire;
using Hangfire.Dashboard;
using Serilog;
using System.Text.Json.Serialization;
using TaskManager.Web.Extensions;
using TaskManager.Web.BackgroundJobs;
using AspNetCoreRateLimit;
using TaskManager.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/taskmanager-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services using extension methods
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddCachingServices(builder.Configuration);
builder.Services.AddHangfireServices(builder.Configuration);
builder.Services.AddRateLimitingServices(builder.Configuration);
builder.Services.AddConfigurationServices(builder.Configuration);
builder.Services.AddBusinessServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddBackgroundServices();
builder.Services.AddNotificationServices();
builder.Services.AddElasticsearchServices(builder.Configuration);
builder.Services.AddExternalServices();

// MVC with JSON options
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.MaxDepth = 64;
    });

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

// HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add rate limiting middleware
app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Map SignalR hub for notifications
app.MapHub<NotificationHub>("/notificationHub");

// Map default MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Database initialization
await InitializeDatabaseAsync(app);

// Initialize Elasticsearch
await InitializeElasticsearchAsync(app);

// Recurring jobs AFTER database initialization
RecurringJob.AddOrUpdate<TaskReminderJob>(
    "daily-task-reminder",
    job => job.SendTaskReminders(),
    Cron.Daily);

// Notification recurring jobs
RecurringJob.AddOrUpdate<NotificationJob>(
    "due-task-notifications",
    job => job.SendDueTaskNotifications(),
    Cron.Daily(9)); // Run at 9 AM daily

RecurringJob.AddOrUpdate<NotificationJob>(
    "overdue-task-notifications", 
    job => job.SendOverdueTaskNotifications(),
    Cron.Daily(10)); // Run at 10 AM daily

RecurringJob.AddOrUpdate<NotificationJob>(
    "cleanup-old-notifications",
    job => job.CleanupOldNotifications(),
    Cron.Weekly); // Run weekly

// Elasticsearch recurring job
RecurringJob.AddOrUpdate<ElasticsearchSyncJob>(
    "elasticsearch-sync",
    job => job.SyncAllTasksAsync(),
    Cron.Hourly); // Sync every hour

app.Run();

// Database initialization method
static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TaskManager.Web.Data.ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    try
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Create default roles
        await CreateDefaultRolesAsync(roleManager);
        
        Console.WriteLine("✅ Database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
        throw; // Re-throw to prevent app from starting with broken DB
    }
}

// ADDED: Elasticsearch initialization
static async Task InitializeElasticsearchAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var elasticsearchService = scope.ServiceProvider.GetRequiredService<TaskManager.Web.Services.Interfaces.IElasticsearchService>();
    
    try
    {
        // Test connection
        var connected = await elasticsearchService.TestConnectionAsync();
        if (connected)
        {
            Console.WriteLine("✅ Elasticsearch connection successful");
            
            // Ensure index exists
            var indexExists = await elasticsearchService.IndexExistsAsync();
            if (!indexExists)
            {
                var created = await elasticsearchService.CreateIndexAsync();
                if (created)
                {
                    Console.WriteLine("✅ Elasticsearch index created successfully");
                }
                else
                {
                    Console.WriteLine("⚠️ Failed to create Elasticsearch index");
                }
            }
            else
            {
                Console.WriteLine("ℹ️ Elasticsearch index already exists");
            }
        }
        else
        {
            Console.WriteLine("⚠️ Elasticsearch connection failed - search features will be unavailable");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Elasticsearch initialization failed: {ex.Message}");
        // Don't throw - allow app to continue without Elasticsearch
    }
}

// Create default roles
static async Task CreateDefaultRolesAsync(RoleManager<IdentityRole> roleManager)
{
    string[] roleNames = { "Admin", "User" };
    
    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                Console.WriteLine($"✅ Role '{roleName}' created successfully");
            }
            else
            {
                Console.WriteLine($"❌ Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            Console.WriteLine($"ℹ️ Role '{roleName}' already exists");
        }
    }
}

// Hangfire Auth Filter
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true;
    }
}