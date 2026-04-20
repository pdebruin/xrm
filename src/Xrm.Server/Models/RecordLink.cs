namespace Xrm.Server.Models;

public class RecordLink
{
    public Guid Id { get; set; }
    public Guid RelationshipDefinitionId { get; set; }
    public Guid SourceRecordId { get; set; }
    public Guid TargetRecordId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";

    public RelationshipDefinition RelationshipDefinition { get; set; } = null!;
    public Record SourceRecord { get; set; } = null!;
    public Record TargetRecord { get; set; } = null!;
}
