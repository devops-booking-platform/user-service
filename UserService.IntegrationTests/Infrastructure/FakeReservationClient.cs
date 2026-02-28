using UserService.Infrastructure.Clients;

namespace UserService.IntegrationTests.Infrastructure
{
	public sealed class FakeReservationClient : IReservationClient
	{
		private readonly bool _eligible;

		public FakeReservationClient(bool eligible = true)
			=> _eligible = eligible;

		public Task<bool> GetGuestDeletionEligibilityAsync(Guid guestId, CancellationToken ct)
			=> Task.FromResult(_eligible);

		public Task<bool> GetHostDeletionEligibilityAsync(Guid hostId, CancellationToken ct)
			=> Task.FromResult(_eligible);
	}
}
