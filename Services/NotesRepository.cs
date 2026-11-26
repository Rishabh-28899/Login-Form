
using Microsoft.EntityFrameworkCore;
using Token2Core.Data;
using Writepad.Model;
using WritePad.Model;

namespace WritePad.Services
{
    public class NotesRepository : INotesRepository
    {
        private readonly TokenDbContext _context;

        public NotesRepository(TokenDbContext context)
        {
            _context = context;
        }

        public async Task<Note> CreateAsync(
            Note note,
            CancellationToken cancellationToken = default)
        {
            await _context.Notes.AddAsync(note, cancellationToken);

            return note;
        }

        public async Task<List<Note>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Notes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        // PAGINATED NOTES
        public async Task<(List<Note> Items, int TotalRecords)>
            GetPagedByUserIdAsync(
                int userId,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            var query = _context.Notes
                .Where(n => n.UserId == userId);

            var totalRecords = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalRecords);
        }

        // SEARCH, FILTER AND PAGINATION
        public async Task<(List<Note> Items, int TotalRecords)>
            SearchPagedByUserIdAsync(
                int userId,
                string? searchTerm,
                string? topic,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            var query = _context.Notes
                .Where(n => n.UserId == userId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(n =>
                    n.Title.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(topic))
            {
                query = query.Where(n =>
                    n.Topic == topic);
            }

            var totalRecords = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalRecords);
        }

        public async Task<Note?> GetByIdAndUserIdAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Notes
                .FirstOrDefaultAsync(
                    n => n.Id == id && n.UserId == userId,
                    cancellationToken);
        }

        public Task UpdateAsync(
            Note note,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _context.Notes.Update(note);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            Note note,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _context.Notes.Remove(note);

            return Task.CompletedTask;
        }
    }
}
