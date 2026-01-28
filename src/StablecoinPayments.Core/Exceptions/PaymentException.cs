using StablecoinPayments.Core.Enums;

namespace StablecoinPayments.Core.Exceptions;

public class PaymentException : Exception
{
    public string ErrorCode { get; }
    public PaymentProvider? Provider { get; }

    public PaymentException(string message, string errorCode, PaymentProvider? provider = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Provider = provider;
    }

    public PaymentException(string message, string errorCode, Exception innerException, PaymentProvider? provider = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Provider = provider;
    }
}

public class ProviderUnavailableException : PaymentException
{
    public ProviderUnavailableException(PaymentProvider provider)
        : base($"Provider {provider} is currently unavailable", "PROVIDER_UNAVAILABLE", provider)
    {
    }
}

public class QuoteExpiredException : PaymentException
{
    public QuoteExpiredException(string quoteId)
        : base($"Quote {quoteId} has expired", "QUOTE_EXPIRED")
    {
    }
}

public class InsufficientFundsException : PaymentException
{
    public InsufficientFundsException()
        : base("Insufficient funds for this transaction", "INSUFFICIENT_FUNDS")
    {
    }
}

public class InvalidCustomerException : PaymentException
{
    public InvalidCustomerException(string customerId)
        : base($"Customer {customerId} not found or invalid", "INVALID_CUSTOMER")
    {
    }
}

public class VerificationRequiredException : PaymentException
{
    public VerificationRequiredException(string customerId, VerificationLevel requiredLevel)
        : base($"Customer {customerId} requires {requiredLevel} verification", "VERIFICATION_REQUIRED")
    {
    }
}
