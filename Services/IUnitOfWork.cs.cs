
namespace WritePad.Services
{
    public interface IUnitOfWork
    {
        INotesRepository Notes { get; }

        Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
