namespace Finbridge.Web.ViewModels
{
    public class BalanceHistoryViewModel
    {
        public decimal AmountChanged { get; set; }
        public decimal NewBalance { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}