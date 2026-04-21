using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xrm.Server.Models;

namespace Xrm.Server.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(XrmDbContext db)
    {
        if (await db.EntityDefinitions.AnyAsync())
            return;

        // --- Entity Definitions ---
        var company = new EntityDefinition
        {
            Id = Guid.NewGuid(), Name = "Company", DisplayName = "Company", PluralName = "Companies",
            Description = "Organizations and businesses", Icon = "building", IsHomeEntity = true, SortOrder = 1
        };
        var contact = new EntityDefinition
        {
            Id = Guid.NewGuid(), Name = "Contact", DisplayName = "Contact", PluralName = "Contacts",
            Description = "People and individuals", Icon = "person", SortOrder = 2
        };
        var product = new EntityDefinition
        {
            Id = Guid.NewGuid(), Name = "Product", DisplayName = "Product", PluralName = "Products",
            Description = "Products and services offered", Icon = "box", SortOrder = 3
        };
        var activity = new EntityDefinition
        {
            Id = Guid.NewGuid(), Name = "Activity", DisplayName = "Activity", PluralName = "Activities",
            Description = "Calls, meetings, tasks", Icon = "calendar", SortOrder = 4
        };
        var order = new EntityDefinition
        {
            Id = Guid.NewGuid(), Name = "Order", DisplayName = "Order", PluralName = "Orders",
            Description = "Sales orders", Icon = "cart", SortOrder = 5
        };
        var orderLine = new EntityDefinition
        {
            Id = Guid.NewGuid(), Name = "OrderLine", DisplayName = "Order Line", PluralName = "Order Lines",
            Description = "Individual line items in an order", Icon = "list", SortOrder = 6
        };

        db.EntityDefinitions.AddRange(company, contact, product, activity, order, orderLine);

        // --- Field Definitions ---
        var fields = new List<FieldDefinition>
        {
            // Company fields (inspired by AWLT Customer)
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Name", DisplayName = "Company Name", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 200, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Industry", DisplayName = "Industry", DataType = FieldDataType.Choice, OptionsJson = Json(new[] { "Technology", "Finance", "Healthcare", "Manufacturing", "Retail", "Consulting", "Education", "Other" }), SortOrder = 2 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Website", DisplayName = "Website", DataType = FieldDataType.Url, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Phone", DisplayName = "Phone", DataType = FieldDataType.Phone, SortOrder = 4 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Email", DisplayName = "Email", DataType = FieldDataType.Email, SortOrder = 5 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "AddressLine", DisplayName = "Address", DataType = FieldDataType.Text, MaxLength = 200, SortOrder = 6 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "City", DisplayName = "City", DataType = FieldDataType.Text, MaxLength = 100, SortOrder = 7 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "State", DisplayName = "State/Province", DataType = FieldDataType.Text, MaxLength = 50, SortOrder = 8 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Country", DisplayName = "Country", DataType = FieldDataType.Text, MaxLength = 100, SortOrder = 9 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Revenue", DisplayName = "Annual Revenue", DataType = FieldDataType.Decimal, SortOrder = 10 },

            // Contact fields
            new() { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, Name = "FirstName", DisplayName = "First Name", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 100, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, Name = "LastName", DisplayName = "Last Name", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 100, SortOrder = 2 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, Name = "Email", DisplayName = "Email", DataType = FieldDataType.Email, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, Name = "Phone", DisplayName = "Phone", DataType = FieldDataType.Phone, SortOrder = 4 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, Name = "JobTitle", DisplayName = "Job Title", DataType = FieldDataType.Text, MaxLength = 100, SortOrder = 5 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, Name = "Department", DisplayName = "Department", DataType = FieldDataType.Choice, OptionsJson = Json(new[] { "Executive", "Sales", "Marketing", "Engineering", "Operations", "Finance", "HR", "Support" }), SortOrder = 6 },

            // Product fields (inspired by AWLT Product)
            new() { Id = Guid.NewGuid(), EntityDefinitionId = product.Id, Name = "Name", DisplayName = "Product Name", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 200, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = product.Id, Name = "ProductNumber", DisplayName = "SKU", DataType = FieldDataType.Text, MaxLength = 50, SortOrder = 2 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = product.Id, Name = "Category", DisplayName = "Category", DataType = FieldDataType.Choice, OptionsJson = Json(new[] { "Bikes", "Components", "Clothing", "Accessories" }), SortOrder = 3 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = product.Id, Name = "ListPrice", DisplayName = "List Price", DataType = FieldDataType.Decimal, IsRequired = true, SortOrder = 4 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = product.Id, Name = "StandardCost", DisplayName = "Standard Cost", DataType = FieldDataType.Decimal, SortOrder = 5 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = product.Id, Name = "Weight", DisplayName = "Weight (kg)", DataType = FieldDataType.Decimal, SortOrder = 6 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = product.Id, Name = "Description", DisplayName = "Description", DataType = FieldDataType.RichText, SortOrder = 7 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = product.Id, Name = "Active", DisplayName = "Active", DataType = FieldDataType.Boolean, DefaultValue = "true", SortOrder = 8 },

            // Activity fields
            new() { Id = Guid.NewGuid(), EntityDefinitionId = activity.Id, Name = "Subject", DisplayName = "Subject", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 200, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = activity.Id, Name = "Type", DisplayName = "Type", DataType = FieldDataType.Choice, OptionsJson = Json(new[] { "Call", "Meeting", "Email", "Task", "Follow-up" }), SortOrder = 2 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = activity.Id, Name = "DueDate", DisplayName = "Due Date", DataType = FieldDataType.DateTime, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = activity.Id, Name = "Priority", DisplayName = "Priority", DataType = FieldDataType.Choice, OptionsJson = Json(new[] { "Low", "Normal", "High", "Urgent" }), SortOrder = 4 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = activity.Id, Name = "Completed", DisplayName = "Completed", DataType = FieldDataType.Boolean, DefaultValue = "false", SortOrder = 5 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = activity.Id, Name = "Notes", DisplayName = "Notes", DataType = FieldDataType.RichText, SortOrder = 6 },

            // Order fields (inspired by AWLT SalesOrderHeader)
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "OrderNumber", DisplayName = "Order Number", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 50, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "OrderDate", DisplayName = "Order Date", DataType = FieldDataType.Date, IsRequired = true, SortOrder = 2 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "DueDate", DisplayName = "Due Date", DataType = FieldDataType.Date, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "Status", DisplayName = "Status", DataType = FieldDataType.Choice, OptionsJson = Json(new[] { "Draft", "Submitted", "In Progress", "Shipped", "Fulfilled", "Cancelled" }), SortOrder = 4 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "SubTotal", DisplayName = "Subtotal", DataType = FieldDataType.Decimal, SortOrder = 5 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "TaxAmount", DisplayName = "Tax", DataType = FieldDataType.Decimal, SortOrder = 6 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "TotalDue", DisplayName = "Total Due", DataType = FieldDataType.Decimal, SortOrder = 7 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "ShipMethod", DisplayName = "Ship Method", DataType = FieldDataType.Choice, OptionsJson = Json(new[] { "Standard", "Express", "Overnight" }), SortOrder = 8 },

            // Order Line fields (inspired by AWLT SalesOrderDetail)
            new() { Id = Guid.NewGuid(), EntityDefinitionId = orderLine.Id, Name = "ProductName", DisplayName = "Product", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 200, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = orderLine.Id, Name = "Quantity", DisplayName = "Quantity", DataType = FieldDataType.Number, IsRequired = true, MinValue = 1, SortOrder = 2 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = orderLine.Id, Name = "UnitPrice", DisplayName = "Unit Price", DataType = FieldDataType.Decimal, IsRequired = true, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = orderLine.Id, Name = "Discount", DisplayName = "Discount %", DataType = FieldDataType.Decimal, MinValue = 0, MaxValue = 100, SortOrder = 4 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = orderLine.Id, Name = "LineTotal", DisplayName = "Line Total", DataType = FieldDataType.Decimal, SortOrder = 5 },
        };
        db.FieldDefinitions.AddRange(fields);

        // --- Relationship Definitions ---
        var companyContacts = new RelationshipDefinition
        {
            Id = Guid.NewGuid(), Name = "CompanyContacts", DisplayName = "Company → Contacts",
            SourceEntityId = company.Id, TargetEntityId = contact.Id,
            RelationshipType = RelationshipType.OneToMany, CascadeBehavior = CascadeBehavior.RemoveLink
        };
        var contactActivities = new RelationshipDefinition
        {
            Id = Guid.NewGuid(), Name = "ContactActivities", DisplayName = "Contact → Activities",
            SourceEntityId = contact.Id, TargetEntityId = activity.Id,
            RelationshipType = RelationshipType.OneToMany, CascadeBehavior = CascadeBehavior.Cascade
        };
        var companyOrders = new RelationshipDefinition
        {
            Id = Guid.NewGuid(), Name = "CompanyOrders", DisplayName = "Company → Orders",
            SourceEntityId = company.Id, TargetEntityId = order.Id,
            RelationshipType = RelationshipType.OneToMany, CascadeBehavior = CascadeBehavior.RemoveLink
        };
        var orderLines = new RelationshipDefinition
        {
            Id = Guid.NewGuid(), Name = "OrderLines", DisplayName = "Order → Lines",
            SourceEntityId = order.Id, TargetEntityId = orderLine.Id,
            RelationshipType = RelationshipType.OneToMany, CascadeBehavior = CascadeBehavior.Cascade
        };

        db.RelationshipDefinitions.AddRange(companyContacts, contactActivities, companyOrders, orderLines);

        // --- Sample Records (inspired by AdventureWorksLT) ---

        // Companies
        var adventureWorks = Rec(company.Id, new { Name = "Adventure Works Cycles", Industry = "Manufacturing", Website = "https://adventure-works.com", Phone = "+1-425-555-0100", Email = "info@adventure-works.com", AddressLine = "1234 Innovation Way", City = "Bothell", State = "WA", Country = "USA", Revenue = 45000000.00 });
        var contoso = Rec(company.Id, new { Name = "Contoso Ltd", Industry = "Technology", Website = "https://contoso.com", Phone = "+1-206-555-0200", Email = "sales@contoso.com", AddressLine = "800 Tech Drive", City = "Redmond", State = "WA", Country = "USA", Revenue = 120000000.00 });
        var fabricam = Rec(company.Id, new { Name = "Fabricam Inc", Industry = "Manufacturing", Phone = "+1-312-555-0300", Email = "hello@fabricam.com", City = "Chicago", State = "IL", Country = "USA", Revenue = 28000000.00 });
        var wideworldImporters = Rec(company.Id, new { Name = "Wide World Importers", Industry = "Retail", Website = "https://wideworldimporters.com", Phone = "+44-20-5555-0400", City = "London", Country = "UK", Revenue = 15000000.00 });
        var northwind = Rec(company.Id, new { Name = "Northwind Traders", Industry = "Retail", Phone = "+1-503-555-0500", Email = "orders@northwind.com", City = "Portland", State = "OR", Country = "USA", Revenue = 8500000.00 });

        // Contacts
        var orlando = Rec(contact.Id, new { FirstName = "Orlando", LastName = "Gee", Email = "orlando.gee@adventure-works.com", Phone = "+1-425-555-0101", JobTitle = "VP Engineering", Department = "Engineering" });
        var keith = Rec(contact.Id, new { FirstName = "Keith", LastName = "Harris", Email = "keith.harris@adventure-works.com", Phone = "+1-425-555-0102", JobTitle = "Sales Manager", Department = "Sales" });
        var donna = Rec(contact.Id, new { FirstName = "Donna", LastName = "Carreras", Email = "donna.carreras@contoso.com", Phone = "+1-206-555-0201", JobTitle = "CTO", Department = "Executive" });
        var janet = Rec(contact.Id, new { FirstName = "Janet", LastName = "Gates", Email = "janet.gates@contoso.com", Phone = "+1-206-555-0202", JobTitle = "Purchasing Manager", Department = "Operations" });
        var lucy = Rec(contact.Id, new { FirstName = "Lucy", LastName = "Harrington", Email = "lucy.h@fabricam.com", Phone = "+1-312-555-0301", JobTitle = "Director of Sales", Department = "Sales" });
        var rosmarie = Rec(contact.Id, new { FirstName = "Rosmarie", LastName = "Carroll", Email = "rosmarie@wideworldimporters.com", Phone = "+44-20-5555-0401", JobTitle = "Import Manager", Department = "Operations" });
        var dominic = Rec(contact.Id, new { FirstName = "Dominic", LastName = "Pinto", Email = "dominic.p@northwind.com", Phone = "+1-503-555-0501", JobTitle = "Owner", Department = "Executive" });

        // Products
        var roadBike = Rec(product.Id, new { Name = "Road-150 Red", ProductNumber = "BK-R93R-62", Category = "Bikes", ListPrice = 3578.27, StandardCost = 1898.09, Weight = 6.7, Description = "Top-of-the-line competition road bike.", Active = true });
        var mountainBike = Rec(product.Id, new { Name = "Mountain-100 Silver", ProductNumber = "BK-M82S-44", Category = "Bikes", ListPrice = 3399.99, StandardCost = 1912.15, Weight = 9.2, Description = "High-performance mountain bike for trail riding.", Active = true });
        var touringBike = Rec(product.Id, new { Name = "Touring-1000 Blue", ProductNumber = "BK-T79U-50", Category = "Bikes", ListPrice = 2384.07, StandardCost = 1481.92, Weight = 11.5, Description = "Comfortable touring bike for long distances.", Active = true });
        var helmet = Rec(product.Id, new { Name = "Sport-100 Helmet Black", ProductNumber = "HL-U509", Category = "Accessories", ListPrice = 34.99, StandardCost = 13.09, Weight = 0.3, Active = true });
        var jersey = Rec(product.Id, new { Name = "Long-Sleeve Logo Jersey L", ProductNumber = "LJ-0192-L", Category = "Clothing", ListPrice = 49.99, StandardCost = 22.19, Weight = 0.2, Active = true });
        var tire = Rec(product.Id, new { Name = "ML Road Tire", ProductNumber = "TI-R092", Category = "Components", ListPrice = 21.49, StandardCost = 9.30, Weight = 0.5, Active = true });

        // Activities
        var act1 = Rec(activity.Id, new { Subject = "Follow up on bike order", Type = "Call", DueDate = "2026-04-22T10:00:00", Priority = "High", Completed = false, Notes = "Discuss delivery timeline for 50-unit order" });
        var act2 = Rec(activity.Id, new { Subject = "Product demo for Contoso", Type = "Meeting", DueDate = "2026-04-23T14:00:00", Priority = "Normal", Completed = false, Notes = "Demo new Mountain-100 line" });
        var act3 = Rec(activity.Id, new { Subject = "Send pricing proposal to Fabricam", Type = "Email", DueDate = "2026-04-21T17:00:00", Priority = "High", Completed = true });

        // Orders
        var order1 = Rec(order.Id, new { OrderNumber = "SO-71774", OrderDate = "2026-04-01", DueDate = "2026-04-15", Status = "Shipped", SubTotal = 7156.54, TaxAmount = 572.52, TotalDue = 7729.06, ShipMethod = "Express" });
        var order2 = Rec(order.Id, new { OrderNumber = "SO-71776", OrderDate = "2026-04-10", DueDate = "2026-04-24", Status = "In Progress", SubTotal = 3399.99, TaxAmount = 272.00, TotalDue = 3671.99, ShipMethod = "Standard" });
        var order3 = Rec(order.Id, new { OrderNumber = "SO-71780", OrderDate = "2026-04-18", DueDate = "2026-05-02", Status = "Draft", SubTotal = 2469.06, TaxAmount = 197.52, TotalDue = 2666.58, ShipMethod = "Standard" });

        // Order Lines
        var line1a = Rec(orderLine.Id, new { ProductName = "Road-150 Red", Quantity = 2, UnitPrice = 3578.27, Discount = 0, LineTotal = 7156.54 });
        var line2a = Rec(orderLine.Id, new { ProductName = "Mountain-100 Silver", Quantity = 1, UnitPrice = 3399.99, Discount = 0, LineTotal = 3399.99 });
        var line3a = Rec(orderLine.Id, new { ProductName = "Touring-1000 Blue", Quantity = 1, UnitPrice = 2384.07, Discount = 0, LineTotal = 2384.07 });
        var line3b = Rec(orderLine.Id, new { ProductName = "Sport-100 Helmet Black", Quantity = 2, UnitPrice = 34.99, Discount = 10, LineTotal = 62.98 });
        var line3c = Rec(orderLine.Id, new { ProductName = "ML Road Tire", Quantity = 4, UnitPrice = 21.49, Discount = 0, LineTotal = 85.96 });

        db.Records.AddRange(
            adventureWorks, contoso, fabricam, wideworldImporters, northwind,
            orlando, keith, donna, janet, lucy, rosmarie, dominic,
            roadBike, mountainBike, touringBike, helmet, jersey, tire,
            act1, act2, act3,
            order1, order2, order3,
            line1a, line2a, line3a, line3b, line3c
        );

        // --- Record Links ---
        db.RecordLinks.AddRange(
            // Company → Contacts
            Link(companyContacts.Id, adventureWorks.Id, orlando.Id),
            Link(companyContacts.Id, adventureWorks.Id, keith.Id),
            Link(companyContacts.Id, contoso.Id, donna.Id),
            Link(companyContacts.Id, contoso.Id, janet.Id),
            Link(companyContacts.Id, fabricam.Id, lucy.Id),
            Link(companyContacts.Id, wideworldImporters.Id, rosmarie.Id),
            Link(companyContacts.Id, northwind.Id, dominic.Id),

            // Contact → Activities
            Link(contactActivities.Id, keith.Id, act1.Id),
            Link(contactActivities.Id, donna.Id, act2.Id),
            Link(contactActivities.Id, lucy.Id, act3.Id),

            // Company → Orders
            Link(companyOrders.Id, adventureWorks.Id, order1.Id),
            Link(companyOrders.Id, contoso.Id, order2.Id),
            Link(companyOrders.Id, fabricam.Id, order3.Id),

            // Order → Lines
            Link(orderLines.Id, order1.Id, line1a.Id),
            Link(orderLines.Id, order2.Id, line2a.Id),
            Link(orderLines.Id, order3.Id, line3a.Id),
            Link(orderLines.Id, order3.Id, line3b.Id),
            Link(orderLines.Id, order3.Id, line3c.Id)
        );

        await db.SaveChangesAsync();
    }

    private static string Json(object obj) => JsonSerializer.Serialize(obj);

    private static Record Rec(Guid entityId, object data) => new()
    {
        Id = Guid.NewGuid(),
        EntityDefinitionId = entityId,
        DataJson = JsonSerializer.Serialize(data)
    };

    private static RecordLink Link(Guid relId, Guid sourceId, Guid targetId) => new()
    {
        Id = Guid.NewGuid(),
        RelationshipDefinitionId = relId,
        SourceRecordId = sourceId,
        TargetRecordId = targetId
    };
}
