namespace Xrm.Core.Models;

/// <summary>
/// Assigns a role to a user for a specific domain (or system-wide).
/// </summary>
public class DomainAssignment
{
    public Guid Id { get; set; }

    /// <summary>
    /// User email (normalized to lowercase). Matches KnownUser.Email.
    /// Can be pre-assigned before the user's first login.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Role: "SystemAdmin", "Writer", or "Reader".
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Domain scope. Null for SystemAdmin (applies to all domains).
    /// Must match EntityDefinition.Domain for scoped roles.
    /// </summary>
    public string? Domain { get; set; }

    public DateTime AssignedAt { get; set; }
    public string AssignedBy { get; set; } = "system";
    public bool IsActive { get; set; } = true;
}
