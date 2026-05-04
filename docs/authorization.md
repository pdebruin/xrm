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

## Quick Start: Microsoft Entra ID

The most common identity provider for XRM consumers. Full setup takes ~15 minutes.

### Prerequisites

| Item | How to get it |
|------|---------------|
| Entra app registration | Azure Portal → Entra ID → App registrations → New |
| `Microsoft.Identity.Web` NuGet | `dotnet add package Microsoft.Identity.Web` |
| `Microsoft.Identity.Web.UI` NuGet | `dotnet add package Microsoft.Identity.Web.UI` |

### 1. Register the app in Entra ID

- **Supported account types:** Single tenant (your org)
- **Redirect URI:** Web → `https://localhost:{port}/signin-oidc`
- **Authentication blade:** Check ✅ **ID tokens** under implicit/hybrid flows
- **Certificates & secrets:** Create a client secret (copy the value immediately)

### 2. Configure appsettings.json

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "",
    "ClientId": "",
    "ClientSecret": "",
    "CallbackPath": "/signin-oidc"
  },
  "Xrm": {
    "InitialSystemAdmins": []
  }
}
```

Leave values empty in source control — override with environment variables at runtime:

```bash
export AzureAd__TenantId="your-tenant-id"
export AzureAd__ClientId="your-client-id"
export AzureAd__ClientSecret="your-client-secret"
export Xrm__InitialSystemAdmins__0="admin@yourorg.com"
```

> .NET uses `__` (double underscore) to represent `:` in the config hierarchy.
> Array elements use `__0`, `__1`, etc.

### 3. Program.cs

```csharp
using Microsoft.Identity.Web;

// Authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// XRM core + authorization
builder.Services.AddXrmCore(connectionString);

var initialAdmins = builder.Configuration
    .GetSection("Xrm:InitialSystemAdmins").Get<List<string>>() ?? [];
builder.Services.AddXrmAuthorization(options =>
{
    options.InitialSystemAdmins = initialAdmins;
});

// ... build app ...

app.UseAuthentication();
app.UseAuthorization();

// Require auth on all Blazor pages (triggers OIDC redirect)
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();
```

### 4. HTTPS (required for OIDC)

```bash
dotnet dev-certs https --trust   # one-time
```

### 5. First login

1. Navigate to the app → redirected to Microsoft login
2. Authenticate with your org account
3. XRM creates a `KnownUser` record from the `preferred_username` claim
4. If your email matches `InitialSystemAdmins` → auto-assigned **SystemAdmin**
5. Assign additional users/roles via Admin → Security

### Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| `AADSTS700054: response_type 'id_token' is not enabled` | ID tokens not checked | Entra → Authentication blade → check ID tokens |
| `Access denied: cannot read in domain 'X'` | No domain roles assigned | Assign role via Admin → Security, or add to `InitialSystemAdmins` |
| No login prompt, immediate error | Missing `.RequireAuthorization()` or HTTP instead of HTTPS | Ensure HTTPS and `.RequireAuthorization()` on mapped components |
| `Correlation failed` | Redirect URI mismatch | Entra redirect URI must match exactly (scheme + host + port + path) |

### Production considerations

- **Managed Identity** — preferred over client secrets for Azure App Service (eliminates rotation)
- **Key Vault references** — if using secrets, store them in Key Vault and reference via App Settings
- **Multiple admins** — add `Xrm__InitialSystemAdmins__1`, `__2`, etc.

---

## Using Other OIDC Providers

XRM is provider-agnostic. For providers other than Entra (Google, Auth0, Keycloak, etc.):

1. Configure your OIDC middleware as usual
2. Call `AddXrmAuthorization()` with claim mappings matching your provider:

```csharp
builder.Services.AddXrmAuthorization(options =>
{
    options.InitialSystemAdmins = new[] { "admin@example.com" };
    options.EmailClaim = "email";    // adjust if your provider uses different claims
    options.NameClaim = "name";
});
```

XRM resolves user identity from standard claims. As long as your provider issues `email` (or your configured `EmailClaim`), it will work.

---

## Admin UI

Navigate to **Admin → Security** (requires SystemAdmin role) to:

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
