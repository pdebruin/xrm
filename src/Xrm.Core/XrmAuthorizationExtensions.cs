using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xrm.Core.Data;
using Xrm.Core.Services;

namespace Xrm.Core;

/// <summary>
/// Extension methods to register XRM authorization services.
/// </summary>
public static class XrmAuthorizationExtensions
{
    /// <summary>
    /// Registers XRM authorization: ICurrentUser resolved from claims + DomainAssignment table.
    /// Call this after AddXrmCore() and after configuring authentication.
    /// </summary>
    public static IServiceCollection AddXrmAuthorization(
        this IServiceCollection services,
        Action<XrmAuthorizationOptions>? configure = null)
    {
        services.Configure<XrmAuthorizationOptions>(options =>
        {
            configure?.Invoke(options);
        });

        services.AddHttpContextAccessor();

        // Register ICurrentUser as scoped — resolves from the best available principal
        services.AddScoped<ICurrentUser>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<XrmAuthorizationOptions>>();
            var dbFactory = sp.GetRequiredService<IDbContextFactory<XrmDbContext>>();

            // Try HttpContext first (API controllers, SSR pages)
            var httpContext = sp.GetService<IHttpContextAccessor>()?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return new ClaimsCurrentUser(options, dbFactory, httpContext.User);
            }

            // Try AuthenticationStateProvider (Blazor Server interactive circuits)
            var authStateProvider = sp.GetService<AuthenticationStateProvider>();
            if (authStateProvider is not null)
            {
                // Get state synchronously for property-based access
                var authState = authStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult();
                if (authState.User?.Identity?.IsAuthenticated == true)
                {
                    return new ClaimsCurrentUser(options, dbFactory, authState.User);
                }
            }

            // Not authenticated — return restricted user
            return new UnauthenticatedCurrentUser(options.Value);
        });

        return services;
    }
}

/// <summary>
/// ICurrentUser for unauthenticated requests when auth is configured.
/// Denies all access unless AllowAnonymousForNullDomain is set.
/// </summary>
internal class UnauthenticatedCurrentUser : ICurrentUser
{
    private readonly XrmAuthorizationOptions _options;

    public UnauthenticatedCurrentUser(XrmAuthorizationOptions options) => _options = options;

    public string? UserKey => null;
    public string? Email => null;
    public string? DisplayName => null;
    public bool IsAuthenticated => false;
    public bool IsSystemAdmin => false;
    public bool CanRead(string? domain) => _options.AllowAnonymousForNullDomain && domain is null;
    public bool CanWrite(string? domain) => false;
}
