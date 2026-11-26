
using Token2Core.Data;

namespace WritePad.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly TokenDbContext _context;

        public INotesRepository Notes { get; }

        public UnitOfWork(
            TokenDbContext context,
            INotesRepository notesRepository)
        {
            _context = context;
            Notes = notesRepository;
        }

        public async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
