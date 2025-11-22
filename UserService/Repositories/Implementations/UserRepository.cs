using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Domain.Entities;
using UserService.Repositories.Interfaces;

namespace UserService.Repositories.Implementations
{
    public class UserRepository(ApplicationDbContext context) : IUserRepository
    {
        public async Task AddAsync(User user)
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        public Task<bool> ExistsByUsernameOrEmailAsync(string username, string email)
        {
            return context.Users.AnyAsync(u => u.Username == username || u.Email == email);
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            return context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public Task<User?> GetByUsernameAsync(string username)
        {
            return context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }
        
        public async Task DeleteAsync(User user)
        {
            context.Users.Remove(user);
            // TODO move saving out of repository and create an unit of work to save all entities async
            await context.SaveChangesAsync();
        }
    }
}
