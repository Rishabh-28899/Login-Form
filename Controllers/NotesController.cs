using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Token2Core.Data;
using Writepad.Model;
using WritePad.Model;
using System.Security.Claims;

namespace Token2Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly TokenDbContext _context;

        public NotesController(TokenDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // CREATE NOTE
        // ==========================================
        [HttpPost]
        public IActionResult CreateNote([FromBody] CreateNoteRequest request)
        {
            // Get logged-in user's ID from middleware
            var userIdValue = HttpContext.Items["UserId"]?.ToString();

            if (string.IsNullOrEmpty(userIdValue))
            {
                return Unauthorized("User ID not found");
            }

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized("Invalid User ID");
            }

            // Create note
            var note = new Note
            {
                Title = request.Title,
                Content = request.Content,
                Topic = request.Topic,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };

            _context.Notes.Add(note);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Note created successfully",
                noteId = note.Id,
                userId = note.UserId,
                title = note.Title,
                topic = note.Topic
            });
        }

        // ==========================================
        // GET MY NOTES
        // ==========================================
        [HttpGet]
        public IActionResult GetMyNotes()
        {
           // var userIdValue = HttpContext.Items["UserId"]?.ToString();
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdValue))
            {
                return Unauthorized("User Id not found");
            }
            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized("Invalid User Id");

            }
            var notes = _context.Notes.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToList();
            return Ok(notes);
        }
        [Authorize]
        [HttpGet("all")]
        public IActionResult GetAllNotes()
        {
            var notes = _context.Notes.OrderByDescending(x => x.CreatedAt).ToList();
            return Ok(notes);
        }
        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetNote(int id)
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdValue))
            {
                return Unauthorized("User ID not found in token");
            }

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized("Invalid User ID");
            }

            var note = _context.Notes
                .FirstOrDefault(x => x.Id == id && x.UserId == userId);

            if (note == null)
            {
                return NotFound("Note not found");
            }

            return Ok(note);
      
        
        }

        [Authorize]
        [HttpPut("{id}")]
        public IActionResult UpdateNote(
    int id,
        [FromBody] UpdateNoteRequest request)
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdValue))
            {
                return Unauthorized("User ID not found in token");
            }

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized("Invalid User ID");
            }

            var note = _context.Notes
                .FirstOrDefault(x => x.Id == id && x.UserId == userId);

            if (note == null)
            {
                return NotFound("Note not found");
            }

            note.Title = request.Title;
            note.Content = request.Content;
            note.Topic = request.Topic;
            note.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            return Ok(new
            {
                message = "Note updated successfully",
                noteId = note.Id
            });
        }
        [Authorize]
        [HttpDelete("{id}")]
        public IActionResult DeleteNote(int id)
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdValue))
            {
                return Unauthorized("User ID not found in token");
            }

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized("Invalid User ID");
            }

            var note = _context.Notes
                .FirstOrDefault(x => x.Id == id && x.UserId == userId);

            if (note == null)
            {
                return NotFound("Note not found");
            }

            _context.Notes.Remove(note);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Note deleted successfully"
            });
        }
    }
}
