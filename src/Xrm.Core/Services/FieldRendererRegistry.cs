namespace Xrm.Core.Services;

/// <summary>
/// In-memory implementation of field renderer registry.
/// Applies deferred registrations from DI on first access.
/// </summary>
public class FieldRendererRegistry : IFieldRendererRegistry
{
    private readonly Dictionary<string, Type> _renderers = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;
    private readonly Lock _lock = new();
    private readonly IEnumerable<FieldRendererRegistration> _registrations;

    public FieldRendererRegistry(IEnumerable<FieldRendererRegistration> registrations)
    {
        _registrations = registrations;
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            foreach (var reg in _registrations)
                Register(reg.EntityName, reg.FieldName, reg.ComponentType, reg.Replace);
            _initialized = true;
        }
    }

    public void Register(string entityName, string fieldName, Type componentType, bool replace = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        ArgumentNullException.ThrowIfNull(componentType);

        var key = $"{entityName}.{fieldName}";

        if (!replace && _renderers.ContainsKey(key))
            throw new InvalidOperationException(
                $"A field renderer is already registered for '{entityName}.{fieldName}'. " +
                $"Use replace: true to override.");

        _renderers[key] = componentType;
    }

    public Type? GetRenderer(string entityName, string fieldName)
    {
        EnsureInitialized();
        var key = $"{entityName}.{fieldName}";
        return _renderers.TryGetValue(key, out var type) ? type : null;
    }
}
