using Finbridge.Api.Dtos;
using Finbridge.Api.Services;
using Finbridge.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Finbridge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BalancesController : ControllerBase
    {
        private readonly BalanceService _balanceService;

        public BalancesController(BalanceService balanceService)
        {
            _balanceService = balanceService;
        }

        // POST api/balances
        [HttpPost]
        public async Task<ActionResult<User>> UpdateBalance(UpdateBalanceDto dto)
        {
            try
            {
                var user = await _balanceService.UpdateBalance(dto.UserId, dto.Amount);
                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST api/balances/batch
        [HttpPost("batch")]
        public async Task<IActionResult> UpdateBalances(List<UpdateBalanceDto> updates)
        {
            try
            {
                await _balanceService.UpdateBalances(updates);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET api/balances/history/{userId}
        [HttpGet("history/{userId}")]
        public ActionResult<List<BalanceHistory>> GetBalanceHistory(int userId)
        {
            var history = _balanceService.GetBalanceHistory(userId);
            return Ok(history);
        }
    }
}