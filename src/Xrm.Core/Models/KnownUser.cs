namespace Xrm.Core.Models;

/// <summary>
/// A user known to the system, auto-registered on first authenticated access.
/// </summary>
public class KnownUser
{
    public Guid Id { get; set; }

    /// <summary>
    /// Stable identifier from the identity provider (issuer + subject claim).
    /// Used as the primary key for matching across sessions.
    /// </summary>
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// Email address from claims. Used for display and pre-assignment matching.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name from claims.
    /// </summary>
    public string? DisplayName { get; set; }

    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
