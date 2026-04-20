namespace Xrm.Server.Models;

public class EntityDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? PluralName { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsHomeEntity { get; set; }
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "system";

    public ICollection<FieldDefinition> Fields { get; set; } = new List<FieldDefinition>();
    public ICollection<RelationshipDefinition> SourceRelationships { get; set; } = new List<RelationshipDefinition>();
    public ICollection<RelationshipDefinition> TargetRelationships { get; set; } = new List<RelationshipDefinition>();
    public ICollection<Record> Records { get; set; } = new List<Record>();
}
