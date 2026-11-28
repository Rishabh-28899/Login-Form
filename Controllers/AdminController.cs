using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Token2Core.Data;
using WritePad.DTOs;
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
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            TokenDbContext context,
            PasswordService passwordService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _logger = logger;
        }

        // =====================================
        // 1. GET ALL USERS
        // GET: api/Admin/users
        // =====================================

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers(
            CancellationToken cancellationToken)
        {
            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .Select(u => new AdminUserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Admin retrieved {UserCount} users",
                users.Count);

            return Ok(ApiResponse<List<AdminUserResponseDto>>
                .SuccessResponse(users, "Users retrieved successfully."));
        }

        // =====================================
        // 1A. GET PAGINATED USERS
        // GET: api/Admin/users/paged?pageNumber=1&pageSize=10
        // =====================================

        [HttpGet("users/paged")]
        public async Task<IActionResult> GetPaginatedUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            // Validate pagination parameters

            if (pageNumber < 1)
            {
                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Page number must be greater than zero."));
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Page size must be between 1 and 100."));
            }

            // Get total user count

            var totalCount = await _context.Users
                .CountAsync(cancellationToken);

            // Calculate total pages

            var totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            // Fetch only the requested page

            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role
                })
                .ToListAsync(cancellationToken);

            // Prepare paginated response

            var response = new PagedResponseDto<AdminUserResponseDto>
            {
                Items = users,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            _logger.LogInformation(
                "Admin retrieved users page {PageNumber} with page size {PageSize}. Total users: {TotalCount}",
                pageNumber,
                pageSize,
                totalCount);

            return Ok(
                ApiResponse<PagedResponseDto<AdminUserResponseDto>>
                    .SuccessResponse(
                        response,
                        "Users retrieved successfully."));
        }


        // =====================================
        // 2. DASHBOARD STATISTICS
        // GET: api/Admin/dashboard
        // =====================================

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            CancellationToken cancellationToken)
        {
            var totalUsers = await _context.Users
                .CountAsync(cancellationToken);

            var totalAdmins = await _context.Users
                .CountAsync(u => u.Role == "Admin", cancellationToken);

            var regularUsers = await _context.Users
                .CountAsync(u => u.Role == "User", cancellationToken);

            var totalNotes = await _context.Notes
                .CountAsync(cancellationToken);

            var dashboard = new AdminDashboardResponseDto
            {
                TotalUsers = totalUsers,
                TotalAdmins = totalAdmins,
                RegularUsers = regularUsers,
                TotalNotes = totalNotes
            };

            _logger.LogInformation(
                "Admin retrieved dashboard statistics");

            return Ok(ApiResponse<AdminDashboardResponseDto>
                .SuccessResponse(
                    dashboard,
                    "Dashboard statistics retrieved successfully."));
        }

        // =====================================
        // 3. GET ALL NOTES
        // GET: api/Admin/notes
        // =====================================

        [HttpGet("notes")]
        public async Task<IActionResult> GetAllNotes(
            CancellationToken cancellationToken)
        {
            var notes = await _context.Notes
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new AdminNoteResponseDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Topic = n.Topic,
                    Content = n.Content,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt,
                    UserId = n.UserId,
                    Username = n.User.Username ?? string.Empty
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Admin retrieved {NoteCount} notes",
                notes.Count);

            return Ok(ApiResponse<List<AdminNoteResponseDto>>
                .SuccessResponse(notes, "Notes retrieved successfully."));
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
            [FromBody] AddUserRequest request,
            CancellationToken cancellationToken)
        {
            var username = request.Username.Trim();
            var email = request.Email.Trim();

            // Check duplicate username

            var usernameExists = await _context.Users
                .AnyAsync(
                    u => u.Username == username,
                    cancellationToken);

            if (usernameExists)
            {
                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Username already exists."));
            }

            // Check duplicate email

            var emailExists = await _context.Users
                .AnyAsync(
                    u => u.Email == email,
                    cancellationToken);

            if (emailExists)
            {
                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Email already exists."));
            }

            // Validate role

            if (request.Role != "User" &&
                request.Role != "Admin")
            {
                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Invalid role."));
            }

            // Create user

            var user = new User
            {
                Username = username,
                Email = email,
                Role = request.Role
            };

            // Hash password

            user.PasswordHash =
                _passwordService.HashPassword(request.Password);

            // Save to database

            _context.Users.Add(user);

            await _context.SaveChangesAsync(cancellationToken);

            var response = new AdminUserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            };

            _logger.LogInformation(
                "Admin created user with ID {UserId}",
                user.Id);

            return Ok(
                ApiResponse<AdminUserResponseDto>.SuccessResponse(
                    response,
                    "User created successfully."));
        }
    }
}


