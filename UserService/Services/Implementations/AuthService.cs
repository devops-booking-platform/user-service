using UserService.Common.Exceptions;
using UserService.Domain.Entities;
using UserService.DTO;
using UserService.Repositories.Interfaces;
using UserService.Services.Interfaces;

namespace UserService.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task RegisterAsync(RegisterRequestDTO registerRequest)
        {
            var userExists = await _userRepository
                .ExistsByUsernameOrEmailAsync(registerRequest.Username, registerRequest.Email);

            if (userExists)
            {
                throw new ConflictException("User with given username or email already exists.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = registerRequest.Username,
                PasswordHash = passwordHash,
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                Email = registerRequest.Email,
                Address = registerRequest.Address,
                Role = registerRequest.Role
            };

            await _userRepository.AddAsync(user);
        }
    }
}
