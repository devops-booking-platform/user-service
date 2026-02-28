namespace UserService.Common.Events.Published
{
    public record GuestDeletedIntegrationEvent(Guid UserId) : IIntegrationEvent;
}
