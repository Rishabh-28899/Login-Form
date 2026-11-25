
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Token2Core.Validators;
using Writepad.Model;
using WritePad.Model;
using WritePad.Services;

namespace Token2Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly INotesService _notesService;
        private readonly ILogger<NotesController> _logger;

        private readonly IValidator<CreateNoteRequest> _createValidator;
        private readonly IValidator<UpdateNoteRequest> _updateValidator;

        public NotesController(
            INotesService notesService,
            ILogger<NotesController> logger,
            IValidator<CreateNoteRequest> createValidator,
            IValidator<UpdateNoteRequest> updateValidator)
        {
            _notesService = notesService;
            _logger = logger;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

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
        [HttpPost]
        public async Task<IActionResult> CreateNote(
            [FromBody] CreateNoteRequest request)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

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

            var note = await _notesService.CreateNoteAsync(
                request, userId);

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
        [HttpGet]
        public async Task<IActionResult> GetMyNotes()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

            var notes = await _notesService.GetMyNotesAsync(userId);

            return Ok(notes);
        }

        // GET ALL NOTES BELONGING TO CURRENT USER
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotes()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

            var notes = await _notesService.GetMyNotesAsync(userId);

            return Ok(notes);
        }

        // GET SINGLE NOTE
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetNote(int id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

            var note = await _notesService.GetNoteAsync(id, userId);

            if (note == null)
                return NotFound("Note not found.");

            return Ok(note);
        }

        // UPDATE NOTE
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateNote(
            int id,
            [FromBody] UpdateNoteRequest request)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

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

            var updated = await _notesService.UpdateNoteAsync(
                id, request, userId);

            if (!updated)
                return NotFound("Note not found.");

            return Ok(new
            {
                message = "Note updated successfully",
                noteId = id
            });
        }

        // DELETE NOTE
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized("Invalid or missing user ID in token.");

            var deleted = await _notesService.DeleteNoteAsync(id, userId);

            if (!deleted)
                return NotFound("Note not found.");

            return Ok(new
            {
                message = "Note deleted successfully"
            });
        }
    }
}
