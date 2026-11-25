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
            await _context.SaveChangesAsync();

            return note;
        }

        public async Task<List<Note>> GetByUserIdAsync(int userId)
        {
            return await _context.Notes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Note?> GetByIdAndUserIdAsync(int id, int userId)
        {
            return await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        }

        public async Task UpdateAsync(Note note)
        {
            _context.Notes.Update(note);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Note note)
        {
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
        }
    }
}