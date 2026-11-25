
using WritePad.Model;
using Writepad.Model;

namespace WritePad.Services
{
    public class NotesService : INotesService
    {
        private readonly INotesRepository _repository;
        private readonly ILogger<NotesService> _logger;

        public NotesService(
            INotesRepository repository,
            ILogger<NotesService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // CREATE NOTE
        public async Task<Note> CreateNoteAsync(
            CreateNoteRequest request,
            int userId)
        {
            var note = new Note
            {
                Title = request.Title,
                Content = request.Content,
                Topic = request.Topic,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };

            await _repository.CreateAsync(note);

            _logger.LogInformation(
                "Note created. NoteId: {NoteId}, UserId: {UserId}",
                note.Id,
                userId);

            return note;
        }

        // GET USER NOTES
        public async Task<List<Note>> GetMyNotesAsync(int userId)
        {
            var notes = await _repository.GetByUserIdAsync(userId);

            _logger.LogInformation(
                "Retrieved {Count} notes for UserId: {UserId}",
                notes.Count,
                userId);

            return notes;
        }

        // GET SINGLE NOTE
        public async Task<Note?> GetNoteAsync(
            int id,
            int userId)
        {
            var note = await _repository.GetByIdAndUserIdAsync(
                id,
                userId);

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
            int userId)
        {
            var note = await _repository.GetByIdAndUserIdAsync(
                id,
                userId);

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

            await _repository.UpdateAsync(note);

            _logger.LogInformation(
                "Note updated. NoteId: {NoteId}, UserId: {UserId}",
                id,
                userId);

            return true;
        }

        // DELETE NOTE
        public async Task<bool> DeleteNoteAsync(
            int id,
            int userId)
        {
            var note = await _repository.GetByIdAndUserIdAsync(
                id,
                userId);

            if (note == null)
            {
                _logger.LogWarning(
                    "Delete failed. NoteId: {NoteId}, UserId: {UserId}",
                    id,
                    userId);

                return false;
            }

            await _repository.DeleteAsync(note);

            _logger.LogInformation(
                "Note deleted. NoteId: {NoteId}, UserId: {UserId}",
                id,
                userId);

            return true;
        }
    }
}
