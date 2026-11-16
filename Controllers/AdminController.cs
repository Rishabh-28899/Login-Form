
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Token2Core.Data;
using WritePad.Model;
using WritePad.Services;

namespace Token2Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly TokenDbContext _context;
        private readonly PasswordService _passwordService;

        public AdminController(
            TokenDbContext context,
            PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        // =====================================
        // 1. GET ALL USERS
        // GET: api/Admin/users
        // =====================================

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        // =====================================
        // 2. DASHBOARD STATISTICS
        // GET: api/Admin/dashboard
        // =====================================

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var totalUsers = await _context.Users.CountAsync();

            var totalAdmins = await _context.Users
                .CountAsync(u => u.Role == "Admin");

            var regularUsers = await _context.Users
                .CountAsync(u => u.Role == "User");

            var totalNotes = await _context.Notes.CountAsync();

            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalAdmins = totalAdmins,
                RegularUsers = regularUsers,
                TotalNotes = totalNotes
            });
        }

        // =====================================
        // 3. GET ALL NOTES
        // GET: api/Admin/notes
        // =====================================

        [HttpGet("notes")]
        public async Task<IActionResult> GetAllNotes()
        {
            var notes = await _context.Notes
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Topic,
                    n.Content,
                    n.CreatedAt,
                    n.UpdatedAt,
                    n.UserId,
                    Username = n.User.Username
                })
                .ToListAsync();

            return Ok(notes);
        }

        // =====================================
        // 4. ADD NEW USER
        // POST: api/Admin/users
        // =====================================

        public class AddUserRequest
        {
            [Required]
            [StringLength(50, MinimumLength = 3)]
            public string Username { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [MinLength(8)]
            public string Password { get; set; } = string.Empty;

            [Required]
            public string Role { get; set; } = "User";
        }

        [HttpPost("users")]
        public async Task<IActionResult> AddUser(
            [FromBody] AddUserRequest request)
        {
            // Check duplicate username
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == request.Username.Trim());

            if (usernameExists)
            {
                return BadRequest(new
                {
                    message = "Username already exists."
                });
            }

            // Check duplicate email
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email.Trim());

            if (emailExists)
            {
                return BadRequest(new
                {
                    message = "Email already exists."
                });
            }

            // Validate role
            if (request.Role != "User" &&
                request.Role != "Admin")
            {
                return BadRequest(new
                {
                    message = "Invalid role."
                });
            }

            // Create user
            var user = new User
            {
                Username = request.Username.Trim(),
                Email = request.Email.Trim(),
                Role = request.Role
            };

            // Hash password using existing PasswordService
            user.PasswordHash =
                _passwordService.HashPassword(request.Password);

            // Save to database
            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User created successfully!",
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Role
                }
            });
        }
    }
}

