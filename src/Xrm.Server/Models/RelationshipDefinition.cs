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

public class RelationshipDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public Guid SourceEntityId { get; set; }
    public Guid TargetEntityId { get; set; }
    public RelationshipType RelationshipType { get; set; }
    public CascadeBehavior CascadeBehavior { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "system";

    public EntityDefinition SourceEntity { get; set; } = null!;
    public EntityDefinition TargetEntity { get; set; } = null!;
    public ICollection<RecordLink> RecordLinks { get; set; } = new List<RecordLink>();
}
