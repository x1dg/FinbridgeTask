namespace Finbridge.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
