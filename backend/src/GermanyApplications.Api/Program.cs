using System.Threading.RateLimiting;
using FluentValidation;
using GermanyApplications.Api.Authentication;
using GermanyApplications.Api.Authorization;
using GermanyApplications.Api.Data;
using GermanyApplications.Api.Domain.Entities;
using GermanyApplications.Api.Email;
using GermanyApplications.Api.Health;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("At least one explicit CORS origin must be configured.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("GermanyApplications");
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}
else if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("DataProtection:KeysPath must use a protected persistent volume outside Development.");
}

builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequiredUniqueChars = 4;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<Role>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = builder.Environment.IsDevelopment()
        ? "GermanyApplications.Session"
        : "__Host-GermanyApplications.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.Path = "/";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    };
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(15));
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(2));

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = builder.Environment.IsDevelopment()
        ? "GermanyApplications.Antiforgery"
        : "__Host-GermanyApplications.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context => CreateFixedWindowPartition(context, 5, TimeSpan.FromMinutes(1)));
    options.AddPolicy("registration", context => CreateFixedWindowPartition(context, 3, TimeSpan.FromMinutes(10)));
    options.AddPolicy("password-recovery", context => CreateFixedWindowPartition(context, 3, TimeSpan.FromMinutes(10)));
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.ContentManagement, policy =>
        policy.RequireRole(AppRoles.ContentEditor, AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy(AuthorizationPolicies.Review, policy =>
        policy.RequireRole(AppRoles.Reviewer, AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy(AuthorizationPolicies.Support, policy =>
        policy.RequireRole(AppRoles.SupportAgent, AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy(AuthorizationPolicies.Administration, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy(AuthorizationPolicies.OwnsStudentResource, policy =>
        policy.AddRequirements(new StudentOwnershipRequirement()));
});
builder.Services.AddSingleton<IAuthorizationHandler, StudentOwnershipHandler>();
builder.Services.AddScoped<ISecurityAuditWriter, SecurityAuditWriter>();
builder.Services.AddScoped<IAccountEmailSender, DevelopmentAccountEmailSender>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<ApplicationDbContext>("postgresql", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/", () => Results.Ok(new { service = "Germany Applications API" }))
    .ExcludeFromDescription();
app.MapAuthEndpoints();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = HealthResponseWriter.WriteAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync
});

app.Run();

static RateLimitPartition<string> CreateFixedWindowPartition(
    HttpContext context,
    int permitLimit,
    TimeSpan window)
{
    var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = window,
        QueueLimit = 0,
        AutoReplenishment = true
    });
}

public partial class Program;
