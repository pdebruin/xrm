# State Machine Transitions

XRM supports optional state machine enforcement on Choice fields via the `TransitionsJson` property on `FieldDefinition`.

## How it works

- **TransitionsJson** is a JSON object where each key is a current state and the value is an array of allowed next states.
- On **create**, any valid option (from `OptionsJson`) is allowed — no transition check.
- On **update**, if `TransitionsJson` is set, the system validates that the transition from current → new value is allowed.
- If a state has **no key** in the transitions object, it is treated as a terminal state (no changes allowed).
- The UI **filters the dropdown** to show only valid next states.

## Consumer setup

### 1. Define the field with transitions

```csharp
await fieldService.CreateAsync(entityId, new FieldDefinition
{
    Name = "Status",
    DisplayName = "Status",
    DataType = FieldDataType.Choice,
    OptionsJson = """["Melding","Opdracht","Uitvoering","Afmelding"]""",
    TransitionsJson = """{"Melding":["Opdracht"],"Opdracht":["Uitvoering"],"Uitvoering":["Afmelding"]}"""
});
```

This defines: Melding → Opdracht → Uitvoering → Afmelding (linear flow). "Afmelding" is terminal (not a key).

### 2. Non-linear example

```csharp
TransitionsJson = """{"New":["Open","Cancelled"],"Open":["Resolved","Cancelled"],"Resolved":["Closed","Open"],"Cancelled":["Open"]}"""
```

This allows reopening from Cancelled and Resolved, while Closed is terminal.

### 3. Via the API

```http
POST /api/entities/{entityId}/fields
Content-Type: application/json

{
  "name": "Status",
  "dataType": "Choice",
  "optionsJson": "[\"Open\",\"InProgress\",\"Done\"]",
  "transitionsJson": "{\"Open\":[\"InProgress\"],\"InProgress\":[\"Done\",\"Open\"]}"
}
```

## Validation behavior

- **Valid transition:** save succeeds normally.
- **Invalid transition:** throws `InvalidOperationException` with message:  
  `'Status' cannot transition from 'New' to 'Closed'. Allowed: Open, Cancelled`
- **Terminal state:** throws with message:  
  `'Status' cannot transition from 'Closed' (terminal state)`

## UI behavior

When `TransitionsJson` is set on a Choice field:
- The dropdown shows only valid next states (plus the current value)
- On new records, all options are shown (no restriction)
- Terminal states show only the current value (dropdown is effectively read-only)

## Notes

- Transitions are **not enforced on create** — the consumer decides the initial state.
- XRM validates transitions but does **not** route work, assign teams, or manage queues. Use [lifecycle hooks](lifecycle-hooks.md) for business logic triggered by state changes.
- To add transitions to an existing Choice field, update the field's `TransitionsJson` property. Existing records are not affected until their next update.
