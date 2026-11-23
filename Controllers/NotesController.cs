
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Token2Core.Data;
using Token2Core.Validators;
using Writepad.Model;
using WritePad.Model;

namespace Token2Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly TokenDbContext _context;
        private readonly ILogger<NotesController> _logger;

        private readonly IValidator<CreateNoteRequest> _createValidator;
        private readonly IValidator<UpdateNoteRequest> _updateValidator;

        public NotesController(
            TokenDbContext context,
            ILogger<NotesController> logger,
            IValidator<CreateNoteRequest> createValidator,
            IValidator<UpdateNoteRequest> updateValidator)
        {
            _context = context;
            _logger = logger;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        // GET AUTHENTICATED USER ID
        private bool TryGetUserId(out int userId)
        {
            userId = 0;

            var userIdValue =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var isValid = int.TryParse(userIdValue, out userId)
                          && userId > 0;

            if (!isValid)
            {
                _logger.LogWarning(
                    "Invalid or missing UserId in authentication token.");
            }

            return isValid;
        }

        // CREATE NOTE
        // POST: api/Notes
        [HttpPost]
        public async Task<IActionResult> CreateNote(
            [FromBody] CreateNoteRequest request)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

            // FLUENT VALIDATION
            var validationResult =
                await _createValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Create note validation failed. UserId: {UserId}",
                    userId);

                return BadRequest(new
                {
                    message = "Validation failed.",
                    errors = validationResult.Errors.Select(error => new
                    {
                        field = error.PropertyName,
                        error = error.ErrorMessage
                    })
                });
            }

            var note = new Note
            {
                Title = request.Title,
                Content = request.Content,
                Topic = request.Topic,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Note created successfully. NoteId: {NoteId}, UserId: {UserId}",
                note.Id, userId);

            return Ok(new
            {
                message = "Note created successfully",
                noteId = note.Id,
                userId = note.UserId,
                title = note.Title,
                topic = note.Topic
            });
        }

        // GET MY NOTES
        // GET: api/Notes
        [HttpGet]
        public async Task<IActionResult> GetMyNotes()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

            var notes = await _context.Notes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            _logger.LogInformation(
                "Retrieved {Count} notes for UserId: {UserId}",
                notes.Count, userId);

            return Ok(notes);
        }

        // GET MY NOTES
        // GET: api/Notes/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotes()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

            var notes = await _context.Notes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            _logger.LogInformation(
                "Retrieved all notes. Count: {Count}, UserId: {UserId}",
                notes.Count, userId);

            return Ok(notes);
        }

        // GET ONE NOTE
        // GET: api/Notes/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetNote(int id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

            var note = await _context.Notes
                .FirstOrDefaultAsync(n =>
                    n.Id == id && n.UserId == userId);

            if (note == null)
            {
                _logger.LogWarning(
                    "Note not found. NoteId: {NoteId}, UserId: {UserId}",
                    id, userId);

                return NotFound("Note not found.");
            }

            _logger.LogInformation(
                "Note retrieved successfully. NoteId: {NoteId}, UserId: {UserId}",
                id, userId);

            return Ok(note);
        }

        // UPDATE NOTE
        // PUT: api/Notes/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateNote(
            int id,
            [FromBody] UpdateNoteRequest request)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

            // FLUENT VALIDATION
            var validationResult =
                await _updateValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Update note validation failed. NoteId: {NoteId}, UserId: {UserId}",
                    id, userId);

                return BadRequest(new
                {
                    message = "Validation failed.",
                    errors = validationResult.Errors.Select(error => new
                    {
                        field = error.PropertyName,
                        error = error.ErrorMessage
                    })
                });
            }

            var note = await _context.Notes
                .FirstOrDefaultAsync(n =>
                    n.Id == id && n.UserId == userId);

            if (note == null)
            {
                _logger.LogWarning(
                    "Update failed: Note not found. NoteId: {NoteId}, UserId: {UserId}",
                    id, userId);

                return NotFound("Note not found.");
            }

            note.Title = request.Title;
            note.Content = request.Content;
            note.Topic = request.Topic;
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Note updated successfully. NoteId: {NoteId}, UserId: {UserId}",
                id, userId);

            return Ok(new
            {
                message = "Note updated successfully",
                noteId = note.Id
            });
        }

        // DELETE NOTE
        // DELETE: api/Notes/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

            var note = await _context.Notes
                .FirstOrDefaultAsync(n =>
                    n.Id == id && n.UserId == userId);

            if (note == null)
            {
                _logger.LogWarning(
                    "Delete failed: Note not found. NoteId: {NoteId}, UserId: {UserId}",
                    id, userId);

                return NotFound("Note not found.");
            }

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Note deleted successfully. NoteId: {NoteId}, UserId: {UserId}",
                id, userId);

            return Ok(new
            {
                message = "Note deleted successfully"
            });
        }
    }
}

