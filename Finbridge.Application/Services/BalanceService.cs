using Finbridge.Application.Contracts;
using Finbridge.Domain.Common;
using Finbridge.Domain.Users;
using Finbridge.Domain.Users.Exceptions;
using Finbridge.Domain.Users.Repositories;
using Finbridge.Domain.Users.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finbridge.Application.Services;

public sealed class BalanceService : IBalanceService
{
    private const int MaxRetryAttempts = 3;

    private readonly IUserRepository _userRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly Money _maxBalance;
    private readonly ILogger<BalanceService> _logger;

    public BalanceService(
        IUserRepository userRepository,
        IDomainEventDispatcher eventDispatcher,
        IOptions<BalanceSettings> settings,
        ILogger<BalanceService> logger)
    {
        _userRepository = userRepository;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
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
            }
            catch (ConcurrencyConflictException)
            {
                if (attempt == MaxRetryAttempts - 1)
                {
                    throw new ConcurrencyConflictException();
                }

                _logger.LogWarning(
                    "Конфликт оптимистичной блокировки при обновлении баланса пользователя {UserId} (попытка {Attempt}/{Max}), повтор.",
                    request.UserId, attempt + 1, MaxRetryAttempts);
                continue;
            }

            await DispatchDomainEventsAsync(user, cancellationToken);
            return user.ToResponse();
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

    private async Task DispatchDomainEventsAsync(User user, CancellationToken cancellationToken)
    {
        foreach (var @event in user.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(@event, cancellationToken);
        }
        user.ClearDomainEvents();
    }
}
