using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xrm.Core.Data;
using Xrm.Core.Services;

namespace Xrm.Core;

/// <summary>
/// Extension methods to register all XRM core services in a host application.
/// </summary>
public static class XrmServiceExtensions
{
    /// <summary>
    /// Registers XRM core services: DbContext factory, entity/field/relationship/record services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">SQLite connection string (e.g., "Data Source=xrm.db").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddXrmCore(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<XrmDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IEntityService, EntityService>();
        services.AddScoped<IFieldService, FieldService>();
        services.AddScoped<IRelationshipService, RelationshipService>();
        services.AddScoped<IRecordService, RecordService>();
        services.AddScoped<IAuditService, AuditService>();

        // Field renderer registry (singleton — registrations happen at startup)
        services.TryAddSingleton<IFieldRendererRegistry, FieldRendererRegistry>();

        // Default: permit all access (no auth configured). Overridden by AddXrmAuthorization().
        services.TryAddScoped<ICurrentUser, AnonymousCurrentUser>();

        return services;
    }

    /// <summary>
    /// Registers a data seeder implementation.
    /// </summary>
    public static IServiceCollection AddXrmSeeder<T>(this IServiceCollection services) where T : class, IDataSeeder
    {
        services.AddScoped<IDataSeeder, T>();
        return services;
    }

    /// <summary>
    /// Registers a custom Blazor field renderer for a specific entity+field combination.
    /// The component must accept parameters: Value, ValueChanged, Field, Entity, RecordDataJson, ReadOnly, ValidationChanged.
    /// </summary>
    public static IServiceCollection AddXrmFieldRenderer(
        this IServiceCollection services, string entityName, string fieldName, Type componentType, bool replace = false)
    {
        services.TryAddSingleton<IFieldRendererRegistry, FieldRendererRegistry>();
        services.AddSingleton(new FieldRendererRegistration(entityName, fieldName, componentType, replace));
        return services;
    }
}
