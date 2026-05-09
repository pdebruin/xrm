# Soft Delete (Deactivate / Reactivate)

Records are never hard-deleted from the database. Instead, `DeleteAsync` sets `IsActive = false`. Links and audit history are preserved, and records can be reactivated.

## Behavior

| Action | What happens |
|---|---|
| Delete / Deactivate | `Record.IsActive` set to `false`, audit logs "Deactivated" |
| Reactivate | `Record.IsActive` set to `true`, audit logs "Reactivated" |
| Links | Preserved on deactivate — restored automatically on reactivate |

## Service API

```csharp
// Deactivate a record (replaces hard delete)
await recordService.DeleteAsync(entityId, recordId);

// Reactivate a deactivated record
await recordService.ReactivateAsync(entityId, recordId);

// Query by active status: true (default), false, or null (all)
var active = await recordService.GetAllAsync(entityId, isActive: true);
var inactive = await recordService.GetAllAsync(entityId, isActive: false);
var all = await recordService.GetAllAsync(entityId, isActive: null);
```

## REST API

```
DELETE /api/entities/{entityId}/records/{id}        → deactivates
POST   /api/entities/{entityId}/records/{id}/reactivate → reactivates
GET    /api/entities/{entityId}/records?isActive=true|false  (default: true, omit for all)
```

## UI

- **Record list**: Active / All / Deactivated dropdown next to the filter box. Deactivated records show a ↺ reactivate button instead of ✕.
- **Record detail**: Deactivated records display a banner with a Reactivate button. Editing is blocked while deactivated.

## Schema upgrade

Existing databases are upgraded automatically on startup — `ApplySchemaUpgradesAsync()` adds the `IsActive` column (default `1`) if missing. All existing records are treated as active.
