namespace Xrm.Core.Models;

/// <summary>
/// Relationship cardinality. Convention: ParentEntity = "one" side, ChildEntity = "many" side.
/// Use OneToMany for all parent-child relationships.
/// </summary>
public enum RelationshipType
{
    /// <summary>ParentEntity has many ChildEntity records.</summary>
    OneToMany,
    [Obsolete("Use OneToMany with swapped Parent/Child instead. Kept for backward compatibility.")]
    ManyToOne,
    ManyToMany
}

public enum CascadeBehavior
{
    None,
    RemoveLink,
    Cascade
}
