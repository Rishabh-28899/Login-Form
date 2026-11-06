
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Token2Core.Data;

namespace Token2Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly TokenDbContext _context;

        public AdminController(TokenDbContext context)
        {
            _context = context;
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

            var dashboard = new
            {
                TotalUsers = totalUsers,
                TotalAdmins = totalAdmins,
                RegularUsers = regularUsers,
                TotalNotes = totalNotes
            };

            return Ok(dashboard);
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
    }
}

