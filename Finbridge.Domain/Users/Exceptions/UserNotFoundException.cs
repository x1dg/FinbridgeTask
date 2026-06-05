using Finbridge.Domain.Common;

namespace Finbridge.Domain.Users.Exceptions;

public sealed class UserNotFoundException : DomainException
{
    public int UserId { get; }

    public UserNotFoundException(int userId)
        : base($"User with id {userId} was not found.")
    {
        UserId = userId;
    }
}
