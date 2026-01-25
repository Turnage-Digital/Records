using MediatR;

namespace Records.Core.Domain;

public interface IHasDomainEvents
{
    IReadOnlyCollection<INotification> DomainEvents { get; }

    void ClearDomainEvents();
}