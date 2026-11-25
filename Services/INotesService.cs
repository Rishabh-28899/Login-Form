
using WritePad.Model;
using Writepad.Model;

namespace WritePad.Services
{
    public interface INotesService
    {
        Task<Note> CreateNoteAsync(
            CreateNoteRequest request,
            int userId);

        Task<List<Note>> GetMyNotesAsync(int userId);

        Task<Note?> GetNoteAsync(int id, int userId);

        Task<bool> UpdateNoteAsync(
            int id,
            UpdateNoteRequest request,
            int userId);

        Task<bool> DeleteNoteAsync(int id, int userId);
    }
}


