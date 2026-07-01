namespace LitXus.Domain.Common;

/// <summary>
/// Base type for business-rule violations raised from domain entities.
/// The Api layer maps these to 422 with the ErrorCode as the response code
/// (see docs/03_API_Specification.md §3.2).
/// </summary>
public abstract class DomainException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
