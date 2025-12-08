using UserService.Common.Constants;
using UserService.Common.Events;
using UserService.Common.Exceptions;
using UserService.Domain.Enums;
using UserService.DTO;
using UserService.Infrastructure.Clients;
using UserService.Repositories.Interfaces;
using UserService.Services.Interfaces;
using User = UserService.Domain.Entities.User;

namespace UserService.Services.Implementations
{
	public class AuthService(IUserRepository userRepository,
		ITokenService tokenService,
		ICurrentUserService currentUserService,
		IUnitOfWork unitOfWork,
		IReservationClient reservationClient,
		IEventBus eventBus) : IAuthService
	{
		private async Task<User> GetCurrentUserOrThrowAsync()
		{
			var userId = currentUserService.UserId;
			if (userId == null)
				throw new UnauthorizedAccessException();

			var user = await userRepository.GetByIdAsync(userId.Value);
			if (user == null)
				throw new NotFoundException(ExceptionMessages.User.UserNotFound);

			return user;
		}

		public async Task<string> LoginAsync(LoginRequestDTO loginRequest)
		{
			var user = await userRepository.GetByUsernameAsync(loginRequest.Username);

			if (user == null)
			{
				throw new UnauthorizedAccessException(ExceptionMessages.Auth.InvalidCredentials);
			}

			var isValidPassword = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash);

			if (!isValidPassword)
			{
				throw new UnauthorizedAccessException(ExceptionMessages.Auth.InvalidCredentials);
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
				throw new ConflictException(ExceptionMessages.Auth.UserAlreadyExists);
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
			await unitOfWork.SaveChangesAsync();
		}

		public async Task Delete(CancellationToken ct = default)
		{
			var user = await GetCurrentUserOrThrowAsync();
			bool eligible = user.Role == UserRole.Guest
		? await reservationClient.GetGuestDeletionEligibilityAsync(user.Id, ct)
		: await reservationClient.GetHostDeletionEligibilityAsync(user.Id, ct);

			if (!eligible)
				throw new ConflictException("Account cannot be deleted because you have active or upcoming reservations.");
			userRepository.Remove(user);
			await unitOfWork.SaveChangesAsync();

			var @event = new UserDeletedIntegrationEvent(user.Id, user.Role.ToString());
			await eventBus.PublishAsync(@event);
		}

		public async Task<UserProfileResponseDTO> GetProfileAsync()
		{
			var user = await GetCurrentUserOrThrowAsync();
			return new UserProfileResponseDTO
			{
				Username = user.Username,
				Email = user.Email,
				FirstName = user.FirstName,
				LastName = user.LastName,
				Address = user.Address,
				Role = user.Role.ToString()
			};
		}

		public async Task<UserProfileResponseDTO> UpdateProfileAsync(UpdateProfileRequestDTO updateRequest)
		{
			var user = await GetCurrentUserOrThrowAsync();

			if (user.Username != updateRequest.Username && await userRepository.ExistsWithUsernameAsync(updateRequest.Username, user.Id))
			{
				throw new ConflictException(ExceptionMessages.User.UsernameTaken);
			}
			if (user.Email != updateRequest.Email && await userRepository.ExistsWithEmailAsync(updateRequest.Email, user.Id))
			{
				throw new ConflictException(ExceptionMessages.User.EmailTaken);
			}

			user.FirstName = updateRequest.FirstName;
			user.Address = updateRequest.Address;
			user.Username = updateRequest.Username;
			user.Email = updateRequest.Email;
			user.LastName = updateRequest.LastName;
			await unitOfWork.SaveChangesAsync();

			return new UserProfileResponseDTO
			{
				Username = user.Username,
				Email = user.Email,
				FirstName = user.FirstName,
				LastName = user.LastName,
				Address = user.Address,
				Role = user.Role.ToString()
			};
		}

		public async Task UpdatePasswordAsync(UpdatePasswordRequestDTO updatePasswordRequest)
		{
			var user = await GetCurrentUserOrThrowAsync();

			var isValidPassword = BCrypt.Net.BCrypt.Verify(updatePasswordRequest.CurrentPassword, user.PasswordHash);

			if (!isValidPassword)
			{
				throw new UnauthorizedAccessException(ExceptionMessages.Auth.InvalidCurrentPassword);
			}
			var passwordHash = BCrypt.Net.BCrypt.HashPassword(updatePasswordRequest.NewPassword);
			user.PasswordHash = passwordHash;
			await unitOfWork.SaveChangesAsync();
		}
	}
}
