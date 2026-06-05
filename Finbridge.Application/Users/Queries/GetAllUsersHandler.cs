using Finbridge.Application.Abstractions;
using Finbridge.Application.Contracts;
using Finbridge.Domain.Users.Repositories;

namespace Finbridge.Application.Users.Queries;

public sealed class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, IReadOnlyList<UserResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetAllUsersHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<UserResponse>> HandleAsync(GetAllUsersQuery request, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.ToResponses();
    }
}
