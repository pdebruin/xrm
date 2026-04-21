# XRM — Extensible Relationship Management Platform

## Vision

A meta-CRM platform that lets users define their own entities, fields, and
relationships — then perform CRUD operations on records of those entities at
runtime. Instead of shipping a fixed "Contacts + Deals" schema, XRM ships an
**empty canvas** where any domain can be modeled: customer management, sports
teams, inventory, project tracking, etc.

## Guiding Principles

- **Fixed meta-schema** — the database schema never changes when users define
  new entities or fields. All user-defined structure is stored as metadata rows.
- **Logical relationships** — relationships between records are metadata, not
  database foreign keys. The platform resolves them at query time.
- **Self-hosted & simple** — runs on a single machine for a small team. No
  multi-tenancy, no cloud dependencies. A single SQLite file holds everything.
- **Auth-ready, not auth-included** — no built-in user store. The architecture
  must allow plugging in an external identity provider (Entra ID / OIDC) later.
- **API-first with a UI** — a REST API for external consumers (integrations,
  scripts). The Blazor UI injects application services directly for performance
  and simplicity (no HTTP roundtrip to self). Both share the same service layer.

---

## Tech Stack

| Layer        | Choice                              |
|------------- |-------------------------------------|
| Backend      | C# / .NET 10 (ASP.NET Core)          |
| Frontend     | Blazor Server (interactive)           |
| Database     | SQLite via EF Core                   |
| Data model   | EAV-style with JSON value storage    |
| Auth         | None initially; OIDC-ready           |

---

## Core Concepts

### 1. Entity Definitions (Design Time)

An **Entity Definition** is a user-created template that describes a type of
thing (e.g., "Contact", "Company", "Player", "Match").

Each entity definition has:
- A unique name and optional display name / plural name
- An optional description
- An icon (optional, for UI)

### 2. Field Definitions (Design Time)

A **Field Definition** belongs to an entity definition and describes one
attribute (e.g., "First Name", "Email", "Score").

Field properties:
- Name, display name
- Data type: `Text`, `Number`, `Decimal`, `Boolean`, `Date`, `DateTime`,
  `Choice` (single-select), `MultiChoice`, `RichText`, `Email`, `Phone`, `URL`
- Required flag
- Default value (optional)
- Constraints: max length, min/max numeric value, regex pattern
- Sort order (for UI display)
- For Choice/MultiChoice: a set of allowed option values

### 3. Relationship Definitions (Design Time)

A **Relationship Definition** links two entity definitions.

Properties:
- Source entity definition
- Target entity definition
- Relationship type: `OneToMany`, `ManyToOne`, `ManyToMany`
- Name / display name (e.g., "Company → Contacts")
- Cascade behavior on delete: `None`, `RemoveLink`, `Cascade`

Relationships are **logical** — stored as metadata + link records, not as
database foreign keys.

### 4. Records (Runtime)

A **Record** is an instance of an entity definition. Records store their field
values in an EAV-style table where each row holds one field value (or in a
JSON column — TBD during design).

### 5. Record Links (Runtime)

A **Record Link** connects two records according to a relationship definition.
It is a simple join row: `(relationship_id, source_record_id, target_record_id)`.

---

## Functional Requirements

### FR-1: Schema Designer (Maintenance Section)

| ID     | Requirement |
|--------|-------------|
| FR-1.1 | CRUD entity definitions |
| FR-1.2 | CRUD field definitions for an entity |
| FR-1.3 | CRUD relationship definitions between entities |
| FR-1.4 | Validate that deleting an entity warns about dependent relationships and existing records |
| FR-1.5 | Validate that changing a field type is only allowed if no records exist or values are compatible |
| FR-1.6 | Provide a visual overview of all entities and their relationships |
| FR-1.7 | Allow the user to set a "home entity" — the entity whose record list is shown on the start-up screen |

### FR-2: Record Management (Runtime Section)

