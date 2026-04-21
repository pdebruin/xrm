using System.Text.Json.Serialization;

namespace Xrm.Server.Models;

public class RecordLink
{
    public Guid Id { get; set; }
    public Guid RelationshipDefinitionId { get; set; }
    public Guid SourceRecordId { get; set; }
    public Guid TargetRecordId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";

    [JsonIgnore]
    public RelationshipDefinition? RelationshipDefinition { get; set; }
    [JsonIgnore]
    public Record? SourceRecord { get; set; }
    [JsonIgnore]
    public Record? TargetRecord { get; set; }
}
