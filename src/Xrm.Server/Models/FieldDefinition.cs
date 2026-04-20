namespace Xrm.Server.Models;

public enum FieldDataType
{
    Text,
    Number,
    Decimal,
    Boolean,
    Date,
    DateTime,
    Choice,
    MultiChoice,
    RichText,
    Email,
    Phone,
    Url
}

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

    // For Choice/MultiChoice fields
    public string? OptionsJson { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "system";

    public EntityDefinition EntityDefinition { get; set; } = null!;
}
