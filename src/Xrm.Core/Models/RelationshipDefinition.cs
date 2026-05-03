using System.Text.Json.Serialization;

namespace Xrm.Core.Models;

public class RelationshipDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    /// <summary>The "one" (parent) side of a OneToMany relationship.</summary>
    public Guid ParentEntityId { get; set; }
    /// <summary>The "many" (child) side of a OneToMany relationship.</summary>
    public Guid ChildEntityId { get; set; }
    public RelationshipType RelationshipType { get; set; }
    public CascadeBehavior CascadeBehavior { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "system";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EntityDefinition? ParentEntity { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EntityDefinition? ChildEntity { get; set; }
    [JsonIgnore]
    public ICollection<RecordLink> RecordLinks { get; set; } = new List<RecordLink>();
}
