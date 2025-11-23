using UserService.Data;
using UserService.Repositories.Interfaces;

namespace UserService.Repositories.Implementations;

public class UnitOfWork(
    ApplicationDbContext context,
    IUserRepository userRepository)
    : IUnitOfWork
{
    public IUserRepository Users { get; } = userRepository;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}