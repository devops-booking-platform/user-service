namespace UserService.Common.Events
{
    public interface IIntegrationEvent { }
    public record UserDeletedIntegrationEvent(Guid UserId, string Role) : IIntegrationEvent;
    public record ReservationCreatedIntegrationEvent(Guid UserId, string Role, string Check) : IIntegrationEvent;
}
