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
  "field": "IBAN",
  "whenField": "PaymentMethod",
  "operator": "eq",
  "value": "DirectDebit",
  "message": "IBAN is required for direct debit payments"
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
    Name = "Contract",
    DisplayName = "Contract",
    ValidationRulesJson = """
    [
      {"type":"compare","field":"EndDate","operator":"gt","otherField":"StartDate","message":"End date must be after start date"},
      {"type":"compare","field":"NewPrice","operator":"gte","otherField":"OldPrice","message":"New price must not be lower than current price"}
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
{"type":"compare","field":"ActualHours","operator":"lte","otherField":"BudgetHours","message":"Actual hours must not exceed budget"}
```

### Conditional field: only required for specific type
```json
{"type":"required_if","field":"IBAN","whenField":"PaymentMethod","operator":"eq","value":"DirectDebit","message":"IBAN is required for direct debit payments"}
```
