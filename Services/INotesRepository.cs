using Writepad.Model;
using WritePad.Model;

namespace WritePad.Services
{
    public interface INotesRepository
    {
        Task<Note> CreateAsync(Note note);

        Task<List<Note>> GetByUserIdAsync(int userId);

        Task<Note?> GetByIdAndUserIdAsync(int id, int userId);

        Task UpdateAsync(Note note);

        Task DeleteAsync(Note note);
    }
}