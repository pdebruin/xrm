namespace Xrm.Core.Services;

/// <summary>
/// Registry for custom field renderers. Packages register Blazor components
/// to override the default editor for specific entity+field combinations.
/// </summary>
public interface IFieldRendererRegistry
{
    /// <summary>
    /// Registers a custom renderer component type for a specific entity+field.
    /// </summary>
    /// <param name="entityName">Entity name (case-insensitive).</param>
    /// <param name="fieldName">Field name (case-insensitive).</param>
    /// <param name="componentType">Blazor component type to render.</param>
    /// <param name="replace">If false (default), throws on duplicate. If true, replaces existing.</param>
    void Register(string entityName, string fieldName, Type componentType, bool replace = false);

    /// <summary>
    /// Gets the custom renderer for an entity+field, or null if none registered.
    /// </summary>
    Type? GetRenderer(string entityName, string fieldName);
}
