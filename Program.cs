
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Token2Core.Data;
using Token2Core.Middleware;
using Token2Core.Validators;
using WritePad.Data;
using WritePad.Services;

// ===============================
// SERILOG CONFIGURATION
// ===============================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
    )
    .WriteTo.File(
        "Logs/writepad-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate:
        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
    )
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// ===============================
// CONTROLLERS
// ===============================
builder.Services.AddControllers();

// ===============================
// RATE LIMITING
// ===============================
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext =>
            {
                var ipAddress =
                    httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    ipAddress,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;
});

// ===============================
// FLUENT VALIDATION
// ===============================
builder.Services.AddValidatorsFromAssemblyContaining<CreateNoteRequestValidator>();

// ===============================
// PASSWORD SERVICE
// ===============================
builder.Services.AddScoped<PasswordService>();

// ===============================
// CORS
// ===============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Correlation-ID");
    });
});

// ===============================
// JWT AUTHENTICATION
// ===============================
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT Key is missing from configuration."
    );
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                ),

                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning(
                    context.Exception,
                    "JWT authentication failed"
                );

                return Task.CompletedTask;
            }
        };
    });

// ===============================
// DATABASE
// ===============================
builder.Services.AddDbContext<TokenDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

// ===============================
// AUTHORIZATION
// ===============================
builder.Services.AddAuthorization();

// ===============================
// BUILD APPLICATION
// ===============================
var app = builder.Build();

Log.Information(
    "Current environment: {Environment}",
    app.Environment.EnvironmentName
);

// ===============================
// CORRELATION ID MIDDLEWARE
// ===============================
app.Use(async (context, next) =>
{
    var correlationId = Guid.NewGuid().ToString();

    context.TraceIdentifier = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        Log.Information(
            "CORRELATION MIDDLEWARE EXECUTED: {CorrelationId}",
            correlationId
        );

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] =
                correlationId;

            return Task.CompletedTask;
        });

        await next();
    }
});

// ===============================
// SERILOG REQUEST LOGGING
// ===============================
app.UseSerilogRequestLogging();

// ===============================
// GLOBAL EXCEPTION HANDLING
// ===============================
app.UseMiddleware<GlobalExceptionMiddleware>();

// ===============================
// HTTPS REDIRECTION
// ===============================
app.UseHttpsRedirection();

// ===============================
// CORS
// ===============================
app.UseCors("ReactPolicy");

// ===============================
// RATE LIMITING MIDDLEWARE
// ===============================
app.UseRateLimiter();

// ===============================
// JWT AUTHENTICATION
// ===============================
app.UseAuthentication();

// ===============================
// USER ID MIDDLEWARE
// ===============================
app.UseMiddleware<UserIdMiddleware>();

// ===============================
// AUTHORIZATION
// ===============================
app.UseAuthorization();

// ===============================
// MAP CONTROLLERS
// ===============================
app.MapControllers();

// ===============================
// CREATE ADMIN ACCOUNT
// ===============================
if (!app.Environment.IsEnvironment("Testing"))
{
    await AdminSeeder.SeedAdminAsync(app.Services);
}

// ===============================
// RUN APPLICATION
// ===============================
Log.Information("WritePad API is starting");

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

// ===============================
// MAKE PROGRAM ACCESSIBLE TO TEST PROJECT
// ===============================
public partial class Program
{
}







