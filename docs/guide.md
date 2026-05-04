# XRM — User Guide

XRM is an extensible relationship management platform. Instead of a fixed CRM schema,
you define your own entities, fields, and relationships — then manage records through
a generated UI.

---

## Getting Started

When you open XRM, the demo data seeder automatically loads a CRM-style schema with
sample records on first run.

> The screenshots in this guide show the built-in demo data: Companies, Contacts,
> Products, Activities, Orders, and Order Lines. Your own schemas will look different
> but work the same way.

### Database

Everything is stored in a single `xrm.db` SQLite file. To back up, copy the file.
To start fresh, delete it and restart the app.

> **Note:** Code changes (bug fixes, new features) do not affect the database.
> Only EF model changes (adding columns, changing relationships) require either
> deleting `xrm.db` or running EF migrations.

---

## Part 1: Schema Designer (Design Time)

The Schema Designer is where you define the structure of your data. Access it via
the **Entities** and **Relationships** links in the side navigation.

### Entities

![Schema Designer](Screenshot%202026-04-21%20091303.png)

An entity is a type of thing you want to track — Companies, Contacts, Products,
Players, Matches, or anything else. Each entity gets its own record list and
detail form in the runtime UI.

To create an entity:
1. Click **+ New Entity**
2. Enter a **Name** (identifier, e.g. `Company`), **Display Name** (e.g. `Company`),
   and **Plural Name** (e.g. `Companies`)
3. Click **Save**

The entity immediately appears in the side navigation. You can mark one entity as
**Home** — its record list will show on the start-up screen.

### Fields

Each entity has fields that define what data it holds. Click **Fields** on an entity
card to manage them.

Field properties:
- **Name** — identifier used in the JSON data store
- **Display Name** — label shown in the UI
- **Data Type** — Text, Number, Decimal, Boolean, Date, DateTime, Choice, MultiChoice, AutoNumber, RichText, Email, Phone, URL
- **Required** — marked with a red asterisk (*) in the form; validated on save
- **Constraints** — max length, min/max value, regex pattern
- **Options** — for Choice/MultiChoice fields, a list of allowed values (e.g. `["Low","Medium","High"]`)
- **Sort Order** — controls the display order of fields in the form and grid

Fields are enforced at runtime: required fields must have a value, text respects
max length, numbers respect min/max, and choice fields only accept defined options.

### Relationships

![Relationships](Screenshot%202026-04-21%20091308.png)

Relationships link two entities together. They are logical (metadata + link records),
not database foreign keys.

To create a relationship:
1. Click **+ New Relationship** on the Relationships page
2. Select a **Parent Entity** (the "one" side) and **Child Entity** (the "many" side)
3. Choose a **Name** (e.g. `Company → Contacts`)
4. Currently supported: **OneToMany** relationship type, with **None** or **RemoveLink** on delete

In the runtime UI, relationships appear as:
- A **dropdown** on the child's detail form (to select the parent)
- A **child grid** on the parent's detail form (to see/manage children)

---

## Part 2: Record Management (Runtime)

### Side Navigation

All entities are listed in the left sidebar, grouped by domain with collapsible
headings. Click one to see its record list.

Administrators also see a **Settings** group at the bottom with links to:
- **Entities** — schema designer
- **Relationships** — relationship designer
- **Security** — user and role management

### Master Screen — Record Grid

![Activity List](Screenshot%202026-04-21%20091220.png)

The record grid shows all records of a given entity in a table. Features:

- **Filter** — type in the filter box to search across all field values
- **Sort** — click a column header to sort ascending; click again for descending
- **Pagination** — navigate pages at the bottom; shows total record count
- **Multi-select** — check records for bulk operations
- **Record navigation** — prev/next buttons on the detail page carry your filter/sort context
- **Delete** — click ✕ to delete individual records (writers only)
- **New** — click **+ New** to create a record (writers only)

> **Note:** Users with Reader role see the grid in read-only mode — no New, Edit, or Delete actions.

### Detail Screen — Create & Edit

![Company Detail](Screenshot%202026-04-21%20091241.png)

A single form is used for both creating and editing records. Fields are rendered
based on their data type:

| Data Type | UI Control |
|-----------|------------|
| Text, Email, Phone, URL | Text input |
| Number, Decimal | Number input |
| Boolean | Checkbox |
| Date, DateTime | Date/time picker |
| Choice | Dropdown with defined options |
| MultiChoice | Checkbox group (multiple selections stored as JSON array) |
| AutoNumber | Read-only auto-generated value (see [autonumber.md](autonumber.md)) |
| RichText | Textarea |

Required fields are marked with a red asterisk (*) and validated on save.

### Parent Relationships

![Contact Detail](Screenshot%202026-04-21%20091234.png)

When a record has a parent relationship (e.g. a Contact belongs to a Company),
a **dropdown** appears at the bottom of the form showing the parent entity's records.
Select a parent and click **Save** to link them.

The **↗ button** next to the dropdown navigates directly to the parent record.

### Child Records

![Company with Children](Screenshot%202026-04-21%20091241.png)

