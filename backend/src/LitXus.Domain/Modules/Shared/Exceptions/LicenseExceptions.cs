using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Shared.Exceptions;

public sealed class LicenseKeyInvalidException(string reason)
    : DomainException("INVALID_LICENSE_KEY", $"The license key could not be applied: {reason}");
