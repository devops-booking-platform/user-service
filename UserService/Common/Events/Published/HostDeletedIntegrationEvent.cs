namespace UserService.Common.Events.Published
{
    public record HostDeletedIntegrationEvent(Guid UserId) : IIntegrationEvent;
}
