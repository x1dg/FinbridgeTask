using Finbridge.Application.Contracts;

namespace Finbridge.Application.Services;

public interface IBalanceService
{
    Task<UserResponse> UpdateBalanceAsync(UpdateBalanceRequest request, CancellationToken cancellationToken = default);

    Task UpdateBalancesAsync(BatchUpdateBalancesRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BalanceHistoryResponse>> GetBalanceHistoryAsync(int userId, int limit = 20, CancellationToken cancellationToken = default);
}