| ID      | Requirement |
|---------|-------------|
| FR-2.1  | **Side navigation** lists all entity definitions, allowing the user to switch between entities |
| FR-2.2  | **Master screen**: list records of a given entity in a table/grid with pagination |
| FR-2.3  | Master screen supports column-level sorting and filtering |
| FR-2.4  | Master screen supports multi-select of records (for bulk delete, export, etc.) |
| FR-2.5  | **Detail screen**: single layout for both create and edit modes; auto-generated ID is hidden from the user |
| FR-2.6  | Detail screen shows related records (grouped by relationship) with navigation to their detail screens |
| FR-2.7  | On the detail screen, parent (ManyToOne) relationships are rendered as combo/dropdown lists for selection |
| FR-2.8  | Delete a record (respecting cascade rules on relationships) |
| FR-2.9  | Link / unlink records according to relationship definitions |
| FR-2.10 | Search records across entities (global search) |

### FR-3: API

| ID     | Requirement |
|--------|-------------|
| FR-3.1 | REST API for all schema designer operations (entities, fields, relationships) |
| FR-3.2 | REST API for all record operations (CRUD, linking, searching) |
| FR-3.3 | Consistent error responses with problem details (RFC 7807) |
| FR-3.4 | OpenAPI / Swagger documentation auto-generated |

### FR-4: Import / Export

| ID     | Requirement |
|--------|-------------|
| FR-4.1 | Export entity definitions (schema) as JSON |
| FR-4.2 | Import entity definitions from JSON (to bootstrap a new instance) |
| FR-4.3 | Export records of an entity as JSON; CSV export is nice-to-have |
| FR-4.4 | Import records from JSON; CSV import is nice-to-have |

---

### FR-5: Demo / Seed Data

| ID     | Requirement |
|--------|-------------|
| FR-5.1 | On first start the environment is empty (no entities or records) |
| FR-5.2 | A one-click "Load demo" action seeds a CRM-style sample schema: Companies, Contacts, Activities, Orders, Order Lines — each with common fields, relationships, and a handful of sample records |
| FR-5.3 | The demo data can be deleted or modified like any user-created content |

---

## Non-Functional Requirements

| ID      | Requirement |
|---------|-------------|
| NFR-1   | Single SQLite database file — easy backup (copy the file) |
| NFR-2   | Startup time < 2 seconds |
| NFR-3   | Handle up to ~100 entity definitions and ~100k records comfortably |
| NFR-4   | Record management UI is responsive (desktop, tablet, mobile). Schema designer UI targets desktop/tablet only. |
| NFR-5   | No external service dependencies at runtime |
| NFR-6   | Supported browsers: Edge, Chrome, Firefox (latest stable versions) |
| NFR-7   | Auth middleware placeholder — requests carry a user context that defaults to "system" and can be replaced by OIDC middleware later |
| NFR-8   | Audit trail: created-by, created-at, modified-by, modified-at on all records |

---

## Design & UI Notes

- Use the **frontend-design** Copilot skill when building the UI — generates distinctive, production-grade interfaces that avoid generic AI aesthetics.
- Color palette sourced from [color.adobe.com](https://color.adobe.com) — "Biophilic" green/beige theme:

| Role            | Hex       | Description        |
|-----------------|-----------|--------------------|
| Primary dark    | `#045929` | Deep forest green  |
| Primary darker  | `#03401D` | Dark green         |
| Primary         | `#5C8C4F` | Leaf green         |
| Secondary       | `#93A680` | Muted sage         |
| Background/Neutral | `#D9CBBF` | Warm beige      |
- Component library: TBD (candidates: MudBlazor, Fluent UI Blazor, Radzen).

---

## Out of Scope (for now)

- Multi-tenancy
- Built-in authentication / user management
- Workflow / automation engine
- Custom views / dashboards
- File/attachment fields
- Calculated / formula fields
- Role-based access control per entity

---

## Open Questions

1. **Blazor Server vs. Blazor WASM?** Server is simpler (no separate hosting),
   WASM gives offline potential. Leaning Server for simplicity.
2. **EAV rows vs. JSON column for field values?** EAV is more queryable; JSON
   is simpler to read/write. Could start with JSON and add EAV indexes later.
3. **Versioning of schema changes?** Should we track a history of entity/field
   definition changes, or is current-state-only enough for v1?

---

## Future: Well-Architected Review

Perform a well-architected review once the core is functional, covering:
- Security (input validation, injection, auth surface)
- Reliability (error handling, data integrity, backups)
- Performance (query efficiency, indexing, caching)
- Cost (resource usage, hosting footprint)
- Operational excellence (logging, monitoring, deployment)
