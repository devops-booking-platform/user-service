using Moq;
using UserService.Common.Constants;
using UserService.Common.Events;
using UserService.Common.Exceptions;
using UserService.Domain.Entities;
using UserService.DTO;
using UserService.Infrastructure.Clients;
using UserService.Repositories.Interfaces;
using UserService.Services.Implementations;
using UserService.Services.Interfaces;

namespace UserService.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IReservationClient> _reservationClientMock;
        private readonly Mock<IEventBus> _eventBus;

        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _tokenServiceMock = new Mock<ITokenService>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
			_reservationClientMock = new Mock<IReservationClient>();
            var unitOfWork = new Mock<IUnitOfWork>();
            _eventBus = new Mock<IEventBus>();

            _sut = new AuthService(
                _userRepositoryMock.Object,
                _tokenServiceMock.Object,
                _currentUserServiceMock.Object,
                unitOfWork.Object,
                _reservationClientMock.Object,
                _eventBus.Object

            );
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
            Assert.Equal(ExceptionMessages.Auth.UserAlreadyExists, ex.Message);
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
        
        [Fact]
        public async Task Delete_Should_ThrowUnauthorized_WhenUserIdIsNull()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var act = () => _sut.DeleteAsync(CancellationToken.None);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
            _userRepositoryMock.Verify(r => r.Remove(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_ThrowNotFound_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            var act = () => _sut.DeleteAsync(CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundException>(act);
            _userRepositoryMock.Verify(r => r.Remove(It.IsAny<User>()), Times.Never);
        }

		[Fact]
		public async Task Delete_Should_DeleteUser_WhenUserExists()
		{
			var userId = Guid.NewGuid();

			var user = new User
			{
				Id = userId,
				Username = "tester",
				PasswordHash = "hash",
				Role = Domain.Enums.UserRole.Guest
			};

			_currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

			_userRepositoryMock
				.Setup(r => r.GetByIdAsync(userId))
				.ReturnsAsync(user);

			_reservationClientMock
				.Setup(x => x.GetGuestDeletionEligibilityAsync(userId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);

			await _sut.DeleteAsync(CancellationToken.None);

			_userRepositoryMock.Verify(r => r.Remove(user), Times.Once);
		}

		[Fact]
        public async Task UpdateProfileAsync_Should_ThrowUnauthorized_WhenUserIdIsNull()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var dto = new UpdateProfileRequestDTO
            {
                Username = "newUsername",
                Email = "new@example.com",
                FirstName = "New",
                LastName = "User",
                Address = "New address"
            };

            var act = () => _sut.UpdateProfileAsync(dto);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
        }

        [Fact]
        public async Task UpdateProfileAsync_Should_ThrowNotFound_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            var dto = new UpdateProfileRequestDTO
            {
                Username = "newUsername",
                Email = "new@example.com",
                FirstName = "New",
                LastName = "User",
                Address = "New address"
            };

            var act = () => _sut.UpdateProfileAsync(dto);

            await Assert.ThrowsAsync<NotFoundException>(act);
        }

        [Fact]
        public async Task UpdateProfileAsync_Should_ThrowConflict_WhenUsernameAlreadyTaken()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "oldUsername",
                Email = "old@example.com",
                FirstName = "Old",
                LastName = "User",
                Address = "Old address",
                Role = Domain.Enums.UserRole.Host
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new UpdateProfileRequestDTO
            {
                Username = "newUsername", 
                Email = "old@example.com", 
                FirstName = "New",
                LastName = "User",
                Address = "New address"
            };

            _userRepositoryMock
                .Setup(r => r.ExistsWithUsernameAsync(dto.Username, userId))
                .ReturnsAsync(true);

            var act = () => _sut.UpdateProfileAsync(dto);

            var ex = await Assert.ThrowsAsync<ConflictException>(act);
            Assert.Equal(ExceptionMessages.User.UsernameTaken, ex.Message);
        }

        [Fact]
        public async Task UpdateProfileAsync_Should_ThrowConflict_WhenEmailAlreadyTaken()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "oldUsername",
                Email = "old@example.com",
                FirstName = "Old",
                LastName = "User",
                Address = "Old address",
                Role = Domain.Enums.UserRole.Host
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new UpdateProfileRequestDTO
            {
                Username = "oldUsername",
                Email = "new@example.com",
                FirstName = "New",
                LastName = "User",
                Address = "New address"
            };

            _userRepositoryMock
                .Setup(r => r.ExistsWithEmailAsync(dto.Email, userId))
                .ReturnsAsync(true);

            var act = () => _sut.UpdateProfileAsync(dto);

            var ex = await Assert.ThrowsAsync<ConflictException>(act);
            Assert.Equal(ExceptionMessages.User.EmailTaken, ex.Message);
        }

        [Fact]
        public async Task UpdateProfileAsync_Should_UpdateUserAndReturnProfile_WhenDataIsValid()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "oldUsername",
                Email = "old@example.com",
                FirstName = "Old",
                LastName = "User",
                Address = "Old address",
                Role = Domain.Enums.UserRole.Host
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new UpdateProfileRequestDTO
            {
                Username = "newUsername",
                Email = "new@example.com",
                FirstName = "New",
                LastName = "User",
                Address = "New address"
            };

            _userRepositoryMock
                .Setup(r => r.ExistsWithUsernameAsync(dto.Username, userId))
                .ReturnsAsync(false);

            _userRepositoryMock
                .Setup(r => r.ExistsWithEmailAsync(dto.Email, userId))
                .ReturnsAsync(false);

            var result = await _sut.UpdateProfileAsync(dto);

            Assert.Equal(dto.Username, user.Username);
            Assert.Equal(dto.Email, user.Email);
            Assert.Equal(dto.FirstName, user.FirstName);
            Assert.Equal(dto.LastName, user.LastName);
            Assert.Equal(dto.Address, user.Address);

            Assert.Equal(dto.Username, result.Username);
            Assert.Equal(dto.Email, result.Email);
            Assert.Equal(dto.FirstName, result.FirstName);
            Assert.Equal(dto.LastName, result.LastName);
            Assert.Equal(dto.Address, result.Address);
            Assert.Equal(user.Role.ToString(), result.Role);
        }

    }
}
