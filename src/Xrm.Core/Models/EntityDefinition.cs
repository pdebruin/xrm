using System.Text.Json.Serialization;

namespace Xrm.Core.Models;

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

    /// <summary>
    /// Optional grouping label for UI navigation and authorization scoping.
    /// Entities with the same Domain are displayed together in the nav menu.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Sort order within a domain group. If null, falls back to SortOrder.
    /// </summary>
    public int? DomainSortOrder { get; set; }

    /// <summary>
    /// The field used as the display label for records of this entity (e.g. in dropdowns, relationship grids).
    /// If null, falls back to the first field by SortOrder.
    /// </summary>
    public Guid? PrimaryFieldId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "system";

    public ICollection<FieldDefinition> Fields { get; set; } = new List<FieldDefinition>();

    /// <summary>
    /// JSON array of cross-field validation rules.
    /// Supported types: "compare" (field vs field) and "required_if" (conditional required).
    /// </summary>
    public string? ValidationRulesJson { get; set; }

    [JsonIgnore]
    public ICollection<RelationshipDefinition> ParentRelationships { get; set; } = new List<RelationshipDefinition>();
    [JsonIgnore]
    public ICollection<RelationshipDefinition> ChildRelationships { get; set; } = new List<RelationshipDefinition>();
    [JsonIgnore]
    public ICollection<Record> Records { get; set; } = new List<Record>();
}
