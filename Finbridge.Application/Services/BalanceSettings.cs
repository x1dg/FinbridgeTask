namespace Finbridge.Application.Services;

/// <summary>
/// Конфиг доменной политики лимита баланса. Читается из секции "BalanceSettings".
/// </summary>
public sealed class BalanceSettings
{
    public decimal MaxBalance { get; set; }
}
