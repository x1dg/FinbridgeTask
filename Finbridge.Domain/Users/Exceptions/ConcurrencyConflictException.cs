using Finbridge.Domain.Common;

namespace Finbridge.Domain.Users.Exceptions;

public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException()
        : base("Конфликт оптимистичной блокировки при сохранении агрегата.")
    {
    }

    public ConcurrencyConflictException(Exception innerException)
        : base("Конфликт оптимистичной блокировки при сохранении агрегата.", innerException)
    {
    }
}
