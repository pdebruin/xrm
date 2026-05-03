namespace Xrm.Core.Models;

/// <summary>
/// Tracks the next sequence value for an AutoNumber field.
/// One row per AutoNumber FieldDefinition.
/// </summary>
public class AutoNumberSequence
{
    public Guid Id { get; set; }
    public Guid FieldDefinitionId { get; set; }
    public int NextValue { get; set; } = 1;

    public FieldDefinition? FieldDefinition { get; set; }
}
