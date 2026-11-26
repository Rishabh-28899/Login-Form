
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Token2Core.Validators;
using Writepad.Model;
using WritePad.DTOs;
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

        private readonly IValidator<CreateNoteRequestDto> _createValidator;
        private readonly IValidator<UpdateNoteRequestDto> _updateValidator;

        public NotesController(
            INotesService notesService,
            ILogger<NotesController> logger,
            IValidator<CreateNoteRequestDto> createValidator,
            IValidator<UpdateNoteRequestDto> updateValidator)
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
            [FromBody] CreateNoteRequestDto request)
        {
            var cancellationToken = HttpContext.RequestAborted;

            if (!TryGetUserId(out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            var validationResult =
                await _createValidator.ValidateAsync(
                    request, cancellationToken);

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

            var serviceRequest = new CreateNoteRequest
            {
                Title = request.Title,
                Content = request.Content,
                Topic = request.Topic
            };

            var note = await _notesService.CreateNoteAsync(
                serviceRequest,
                userId,
                cancellationToken);

            var responseData = new NoteResponseDto
            {
                NoteId = note.Id,
                UserId = note.UserId,
                Title = note.Title,
                Topic = note.Topic
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

        // GET MY NOTES WITH SEARCH, FILTER AND PAGINATION
        [HttpGet]
        public async Task<IActionResult> GetMyNotes(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? topic = null)
        {
            var cancellationToken = HttpContext.RequestAborted;

            if (!TryGetUserId(out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            {
                var errors = new List<string>();

                if (pageNumber < 1)
                {
                    errors.Add(
                        "Page number must be greater than 0.");
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    errors.Add(
                        "Page size must be between 1 and 100.");
                }

                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Invalid pagination parameters.",
                        errors));
            }

            var pagedNotes =
                await _notesService.SearchMyNotesPagedAsync(
                    userId,
                    searchTerm,
                    topic,
                    pageNumber,
                    pageSize,
                    cancellationToken);

            return Ok(
                ApiResponse<PagedResponse<NoteResponseDto>>
                    .SuccessResponse(
                        pagedNotes,
                        "Notes retrieved successfully."));
        }

        // GET ALL NOTES BELONGING TO CURRENT USER
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotes()
        {
            var cancellationToken = HttpContext.RequestAborted;

            if (!TryGetUserId(out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            var notes = await _notesService.GetMyNotesAsync(
                userId, cancellationToken);

            var responseData = notes.Select(note => new NoteResponseDto
            {
                NoteId = note.Id,
                UserId = note.UserId,
                Title = note.Title,
                Topic = note.Topic
            }).ToList();

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    responseData,
                    "All notes retrieved successfully."));
        }

        // GET SINGLE NOTE
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetNote(int id)
        {
            var cancellationToken = HttpContext.RequestAborted;

            if (!TryGetUserId(out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            var note = await _notesService.GetNoteAsync(
                id, userId, cancellationToken);

            if (note == null)
            {
                return NotFound(
                    ApiResponse<object>.FailureResponse(
                        "Note not found."));
            }

            var responseData = new NoteResponseDto
            {
                NoteId = note.Id,
                UserId = note.UserId,
                Title = note.Title,
                Topic = note.Topic
            };

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    responseData,
                    "Note retrieved successfully."));
        }

        // UPDATE NOTE
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateNote(
            int id,
            [FromBody] UpdateNoteRequestDto request)
        {
            var cancellationToken = HttpContext.RequestAborted;

            if (!TryGetUserId(out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            var validationResult =
                await _updateValidator.ValidateAsync(
                    request, cancellationToken);

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

            var serviceRequest = new UpdateNoteRequest
            {
                Title = request.Title,
                Content = request.Content,
                Topic = request.Topic
            };

            var updated = await _notesService.UpdateNoteAsync(
                id,
                serviceRequest,
                userId,
                cancellationToken);

            if (!updated)
            {
                return NotFound(
                    ApiResponse<object>.FailureResponse(
                        "Note not found."));
            }

            var responseData = new NoteResponseDto
            {
                NoteId = id,
                UserId = userId,
                Title = request.Title,
                Topic = request.Topic
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
            var cancellationToken = HttpContext.RequestAborted;

            if (!TryGetUserId(out int userId))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or missing user ID in token."));
            }

            var deleted = await _notesService.DeleteNoteAsync(
                id, userId, cancellationToken);

            if (!deleted)
            {
                return NotFound(
                    ApiResponse<object>.FailureResponse(
                        "Note not found."));
            }

            return NoContent();
        }
    }
}
