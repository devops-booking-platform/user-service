namespace UserService.Common.Events
{
    public interface IIntegrationEvent { }
    public record UserDeletedIntegrationEvent(Guid UserId, string Role) : IIntegrationEvent;
}
