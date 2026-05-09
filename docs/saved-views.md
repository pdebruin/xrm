# Saved Views

Saved views let you define pre-configured filtered lists on an entity. Users pick a view from a dropdown in the record list; filters are applied server-side.

## Defining views

Views are stored as JSON on `EntityDefinition.SavedViewsJson`:

```csharp
entity.SavedViewsJson = """
[
  {
    "Name": "My Work Orders",
    "Filters": [
      { "Field": "AssignedTo", "Operator": "eq", "Value": "{{currentUser}}" }
    ],
    "SortField": "DueDate",
    "SortDir": "asc"
  },
  {
    "Name": "Overdue",
    "Filters": [
      { "Field": "Status", "Operator": "neq", "Value": "Completed" },
      { "Field": "DueDate", "Operator": "lt", "Value": "2025-01-01" }
    ]
  }
]
""";
```

## Filter operators

| Operator | Meaning |
|---|---|
| `eq` | Equals |
| `neq` | Not equals |
| `contains` | Contains substring |
| `gt` / `gte` | Greater than / greater-or-equal (numeric) |
| `lt` / `lte` | Less than / less-or-equal (numeric) |

Multiple filters on a view are combined with AND.

## Tokens

Filter values support runtime tokens that are resolved before querying:

| Token | Resolves to |
|---|---|
| `{{currentUser}}` | Current user's email address |

This enables "My …" views without hardcoding user identities. The same mechanism supports "My team's …" views by filtering on a team field.

## UI

The record list shows a view dropdown when an entity has saved views. Selecting a view applies its filters and sort. The text filter works alongside view filters (both must match).

## Service layer

```csharp
// Views are resolved and passed as ViewFilter list
var filters = new List<ViewFilter>
{
    new() { Field = "Status", Operator = "eq", Value = "Open" }
};
var page = await recordService.GetAllAsync(entityId, viewFilters: filters);
```

The service evaluates filters client-side against `DataJson` values. Token resolution happens in the UI layer before calling the service.