Below the parent's form, child records are displayed in a full grid. In the example
above, Contoso Ltd shows its linked Contacts and Orders.

Actions available on the child grid:
- **+ Add** — opens an inline form to create a new child record (automatically linked to the parent)
- **✎ Edit** — opens the inline form pre-populated with existing data
- **✕ Delete** — removes the child record and its link

All child operations happen inline — no need to navigate away from the parent.

### Editing an Activity

![Activity Detail](Screenshot%202026-04-21%20091229.png)

This example shows editing an Activity record. Note the "Contact → Activities"
parent dropdown showing "Donna" as the linked contact, with the ↗ navigation button.

---

## REST API

XRM exposes a REST API for external integrations. When running in development mode,
Swagger UI is available at **/swagger**.

Key endpoints:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/entities` | List all entities |
| POST | `/api/entities` | Create an entity |
| GET | `/api/entities/{id}` | Get entity with fields |
| PUT | `/api/entities/{id}` | Update an entity |
| DELETE | `/api/entities/{id}` | Delete an entity |
| GET | `/api/entities/{id}/fields` | List fields |
| POST | `/api/entities/{id}/fields` | Create a field |
| GET | `/api/entities/{id}/records` | List records (paginated) |
| POST | `/api/entities/{id}/records` | Create a record |
| PUT | `/api/entities/{id}/records/{rid}` | Update a record |
| DELETE | `/api/entities/{id}/records/{rid}` | Delete a record |

Query parameters for record listing: `page`, `pageSize`, `sortField`, `sortDir`, `filter`.

---

## Demo Data

The app ships with a CRM-style demo schema inspired by AdventureWorksLT:

| Entity | Sample Records |
|--------|---------------|
| Company | Contoso Ltd, Fabricam Inc, Adventure Works, Northwind Traders, Litware Corp |
| Contact | 7 contacts across companies |
| Product | Mountain-100, Road-250, Touring-1000, HL Headset, Sport Helmet, Chain |
| Activity | 3 activities (email, meeting, call) |
| Order | 3 orders linked to companies |
| Order Line | 5 order lines linking orders to products |

Demo data can be modified or deleted like any user-created content.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | C# / .NET 10 (ASP.NET Core) |
| Frontend | Blazor Server (interactive SSR) |
| Database | SQLite via EF Core |
| Data model | Fixed meta-schema with JSON field values |
| Tests | xUnit + WebApplicationFactory (104 tests) |
| API docs | Swagger / OpenAPI |

---

## Domain Grouping

Entities can be assigned a **Domain** (e.g. "Sales", "Catalog", "Orders") in the
Entity Designer. The side navigation groups entities by domain with collapsible
headings. Entities without a domain appear ungrouped.

Set **Domain Sort Order** to control the order of groups in the nav menu.

---

## Audit Trail

All record changes (create, update, delete) are automatically logged. On the record
detail screen, expand the **History** section to see a timeline of changes with:
- Timestamp and action type
- Who made the change (user ID)
- Field-level before → after values for updates

No configuration needed — audit is always on.

---

## Lifecycle Hooks

Consumer applications can hook into record create/update/delete events to run
business logic. See [lifecycle-hooks.md](lifecycle-hooks.md) for the full guide.

Post-save hooks return warnings (not failures) via `SaveResult`. See [lifecycle-hooks.md](lifecycle-hooks.md#saveresult-and-warnings) for details.

## State Machine Transitions

Choice fields can optionally define allowed state transitions via `TransitionsJson` on `FieldDefinition`. The UI filters the dropdown and the server rejects invalid transitions on save. See [state-machines.md](state-machines.md) for configuration examples.

## Cross-Field Validation

Entities can define declarative rules that validate relationships between fields (e.g., end date > start date, conditional required fields). See [cross-field-validation.md](cross-field-validation.md) for rule types and examples.

## Computed Fields

Fields of type `Computed` evaluate an expression on every read. Supports arithmetic (`UnitPrice + ShippingCost`), literals (`Subtotal * 1.21`), and aggregates (`COUNT(Order)`, `SUM(OrderLine.Amount)`). See [computed-fields.md](computed-fields.md) for syntax and examples.

## Locale-Aware Formatting

Date, number, and boolean values are formatted for display using `CultureInfo.CurrentCulture`. The host controls the locale — XRM respects it automatically. See [locale-formatting.md](locale-formatting.md) for setup options.

## Audit Trail REST API

In addition to the UI history panel, audit entries are available via the REST API:

```
GET /api/entities/{entityId}/records/{id}/history?limit=50
```

Returns the change log with timestamps, old/new values, and action type.

## Authorization

XRM provides domain-based access control. Entities are scoped to domains; users get Reader/Writer/SystemAdmin roles per domain. See [docs/authorization.md](authorization.md) for full setup.

Key points:
- Call `AddXrmAuthorization()` after `AddXrmCore()` to enable
- Configure any OIDC provider (Entra ID, Google, etc.) — XRM is provider-agnostic
- Admin UI at `/admin/users` for role management
- Without `AddXrmAuthorization()`, all access is permitted (dev mode)
