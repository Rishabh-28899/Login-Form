
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Token2Core.Validators;
using Writepad.Model;
using WritePad.Model;
using WritePad.Services;
using WritePad.DTOs;

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
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            var validationResult =
                await _createValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Create note validation failed. UserId: {UserId}",
                    userId);

                var errors = validationResult.Errors
                    .Select(error =>
                        $"{error.PropertyName}: {error.ErrorMessage}")
                    .ToList();

                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Validation failed.",
                        errors));
            }

            var note = await _notesService.CreateNoteAsync(
                request, userId);

            var responseData = new
            {
                noteId = note.Id,
                userId = note.UserId,
                title = note.Title,
                topic = note.Topic
            };

            var response =
                ApiResponse<object>.SuccessResponse(
                    responseData,
                    "Note created successfully.");

            return CreatedAtAction(
                nameof(GetNote),
                new { id = note.Id },
                response);
        }

        // GET MY NOTES
        [HttpGet]
        public async Task<IActionResult> GetMyNotes()
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            var notes = await _notesService.GetMyNotesAsync(userId);

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    notes,
                    "Notes retrieved successfully."));
        }

        // GET ALL NOTES BELONGING TO CURRENT USER
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotes()
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            var notes = await _notesService.GetMyNotesAsync(userId);

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    notes,
                    "All notes retrieved successfully."));
        }

        // GET SINGLE NOTE
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetNote(int id)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            var note = await _notesService.GetNoteAsync(id, userId);

            if (note == null)
            {
                return NotFound(
                    ApiResponse<object>.FailureResponse(
                        "Note not found."));
            }

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    note,
                    "Note retrieved successfully."));
        }

        // UPDATE NOTE
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateNote(
            int id,
            [FromBody] UpdateNoteRequest request)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            var validationResult =
                await _updateValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Update note validation failed. NoteId: {NoteId}, UserId: {UserId}",
                    id, userId);

                var errors = validationResult.Errors
                    .Select(error =>
                        $"{error.PropertyName}: {error.ErrorMessage}")
                    .ToList();

                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Validation failed.",
                        errors));
            }

            var updated = await _notesService.UpdateNoteAsync(
                id, request, userId);

            if (!updated)
            {
                return NotFound(
                    ApiResponse<object>.FailureResponse(
                        "Note not found."));
            }

            var responseData = new
            {
                noteId = id
            };

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    responseData,
                    "Note updated successfully."));
        }

        // DELETE NOTE
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            var deleted = await _notesService.DeleteNoteAsync(
                id, userId);

            if (!deleted)
            {
                return NotFound(
                    ApiResponse<object>.FailureResponse(
                        "Note not found."));
            }

            // 204 = Successfully deleted, no response body
            return NoContent();
        }
    }
}
