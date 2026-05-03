namespace Xrm.Core.Models;

/// <summary>
/// Records a single mutation (create/update/delete) to a Record for audit purposes.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; set; }
    public Guid EntityDefinitionId { get; set; }
    public Guid RecordId { get; set; }

    /// <summary>Created, Updated, or Deleted.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>User who performed the action. Defaults to "system" until auth is implemented.</summary>
    public string UserId { get; set; } = "system";

    public DateTime Timestamp { get; set; }

    /// <summary>Record data before the change (null for Created).</summary>
    public string? OldDataJson { get; set; }

    /// <summary>Record data after the change (null for Deleted).</summary>
    public string? NewDataJson { get; set; }
}
