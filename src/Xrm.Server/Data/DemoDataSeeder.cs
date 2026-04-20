using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xrm.Server.Models;

namespace Xrm.Server.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(XrmDbContext db)
    {
        if (await db.EntityDefinitions.AnyAsync())
            return; // Already has data

        // Entity Definitions
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
        var activity = new EntityDefinition
        {
            Id = Guid.NewGuid(), Name = "Activity", DisplayName = "Activity", PluralName = "Activities",
            Description = "Calls, meetings, tasks", Icon = "calendar", SortOrder = 3
        };
        var order = new EntityDefinition
        {
            Id = Guid.NewGuid(), Name = "Order", DisplayName = "Order", PluralName = "Orders",
            Description = "Sales orders", Icon = "cart", SortOrder = 4
        };
        var orderLine = new EntityDefinition
        {
            Id = Guid.NewGuid(), Name = "OrderLine", DisplayName = "Order Line", PluralName = "Order Lines",
            Description = "Individual line items in an order", Icon = "list", SortOrder = 5
        };

        db.EntityDefinitions.AddRange(company, contact, activity, order, orderLine);

        // Field Definitions
        var fields = new List<FieldDefinition>
        {
            // Company fields
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Name", DisplayName = "Company Name", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 200, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Industry", DisplayName = "Industry", DataType = FieldDataType.Choice, OptionsJson = JsonSerializer.Serialize(new[] { "Technology", "Finance", "Healthcare", "Manufacturing", "Retail", "Other" }), SortOrder = 2 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Website", DisplayName = "Website", DataType = FieldDataType.Url, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Phone", DisplayName = "Phone", DataType = FieldDataType.Phone, SortOrder = 4 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "City", DisplayName = "City", DataType = FieldDataType.Text, MaxLength = 100, SortOrder = 5 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, Name = "Country", DisplayName = "Country", DataType = FieldDataType.Text, MaxLength = 100, SortOrder = 6 },

            // Contact fields
            new() { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, Name = "FirstName", DisplayName = "First Name", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 100, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, Name = "LastName", DisplayName = "Last Name", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 100, SortOrder = 2 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, Name = "Email", DisplayName = "Email", DataType = FieldDataType.Email, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, Name = "Phone", DisplayName = "Phone", DataType = FieldDataType.Phone, SortOrder = 4 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, Name = "JobTitle", DisplayName = "Job Title", DataType = FieldDataType.Text, MaxLength = 100, SortOrder = 5 },

            // Activity fields
            new() { Id = Guid.NewGuid(), EntityDefinitionId = activity.Id, Name = "Subject", DisplayName = "Subject", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 200, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = activity.Id, Name = "Type", DisplayName = "Type", DataType = FieldDataType.Choice, OptionsJson = JsonSerializer.Serialize(new[] { "Call", "Meeting", "Email", "Task" }), SortOrder = 2 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = activity.Id, Name = "DueDate", DisplayName = "Due Date", DataType = FieldDataType.DateTime, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = activity.Id, Name = "Completed", DisplayName = "Completed", DataType = FieldDataType.Boolean, DefaultValue = "false", SortOrder = 4 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = activity.Id, Name = "Notes", DisplayName = "Notes", DataType = FieldDataType.RichText, SortOrder = 5 },

            // Order fields
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "OrderNumber", DisplayName = "Order Number", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 50, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "OrderDate", DisplayName = "Order Date", DataType = FieldDataType.Date, IsRequired = true, SortOrder = 2 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "Status", DisplayName = "Status", DataType = FieldDataType.Choice, OptionsJson = JsonSerializer.Serialize(new[] { "Draft", "Submitted", "Fulfilled", "Cancelled" }), SortOrder = 3 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = order.Id, Name = "TotalAmount", DisplayName = "Total Amount", DataType = FieldDataType.Decimal, SortOrder = 4 },

            // Order Line fields
            new() { Id = Guid.NewGuid(), EntityDefinitionId = orderLine.Id, Name = "ProductName", DisplayName = "Product Name", DataType = FieldDataType.Text, IsRequired = true, MaxLength = 200, SortOrder = 1 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = orderLine.Id, Name = "Quantity", DisplayName = "Quantity", DataType = FieldDataType.Number, IsRequired = true, MinValue = 1, SortOrder = 2 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = orderLine.Id, Name = "UnitPrice", DisplayName = "Unit Price", DataType = FieldDataType.Decimal, IsRequired = true, SortOrder = 3 },
            new() { Id = Guid.NewGuid(), EntityDefinitionId = orderLine.Id, Name = "LineTotal", DisplayName = "Line Total", DataType = FieldDataType.Decimal, SortOrder = 4 },
        };
        db.FieldDefinitions.AddRange(fields);

        // Relationship Definitions
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
            Id = Guid.NewGuid(), Name = "OrderLines", DisplayName = "Order → Order Lines",
            SourceEntityId = order.Id, TargetEntityId = orderLine.Id,
            RelationshipType = RelationshipType.OneToMany, CascadeBehavior = CascadeBehavior.Cascade
        };

        db.RelationshipDefinitions.AddRange(companyContacts, contactActivities, companyOrders, orderLines);

        // Sample Records
        var acme = new Record { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, DataJson = JsonSerializer.Serialize(new Dictionary<string, object> { ["Name"] = "Acme Corporation", ["Industry"] = "Technology", ["Website"] = "https://acme.example.com", ["Phone"] = "+1-555-0100", ["City"] = "San Francisco", ["Country"] = "USA" }) };
        var globex = new Record { Id = Guid.NewGuid(), EntityDefinitionId = company.Id, DataJson = JsonSerializer.Serialize(new Dictionary<string, object> { ["Name"] = "Globex Industries", ["Industry"] = "Manufacturing", ["Phone"] = "+1-555-0200", ["City"] = "Chicago", ["Country"] = "USA" }) };

        var john = new Record { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, DataJson = JsonSerializer.Serialize(new Dictionary<string, object> { ["FirstName"] = "John", ["LastName"] = "Smith", ["Email"] = "john.smith@acme.example.com", ["Phone"] = "+1-555-0101", ["JobTitle"] = "CTO" }) };
        var jane = new Record { Id = Guid.NewGuid(), EntityDefinitionId = contact.Id, DataJson = JsonSerializer.Serialize(new Dictionary<string, object> { ["FirstName"] = "Jane", ["LastName"] = "Doe", ["Email"] = "jane.doe@globex.example.com", ["Phone"] = "+1-555-0201", ["JobTitle"] = "VP Sales" }) };

        db.Records.AddRange(acme, globex, john, jane);

        // Link contacts to companies
        db.RecordLinks.AddRange(
            new RecordLink { Id = Guid.NewGuid(), RelationshipDefinitionId = companyContacts.Id, SourceRecordId = acme.Id, TargetRecordId = john.Id },
            new RecordLink { Id = Guid.NewGuid(), RelationshipDefinitionId = companyContacts.Id, SourceRecordId = globex.Id, TargetRecordId = jane.Id }
        );

        await db.SaveChangesAsync();
    }
}
