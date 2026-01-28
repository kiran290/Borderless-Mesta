namespace StablecoinPayments.Core.Enums;

/// <summary>
/// Supported stablecoins for payouts.
/// </summary>
public enum Stablecoin
{
    USDT,
    USDC
}

/// <summary>
/// Supported fiat currencies for payouts.
/// </summary>
public enum FiatCurrency
{
    USD,
    EUR,
    GBP,
    NGN,
    KES,
    ZAR,
    GHS,
    TZS,
    UGX,
    INR,
    PHP,
    MXN,
    BRL,
    ARS,
    COP
}

/// <summary>
/// Supported blockchain networks.
/// </summary>
public enum BlockchainNetwork
{
    Ethereum,
    Polygon,
    Tron,
    Solana,
    BinanceSmartChain,
    Avalanche,
    Arbitrum,
    Optimism,
    Base
}

/// <summary>
/// Payment providers.
/// </summary>
public enum PaymentProvider
{
    Mesta,
    Borderless
}

/// <summary>
/// Payment methods for fiat delivery.
/// </summary>
public enum PaymentMethod
{
    BankTransfer,
    MobileMoney,
    CashPickup,
    CardDeposit
}

/// <summary>
/// Payout status.
/// </summary>
public enum PayoutStatus
{
    Pending,
    AwaitingDeposit,
    DepositReceived,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Refunded,
    Expired
}

/// <summary>
/// Customer type.
/// </summary>
public enum CustomerType
{
    Individual,
    Business
}

/// <summary>
/// Customer status.
/// </summary>
public enum CustomerStatus
{
    Pending,
    Active,
    Suspended,
    Closed
}

/// <summary>
/// Verification status.
/// </summary>
public enum VerificationStatus
{
    NotStarted,
    Pending,
    InProgress,
    DocumentsRequired,
    UnderReview,
    Approved,
    Rejected,
    Expired
}

/// <summary>
/// Verification level.
/// </summary>
public enum VerificationLevel
{
    None,
    Basic,
    Standard,
    Enhanced
}

/// <summary>
/// Document type for verification.
/// </summary>
public enum DocumentType
{
    Passport,
    NationalId,
    DriversLicense,
    ProofOfAddress,
    BankStatement,
    BusinessRegistration,
    ArticlesOfIncorporation,
    TaxDocument,
    Selfie
}

/// <summary>
/// Beneficiary type.
/// </summary>
public enum BeneficiaryType
{
    Individual,
    Business
}
