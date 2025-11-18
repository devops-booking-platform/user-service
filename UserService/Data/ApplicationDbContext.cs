using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;

namespace UserService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<User> Users {get; set;}
    }
}
