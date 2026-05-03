using System.Text.Json.Serialization;

namespace Xrm.Core.Models;

public class RecordLink
{
    public Guid Id { get; set; }
    public Guid RelationshipDefinitionId { get; set; }
    public Guid ParentRecordId { get; set; }
    public Guid ChildRecordId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";

    [JsonIgnore]
    public RelationshipDefinition? RelationshipDefinition { get; set; }
    [JsonIgnore]
    public Record? ParentRecord { get; set; }
    [JsonIgnore]
    public Record? ChildRecord { get; set; }
}
