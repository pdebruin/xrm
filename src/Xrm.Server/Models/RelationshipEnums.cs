namespace Xrm.Server.Models;

public enum RelationshipType
{
    OneToMany,
    ManyToOne,
    ManyToMany
}

public enum CascadeBehavior
{
    None,
    RemoveLink,
    Cascade
}
