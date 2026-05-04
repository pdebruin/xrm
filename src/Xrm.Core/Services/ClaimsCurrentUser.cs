using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xrm.Core.Data;
using Xrm.Core.Models;

namespace Xrm.Core.Services;

/// <summary>
/// Resolves the current user from ClaimsPrincipal and loads domain assignments.
/// Works with both HttpContext (API) and AuthenticationState (Blazor Server).
/// </summary>
public class ClaimsCurrentUser : ICurrentUser
{
    private readonly XrmAuthorizationOptions _options;
    private readonly IDbContextFactory<XrmDbContext> _dbFactory;
    private readonly ClaimsPrincipal? _principal;

    private bool _initialized;
    private string? _userKey;
    private string? _email;
    private string? _displayName;
    private bool _isAuthenticated;
    private bool _isSystemAdmin;
    private List<DomainAssignment> _assignments = new();

    public ClaimsCurrentUser(
        IOptions<XrmAuthorizationOptions> options,
        IDbContextFactory<XrmDbContext> dbFactory,
        ClaimsPrincipal? principal)
    {
        _options = options.Value;
        _dbFactory = dbFactory;
        _principal = principal;
    }

    public string? UserKey { get { EnsureInitialized(); return _userKey; } }
    public string? Email { get { EnsureInitialized(); return _email; } }
    public string? DisplayName { get { EnsureInitialized(); return _displayName; } }
    public bool IsAuthenticated { get { EnsureInitialized(); return _isAuthenticated; } }
    public bool IsSystemAdmin { get { EnsureInitialized(); return _isSystemAdmin; } }

    public bool CanRead(string? domain)
    {
        EnsureInitialized();
        if (!_isAuthenticated) return _options.AllowAnonymousForNullDomain && domain is null;
        if (_isSystemAdmin) return true;
        if (domain is null) return true; // No domain restriction
        return _assignments.Any(a => a.IsActive && a.Domain != null &&
            string.Equals(a.Domain, domain, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanWrite(string? domain)
    {
        EnsureInitialized();
        if (!_isAuthenticated) return false;
        if (_isSystemAdmin) return true;
        if (domain is null) return true; // No domain restriction
        return _assignments.Any(a => a.IsActive &&
            string.Equals(a.Domain, domain, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.Role, "Writer", StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        if (_principal?.Identity?.IsAuthenticated != true) return;

        _isAuthenticated = true;
        _email = _principal.FindFirst(_options.EmailClaim)?.Value
              ?? _principal.FindFirst(ClaimTypes.Email)?.Value
              ?? _principal.FindFirst("preferred_username")?.Value;
        _displayName = _principal.FindFirst(_options.NameClaim)?.Value
                    ?? _principal.FindFirst(ClaimTypes.Name)?.Value;

        var subject = _principal.FindFirst(_options.SubjectClaim)?.Value
                   ?? _principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var issuer = _principal.FindFirst("iss")?.Value ?? "unknown";
        _userKey = !string.IsNullOrEmpty(subject) ? $"{issuer}|{subject}" : _email;

        if (string.IsNullOrEmpty(_email)) return;

        var normalizedEmail = _email.ToLowerInvariant();

        // Load assignments and register user (synchronous for property access)
        using var db = _dbFactory.CreateDbContext();

        // Upsert KnownUser
        var user = db.Set<KnownUser>().FirstOrDefault(u => u.Email == normalizedEmail);
        if (user is null)
        {
            user = new KnownUser
            {
                Id = Guid.NewGuid(),
                SubjectId = _userKey ?? normalizedEmail,
                Email = normalizedEmail,
                DisplayName = _displayName,
                FirstSeenAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };
            db.Set<KnownUser>().Add(user);

            // Bootstrap: auto-assign SystemAdmin if in initial admins list
            if (_options.InitialSystemAdmins.Any(e =>
                string.Equals(e, normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                db.Set<DomainAssignment>().Add(new DomainAssignment
                {
                    Id = Guid.NewGuid(),
                    UserEmail = normalizedEmail,
                    Role = "SystemAdmin",
                    Domain = null,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = "system-bootstrap"
                });
            }

            db.SaveChanges();
        }
        else
        {
            user.LastSeenAt = DateTime.UtcNow;
            user.DisplayName = _displayName ?? user.DisplayName;
            if (!string.IsNullOrEmpty(_userKey)) user.SubjectId = _userKey;

            // Bootstrap: elevate to SystemAdmin if in initial admins list but not yet assigned
            if (_options.InitialSystemAdmins.Any(e =>
                string.Equals(e, normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                var hasAdmin = db.Set<DomainAssignment>().Any(a =>
                    a.UserEmail == normalizedEmail &&
                    a.Role == "SystemAdmin" &&
                    a.IsActive);

                if (!hasAdmin)
                {
                    db.Set<DomainAssignment>().Add(new DomainAssignment
                    {
                        Id = Guid.NewGuid(),
                        UserEmail = normalizedEmail,
                        Role = "SystemAdmin",
                        Domain = null,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = "system-bootstrap"
                    });
                }
            }

            db.SaveChanges();
        }

        // Load assignments
        _assignments = db.Set<DomainAssignment>()
            .Where(a => a.UserEmail == normalizedEmail && a.IsActive)
            .ToList();

        _isSystemAdmin = _assignments.Any(a =>
            string.Equals(a.Role, "SystemAdmin", StringComparison.OrdinalIgnoreCase));
    }
}
