namespace UserService.Infrastructure.Clients
{
	public interface IReservationClient
	{
		Task<bool> GetGuestDeletionEligibilityAsync(Guid guestId, CancellationToken ct);
		Task<bool> GetHostDeletionEligibilityAsync(Guid hostId, CancellationToken ct);
	}
}
