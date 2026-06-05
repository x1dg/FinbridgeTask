using Finbridge.Application.Abstractions;
using Finbridge.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finbridge.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class BalancesController : ControllerBase
{
    private readonly IRequestDispatcher _dispatcher;

    public BalancesController(IRequestDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> UpdateBalance(
        [FromBody] UpdateBalanceRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dispatcher.SendAsync(request, cancellationToken);
        return Ok(user);
    }

    [HttpPost("batch")]
    public async Task<IActionResult> UpdateBalances(
        [FromBody] BatchUpdateBalancesRequest request,
        CancellationToken cancellationToken)
    {
        await _dispatcher.SendAsync(request, cancellationToken);
        return Ok();
    }

    [HttpGet("history/{userId:int}")]
    public async Task<ActionResult<IReadOnlyList<BalanceHistoryResponse>>> GetBalanceHistory(
        int userId,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var history = await _dispatcher.SendAsync(new GetBalanceHistoryQuery(userId, limit), cancellationToken);
        return Ok(history);
    }
}
