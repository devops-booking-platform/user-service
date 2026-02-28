using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Domain.Entities;
using UserService.Repositories.Interfaces;

namespace UserService.Repositories.Implementations;

public class UserRepository(ApplicationDbContext context) : Repository<User>(context), IUserRepository
{
    public Task<User?> GetByUsernameAsync(string username) =>
        Context.Users.FirstOrDefaultAsync(u => u.Username == username);

    public Task<bool> ExistsByUsernameOrEmailAsync(string username, string email) =>
        Context.Users.AnyAsync(u => u.Username == username || u.Email == email);

    public Task<bool> ExistsWithUsernameAsync(string username, Guid userId) =>
        Context.Users.AnyAsync(u => u.Username == username && u.Id != userId);
    
    public Task<bool> ExistsWithEmailAsync(string email, Guid userId) =>
        Context.Users.AnyAsync(u => u.Email == email && u.Id != userId);
}