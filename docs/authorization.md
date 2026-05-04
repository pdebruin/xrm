# Authorization (CR-007)

XRM provides a pluggable identity and authorization layer. It enforces domain-based access
control on entities while remaining identity-provider agnostic.

## Concepts

| Concept | Description |
|---------|-------------|
| **Domain** | Logical grouping on `EntityDefinition.Domain` (e.g., "finance", "hr") |
| **Role** | `SystemAdmin`, `Writer`, or `Reader` |
| **DomainAssignment** | Maps a user email → role → domain |
| **KnownUser** | Auto-registered on first authenticated access |

### Access rules

- **SystemAdmin** — full access to all domains
- **Writer** — can read and write records in the assigned domain
- **Reader** — can read records in the assigned domain, but not create/update/delete
- **Null-domain entities** — accessible to all authenticated users (no restriction)
- **No auth configured** — `AnonymousCurrentUser` is used (permits everything, for dev/testing)

## Setup

### 1. Configure authentication (host responsibility)

```csharp
// Program.cs in your host app
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
// Or any OIDC provider — XRM is provider-agnostic
```

### 2. Register XRM authorization

```csharp
builder.Services.AddXrmCore(connectionString);
builder.Services.AddXrmAuthorization(options =>
{
    // Bootstrap: these emails get SystemAdmin on first login
    options.InitialSystemAdmins = new[] { "admin@contoso.com" };

    // Optional: customize claim mapping for non-standard providers
    options.EmailClaim = "email";          // default
    options.NameClaim = "name";            // default
    options.SubjectClaim = "sub";          // default

    // Allow unauthenticated access to null-domain entities (e.g., public dashboards)
    options.AllowAnonymousForNullDomain = false; // default
});
```

### 3. (Optional) Protect Blazor pages

XRM enforces authorization at the service layer (RecordService, EntityService nav filtering).
If you want to also protect routes:

```csharp
app.MapBlazorHub().RequireAuthorization();
```

## Configuration options

| Option | Default | Description |
|--------|---------|-------------|
| `EmailClaim` | `"email"` | Claim type for user's email |
| `NameClaim` | `"name"` | Claim type for display name |
| `SubjectClaim` | `"sub"` | Claim type for stable subject identifier |
| `InitialSystemAdmins` | `[]` | Emails that auto-receive SystemAdmin on first login |
| `AllowAnonymousForNullDomain` | `false` | Whether unauthenticated users can access null-domain entities |

## Admin UI

Navigate to **Admin → Users & Access** (requires SystemAdmin role) to:

- View all known users and their current roles
- Assign roles (Reader/Writer/SystemAdmin) to users by email
- Scope roles to specific domains
- Revoke assignments

Roles can be pre-assigned before a user's first login — matching is done on email.

## How it works

1. **User logs in** via any OIDC provider (Entra ID, Google, etc.)
2. **XRM auto-registers** the user in `KnownUsers` table on first access
3. **Bootstrap check** — if email is in `InitialSystemAdmins` and no prior record exists, auto-assigns SystemAdmin
4. **Domain assignments loaded** from `DomainAssignments` table
5. **Service-layer enforcement:**
   - `RecordService` checks `CanRead`/`CanWrite` on every operation
   - `NavMenu` filters entities by `CanRead`
   - Admin pages check `IsSystemAdmin`

## ICurrentUser interface

Available via DI for custom authorization logic in your domain code:

```csharp
public interface ICurrentUser
{
    string? UserKey { get; }        // Stable ID (issuer|subject)
    string? Email { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
    bool IsSystemAdmin { get; }
    bool CanRead(string? domain);
    bool CanWrite(string? domain);
}
```

## Testing without auth

When `AddXrmAuthorization()` is **not** called, XRM registers `AnonymousCurrentUser` which
permits all operations. This is the default for development and testing.

For integration tests with authorization, provide a custom `ICurrentUser`:

```csharp
services.AddScoped<ICurrentUser>(_ => new TestUser { ... });
```
