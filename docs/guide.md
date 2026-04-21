# XRM — User Guide

XRM is an extensible relationship management platform. Instead of a fixed CRM schema,
you define your own entities, fields, and relationships — then manage records through
a generated UI.

---

## Getting Started

```bash
cd src/Xrm.Server
dotnet run
```

Open **http://localhost:5186** in your browser. On first run the database is empty —
the demo data seeder automatically loads a CRM-style schema with sample records.

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
- **Data Type** — Text, Number, Decimal, Boolean, Date, DateTime, Choice, RichText, Email, Phone, URL
- **Required** — marked with a red asterisk (*) in the form; validated on save
- **Constraints** — max length, min/max value, regex pattern
- **Options** — for Choice fields, a list of allowed values (e.g. `["Low","Medium","High"]`)
- **Sort Order** — controls the display order of fields in the form and grid

Fields are enforced at runtime: required fields must have a value, text respects
max length, numbers respect min/max, and choice fields only accept defined options.

### Relationships

![Relationships](Screenshot%202026-04-21%20091308.png)

Relationships link two entities together. They are logical (metadata + link records),
not database foreign keys.

To create a relationship:
1. Click **+ New Relationship** on the Relationships page
2. Select a **Source Entity** (the parent / "one" side) and **Target Entity** (the child / "many" side)
3. Choose a **Name** (e.g. `Company → Contacts`)
4. Currently supported: **OneToMany** relationship type, with **None** or **RemoveLink** on delete

In the runtime UI, relationships appear as:
- A **dropdown** on the child's detail form (to select the parent)
- A **child grid** on the parent's detail form (to see/manage children)

---

## Part 2: Record Management (Runtime)

### Side Navigation

All entities are listed in the left sidebar under **ENTITIES**. Click one to see its
record list. The **Entities** and **Relationships** links at the bottom take you to
the schema designer.

### Master Screen — Record Grid

![Activity List](Screenshot%202026-04-21%20091220.png)

The record grid shows all records of a given entity in a table. Features:

- **Filter** — type in the filter box to search across all field values
- **Sort** — click a column header to sort ascending; click again for descending
- **Pagination** — navigate pages at the bottom; shows total record count
- **Multi-select** — check records for bulk operations
- **Delete** — click ✕ to delete individual records
- **New** — click **+ New** to create a record

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
| Tests | xUnit + WebApplicationFactory (40 tests) |
| API docs | Swagger / OpenAPI |
