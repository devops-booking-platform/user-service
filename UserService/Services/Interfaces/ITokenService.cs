using UserService.Domain.Entities;

namespace UserService.Services.Interfaces
{
    public interface ITokenService
    {
        public string GenerateToken(User user);
    }
}
