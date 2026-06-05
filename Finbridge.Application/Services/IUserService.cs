using Finbridge.Application.Contracts;

namespace Finbridge.Application.Services;

public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
