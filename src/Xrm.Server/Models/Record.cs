using System.Text.Json.Serialization;

namespace Xrm.Server.Models;

public class Record
{
    public Guid Id { get; set; }
    public Guid EntityDefinitionId { get; set; }

    // Field values stored as JSON: { "fieldName": value, ... }
    public string DataJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "system";

    [JsonIgnore]
    public EntityDefinition? EntityDefinition { get; set; }
    [JsonIgnore]
    public ICollection<RecordLink> SourceLinks { get; set; } = new List<RecordLink>();
    [JsonIgnore]
    public ICollection<RecordLink> TargetLinks { get; set; } = new List<RecordLink>();
}
