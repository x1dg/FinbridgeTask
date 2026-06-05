using Finbridge.Application.Contracts;
using Finbridge.Application.Events;
using Finbridge.Domain.Common;
using Finbridge.Domain.Users;
using Finbridge.Domain.Users.Repositories;
using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UserService(IUserRepository userRepository, IDomainEventDispatcher eventDispatcher)
    {
        _userRepository = userRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = User.NewUser(
            FullName.Of(request.FullName),
            request.DateOfBirth,
            request.PlaceOfBirth);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        await DispatchDomainEventsAsync(user, cancellationToken);

        return user.ToResponse();
    }

    public async Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user?.ToResponse();
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.ToResponses();
    }

    private async Task DispatchDomainEventsAsync(User user, CancellationToken cancellationToken)
    {
        foreach (var @event in user.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(@event, cancellationToken);
        }
        user.ClearDomainEvents();
    }
}
