# AutoNumber Fields

AutoNumber fields generate sequential, formatted identifiers automatically when a record is created. They are read-only — users cannot edit the generated value.

## Configuration

AutoNumber fields store their configuration as JSON in the `DefaultValue` property of a `FieldDefinition`:

```json
{"prefix": "ORD", "width": 4}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `prefix` | string | `""` | Text prepended before the number. When non-empty, a `-` separator is added. |
| `width` | int | `4` | Minimum digit count, zero-padded. |

## Format Examples

| Config | Generated sequence |
|--------|-------------------|
| `{"prefix": "ORD", "width": 4}` | ORD-0001, ORD-0002, ORD-0003 |
| `{"prefix": "INV-2025", "width": 4}` | INV-2025-0001, INV-2025-0002 |
| `{"prefix": "GAR", "width": 3}` | GAR-001, GAR-002 |
| `{"prefix": "", "width": 6}` | 000001, 000002 (pure numeric) |
| `{"prefix": "T", "width": 1}` | T-1, T-2, ... T-10, T-11 (overflows width gracefully) |

## Seeder Usage

```csharp
new FieldDefinition
{
    Name = "OrderNumber",
    DisplayName = "Order Number",
    DataType = FieldDataType.AutoNumber,
    DefaultValue = """{"prefix":"ORD","width":4}""",
    SortOrder = 1
}
```

When seeding records directly (bypassing RecordService), also seed the `AutoNumberSequence`:

```csharp
db.AutoNumberSequences.Add(new AutoNumberSequence
{
    Id = Guid.NewGuid(),
    FieldDefinitionId = orderNumberField.Id,
    NextValue = 4  // next value after your seeded records
});
```

## Behavior

- **Generated on create only** — the value is assigned once and never changes on update.
- **Read-only in forms** — the UI shows the value but does not allow editing.
- **Validation skipped** — AutoNumber fields bypass required/pattern/length validation since they are system-generated.
- **Unique per entity** — sequential assignment guarantees uniqueness within the entity.
- **Overflow** — if the number exceeds the configured width, it simply grows (e.g., width=3 with value 1000 → `PRE-1000`).

## UI (FieldDesigner)

When creating/editing a field with type `AutoNumber`, the designer shows:
- **Prefix** — the text prefix (leave empty for pure numeric)
- **Width** — the zero-padded digit count

The `IsRequired` flag is automatically set to `false` for AutoNumber fields since the system always provides a value.
