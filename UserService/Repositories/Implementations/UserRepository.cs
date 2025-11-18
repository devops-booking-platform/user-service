using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Domain.Entities;
using UserService.Repositories.Interfaces;

namespace UserService.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public Task<bool> ExistsByUsernameOrEmailAsync(string username, string email)
        {
            return _context.Users.AnyAsync(u => u.Username == username || u.Email == email);
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public Task<User?> GetByUsernameAsync(string username)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}
