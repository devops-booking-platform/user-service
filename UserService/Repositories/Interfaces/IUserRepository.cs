
using UserService.Domain.Entities;

namespace UserService.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistsByUsernameOrEmailAsync(string username, string email);
        Task AddAsync(User user);
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByUsernameAsync(string username);
        Task DeleteAsync(User user);
    }
}
