using Finbridge.Application.Contracts;
using Finbridge.Application.Services;
using Finbridge.Domain.Users.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Finbridge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BalancesController : ControllerBase
{
    private readonly IBalanceService _balanceService;

    public BalancesController(IBalanceService balanceService)
    {
        _balanceService = balanceService;
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> UpdateBalance(
        [FromBody] UpdateBalanceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _balanceService.UpdateBalanceAsync(request, cancellationToken);
            return Ok(user);
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (NegativeBalanceException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (BalanceLimitExceededException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("batch")]
    public async Task<IActionResult> UpdateBalances(
        [FromBody] BatchUpdateBalancesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _balanceService.UpdateBalancesAsync(request, cancellationToken);
            return Ok();
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (NegativeBalanceException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (BalanceLimitExceededException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("history/{userId:int}")]
    public async Task<ActionResult<IReadOnlyList<BalanceHistoryResponse>>> GetBalanceHistory(
        int userId,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var history = await _balanceService.GetBalanceHistoryAsync(userId, limit, cancellationToken);
        return Ok(history);
    }
}
