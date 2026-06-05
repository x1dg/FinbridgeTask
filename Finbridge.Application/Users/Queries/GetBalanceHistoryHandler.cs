using Finbridge.Application.Abstractions;
using Finbridge.Application.Contracts;
using Finbridge.Domain.Users.Repositories;

namespace Finbridge.Application.Users.Queries;

public sealed class GetBalanceHistoryHandler : IRequestHandler<GetBalanceHistoryQuery, IReadOnlyList<BalanceHistoryResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetBalanceHistoryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<BalanceHistoryResponse>> HandleAsync(GetBalanceHistoryQuery request, CancellationToken cancellationToken = default)
    {
        var history = await _userRepository.GetBalanceHistoryAsync(request.UserId, request.Limit, cancellationToken);
        return history.ToResponses();
    }
}
