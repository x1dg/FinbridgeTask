using Finbridge.Application.Contracts;
using Finbridge.Domain.Users;
using Finbridge.Domain.Users.Exceptions;
using Finbridge.Domain.Users.Repositories;
using Finbridge.Domain.Users.ValueObjects;
using Microsoft.Extensions.Options;

namespace Finbridge.Application.Services;

public sealed class BalanceService : IBalanceService
{
    private const int MaxRetryAttempts = 3;

    private readonly IUserRepository _userRepository;
    private readonly Money _maxBalance;

    public BalanceService(
        IUserRepository userRepository,
        IOptions<BalanceSettings> settings)
    {
        _userRepository = userRepository;
        _maxBalance = Money.Of(settings.Value.MaxBalance);
    }

    public async Task<UserResponse> UpdateBalanceAsync(UpdateBalanceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        for (var attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new UserNotFoundException(request.UserId);

            var delta = Money.DeltaOf(request.Amount);
            user.UpdateBalance(delta, _maxBalance, DateTime.UtcNow);

            try
            {
                await _userRepository.SaveChangesAsync(cancellationToken);
                return user.ToResponse();
            }
            catch (ConcurrencyConflictException) when (attempt < MaxRetryAttempts - 1)
            {
                continue;
            }
        }

        throw new ConcurrencyConflictException();
    }

    public async Task UpdateBalancesAsync(BatchUpdateBalancesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        foreach (var update in request.Updates)
        {
            await UpdateBalanceAsync(update, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<BalanceHistoryResponse>> GetBalanceHistoryAsync(
        int userId, int limit = 20, CancellationToken cancellationToken = default)
    {
        var history = await _userRepository.GetBalanceHistoryAsync(userId, limit, cancellationToken);
        return history.ToResponses();
    }
}
