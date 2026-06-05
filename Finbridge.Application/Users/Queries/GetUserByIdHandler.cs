using Finbridge.Application.Abstractions;
using Finbridge.Application.Contracts;
using Finbridge.Domain.Users.Repositories;

namespace Finbridge.Application.Users.Queries;

public sealed class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserResponse?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponse?> HandleAsync(GetUserByIdQuery request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        return user?.ToResponse();
    }
}
