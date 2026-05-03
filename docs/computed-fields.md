# Computed Fields

XRM supports a `Computed` field type that automatically calculates values from other fields or related records. Computed fields are read-only and evaluated on every read.

## Field Types

### Intra-record formulas (CR-020)

Reference other fields in the same record with arithmetic operators.

```csharp
await fieldService.CreateAsync(entityId, new FieldDefinition
{
    Name = "TotaalMaandhuur",
    DisplayName = "Totaal maandhuur",
    DataType = FieldDataType.Computed,
    Expression = "NettoHuur + ServicekostenVoorschot"
});
```

### Cross-record aggregates (CR-003)

Count or sum child records via relationships.

```csharp
await fieldService.CreateAsync(entityId, new FieldDefinition
{
    Name = "AantalContracten",
    DisplayName = "Aantal contracten",
    DataType = FieldDataType.Computed,
    Expression = "COUNT(Huurcontract)"
});

await fieldService.CreateAsync(entityId, new FieldDefinition
{
    Name = "TotaalHuur",
    DisplayName = "Totale huur",
    DataType = FieldDataType.Computed,
    Expression = "SUM(Huurcontract.Maandhuur)"
});
```

## Expression Syntax

### Operators

| Operator | Example |
|----------|---------|
| `+` | `FieldA + FieldB` |
| `-` | `Price - Discount` |
| `*` | `Netto * 1.21` |
| `/` | `Total / Count` |
| `()` | `(A + B) * C` |

### Aggregate functions

| Function | Syntax | Description |
|----------|--------|-------------|
| `COUNT` | `COUNT(EntityName)` | Count child records linked via relationship |
| `SUM` | `SUM(EntityName.FieldName)` | Sum a numeric field across child records |

### Rules

- Field names are case-sensitive and must match exactly
- Numeric literals use dot decimal: `1.21`, `100`, `0.5`
- Missing or null fields resolve to `0`
- Division by zero returns `0`
- Invalid expressions return no value (field stays empty)

## Behavior

- **Evaluated on read** — never stored in the database
- **Always current** — reflects latest data from record and children
- **Skipped in validation** — cannot be set by user or API
- **Displayed read-only** — rendered with locale formatting (Decimal format)
- **Available in lists** — computed values appear in record grids
- **Available via API** — GET responses include computed values

## Aggregates and Relationships

`COUNT(EntityName)` and `SUM(EntityName.FieldName)` work via XRM's relationship system:
- The computed field must be on the **parent** entity
- The `EntityName` in the expression must match the **child** entity's `Name`
- A relationship must exist linking parent → child
- Only records linked via `RecordLink` are counted/summed

## Examples

### BTW calculation
```
Expression = "Netto * 0.21"
```

### Gross from net
```
Expression = "Netto + BTW"
```
(Where `BTW` is itself a computed field — computed fields can reference other computed fields)

### Complex with occupied unit count
```
Expression = "COUNT(Eenheid)"
```

### Total service charges across tenants
```
Expression = "SUM(Huurcontract.Servicekosten)"
```

### Combined: arithmetic + aggregate
```
Expression = "SUM(Huurcontract.NettoHuur) + SUM(Huurcontract.Servicekosten)"
```

## Via the API

```http
POST /api/entities/{entityId}/fields
Content-Type: application/json

{
  "name": "TotaalMaandhuur",
  "dataType": "Computed",
  "expression": "NettoHuur + ServicekostenVoorschot"
}
```

The computed value appears in GET responses but is ignored in POST/PUT payloads.
