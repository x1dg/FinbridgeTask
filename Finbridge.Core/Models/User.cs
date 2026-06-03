using System;
using System.Collections.Generic;

namespace Finbridge.Core.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string PlaceOfBirth { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public int Version { get; set; } // For optimistic locking
        public List<BalanceHistory> BalanceHistory { get; set; } = new List<BalanceHistory>();
    }
}