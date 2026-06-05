using Finbridge.Application.Abstractions;
using Finbridge.Application.Contracts;

namespace Finbridge.Application.Users.Commands;

public sealed class BatchUpdateBalancesHandler : IRequestHandler<BatchUpdateBalancesRequest, Unit>
{
    private readonly IRequestDispatcher _dispatcher;

    public BatchUpdateBalancesHandler(IRequestDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<Unit> HandleAsync(BatchUpdateBalancesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        foreach (var update in request.Updates)
        {
            await _dispatcher.SendAsync(update, cancellationToken);
        }

        return Unit.Value;
    }
}
