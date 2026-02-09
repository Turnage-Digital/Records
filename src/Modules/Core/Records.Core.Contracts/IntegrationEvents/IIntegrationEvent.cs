using MediatR;

namespace Records.Core.Contracts.IntegrationEvents;

public interface IIntegrationEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
    string EventType { get; }
}
