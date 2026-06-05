using Finbridge.Application.Abstractions;
using Finbridge.Application.Configuration;
using Finbridge.Application.Contracts;
using Finbridge.Domain.Users;
using Finbridge.Domain.Users.Exceptions;
using Finbridge.Domain.Users.Repositories;
using Finbridge.Domain.Users.ValueObjects;
using Microsoft.Extensions.Options;

namespace Finbridge.Application.Users.Commands;

public sealed class UpdateBalanceHandler : IRequestHandler<UpdateBalanceRequest, UserResponse>
{
    private const int MaxRetryAttempts = 3;

    private readonly IUserRepository _userRepository;
    private readonly Money _maxBalance;

    public UpdateBalanceHandler(IUserRepository userRepository, IOptions<BalanceSettings> settings)
    {
        _userRepository = userRepository;
        _maxBalance = Money.Of(settings.Value.MaxBalance);
    }

    public async Task<UserResponse> HandleAsync(UpdateBalanceRequest request, CancellationToken cancellationToken = default)
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
}
