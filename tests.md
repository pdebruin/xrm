# XRM — Test Validation Document

This document provides manual test cases to validate each requirement.
Status values: ⬜ Not tested | ✅ Pass | ❌ Fail | ⚠️ Partial

---

## FR-1: Schema Designer

### TC-1.1: CRUD Entity Definitions (FR-1.1)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Navigate to Admin > Schema Designer | Entity list is displayed | ⬜ |
| 2 | Click "Add Entity", enter Name="Project", DisplayName="Project", PluralName="Projects" | Form accepts input | ⬜ |
| 3 | Click Save | Entity appears in the list | ⬜ |
| 4 | Click Edit on "Project", change DisplayName to "My Project" | Form shows existing values | ⬜ |
| 5 | Click Save | Updated name shown in list | ⬜ |
| 6 | Click Delete on "Project" | Entity is removed from list | ⬜ |

### TC-1.2: CRUD Field Definitions (FR-1.2)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Create an entity "Task", navigate to its Fields page | Empty field list shown | ⬜ |
| 2 | Add field: Name="Title", Type=Text, Required=true, MaxLength=200 | Field appears in list | ⬜ |
| 3 | Add field: Name="DueDate", Type=Date | Field appears in list | ⬜ |
| 4 | Add field: Name="Priority", Type=Choice, Options=["Low","Medium","High"] | Field appears with options | ⬜ |
| 5 | Edit "Title" field, change MaxLength to 100 | Updated value saved | ⬜ |
| 6 | Delete "DueDate" field | Field removed from list | ⬜ |

### TC-1.3: CRUD Relationship Definitions (FR-1.3)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Create entities "Department" and "Employee" | Both appear in entity list | ⬜ |
| 2 | Navigate to Relationships, add: Source=Department, Target=Employee, Type=OneToMany, Name="Department Employees" | Relationship saved | ⬜ |
| 3 | Edit relationship, change name to "Dept → Staff" | Updated name shown | ⬜ |
| 4 | Delete the relationship | Removed from list | ⬜ |

### TC-1.4: Delete Entity Warnings (FR-1.4)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Create entity "Temp" with a field and a relationship to another entity | Setup complete | ⬜ |
| 2 | Create a record for "Temp" | Record exists | ⬜ |
| 3 | Attempt to delete "Temp" entity | Warning about existing records/relationships shown | ⬜ |

### TC-1.5: Field Type Change Validation (FR-1.5)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Create entity with a Text field, create a record with a value | Record saved | ⬜ |
| 2 | Attempt to change field type from Text to Number | Blocked or warning shown | ⬜ |
| 3 | Delete the record, then change field type | Change allowed | ⬜ |

### TC-1.6: Visual Entity Overview (FR-1.6)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | With demo data loaded, open schema overview | Entities and relationships visible | ⬜ |

### TC-1.7: Home Entity Setting (FR-1.7)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Set "Company" as home entity | Setting saved | ⬜ |
| 2 | Navigate to home/root page | Company record list shown | ⬜ |

---

## FR-2: Record Management

### TC-2.1: Side Navigation (FR-2.1)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Load app with demo data | Side nav shows all entities (Company, Contact, etc.) | ⬜ |
| 2 | Click "Contacts" in side nav | Contacts record list loads | ⬜ |
| 3 | Click "Orders" in side nav | Orders record list loads | ⬜ |

### TC-2.2: Master Screen — Record Grid (FR-2.2)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Navigate to Contacts | Grid shows records with field columns | ⬜ |
| 2 | Verify pagination controls | Page size and navigation shown | ⬜ |
| 3 | Navigate to page 2 (if enough records) | Different records shown | ⬜ |

### TC-2.3: Column Sorting and Filtering (FR-2.3)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Click "LastName" column header | Records sort ascending by last name | ⬜ |
| 2 | Click same header again | Sort toggles to descending | ⬜ |
| 3 | Type in filter box | Grid filters to matching records | ⬜ |

### TC-2.4: Multi-Select (FR-2.4)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Check multiple record checkboxes | Records are selected | ⬜ |
| 2 | Click bulk delete | Selected records are removed | ⬜ |

### TC-2.5: Detail Screen — Create and Edit (FR-2.5)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Click "New" on Contacts grid | Empty form with all contact fields shown | ⬜ |
| 2 | Fill FirstName, LastName, Email, Save | Record created, back to grid | ⬜ |
| 3 | Click existing contact row | Form pre-filled with record data | ⬜ |
| 4 | Change Email, Save | Updated value persisted | ⬜ |
| 5 | Verify ID is not shown on form | No ID field visible | ⬜ |

### TC-2.6: Child Records Inline CRUD (FR-2.6)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Open an Order record | Order Lines grid shown below form | ⬜ |
| 2 | Click "+ Add" on Order Lines | Inline form appears | ⬜ |
| 3 | Fill in product, quantity, price; click Save | New line appears in grid | ⬜ |
| 4 | Click ✎ on an existing order line | Inline form with pre-filled values | ⬜ |
| 5 | Change quantity, Save | Updated value in grid | ⬜ |
| 6 | Click ✕ on an order line | Line removed from grid | ⬜ |

