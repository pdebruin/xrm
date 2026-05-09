namespace Xrm.Core.Models;

/// <summary>
/// A named filter preset for an entity's record list.
/// Defined by admin in EntityDefinition.SavedViewsJson.
/// </summary>
public class SavedView
{
    public string Name { get; set; } = string.Empty;
    public List<ViewFilter> Filters { get; set; } = new();

    /// <summary>
    /// Optional: default sort field name for this view.
    /// </summary>
    public string? SortField { get; set; }

    /// <summary>
    /// Optional: sort direction ("asc" or "desc") for this view.
    /// </summary>
    public string? SortDir { get; set; }
}

/// <summary>
/// A single filter condition within a saved view.
/// Values may contain tokens like {{currentUser}} resolved at query time.
/// </summary>
public class ViewFilter
{
    /// <summary>
    /// The field name to filter on (must match a FieldDefinition.Name).
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Comparison operator: "eq", "neq", "contains", "gt", "lt", "gte", "lte".
    /// </summary>
    public string Operator { get; set; } = "eq";

    /// <summary>
    /// The value to compare against. Supports tokens:
    /// {{currentUser}} — resolved to ICurrentUser.Email at query time.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
