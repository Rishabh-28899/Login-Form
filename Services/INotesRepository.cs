
using Writepad.Model;
using WritePad.Model;

namespace WritePad.Services
{
    public interface INotesRepository
    {
        Task<Note> CreateAsync(
            Note note,
            CancellationToken cancellationToken = default);

        Task<List<Note>> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<(List<Note> Items, int TotalRecords)> GetPagedByUserIdAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<(List<Note> Items, int TotalRecords)> SearchPagedByUserIdAsync(
            int userId,
            string? searchTerm,
            string? topic,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<Note?> GetByIdAndUserIdAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Note note,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Note note,
            CancellationToken cancellationToken = default);
    }
}
