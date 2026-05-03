using Xrm.Core.Models;

namespace Xrm.Core.Services;

/// <summary>
/// Extension point for consumer business logic on record changes.
/// Implement in your host and register via DI.
/// </summary>
public interface IRecordLifecycleHandler
{
    /// <summary>Called before a record is saved (create). Can modify dataJson.</summary>
    Task<string> OnCreatingAsync(Guid entityId, string dataJson, EntityDefinition entity, CancellationToken ct = default)
        => Task.FromResult(dataJson);

    /// <summary>Called after a record is created and saved.</summary>
    Task OnCreatedAsync(Record record, EntityDefinition entity, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <summary>Called before a record is updated. Can modify newDataJson.</summary>
    Task<string> OnUpdatingAsync(Record record, string newDataJson, EntityDefinition entity, CancellationToken ct = default)
        => Task.FromResult(newDataJson);

    /// <summary>Called after a record is updated and saved.</summary>
    Task OnUpdatedAsync(Record record, string oldDataJson, EntityDefinition entity, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <summary>Called before a record is deleted.</summary>
    Task OnDeletingAsync(Record record, EntityDefinition entity, CancellationToken ct = default)
        => Task.CompletedTask;
}
