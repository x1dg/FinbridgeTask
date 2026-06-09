using Finbridge.Application.Abstractions;
using Finbridge.Application.Configuration;
using Finbridge.Application.Contracts;
using Finbridge.Domain.Users;
using Finbridge.Domain.Users.Repositories;
using Finbridge.Domain.Users.ValueObjects;
using Microsoft.Extensions.Options;

namespace Finbridge.Application.Users.Commands;

public sealed class BatchUpdateBalancesHandler : IRequestHandler<BatchUpdateBalancesRequest, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly Money _maxBalance;

    public BatchUpdateBalancesHandler(IUserRepository userRepository, IOptions<BalanceSettings> settings)
    {
        _userRepository = userRepository;
        _maxBalance = Money.Of(settings.Value.MaxBalance);
    }

    public async Task<Unit> HandleAsync(BatchUpdateBalancesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTime.UtcNow;

        foreach (var update in request.Updates)
        {
            var user = await _userRepository.GetByIdAsync(update.UserId, cancellationToken)
                ?? throw new Domain.Users.Exceptions.UserNotFoundException(update.UserId);

            var delta = Money.DeltaOf(update.Amount);
            user.UpdateBalance(delta, _maxBalance, now);
        }

        await _userRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
