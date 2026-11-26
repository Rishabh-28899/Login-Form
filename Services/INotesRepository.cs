
using Writepad.Model;
using WritePad.Model;

namespace WritePad.Services
{
    public interface INotesRepository
    {
        Task<Note> CreateAsync(Note note);

        Task<List<Note>> GetByUserIdAsync(int userId);

        Task<(List<Note> Items, int TotalRecords)> GetPagedByUserIdAsync(
            int userId,
            int pageNumber,
            int pageSize);

        Task<(List<Note> Items, int TotalRecords)> SearchPagedByUserIdAsync(
            int userId,
            string? searchTerm,
            string? topic,
            int pageNumber,
            int pageSize);

        Task<Note?> GetByIdAndUserIdAsync(int id, int userId);

        Task UpdateAsync(Note note);

        Task DeleteAsync(Note note);
    }
}


