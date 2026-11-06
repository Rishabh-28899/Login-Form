
using WritePad.Model;
using WritePad.Services;
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
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username, email and password are required.");
            }

            var username = request.Username.Trim();
            var email = request.Email.Trim();

            var existingUser = _context.Users
                .FirstOrDefault(x => x.Username == username);

            if (existingUser != null)
            {
                return BadRequest("Username already exists.");
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
            _context.SaveChanges();

            return Ok(new
            {
                message = "User registered successfully.",
                username = user.Username
            });
        }

        // GET CURRENT LOGGED-IN USER
        [Authorize]
        [HttpGet("getuser")]
        public IActionResult GetUser()
        {
            var userIdValue =
                HttpContext.Items["UserId"]?.ToString();

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized("Valid User ID was not found.");
            }

            var user = _context.Users
                .FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                role = user.Role
            });
        }

        // GET ALL USERS: ADMIN ONLY
        [Authorize(Roles = "Admin")]
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





