namespace LitXus.Application.Common.Exceptions;

/// <summary>Raised by RequireModule/RequirePermission checks that happen inside handlers (defense in depth
/// behind the Api-layer action filters — see docs/06_RBAC_Auth.md §6.5).</summary>
public sealed class ForbiddenException(string message = "You do not have permission to perform this action.")
    : Exception(message);
