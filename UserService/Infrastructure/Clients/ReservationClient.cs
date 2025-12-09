using System.Net;
using UserService.Common.Exceptions;

namespace UserService.Infrastructure.Clients
{
	public sealed class ReservationClient(HttpClient http) : IReservationClient
	{
		public Task<bool> GetGuestDeletionEligibilityAsync(Guid guestId, CancellationToken ct)
		   => GetEligibilityAsync($"/api/reservations/internal/deletion-eligibility/guest/{guestId}", ct);

		public Task<bool> GetHostDeletionEligibilityAsync(Guid hostId, CancellationToken ct)
			=> GetEligibilityAsync($"/api/reservations/internal/deletion-eligibility/host/{hostId}", ct);

		private async Task<bool> GetEligibilityAsync(string url, CancellationToken ct)
		{
			HttpResponseMessage res;

			try
			{
				res = await http.GetAsync(url, ct);
			}
			catch (HttpRequestException ex)
			{
				throw new ExternalServiceException("ReservationService is unreachable.", ex);
			}
			catch (TaskCanceledException ex)
			{
				throw new ExternalServiceException("ReservationService request timed out.", ex);
			}

			using (res)
			{
				if (!res.IsSuccessStatusCode)
				{
					if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
						throw new ExternalServiceException("ReservationService internal authorization failed.");

					throw new ExternalServiceException($"ReservationService returned {(int)res.StatusCode}.");
				}

				var eligible = await res.Content.ReadFromJsonAsync<bool?>(cancellationToken: ct);
				if (eligible is null)
					throw new ExternalServiceException("ReservationService returned invalid response.");

				return eligible.Value;
			}
		}
	}
}
