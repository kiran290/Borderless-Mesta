namespace Payments.Core.Enums;

/// <summary>
/// Supported blockchain networks for stablecoin transactions.
/// </summary>
public enum BlockchainNetwork
{
    /// <summary>
    /// Ethereum mainnet.
    /// </summary>
    Ethereum = 1,

    /// <summary>
    /// Polygon (formerly Matic) network.
    /// </summary>
    Polygon = 2,

    /// <summary>
    /// Arbitrum Layer 2 network.
    /// </summary>
    Arbitrum = 3,

    /// <summary>
    /// Optimism Layer 2 network.
    /// </summary>
    Optimism = 4,

    /// <summary>
    /// Base network (Coinbase Layer 2).
    /// </summary>
    Base = 5,

    /// <summary>
    /// Tron network.
    /// </summary>
    Tron = 6,

    /// <summary>
    /// Solana network.
    /// </summary>
    Solana = 7
}
