// Extensions/ServiceCollectionExtensions.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.Web.Data;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Interfaces;
using TaskManager.Web.Services.Business;
using TaskManager.Web.Services.Infrastructure;
using TaskManager.Web.BackgroundJobs;
using TaskManager.Web.Configuration;
using Hangfire;
using Hangfire.SqlServer;
using AspNetCoreRateLimit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using TaskManager.Web.Services.infrastructure;

namespace TaskManager.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                       .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning)));

            return services;
        }

        public static IServiceCollection AddIdentityServices(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;

                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Configure application cookies
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            return services;
        }

        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IProjectService, ProjectService>();

            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IEmailService, SimpleEmailService>();
            services.AddScoped<IFileService, FileService>();

            return services;
        }

        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            services.AddScoped<TaskReminderJob>();
            services.AddScoped<NotificationJob>();
            return services;
        }

        public static IServiceCollection AddNotificationServices(this IServiceCollection services)
        {
            services.AddScoped<INotificationService, NotificationService>();
            services.AddSignalR();
            return services;
        }

        // FIXED CACHING SERVICES
        public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConnection = configuration.GetConnectionString("Redis");

            if (!string.IsNullOrEmpty(redisConnection))
            {
                try
                {
                    // Try to use Redis
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = redisConnection;
                        options.InstanceName = "TaskManager";
                    });

                    Console.WriteLine("✅ Redis cache configured successfully");
                }
                catch (Exception ex)
                {
                    // Fallback to memory cache if Redis fails
                    Console.WriteLine($"⚠️ Redis connection failed: {ex.Message}, falling back to memory cache");
                    services.AddMemoryCache();
                    services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
                }
            }
            else
            {
                // Use memory cache if no Redis connection string
                Console.WriteLine("ℹ️ No Redis configured, using memory cache");
                services.AddMemoryCache();
                services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
            }

            // Register cache services
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<ICachedDashboardService, CachedDashboardService>();
            services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();

            return services;
        }

        public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHangfire(hangfireConfiguration => hangfireConfiguration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"),
                    new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    }));

            services.AddHangfireServer();
            return services;
        }

        public static IServiceCollection AddRateLimitingServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

            return services;
        }

        public static IServiceCollection AddConfigurationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Email Settings
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

            return services;
        }

        public static IServiceCollection AddExternalServices(this IServiceCollection services)
        {
            // AutoMapper
            services.AddAutoMapper(typeof(Program));

            // HttpClient
            services.AddHttpClient();

            return services;
        }

        // FIXED: Elasticsearch services registration
        public static IServiceCollection AddElasticsearchServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure Elasticsearch settings
            services.Configure<ElasticsearchSettings>(configuration.GetSection("Elasticsearch"));
            
            // FIXED: Register as Scoped (not Singleton) and register both interface and concrete class
            services.AddScoped<IElasticsearchService, ElasticsearchService>();
            services.AddScoped<ElasticsearchService>(); // For direct injection in debug controller
            
            // Register background job
            services.AddScoped<ElasticsearchSyncJob>();
            
            return services;
        }
    }
}