### TC-2.7: Parent Dropdown with Navigation (FR-2.7)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Open a Contact record | "Company" dropdown shown with company names | ⬜ |
| 2 | Select a company from dropdown, Save | Link persisted | ⬜ |
| 3 | Re-open contact, verify ↗ button visible | Button appears next to dropdown | ⬜ |
| 4 | Click ↗ | Navigates to the parent company detail | ⬜ |

### TC-2.8: Delete Record with Cascade (FR-2.8)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Create a parent with child records | Relationship established | ⬜ |
| 2 | Delete the parent record | Child links removed per cascade rule | ⬜ |
| 3 | Verify orphan handling based on cascade setting | Correct behavior | ⬜ |

### TC-2.9: Link / Unlink Records (FR-2.9)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Open Contact, set parent Company via dropdown | Link created | ⬜ |
| 2 | Change dropdown to "-- None --", Save | Link removed | ⬜ |

### TC-2.10: Global Search (FR-2.10)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Enter search term that matches records in multiple entities | Results from all matching entities shown | ⬜ |

---

## FR-3: API

### TC-3.1: Schema API (FR-3.1)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | GET /api/entities | Returns list of entities | ⬜ |
| 2 | POST /api/entities with valid body | Creates entity, returns 201 | ⬜ |
| 3 | PUT /api/entities/{id} | Updates entity | ⬜ |
| 4 | DELETE /api/entities/{id} | Removes entity | ⬜ |
| 5 | CRUD /api/entities/{id}/fields | Fields endpoints work | ⬜ |
| 6 | CRUD /api/entities/{id}/relationships | Relationships endpoints work | ⬜ |

### TC-3.2: Records API (FR-3.2)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | GET /api/entities/{id}/records | Returns paged records | ⬜ |
| 2 | POST /api/entities/{id}/records | Creates record | ⬜ |
| 3 | PUT /api/entities/{id}/records/{rid} | Updates record | ⬜ |
| 4 | DELETE /api/entities/{id}/records/{rid} | Deletes record | ⬜ |
| 5 | POST link, DELETE link | Linking works via API | ⬜ |

### TC-3.3: Error Responses (FR-3.3)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | POST entity with missing required Name | 400 with problem details JSON | ⬜ |
| 2 | GET /api/entities/{nonexistent-id} | 404 with problem details | ⬜ |

### TC-3.4: Swagger (FR-3.4)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Navigate to /swagger | Swagger UI loads with all endpoints documented | ⬜ |

---

## FR-4: Import / Export

### TC-4.1–4.4: Import and Export (FR-4.1–4.4)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Export entity schema as JSON | Valid JSON file with entity + fields + relationships | ⬜ |
| 2 | Import entity schema JSON into fresh instance | Entities recreated | ⬜ |
| 3 | Export records of an entity as JSON | Valid JSON array of records | ⬜ |
| 4 | Import records from JSON | Records created with correct field values | ⬜ |

---

## FR-5: Demo / Seed Data

### TC-5.1: Empty First Start (FR-5.1)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Delete xrm.db, start app | No entities or records exist | ⬜ |

### TC-5.2: Load Demo Data (FR-5.2)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Trigger "Load demo" action | 6 entities seeded with fields, relationships, ~29 records | ⬜ |
| 2 | Verify Company has contacts linked | Relationships visible | ⬜ |

### TC-5.3: Demo Data is Editable (FR-5.3)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Edit a seeded Company record | Changes saved | ⬜ |
| 2 | Delete a seeded Contact | Record removed | ⬜ |

---

## NFR: Non-Functional Requirements

### TC-NFR-1: Single SQLite File (NFR-1)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Check that only one .db file exists in the app directory | Confirmed | ⬜ |
| 2 | Copy the .db file, restore on another instance | Data intact | ⬜ |

### TC-NFR-2: Startup Time (NFR-2)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Start the app, measure time to first page render | < 2 seconds | ⬜ |

### TC-NFR-4: Mobile Responsiveness (NFR-4)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Open record list on mobile viewport (375px) | Grid readable, scrollable | ⬜ |
| 2 | Open record detail on mobile viewport | Form fields stack vertically | ⬜ |
| 3 | Open schema designer on mobile | Acceptable but not optimized | ⬜ |

### TC-NFR-5: No External Dependencies (NFR-5)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Disconnect network, use the app | All features work offline | ⬜ |

### TC-NFR-7: Auth Placeholder (NFR-7)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Check audit fields on a record | CreatedBy = "system" | ⬜ |

### TC-NFR-8: Audit Trail (NFR-8)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Create a record | CreatedAt and CreatedBy populated | ⬜ |
| 2 | Edit the record | ModifiedAt and ModifiedBy updated | ⬜ |

---

## UI/UX Validation

| # | Check | Expected | Status |
|---|-------|----------|--------|
| 1 | Browser tab shows "XRM" on all pages | Confirmed | ⬜ |
| 2 | Favicon is green circle with white "XRM" | Visible in tab | ⬜ |
| 3 | Color palette uses biophilic greens/beige | Visual check | ⬜ |

---

## Notes

- Test with demo data loaded unless testing FR-5.1 (empty start)
- API tests can be run with curl, Postman, or Swagger UI
- Mobile tests use browser DevTools responsive mode
