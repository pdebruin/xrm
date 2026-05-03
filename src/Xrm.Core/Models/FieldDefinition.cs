using System.Text.Json.Serialization;

namespace Xrm.Core.Models;

public class FieldDefinition
{
    public Guid Id { get; set; }
    public Guid EntityDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public FieldDataType DataType { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public int SortOrder { get; set; }

    // Constraints stored as JSON
    public int? MaxLength { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public string? Pattern { get; set; }

    /// <summary>
    /// JSON array of option strings for Choice/MultiChoice fields.
    /// Must be a valid JSON array, e.g.: ["Option1","Option2","Option3"]
    /// </summary>
    public string? OptionsJson { get; set; }

    /// <summary>
    /// JSON object defining allowed state transitions for Choice fields.
    /// Format: {"CurrentState":["AllowedNext1","AllowedNext2"], ...}
    /// When set, only defined transitions are permitted on update.
    /// </summary>
    public string? TransitionsJson { get; set; }

    /// <summary>
    /// Expression for Computed fields. References other field names and supports +, -, *, /, parentheses.
    /// Example: "NettoHuur + Servicekosten" or "Prijs * 1.21"
    /// </summary>
    public string? Expression { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "system";

    [JsonIgnore]
    public EntityDefinition? EntityDefinition { get; set; }
}
