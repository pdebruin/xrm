using System.Security.Claims;

namespace Xrm.Core.Services;

/// <summary>
/// Configuration options for XRM authorization.
/// </summary>
public class XrmAuthorizationOptions
{
    /// <summary>
    /// Claim type used to extract the user's email address.
    /// Default: ClaimTypes.Email. Some providers use "preferred_username" or "email".
    /// </summary>
    public string EmailClaim { get; set; } = ClaimTypes.Email;

    /// <summary>
    /// Claim type used to extract the display name.
    /// Default: ClaimTypes.Name.
    /// </summary>
    public string NameClaim { get; set; } = ClaimTypes.Name;

    /// <summary>
    /// Claim type used for the stable subject identifier.
    /// Default: ClaimTypes.NameIdentifier. Combined with issuer for uniqueness.
    /// </summary>
    public string SubjectClaim { get; set; } = ClaimTypes.NameIdentifier;

    /// <summary>
    /// Email addresses that are automatically granted SystemAdmin on first login.
    /// Use for bootstrapping the first admin user.
    /// </summary>
    public List<string> InitialSystemAdmins { get; set; } = new();

    /// <summary>
    /// Whether unauthenticated users can access entities with Domain = null.
    /// Default: false (authentication required for all access).
    /// </summary>
    public bool AllowAnonymousForNullDomain { get; set; } = false;
}
