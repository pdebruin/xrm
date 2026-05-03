# Cross-Field Validation Rules

XRM supports declarative cross-field validation rules on entities via `ValidationRulesJson` on `EntityDefinition`. Rules are evaluated on every create and update.

## Rule Types

### `compare` — Field vs Field comparison

Compares two field values using an operator. Works with dates, numbers, and strings.

```json
{
  "type": "compare",
  "field": "EndDate",
  "operator": "gt",
  "otherField": "StartDate",
  "message": "End date must be after start date"
}
```

### `required_if` — Conditional required

Makes a field required when another field meets a condition.

```json
{
  "type": "required_if",
  "field": "VveNummer",
  "whenField": "VveLidmaatschap",
  "operator": "neq",
  "value": "Nee",
  "message": "VvE-nummer is required when VvE-lidmaatschap is not Nee"
}
```

## Operators

| Operator | Meaning |
|----------|---------|
| `gt` | Greater than |
| `gte` | Greater than or equal |
| `lt` | Less than |
| `lte` | Less than or equal |
| `eq` | Equal |
| `neq` | Not equal |

For `compare` rules, all operators are supported. For `required_if` rules, only `eq` and `neq` are used (to evaluate the condition).

## Consumer Setup

### In a seeder

```csharp
var entity = await entityService.CreateAsync(new EntityDefinition
{
    Name = "Huurcontract",
    DisplayName = "Huurcontract",
    ValidationRulesJson = """
    [
      {"type":"compare","field":"EindDatum","operator":"gt","otherField":"IngangsDatum","message":"Einddatum moet na ingangsdatum liggen"},
      {"type":"compare","field":"HuurprijsNieuw","operator":"gte","otherField":"HuurprijsOud","message":"Nieuwe huurprijs mag niet lager zijn dan huidige"}
    ]
    """
});
```

### Via the API

```http
PUT /api/entities/{entityId}
Content-Type: application/json

{
  "validationRulesJson": "[{\"type\":\"compare\",\"field\":\"EndDate\",\"operator\":\"gt\",\"otherField\":\"StartDate\",\"message\":\"End date must be after start date\"}]"
}
```

## Behavior

- Rules are evaluated **after** per-field validation (required, max length, pattern, choice options, transitions).
- If a compared field is null/empty, the rule is **skipped** (no error). Use `IsRequired` on the field if it must always have a value.
- Multiple rules can be defined — all are evaluated, and all violations are returned together.
- Errors are returned as `InvalidOperationException` with messages joined by `;`.

## Comparison Logic

- **Dates**: Parsed with `DateTime.TryParse` — use ISO format in JSON (`"2026-01-15"`).
- **Numbers**: Parsed with invariant culture — use dot decimal (`1234.56`).
- **Strings**: Compared ordinally (alphabetical) as a fallback.

## Examples

### Maintenance workflow: actual ≤ budget
```json
{"type":"compare","field":"ActueleUren","operator":"lte","otherField":"BudgetUren","message":"Actuele uren mogen budget niet overschrijden"}
```

### Conditional field: only required for specific type
```json
{"type":"required_if","field":"IBAN","whenField":"BetalingsMethode","operator":"eq","value":"Automatisch","message":"IBAN is verplicht bij automatische incasso"}
```
