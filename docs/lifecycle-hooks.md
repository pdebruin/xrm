# Record Lifecycle Hooks

XRM provides extension points that let consumers run business logic before or after record changes. Hooks are defined in your host project — XRM only provides the interface and invocation.

## Interface

```csharp
public interface IRecordLifecycleHandler
{
    // Before create — can modify dataJson
    Task<string> OnCreatingAsync(Guid entityId, string dataJson, EntityDefinition entity, CancellationToken ct = default);

    // After create is saved
    Task OnCreatedAsync(Record record, EntityDefinition entity, CancellationToken ct = default);

    // Before update — can modify newDataJson
    Task<string> OnUpdatingAsync(Record record, string newDataJson, EntityDefinition entity, CancellationToken ct = default);

    // After update is saved — receives old data for comparison
    Task OnUpdatedAsync(Record record, string oldDataJson, EntityDefinition entity, CancellationToken ct = default);

    // Before delete
    Task OnDeletingAsync(Record record, EntityDefinition entity, CancellationToken ct = default);
}
```

All methods have default no-op implementations. Only override the ones you need.

## Registering a handler

In your host's `Program.cs` (or service registration):

```csharp
builder.Services.AddTransient<IRecordLifecycleHandler, MyHandler>();
```

Multiple handlers can be registered — all will fire (order not guaranteed).

## Example: Auto-create a related record on status change

```csharp
using System.Text.Json;
using Xrm.Core.Models;
using Xrm.Core.Services;

public class WerkorderCreator : IRecordLifecycleHandler
{
    private readonly IRecordService _records;
    private readonly IEntityService _entities;

    public WerkorderCreator(IRecordService records, IEntityService entities)
    {
        _records = records;
        _entities = entities;
    }

    public async Task OnUpdatedAsync(Record record, string oldDataJson, EntityDefinition entity, CancellationToken ct = default)
    {
        if (entity.Name != "Melding") return;

        using var oldDoc = JsonDocument.Parse(oldDataJson);
        using var newDoc = JsonDocument.Parse(record.DataJson);

        var oldStatus = oldDoc.RootElement.TryGetProperty("Status", out var os) ? os.GetString() : null;
        var newStatus = newDoc.RootElement.TryGetProperty("Status", out var ns) ? ns.GetString() : null;

        if (oldStatus != "Opdracht" && newStatus == "Opdracht")
        {
            // Find the Werkorder entity
            var entities = await _entities.GetAllAsync();
            var werkorder = entities.FirstOrDefault(e => e.Name == "Werkorder");
            if (werkorder is null) return;

            var data = JsonSerializer.Serialize(new
            {
                MeldingId = record.Id.ToString(),
                Status = "Nieuw",
                Omschrijving = newDoc.RootElement.TryGetProperty("Omschrijving", out var o) ? o.GetString() : ""
            });

            await _records.CreateAsync(werkorder.Id, data);
        }
    }
}
```

## Example: Enrich data on create (pre-save hook)

```csharp
public class DefaultFieldInjector : IRecordLifecycleHandler
{
    public Task<string> OnCreatingAsync(Guid entityId, string dataJson, EntityDefinition entity, CancellationToken ct = default)
    {
        // Add a CreatedBy field to all records
        var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(dataJson)!;
        obj["CreatedBy"] = "current-user";
        return Task.FromResult(JsonSerializer.Serialize(obj));
    }
}
```

## Hook execution order

1. Validation runs first (required fields, multichoice values)
2. AutoNumber generation
3. `OnCreating` / `OnUpdating` (pre-save — can modify data)
4. Database save + audit entry
5. `OnCreated` / `OnUpdated` / (post-save — for side effects)

For deletes: `OnDeleting` runs before the record is removed.

## Design notes

- Handlers receive the `EntityDefinition` so they can filter by entity name/domain
- Pre-save hooks return the (possibly modified) `dataJson`; post-save hooks are void
- If a handler throws, the operation fails — no partial commit
- Handlers are resolved via DI, so they can inject other services
- XRM contains zero business logic — all domain rules live in your handlers
