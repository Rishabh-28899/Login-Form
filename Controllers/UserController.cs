using WritePad.Model;
using WritePad.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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

        // ==========================================
        // REGISTER
        // ==========================================
        [HttpPost("register")]
        public IActionResult Register(
            [FromBody] RegisterRequest request)
        {
            // Check if username already exists
            var existingUser = _context.Users
                .FirstOrDefault(x => x.Username == request.Username);

            if (existingUser != null)
            {
                return BadRequest("Username already exists");
            }

            // Hash the password
            var passwordHash = _passwordService
                .HashPassword(request.Password);

            // Create user
            var user = new User
            {
                Username = request.Username,
                PasswordHash = passwordHash,
                Email = request.Email,
                Role = request.Role
            };

            // Save user in SQL Server
            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new
            {
                message = "User registered successfully",
                username = user.Username
            });
        }

        // ==========================================
        // GET CURRENT LOGGED-IN USER
        // ==========================================
        [Authorize]
        [HttpGet("getuser")]
        public IActionResult GetUser()
        {
            // Get UserId added by UserIdMiddleware
            var userIdValue = HttpContext.Items["UserId"]?.ToString();

            if (string.IsNullOrEmpty(userIdValue))
            {
                return Unauthorized("User ID not found");
            }

            // Convert UserId to int
            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized("Invalid User ID");
            }

            // Find user in database
            var user = _context.Users
                .FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Return current logged-in user
            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                role = user.Role
            });
        }

        // ==========================================
        // GET ALL USERS
        // ==========================================
        [Authorize]
        [HttpGet("getallusers")]
        public IActionResult GetAllUsers()
        {
            var users = _context.Users
                .Select(user => new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    role = user.Role
                })
                .ToList();

            return Ok(users);
        }
    }
}



