using Finbridge.Domain.Common;

namespace Finbridge.Domain.Users.Exceptions;

public sealed class UserNotFoundException : DomainException
{
    public int UserId { get; }

    public UserNotFoundException(int userId)
        : base($"Пользователь с id {userId} не найден.")
    {
        UserId = userId;
    }
}
