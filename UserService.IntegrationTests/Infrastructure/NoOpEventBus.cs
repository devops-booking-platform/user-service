using UserService.Common.Events;

namespace UserService.IntegrationTests.Infrastructure
{
    public sealed class NoOpEventBus : IEventBus
    {
        public Task PublishAsync<T>(T @event, CancellationToken ct = default)
            where T : IIntegrationEvent
            => Task.CompletedTask;
    }
}
