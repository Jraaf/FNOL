using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Common;
using MediatR;

namespace ClaimsModule.Infrastructure.Common;

public class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;
    public MediatRDomainEventDispatcher(IPublisher publisher) => _publisher = publisher;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in events)
        {
            var notification = new DomainEventNotification(domainEvent);
            await _publisher.Publish(notification, cancellationToken);
        }
    }
}

public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
