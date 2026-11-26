
using WritePad.Model;
using Writepad.Model;
using WritePad.DTOs;

namespace WritePad.Services
{
    public interface INotesService
    {
        Task<Note> CreateNoteAsync(
            CreateNoteRequest request,
            int userId);

        Task<List<Note>> GetMyNotesAsync(int userId);

        Task<PagedResponse<NoteResponseDto>> GetMyNotesPagedAsync(
            int userId,
            int pageNumber,
            int pageSize);

        Task<PagedResponse<NoteResponseDto>> SearchMyNotesPagedAsync(
            int userId,
            string? searchTerm,
            string? topic,
            int pageNumber,
            int pageSize);

        Task<Note?> GetNoteAsync(
            int id,
            int userId);

        Task<bool> UpdateNoteAsync(
            int id,
            UpdateNoteRequest request,
            int userId);

        Task<bool> DeleteNoteAsync(
            int id,
            int userId);
    }
}
