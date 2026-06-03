using Finbridge.Api.Dtos;
using Finbridge.Core.Models;
using Finbridge.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Finbridge.Api.Services
{
    public class BalanceService
    {
        private readonly FinbridgeDbContext _context;
        private readonly BalanceSettings _settings;
        private readonly IKafkaProducer _kafkaProducer;

        public BalanceService(FinbridgeDbContext context, IOptions<BalanceSettings> settings, IKafkaProducer kafkaProducer)
        {
            _context = context;
            _settings = settings.Value;
            _kafkaProducer = kafkaProducer;
        }

        public async Task<User> UpdateBalance(int userId, decimal amount)
        {
            // Optimistic concurrency handling: retry up to 3 times on concurrency conflict
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with id {userId} not found");
                }

                var newBalance = user.Balance + amount;

                if (newBalance < 0)
                {
                    throw new InvalidOperationException("Balance cannot be negative");
                }

                if (newBalance > _settings.MaxBalance)
                {
                    throw new InvalidOperationException($"Balance cannot exceed maximum allowed value of {_settings.MaxBalance}");
                }

                user.Balance = newBalance;
                user.Version++; // Increment version for optimistic locking

                // Record the balance change in history
                var history = new BalanceHistory
                {
                    UserId = user.Id,
                    AmountChanged = amount,
                    NewBalance = newBalance,
                    ChangedAt = DateTime.UtcNow
                };

                _context.BalanceHistories.Add(history);

                try
                {
                    await _context.SaveChangesAsync();
                    // Send Kafka event
                    await _kafkaProducer.ProduceUserEventAsync(user);
                    return user;
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If concurrency conflict, retry (unless last attempt)
                    if (attempt == 2)
                    {
                        throw new InvalidOperationException("Unable to update balance due to concurrent updates. Please try again.");
                    }
                    // Otherwise loop again to get fresh data
                }
            }

            // Should not reach here
            throw new InvalidOperationException("Unexpected error in balance update");
        }

        public async Task UpdateBalances(List<UpdateBalanceDto> updates)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                foreach (var update in updates)
                {
                    await UpdateBalance(update.UserId, update.Amount);
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public List<BalanceHistory> GetBalanceHistory(int userId, int limit = 20)
        {
            return _context.BalanceHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.ChangedAt)
                .Take(limit)
                .ToList();
        }
    }
}