using System.Text;
using System.Security.Claims;

using WritePad.Services;
using Token2Core.Middleware;
using Token2Core.Data;
using WritePad.Data;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Serilog;

// ===============================
// SERILOG CONFIGURATION
// ===============================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/writepad-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30
    )
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Integrate Serilog with ASP.NET Core
builder.Host.UseSerilog();

// ===============================
// CONTROLLERS
// ===============================
builder.Services.AddControllers();

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
            .AllowAnyMethod();
    });
});

// ===============================
// JWT AUTHENTICATION
// ===============================
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT Key is missing from configuration.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
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

// ===============================
// REQUEST LOGGING
// ===============================
app.UseSerilogRequestLogging();

// ===============================
// MIDDLEWARE PIPELINE
// ===============================
app.UseHttpsRedirection();

app.UseCors("ReactPolicy");

app.UseAuthentication();

app.UseMiddleware<UserIdMiddleware>();

app.UseAuthorization();

// ===============================
// MAP CONTROLLERS
// ===============================
app.MapControllers();

// ===============================
// CREATE ADMIN ACCOUNT
// ===============================
await AdminSeeder.SeedAdminAsync(app.Services);

// ===============================
// RUN APPLICATION
// ===============================
Log.Information("WritePad API is starting");

app.Run();

Log.CloseAndFlush();





