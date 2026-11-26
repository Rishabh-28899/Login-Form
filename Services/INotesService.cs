
using WritePad.Model;
using Writepad.Model;
using WritePad.DTOs;

using WritePad.Model;
using Writepad.Model;
using WritePad.DTOs;

namespace WritePad.Services
{
    public interface INotesService
    {
        Task<Note> CreateNoteAsync(
            CreateNoteRequest request,
            int userId,
            CancellationToken cancellationToken = default);

        Task<List<Note>> GetMyNotesAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<PagedResponse<NoteResponseDto>> GetMyNotesPagedAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<PagedResponse<NoteResponseDto>> SearchMyNotesPagedAsync(
            int userId,
            string? searchTerm,
            string? topic,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<Note?> GetNoteAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateNoteAsync(
            int id,
            UpdateNoteRequest request,
            int userId,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteNoteAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default);
    }
}
