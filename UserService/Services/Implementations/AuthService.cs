using UserService.Common.Exceptions;
using UserService.Domain.Entities;
using UserService.DTO;
using UserService.Repositories.Interfaces;
using UserService.Services.Interfaces;

namespace UserService.Services.Implementations
{
    public class AuthService(IUserRepository userRepository,
        ITokenService tokenService,
        ICurrentUserService currentUserService) : IAuthService
    {
        public async Task<string> LoginAsync(LoginRequestDTO loginRequest)
        {
            var user = await userRepository.GetByUsernameAsync(loginRequest.Username);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            var isValidPassword = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash);

            if (!isValidPassword)
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            var token = tokenService.GenerateToken(user);
            return token;
        }

        public async Task RegisterAsync(RegisterRequestDTO registerRequest)
        {
            var userExists = await userRepository
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

            await userRepository.AddAsync(user);
        }

        public async Task Delete()
        {
            // get user from token
            var userId = currentUserService.UserId;

            // based on role do validations and deletions on other services
            // var role = currentUserService.Role;

            if (userId == null)
            {
                throw new UnauthorizedAccessException();
            }
            var user = await userRepository
                .GetByIdAsync(userId.Value);

            if (user == null)
            {
                throw new NotFoundException("User with given username or email already exists.");
            }

            await userRepository.DeleteAsync(user);
        }
    }
}
