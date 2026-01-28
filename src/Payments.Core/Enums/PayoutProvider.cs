namespace Payments.Core.Enums;

/// <summary>
/// Supported payout providers for stablecoin to fiat conversions.
/// </summary>
public enum PayoutProvider
{
    /// <summary>
    /// Mesta payment provider - Enterprise-grade cross-border payment network.
    /// </summary>
    Mesta = 1,

    /// <summary>
    /// Borderless payment provider - Global stablecoin orchestration and liquidity network.
    /// </summary>
    Borderless = 2
}
