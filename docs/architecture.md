# XRM — Architecture & Implementation Guide

This document covers the technical design of XRM and serves as the entry point
for developers building on or contributing to the framework.

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| Runtime | .NET 10 | Application framework |
| UI | Blazor Server (RCL) | Interactive server-rendered UI |
| ORM | EF Core 10 | Database access with LINQ |
| Database | SQLite | Single-file embedded database |
| Auth | Microsoft.Identity.Web (optional) | OIDC authentication |
| API docs | Swashbuckle | Swagger/OpenAPI generation |
| Tests | xUnit + WebApplicationFactory | Unit and API integration testing |
| CI | GitHub Actions | Build and test on push |

## Project Structure

```
src/
  Xrm.Core/              → Models, services, controllers, auth — the reusable framework
  Xrm.Blazor/            → Razor Class Library — UI components, pages, layouts
  Xrm.Server/            → Thin host app (Program.cs + demo data seeder)
tests/
  Xrm.Tests/             → 104 automated tests (unit + API integration)
docs/                     → All documentation
```

### Reuse Pattern

XRM is designed as a framework consumed by domain-specific host projects:

```
[Xrm.Core]  ←  [Xrm.Blazor RCL]  ←  [Your Host Project]
  services        UI components         Program.cs
  models          pages                 domain seeders
  controllers     layouts               auth config
  auth layer
```

A consumer calls `AddXrmCore(connectionString)` and references the Blazor RCL.
See [reuse-approach.md](reuse-approach.md) for the full pattern.

## Architecture

### Service Layer

Blazor pages and REST controllers share the same business logic:

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
│  IEntityService  IRecordService │
│  IRelationshipService           │
│  IAuditService   ICurrentUser   │
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
- Single process, no separate API hosting needed
- Services injected directly into pages — no HTTP round-trip
- Trade-off: requires persistent SignalR connection (fine for self-hosted/small team)

**IDbContextFactory (not scoped DbContext)**
- Blazor Server circuits live for the entire user session
- Factory creates short-lived contexts per operation, then disposes them

**JSON field values (not EAV rows)**
- Record data stored as `DataJson` column
- Simpler than entity-attribute-value rows
- Trade-off: sorting requires client-side materialization (SQLite JSON functions could improve this)

**Logical relationships (not foreign keys)**
- Relationships are metadata + a `RecordLinks` join table
- No schema changes when users create/modify relationships

**AnonymousCurrentUser by default**
- Dev inner loop works without auth configuration
- Consumers opt in with `AddXrmAuthorization()` when ready

### Data Model

```
EntityDefinition ──< FieldDefinition
       │
       │ (parent/child)
       ▼
RelationshipDefinition
       │
       ▼
RecordLink (parent_record ──── child_record)

Record ──── DataJson (field values as JSON)

KnownUser ──< DomainAssignment (role + domain)
```

## Feature Documentation

These guides cover individual features in depth — both for framework contributors
and for consumers building on XRM:

### Core Features

| Document | Feature |
|----------|---------|
| [authorization.md](authorization.md) | Identity, RBAC, domain-scoped access control, Entra ID setup |
| [lifecycle-hooks.md](lifecycle-hooks.md) | Pre/post create/update/delete hooks for domain logic |
| [computed-fields.md](computed-fields.md) | Formula and aggregate computed fields |
| [state-machines.md](state-machines.md) | Choice field transition rules |
| [cross-field-validation.md](cross-field-validation.md) | Multi-field validation rules |
| [autonumber.md](autonumber.md) | Auto-incrementing number fields with prefix/format |
| [locale-formatting.md](locale-formatting.md) | Culture-aware display formatting |

### Architecture & Process

| Document | Topic |
|----------|-------|
| [reuse-approach.md](reuse-approach.md) | How to consume XRM as a framework |
| [evolution.md](evolution.md) | Project history — from MVP to production |
| [backlog.md](backlog.md) | Planned features and improvements |

## Validation

Field metadata is enforced at runtime when creating/updating records:
- Required fields must have a non-empty value
- Text fields respect `MaxLength` and `MinLength`
- Numeric fields respect `MinValue`/`MaxValue`
- Choice fields only accept defined options
- Pattern fields are validated with regex
- Cross-field rules evaluated after individual field validation
- State machine transitions enforce allowed paths

## Database Lifecycle

- `EnsureCreatedAsync()` creates the DB on first run — no migrations needed
- EF model changes require deleting `xrm.db` and restarting, or adding migrations
- Backup: copy the `xrm.db` file

## What's Not Yet Implemented

| Feature | Status |
|---------|--------|
| CSV import/export | Planned — API exists for JSON |
| Saved/filtered views | Foundation in place (CR-024) |
| ManyToMany relationships | Enum defined, hidden in UI |
| Global search | Not started |
| Mobile-responsive views | Not started |
