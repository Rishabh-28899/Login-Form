
using WritePad.Model;
using WritePad.Services;
using WritePad.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Token2Core.Data;

namespace Token2Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly TokenDbContext _context;
        private readonly PasswordService _passwordService;

        public UserController(
            TokenDbContext context,
            PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        // REGISTER: Public registration always creates a normal User.
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Username, email and password are required."));
            }

            var username = request.Username.Trim();
            var email = request.Email.Trim();

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.Username == username,
                    cancellationToken);

            if (existingUser != null)
            {
                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Username already exists."));
            }

            var passwordHash =
                _passwordService.HashPassword(request.Password);

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                Role = "User"
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    new
                    {
                        username = user.Username
                    },
                    "User registered successfully."));
        }

        // GET CURRENT LOGGED-IN USER
        [Authorize]
        [HttpGet("getuser")]
        public async Task<IActionResult> GetUser(
            CancellationToken cancellationToken)
        {
            var userIdValue =
                HttpContext.Items["UserId"]?.ToString();

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Valid User ID was not found."));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.Id == userId,
                    cancellationToken);

            if (user == null)
            {
                return NotFound(
                    ApiResponse<object>.FailureResponse(
                        "User not found."));
            }

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    new
                    {
                        id = user.Id,
                        username = user.Username,
                        email = user.Email,
                        role = user.Role
                    },
                    "User details retrieved successfully."));
        }

        // GET ALL USERS: ADMIN ONLY
        [Authorize(Roles = "Admin")]
        [HttpGet("getallusers")]
        public async Task<IActionResult> GetAllUsers(
            CancellationToken cancellationToken)
        {
            var users = await _context.Users
                .Select(user => new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    role = user.Role
                })
                .ToListAsync(cancellationToken);

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    users,
                    "Users retrieved successfully."));
        }
    }
}



