
using Microsoft.Extensions.Logging;
using WritePad.Model;
using Writepad.Model;
using WritePad.DTOs;

namespace WritePad.Services
{
    public class NotesService : INotesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotesService> _logger;

        public NotesService(
            IUnitOfWork unitOfWork,
            ILogger<NotesService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // CREATE NOTE
        public async Task<Note> CreateNoteAsync(
            CreateNoteRequest request,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var note = new Note
            {
                Title = request.Title,
                Content = request.Content,
                Topic = request.Topic,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };

            await _unitOfWork.Notes.CreateAsync(
                note, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Note created. NoteId: {NoteId}, UserId: {UserId}",
                note.Id,
                userId);

            return note;
        }

        // GET ALL NOTES FOR CURRENT USER
        public async Task<List<Note>> GetMyNotesAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var notes = await _unitOfWork.Notes
                .GetByUserIdAsync(userId, cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} notes for UserId: {UserId}",
                notes.Count,
                userId);

            return notes;
        }

        // GET PAGINATED NOTES FOR CURRENT USER
        public async Task<PagedResponse<NoteResponseDto>>
            GetMyNotesPagedAsync(
                int userId,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            var result = await _unitOfWork.Notes
                .GetPagedByUserIdAsync(
                    userId,
                    pageNumber,
                    pageSize,
                    cancellationToken);

            var noteDtos = result.Items.Select(note =>
                new NoteResponseDto
                {
                    NoteId = note.Id,
                    UserId = note.UserId,
                    Title = note.Title,
                    Topic = note.Topic
                }).ToList();

            var response = new PagedResponse<NoteResponseDto>(
                noteDtos,
                pageNumber,
                pageSize,
                result.TotalRecords);

            _logger.LogInformation(
                "Retrieved page {PageNumber} of notes for UserId: {UserId}. Total records: {TotalRecords}",
                pageNumber,
                userId,
                result.TotalRecords);

            return response;
        }

        // SEARCH AND FILTER PAGINATED NOTES
        public async Task<PagedResponse<NoteResponseDto>>
            SearchMyNotesPagedAsync(
                int userId,
                string? searchTerm,
                string? topic,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            var result = await _unitOfWork.Notes
                .SearchPagedByUserIdAsync(
                    userId,
                    searchTerm,
                    topic,
                    pageNumber,
                    pageSize,
                    cancellationToken);

            var noteDtos = result.Items.Select(note =>
                new NoteResponseDto
                {
                    NoteId = note.Id,
                    UserId = note.UserId,
                    Title = note.Title,
                    Topic = note.Topic
                }).ToList();

            var response = new PagedResponse<NoteResponseDto>(
                noteDtos,
                pageNumber,
                pageSize,
                result.TotalRecords);

            _logger.LogInformation(
                "Searched notes for UserId: {UserId}, SearchTerm: {SearchTerm}, Topic: {Topic}, TotalRecords: {TotalRecords}",
                userId,
                searchTerm,
                topic,
                result.TotalRecords);

            return response;
        }

        // GET SINGLE NOTE
        public async Task<Note?> GetNoteAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var note = await _unitOfWork.Notes
                .GetByIdAndUserIdAsync(
                    id, userId, cancellationToken);

            if (note == null)
            {
                _logger.LogWarning(
                    "Note not found. NoteId: {NoteId}, UserId: {UserId}",
                    id,
                    userId);
            }

            return note;
        }

        // UPDATE NOTE
        public async Task<bool> UpdateNoteAsync(
            int id,
            UpdateNoteRequest request,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var note = await _unitOfWork.Notes
                .GetByIdAndUserIdAsync(
                    id, userId, cancellationToken);

            if (note == null)
            {
                _logger.LogWarning(
                    "Update failed. NoteId: {NoteId}, UserId: {UserId}",
                    id,
                    userId);

                return false;
            }

            note.Title = request.Title;
            note.Content = request.Content;
            note.Topic = request.Topic;
            note.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Notes.UpdateAsync(
                note, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Note updated. NoteId: {NoteId}, UserId: {UserId}",
                id,
                userId);

            return true;
        }

        // DELETE NOTE
        public async Task<bool> DeleteNoteAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var note = await _unitOfWork.Notes
                .GetByIdAndUserIdAsync(
                    id, userId, cancellationToken);

            if (note == null)
            {
                _logger.LogWarning(
                    "Delete failed. NoteId: {NoteId}, UserId: {UserId}",
                    id,
                    userId);

                return false;
            }

            await _unitOfWork.Notes.DeleteAsync(
                note, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Note deleted. NoteId: {NoteId}, UserId: {UserId}",
                id,
                userId);

            return true;
        }
    }
}
