using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using LegalDocSystem.Components;
using LegalDocSystem.Data;
using LegalDocSystem.Helpers;
using LegalDocSystem.Middleware;
using LegalDocSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Get connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Override password from environment variable or User Secrets (secure storage)
var dbPassword = builder.Configuration["Database:Password"]
    ?? Environment.GetEnvironmentVariable("LEGALDOC_DB_PASSWORD")
    ?? throw new InvalidOperationException(
        "Database password not configured. Set LEGALDOC_DB_PASSWORD environment variable or use User Secrets: dotnet user-secrets set \"Database:Password\" \"YourPassword\"");

// Replace password placeholder in connection string
if (connectionString.Contains("Password=;") || connectionString.Contains("Password=YOUR_PASSWORD_HERE"))
{
    connectionString = connectionString.Replace("Password=;", $"Password={dbPassword};")
                                      .Replace("Password=YOUR_PASSWORD_HERE;", $"Password={dbPassword};");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register Services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>(); // Singleton for encryption service
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISharedLinkService, SharedLinkService>();
builder.Services.AddScoped<IOutgoingService, OutgoingService>();
builder.Services.AddScoped<IIncomingService, IncomingService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

// Add Session support for authentication
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
        ? CookieSecurePolicy.Always 
        : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add HttpContextAccessor for AuthService
builder.Services.AddHttpContextAccessor();

// Add Authentication with Cookie scheme (for Session-based authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "LegalDocSystem.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
            ? CookieSecurePolicy.Always 
            : CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Match session timeout
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.LogoutPath = "/login";
        options.AccessDeniedPath = "/login";
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add Antiforgery (CSRF Protection) for API Controllers
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
        ? CookieSecurePolicy.Always 
        : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.FormFieldName = "__RequestVerificationToken";
});

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Policy for authenticated users (100 requests per minute per user)
    options.AddPolicy("AuthenticatedUserPolicy", context =>
    {
        var user = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(user))
        {
            // Partition by user identity
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"user_{user}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
        }
        
        // Fallback: partition by IP for unauthenticated requests
        var ipAddress = context.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"ip_{ipAddress}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 50, // Lower limit for unauthenticated users
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            });
    });

    // Policy for login endpoint (stricter limits to prevent brute force)
    options.AddPolicy("LoginPolicy", context =>
    {
        var ipAddress = context.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"login_{ipAddress}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5, // Only 5 login attempts per minute per IP
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No queuing for login attempts
            });
    });

    // Global fallback policy (for unauthenticated requests by IP)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ipAddress = context.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"global_{ipAddress}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200, // Global limit per IP
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
    });

    // Configure rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        
        // Calculate retry-after header
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }
        
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.",
            cancellationToken);
    };
});

// Add API Controllers
builder.Services.AddControllers();

// Hangfire
builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(connectionString)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings());

builder.Services.AddHangfireServer();
builder.Services.AddScoped<IBackgroundJobsService, BackgroundJobsService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365); // 1 year
        options.IncludeSubDomains = true;
        options.Preload = true;
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Rate Limiting middleware (must be after UseRouting and before UseAuthentication)
app.UseRateLimiter();

// Login Rate Limiting Middleware (before Authentication to catch login attempts)
app.UseMiddleware<LoginRateLimitMiddleware>();

// Authentication & Authorization middleware (must be after UseRouting and before UseSession)
app.UseAuthentication();
app.UseAuthorization();

// Antiforgery (CSRF Protection) middleware
app.UseAntiforgery();

// Audit Logging Middleware (must be after Authentication and Authorization)
app.UseMiddleware<AuditLoggingMiddleware>();

app.UseSession();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode()
   .RequireRateLimiting("AuthenticatedUserPolicy");

// Map API Controllers with rate limiting
app.MapControllers()
   .RequireRateLimiting("AuthenticatedUserPolicy");

// Hangfire Dashboard (restricted to Admin users only)
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthorizationFilter() }
});

// Schedule recurring jobs using IBackgroundJobsService
RecurringJob.AddOrUpdate<IBackgroundJobsService>(
    "process-ocr-queue",
    service => service.ProcessOcrQueueAsync(),
    Cron.Minutely);

RecurringJob.AddOrUpdate<IBackgroundJobsService>(
    "send-email-notifications",
    service => service.SendEmailNotificationsAsync(),
    Cron.Hourly);

RecurringJob.AddOrUpdate<IBackgroundJobsService>(
    "cleanup-expired-links",
    service => service.CleanupExpiredLinksAsync(),
    Cron.Daily);

RecurringJob.AddOrUpdate<IBackgroundJobsService>(
    "generate-audit-reports",
    service => service.GenerateAuditReportsAsync(),
    Cron.Daily(0, 0)); // Midnight daily

RecurringJob.AddOrUpdate<IBackgroundJobsService>(
    "unlock-expired-documents",
    service => service.UnlockExpiredDocumentsAsync(),
    Cron.Hourly);

app.Run();

