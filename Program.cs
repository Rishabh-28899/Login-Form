
using System.Text;
using System.Security.Claims;

using WritePad.Services;
using Token2Core.Middleware;
using Token2Core.Data;
using WritePad.Data;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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

        // TEMPORARY: Display JWT authentication errors
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("=================================");
                Console.WriteLine("JWT AUTHENTICATION FAILED");
                Console.WriteLine(context.Exception.Message);
                Console.WriteLine("=================================");

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
app.Run();





