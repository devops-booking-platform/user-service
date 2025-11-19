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
        private readonly ITokenService _tokenService;
        public AuthService(IUserRepository userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<string> LoginAsync(LoginRequestDTO loginRequest)
        {
            var user = await _userRepository.GetByUsernameAsync(loginRequest.Username);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            var isValidPassword = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash);

            if (!isValidPassword)
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            var token = _tokenService.GenerateToken(user);
            return token;
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
