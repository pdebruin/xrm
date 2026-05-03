namespace Xrm.Core.Services;

/// <summary>
/// Represents the currently authenticated user's identity and domain access.
/// Resolved per-request (API) or per-circuit (Blazor Server).
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Stable user identifier (issuer + subject). Null if not authenticated.
    /// </summary>
    string? UserKey { get; }

    /// <summary>
    /// User's email address from claims.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Display name from claims.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Whether the user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Whether the user has SystemAdmin role (full access to all domains).
    /// </summary>
    bool IsSystemAdmin { get; }

    /// <summary>
    /// Whether the user can read records in the given domain.
    /// Null domain = no domain restriction (accessible to all authenticated users).
    /// </summary>
    bool CanRead(string? domain);

    /// <summary>
    /// Whether the user can create/update/delete records in the given domain.
    /// </summary>
    bool CanWrite(string? domain);
}
