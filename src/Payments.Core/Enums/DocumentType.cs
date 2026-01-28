namespace Payments.Core.Enums;

/// <summary>
/// Type of identity or verification document.
/// </summary>
public enum DocumentType
{
    // Individual Documents
    /// <summary>
    /// Passport.
    /// </summary>
    Passport = 1,

    /// <summary>
    /// National ID card.
    /// </summary>
    NationalId = 2,

    /// <summary>
    /// Driver's license.
    /// </summary>
    DriversLicense = 3,

    /// <summary>
    /// Residence permit.
    /// </summary>
    ResidencePermit = 4,

    /// <summary>
    /// Utility bill for proof of address.
    /// </summary>
    UtilityBill = 5,

    /// <summary>
    /// Bank statement for proof of address.
    /// </summary>
    BankStatement = 6,

    /// <summary>
    /// Tax document.
    /// </summary>
    TaxDocument = 7,

    // Business Documents
    /// <summary>
    /// Certificate of incorporation.
    /// </summary>
    CertificateOfIncorporation = 10,

    /// <summary>
    /// Business registration document.
    /// </summary>
    BusinessRegistration = 11,

    /// <summary>
    /// Articles of association.
    /// </summary>
    ArticlesOfAssociation = 12,

    /// <summary>
    /// Memorandum of association.
    /// </summary>
    MemorandumOfAssociation = 13,

    /// <summary>
    /// Shareholder register.
    /// </summary>
    ShareholderRegister = 14,

    /// <summary>
    /// Director register.
    /// </summary>
    DirectorRegister = 15,

    /// <summary>
    /// UBO (Ultimate Beneficial Owner) declaration.
    /// </summary>
    UboDeclaration = 16,

    /// <summary>
    /// Business license.
    /// </summary>
    BusinessLicense = 17,

    /// <summary>
    /// Financial statements.
    /// </summary>
    FinancialStatements = 18,

    /// <summary>
    /// Tax registration certificate.
    /// </summary>
    TaxRegistrationCertificate = 19,

    /// <summary>
    /// Proof of business address.
    /// </summary>
    ProofOfBusinessAddress = 20,

    /// <summary>
    /// Other document type.
    /// </summary>
    Other = 99
}
