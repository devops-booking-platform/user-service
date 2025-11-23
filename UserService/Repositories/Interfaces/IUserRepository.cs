using UserService.Domain.Entities;

namespace UserService.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ExistsByUsernameOrEmailAsync(string username, string email);
}
