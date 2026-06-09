using Finbridge.Application.Abstractions;
using Finbridge.Application.Contracts;
using Finbridge.Domain.Users;
using Finbridge.Domain.Users.Repositories;
using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Application.Users.Commands;

public sealed class CreateUserHandler : IRequestHandler<CreateUserRequest, UserResponse>
{
    private readonly IUserRepository _userRepository;

    public CreateUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponse> HandleAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var dob = request.DateOfBirth.Kind == DateTimeKind.Utc
            ? request.DateOfBirth
            : request.DateOfBirth.ToUniversalTime();

        var user = User.NewUser(
            FullName.Of(request.FullName),
            dob,
            request.PlaceOfBirth);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return user.ToResponse();
    }
}
