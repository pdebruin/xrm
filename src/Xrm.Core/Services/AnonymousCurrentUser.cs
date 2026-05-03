namespace Xrm.Core.Services;

/// <summary>
/// Default ICurrentUser that permits all access.
/// Used when AddXrmAuthorization() is NOT called (development/testing).
/// </summary>
public class AnonymousCurrentUser : ICurrentUser
{
    public string? UserKey => null;
    public string? Email => null;
    public string? DisplayName => "Anonymous";
    public bool IsAuthenticated => false;
    public bool IsSystemAdmin => true;
    public bool CanRead(string? domain) => true;
    public bool CanWrite(string? domain) => true;
}
