# XRM — Project Evolution

How a 3-hour hack became a production-ready framework in two weeks.

## Timeline

### Day 1 (Apr 20) — The 3-hour MVP

Started as a quick proof-of-concept: can you build a generic CRM-like platform where
entities, fields, and relationships are all configurable at runtime?

- Requirements document
- Single-project Blazor Server app with EF Core + SQLite
- Entity/field/relationship designers
- Basic record CRUD with dynamic forms
- Demo data seeded on startup

**Result:** A working prototype — define entities, add fields, create records. Ugly but functional.

### Week 1 (Apr 20–27) — Foundation

Rapid iteration to make it real:

- **Service layer refactoring** — extracted `IEntityService`, `IRecordService`, `IRelationshipService`
- **Child records** — inline CRUD on parent detail pages with relationship links
- **Testing** — 33 unit and API tests, CI workflow (GitHub Actions)
- **REST API** — full CRUD endpoints for entities, fields, relationships, and records
- **Documentation** — user guide with screenshots, architecture doc, MIT license
- **Reuse refactor** — split into `Xrm.Core` (models/services) + `Xrm.Blazor` (RCL) + `Xrm.Server` (thin host)

**Result:** 71 tests, clean architecture, ready for consumption by domain projects.

### Week 2 (Apr 28 – May 4) — Feature CRs & Production Hardening

Driven by the ERP project consuming the framework:

- **CR-001** MultiChoice fields
- **CR-002** AutoNumber fields
- **CR-003/020** Computed fields (formulas + aggregates)
- **CR-005** Parent/Child relationship naming
- **CR-006** Domain metadata on entities
- **CR-007** Identity & authorization (domain-scoped RBAC)
- **CR-013** Record lifecycle hooks
- **CR-014** State machine transitions
- **CR-015** Cross-field validation rules
- **CR-016** Audit trail
- **CR-021** Locale-aware formatting
- **CR-022/023** Child section refresh + post-save warning hooks

Plus UX polish:
- Collapsible nav groups, filter/sort on all lists, record navigation (prev/next)
- User identity chip, access denied handling, role-based UI visibility
- Entra ID integration guide for consumers

**Result:** 104 tests, ~8,000 lines of code, 73 commits, production-deployed via ERP project.

## Architecture evolution

```
Week 0:  [Single Blazor project, everything in one place]
            ↓
Week 1:  [Xrm.Core]  ←  [Xrm.Blazor RCL]  ←  [Xrm.Server host]
            ↓                    ↓
Week 2:  [Xrm.Core]  ←  [Xrm.Blazor RCL]  ←  [Any host project]
           services          UI components        (ERP, Sales, etc.)
           models            pages
           API controllers   layouts
           auth layer
```

## Key design decisions

| Decision | Rationale |
|----------|-----------|
| SQLite + EF Core | Zero infrastructure, single-file DB, good enough for most deployments |
| JSON data storage (`DataJson`) | Schema-flexible records without migrations per entity |
| RCL pattern | UI reusable across multiple host apps without duplication |
| `ICurrentUser` abstraction | Provider-agnostic auth — works with Entra, Google, anonymous |
| Domain-scoped roles | Multi-tenant within one deployment, not per-database |
| Service-layer enforcement | Auth checks can't be bypassed by navigating to URLs |
| AnonymousCurrentUser default | Dev inner loop works without any auth setup |

## Stats

| Metric | Value |
|--------|-------|
| Time span | 2 weeks (Apr 20 – May 4, 2026) |
| Commits | 73 |
| Code | ~8,000 lines (C# + Razor) |
| Tests | 104 (unit + API integration) |
| Docs | 10 guides + architecture + backlog |
| Consumers | 2 (ERP, XRM standalone) |
