using System;

namespace Finbridge.Core.Models
{
    public class BalanceHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal AmountChanged { get; set; }
        public decimal NewBalance { get; set; }
        public DateTime ChangedAt { get; set; }
        public User User { get; set; } = null!;
    }
}