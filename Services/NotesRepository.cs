
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

        public async Task<Note> CreateAsync(Note note)
        {
            await _context.Notes.AddAsync(note);

            return note;
        }

        public async Task<List<Note>> GetByUserIdAsync(int userId)
        {
            return await _context.Notes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        // PAGINATED NOTES
        public async Task<(List<Note> Items, int TotalRecords)>
            GetPagedByUserIdAsync(
                int userId,
                int pageNumber,
                int pageSize)
        {
            var query = _context.Notes
                .Where(n => n.UserId == userId);

            // Count total notes for this user
            var totalRecords = await query.CountAsync();

            // Fetch only the requested page
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalRecords);
        }

        // SEARCH, FILTER AND PAGINATION
        public async Task<(List<Note> Items, int TotalRecords)>
            SearchPagedByUserIdAsync(
                int userId,
                string? searchTerm,
                string? topic,
                int pageNumber,
                int pageSize)
        {
            // First restrict notes to the authenticated user
            var query = _context.Notes
                .Where(n => n.UserId == userId);

            // Search by title
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(n =>
                    n.Title.Contains(searchTerm));
            }

            // Filter by topic
            if (!string.IsNullOrWhiteSpace(topic))
            {
                query = query.Where(n =>
                    n.Topic == topic);
            }

            // Count after applying filters
            var totalRecords = await query.CountAsync();

            // Sort and paginate
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalRecords);
        }

        public async Task<Note?> GetByIdAndUserIdAsync(int id, int userId)
        {
            return await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        }

        public Task UpdateAsync(Note note)
        {
            _context.Notes.Update(note);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Note note)
        {
            _context.Notes.Remove(note);

            return Task.CompletedTask;
        }
    }
}
