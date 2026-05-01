namespace Xrm.Core.Models;

/// <summary>
/// Relationship cardinality. Convention: Source = "one" (parent), Target = "many" (child).
/// Use OneToMany for all parent-child relationships.
/// </summary>
public enum RelationshipType
{
    /// <summary>Source entity is the parent ("one"), Target entity is the child ("many").</summary>
    OneToMany,
    [Obsolete("Use OneToMany with swapped Source/Target instead. Kept for backward compatibility.")]
    ManyToOne,
    ManyToMany
}

public enum CascadeBehavior
{
    None,
    RemoveLink,
    Cascade
}
