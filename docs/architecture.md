# XRM — Architecture & Technology

## Technology Stack

| Layer | Technology | License | Purpose |
|-------|-----------|---------|---------|
| Runtime | .NET 10 | MIT | Application framework |
| UI | Blazor Server | MIT | Interactive server-rendered UI |
| ORM | EF Core 10 | MIT | Database access with LINQ |
| Database | SQLite | Public domain | Single-file embedded database |
| API docs | Swashbuckle | Apache 2.0 | Swagger/OpenAPI generation |
| Tests | xUnit | Apache 2.0 | Unit and integration testing |
| Test host | WebApplicationFactory | MIT | In-process API testing |

## Architecture

### Overview

XRM uses a **service layer architecture** where Blazor pages and REST controllers
share the same business logic:

```
┌─────────────┐    ┌──────────────┐
│  Blazor UI  │    │  REST API    │
│  (Pages)    │    │  (Controllers)│
└──────┬──────┘    └──────┬───────┘
       │                  │
       │  inject          │  inject
       ▼                  ▼
┌─────────────────────────────────┐
│         Service Layer           │
│  IEntityService, IFieldService  │
│  IRelationshipService           │
│  IRecordService                 │
└──────────────┬──────────────────┘
               │
               │  IDbContextFactory
               ▼
┌─────────────────────────────────┐
│     EF Core + SQLite            │
│     (xrm.db)                    │
└─────────────────────────────────┘
```

### Key Decisions

**Blazor Server (not WASM)**
- Simpler deployment: single process, no separate API hosting
- Services injected directly into pages — no HTTP round-trip to self
- Trade-off: requires persistent SignalR connection (fine for self-hosted/small team)

**IDbContextFactory (not scoped DbContext)**
- Blazor Server circuits live for the entire user session (minutes to hours)
- A scoped DbContext would accumulate tracked entities and go stale
- Factory creates short-lived contexts per service operation, then disposes them

**JSON field values (not EAV rows)**
- Record data stored as a JSON string in a single `DataJson` column
- Simpler to read/write than entity-attribute-value rows
- Trade-off: sorting by field values requires client-side materialization
- SQLite JSON functions could be used for server-side queries in the future

**Logical relationships (not foreign keys)**
- Relationships are metadata rows + a `RecordLinks` join table
- No database schema changes when users create/modify relationships
- Enforced at the service layer: link validation checks entity membership

**Service layer (not controllers-only)**
- Controllers are thin wrappers for external REST consumers
- Blazor pages inject services directly — in-process, common pattern, fast (no HTTP overhead)
- An alternative would be to have Blazor consume the REST API ("dogfooding"), which ensures
  one code path and catches API issues early. We chose direct injection because Blazor Server
  runs in the same process, making HTTP calls to itself unnecessary overhead.
- Single source of truth for validation and business rules

### Data Model

```
EntityDefinition ──< FieldDefinition
       │
       │ (source/target)
       ▼
RelationshipDefinition
       │
       │ (definition)
       ▼
RecordLink (source_record ──── target_record)
       │
       │
Record ──── DataJson (field values as JSON)
```

- **EntityDefinition** — name, display name, icon, home flag
- **FieldDefinition** — name, type, required, constraints, sort order
- **RelationshipDefinition** — source entity → target entity, type, cascade behavior
- **Record** — entity reference + JSON blob of field values + audit fields
- **RecordLink** — join table connecting two records via a relationship

### Validation

Field metadata is enforced at runtime when creating/updating records:
- Required fields must have a non-empty value
- Text fields respect `MaxLength`
- Numeric fields respect `MinValue`/`MaxValue`
- Choice fields only accept defined options
- Pattern fields are validated with regex

Record links are validated against the relationship model:
- Relationship must exist
- Source record must belong to the relationship's source entity
- Target record must belong to the relationship's target entity

### Database Lifecycle

- `EnsureCreatedAsync()` creates the DB on first run — no migrations needed
- Code changes (bug fixes, features) do not affect the existing database
- EF model changes (new columns, changed relationships) require either:
  - Deleting `xrm.db` and restarting (loses data), or
  - Adding EF migrations (`dotnet ef migrations add ...`)
- Backup: copy the `xrm.db` file

### What's Not Yet Implemented

| Feature | Status |
|---------|--------|
| ManyToMany relationships | Enum defined, hidden in UI |
| Cascade delete | Enum defined, hidden in UI |
| Global search (FR-2.10) | Not started |
| Import/export (FR-4) | Not started |
| Authentication (OIDC) | Architecture ready, not wired |
| Mobile-responsive record views | Not started |
| Consistent error handling (4xx) | Partial |
| Well-architected review | Planned |

See [requirements.md](../requirements.md) for the full list and
[tests.md](../tests.md) for test coverage mapping.
