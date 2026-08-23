using IPad.Model;
using IPad.Services;
using Microsoft.AspNetCore.Mvc;
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
    }
}
