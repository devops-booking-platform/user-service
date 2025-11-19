using Moq;
using UserService.Common.Exceptions;
using UserService.Domain.Entities;
using UserService.DTO;
using UserService.Repositories.Interfaces;
using UserService.Services.Implementations;
using UserService.Services.Interfaces;

namespace UserService.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _tokenServiceMock = new Mock<ITokenService>();
            _sut = new AuthService(_userRepositoryMock.Object, _tokenServiceMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_Should_ReturnError_WhenEmailOrUsernameExists()
        {
            var dto = new RegisterRequestDTO
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "password",
                FirstName = "Test",
                LastName = "Test",
                Address = "Address",
                Role = Domain.Enums.UserRole.Host
            };

            _userRepositoryMock.Setup(r => r.ExistsByUsernameOrEmailAsync(dto.Username, dto.Email)).ReturnsAsync(true);
            var act = () => _sut.RegisterAsync(dto);
            var ex = await Assert.ThrowsAsync<ConflictException>(act);
            Assert.Equal("User with given username or email already exists.", ex.Message);
            _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_Should_CreateUser_WhenUserDoesNotExist()
        {
            var dto = new RegisterRequestDTO
            {
                Username = "testuser123",
                Email = "test@example.com123",
                Password = "password",
                FirstName = "Test",
                LastName = "Test",
                Address = "Address",
                Role = Domain.Enums.UserRole.Host
            };
            _userRepositoryMock.Setup(r => r.ExistsByUsernameOrEmailAsync(dto.Username, dto.Email)).ReturnsAsync(false);

            await _sut.RegisterAsync(dto);
            _userRepositoryMock.Verify(
               r => r.AddAsync(It.Is<User>(u =>
                   u.Username == dto.Username &&
                   u.Email == dto.Email &&
                   u.FirstName == dto.FirstName &&
                   u.LastName == dto.LastName &&
                   u.Address == dto.Address &&
                   u.Role == dto.Role &&
                   !string.IsNullOrEmpty(u.PasswordHash)
               )),
               Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_ThrowUnauthorized_WhenUserDoesNotExist()
        {
            var dto = new LoginRequestDTO
            {
                Username = "unknownUser",
                Password = "somePassword"
            };

            _userRepositoryMock
                .Setup(r => r.GetByUsernameAsync(dto.Username))
                .ReturnsAsync((User?)null);

            var act = () => _sut.LoginAsync(dto);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
            _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_Should_ThrowUnauthorized_WhenPasswordIsInvalid()
        {
            var dto = new LoginRequestDTO
            {
                Username = "testuser",
                Password = "wrongPassword"
            };

            var correctPassword = "correctPassword";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Address = "Address",
                Role = Domain.Enums.UserRole.Host
            };

            _userRepositoryMock
                .Setup(r => r.GetByUsernameAsync(dto.Username))
                .ReturnsAsync(user);

            var act = () => _sut.LoginAsync(dto);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
            _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_Should_ReturnToken_WhenCredentialsAreValid()
        {
            var dto = new LoginRequestDTO
            {
                Username = "testuser",
                Password = "correctPassword"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Address = "Address",
                Role = Domain.Enums.UserRole.Host
            };

            _userRepositoryMock
                .Setup(r => r.GetByUsernameAsync(dto.Username))
                .ReturnsAsync(user);

            var expectedToken = "jwt-token-value";

            _tokenServiceMock
                .Setup(t => t.GenerateToken(user))
                .Returns(expectedToken);

            var result = await _sut.LoginAsync(dto);

            Assert.Equal(expectedToken, result);
            _tokenServiceMock.Verify(t => t.GenerateToken(user), Times.Once);
        }
    }
}
