using Finbridge.Domain.Common;

namespace Finbridge.Domain.Users.Exceptions;

/// <summary>
/// Бросается, когда сохранение агрегата в БД провалилось из-за
/// конфликта оптимистичной блокировки. Прикладной слой решает, ретраить ли.
/// </summary>
public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException()
        : base("A concurrency conflict occurred while saving the aggregate.") { }

    public ConcurrencyConflictException(Exception inner)
        : base("A concurrency conflict occurred while saving the aggregate.", inner) { }
}
