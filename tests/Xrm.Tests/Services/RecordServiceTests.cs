using System.Text.Json;
using Xrm.Server.Models;
using Xrm.Tests.Infrastructure;

namespace Xrm.Tests.Services;

public class RecordServiceTests : ServiceTestBase
{
    private async Task<Guid> CreateEntityWithFieldsAsync()
    {
        var entitySvc = CreateEntityService();
        var entity = await entitySvc.CreateAsync(new EntityDefinition { Name = "Contact" });

        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition { Name = "FirstName", DataType = FieldDataType.Text, SortOrder = 1 });
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition { Name = "LastName", DataType = FieldDataType.Text, SortOrder = 2 });
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition { Name = "Age", DataType = FieldDataType.Number, SortOrder = 3 });

        return entity.Id;
    }

    [Fact]
    public async Task Create_ValidRecord_Saved()
    {
        var entityId = await CreateEntityWithFieldsAsync();
        var svc = CreateRecordService();
        var json = JsonSerializer.Serialize(new { FirstName = "Alice", LastName = "Smith", Age = 30 });

        var record = await svc.CreateAsync(entityId, json);

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(entityId, record.EntityDefinitionId);
    }

    [Fact]
    public async Task GetAll_Paginated()
    {
        var entityId = await CreateEntityWithFieldsAsync();
        var svc = CreateRecordService();

        for (int i = 0; i < 5; i++)
            await svc.CreateAsync(entityId, JsonSerializer.Serialize(new { FirstName = $"User{i}" }));

        var page1 = await svc.GetAllAsync(entityId, page: 1, pageSize: 3);
        Assert.Equal(3, page1.Records.Count);
        Assert.Equal(5, page1.Total);

        var page2 = await svc.GetAllAsync(entityId, page: 2, pageSize: 3);
        Assert.Equal(2, page2.Records.Count);
    }

    [Fact]
    public async Task GetAll_SortByField_Ascending()
    {
        var entityId = await CreateEntityWithFieldsAsync();
        var svc = CreateRecordService();

        await svc.CreateAsync(entityId, JsonSerializer.Serialize(new { FirstName = "Charlie" }));
        await svc.CreateAsync(entityId, JsonSerializer.Serialize(new { FirstName = "Alice" }));
        await svc.CreateAsync(entityId, JsonSerializer.Serialize(new { FirstName = "Bob" }));

        var page = await svc.GetAllAsync(entityId, sortField: "FirstName", sortDir: "asc");

        var names = page.Records.Select(r =>
        {
            using var doc = JsonDocument.Parse(r.DataJson);
            return doc.RootElement.GetProperty("FirstName").GetString();
        }).ToList();

        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, names);
    }

    [Fact]
    public async Task GetAll_SortByField_Descending()
    {
        var entityId = await CreateEntityWithFieldsAsync();
        var svc = CreateRecordService();

        await svc.CreateAsync(entityId, JsonSerializer.Serialize(new { FirstName = "Alice" }));
        await svc.CreateAsync(entityId, JsonSerializer.Serialize(new { FirstName = "Charlie" }));

        var page = await svc.GetAllAsync(entityId, sortField: "FirstName", sortDir: "desc");

        var names = page.Records.Select(r =>
        {
            using var doc = JsonDocument.Parse(r.DataJson);
            return doc.RootElement.GetProperty("FirstName").GetString();
        }).ToList();

        Assert.Equal("Charlie", names.First());
    }

    [Fact]
    public async Task GetAll_FilterByContent()
    {
        var entityId = await CreateEntityWithFieldsAsync();
        var svc = CreateRecordService();

        await svc.CreateAsync(entityId, JsonSerializer.Serialize(new { FirstName = "Alice", LastName = "Smith" }));
        await svc.CreateAsync(entityId, JsonSerializer.Serialize(new { FirstName = "Bob", LastName = "Jones" }));

        var page = await svc.GetAllAsync(entityId, filter: "Smith");
        Assert.Single(page.Records);
    }

    [Fact]
    public async Task Update_ChangesData()
    {
        var entityId = await CreateEntityWithFieldsAsync();
        var svc = CreateRecordService();
        var record = await svc.CreateAsync(entityId, JsonSerializer.Serialize(new { FirstName = "Old" }));

        var updated = await svc.UpdateAsync(entityId, record.Id, JsonSerializer.Serialize(new { FirstName = "New" }));
        Assert.True(updated);

        var loaded = await svc.GetByIdAsync(entityId, record.Id);
        Assert.Contains("New", loaded!.DataJson);
    }

    [Fact]
    public async Task Delete_RemovesRecord()
    {
        var entityId = await CreateEntityWithFieldsAsync();
        var svc = CreateRecordService();
        var record = await svc.CreateAsync(entityId, JsonSerializer.Serialize(new { FirstName = "Gone" }));

        Assert.True(await svc.DeleteAsync(entityId, record.Id));
        Assert.Null(await svc.GetByIdAsync(entityId, record.Id));
    }

    [Fact]
    public async Task Links_CreateAndRetrieve()
    {
        var entitySvc = CreateEntityService();
        var company = await entitySvc.CreateAsync(new EntityDefinition { Name = "Company" });
        var contact = await entitySvc.CreateAsync(new EntityDefinition { Name = "Contact" });

        var relSvc = CreateRelationshipService();
        var rel = await relSvc.CreateAsync(new RelationshipDefinition
        {
            Name = "CompanyContacts",
            SourceEntityId = company.Id,
            TargetEntityId = contact.Id,
            RelationshipType = RelationshipType.OneToMany
        });

        var recSvc = CreateRecordService();
        var companyRec = await recSvc.CreateAsync(company.Id, """{"Name":"Acme"}""");
        var contactRec = await recSvc.CreateAsync(contact.Id, """{"FirstName":"Alice"}""");

        var link = await recSvc.CreateLinkAsync(companyRec.Id, rel.Id, contactRec.Id);
        Assert.NotEqual(Guid.Empty, link.Id);

        // Check links from company side
        var companyLinks = await recSvc.GetLinksAsync(company.Id, companyRec.Id);
        Assert.Single(companyLinks);
        Assert.Equal("outgoing", companyLinks[0].Direction);

        // Check links from contact side
        var contactLinks = await recSvc.GetLinksAsync(contact.Id, contactRec.Id);
        Assert.Single(contactLinks);
        Assert.Equal("incoming", contactLinks[0].Direction);
    }

    [Fact]
    public async Task DeleteLink_RemovesLink()
    {
        var entitySvc = CreateEntityService();
        var a = await entitySvc.CreateAsync(new EntityDefinition { Name = "A" });
        var b = await entitySvc.CreateAsync(new EntityDefinition { Name = "B" });

        var relSvc = CreateRelationshipService();
        var rel = await relSvc.CreateAsync(new RelationshipDefinition
        {
            Name = "AtoB",
            SourceEntityId = a.Id,
            TargetEntityId = b.Id,
            RelationshipType = RelationshipType.OneToMany
        });

        var recSvc = CreateRecordService();
        var recA = await recSvc.CreateAsync(a.Id, """{"Name":"A1"}""");
        var recB = await recSvc.CreateAsync(b.Id, """{"Name":"B1"}""");

        var link = await recSvc.CreateLinkAsync(recA.Id, rel.Id, recB.Id);
        Assert.True(await recSvc.DeleteLinkAsync(link.Id));

        var links = await recSvc.GetLinksAsync(a.Id, recA.Id);
        Assert.Empty(links);
    }

    [Fact]
    public async Task CreateLink_InvalidRelationship_Throws()
    {
        var entitySvc = CreateEntityService();
        var a = await entitySvc.CreateAsync(new EntityDefinition { Name = "X" });

        var recSvc = CreateRecordService();
        var recA = await recSvc.CreateAsync(a.Id, """{"Name":"X1"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => recSvc.CreateLinkAsync(recA.Id, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateLink_WrongEntityForSource_Throws()
    {
        var entitySvc = CreateEntityService();
        var a = await entitySvc.CreateAsync(new EntityDefinition { Name = "LinkA" });
        var b = await entitySvc.CreateAsync(new EntityDefinition { Name = "LinkB" });
        var c = await entitySvc.CreateAsync(new EntityDefinition { Name = "LinkC" });

        var relSvc = CreateRelationshipService();
        var rel = await relSvc.CreateAsync(new RelationshipDefinition
        {
            Name = "AtoB", SourceEntityId = a.Id, TargetEntityId = b.Id, RelationshipType = RelationshipType.OneToMany
        });

        var recSvc = CreateRecordService();
        var recC = await recSvc.CreateAsync(c.Id, """{"Name":"C1"}""");
        var recB = await recSvc.CreateAsync(b.Id, """{"Name":"B1"}""");

        // recC belongs to entity C, not A (the source entity)
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => recSvc.CreateLinkAsync(recC.Id, rel.Id, recB.Id));
    }

    [Fact]
    public async Task Create_RequiredFieldMissing_Throws()
    {
        var entitySvc = CreateEntityService();
        var entity = await entitySvc.CreateAsync(new EntityDefinition { Name = "Validated" });

        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition
        {
            Name = "Title", DataType = FieldDataType.Text, IsRequired = true
        });

        var recSvc = CreateRecordService();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => recSvc.CreateAsync(entity.Id, """{}"""));
        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public async Task Create_MaxLengthExceeded_Throws()
    {
        var entitySvc = CreateEntityService();
        var entity = await entitySvc.CreateAsync(new EntityDefinition { Name = "MaxLen" });

        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition
        {
            Name = "Code", DataType = FieldDataType.Text, MaxLength = 5
        });

        var recSvc = CreateRecordService();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => recSvc.CreateAsync(entity.Id, """{"Code":"TOOLONG"}"""));
        Assert.Contains("max length", ex.Message);
    }

    [Fact]
    public async Task Create_InvalidChoiceValue_Throws()
    {
        var entitySvc = CreateEntityService();
        var entity = await entitySvc.CreateAsync(new EntityDefinition { Name = "ChoiceVal" });

        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition
        {
            Name = "Status", DataType = FieldDataType.Choice, OptionsJson = """["Open","Closed"]"""
        });

        var recSvc = CreateRecordService();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => recSvc.CreateAsync(entity.Id, """{"Status":"Invalid"}"""));
        Assert.Contains("must be one of", ex.Message);
    }

    [Fact]
    public async Task Create_NumericMinViolation_Throws()
    {
        var entitySvc = CreateEntityService();
        var entity = await entitySvc.CreateAsync(new EntityDefinition { Name = "NumVal" });

        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition
        {
            Name = "Qty", DataType = FieldDataType.Number, MinValue = 1, MaxValue = 100
        });

        var recSvc = CreateRecordService();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => recSvc.CreateAsync(entity.Id, """{"Qty":0}"""));
        Assert.Contains("≥ 1", ex.Message);
    }

    [Fact]
    public async Task GetAll_NumericSort_CorrectOrder()
    {
        var entitySvc = CreateEntityService();
        var entity = await entitySvc.CreateAsync(new EntityDefinition { Name = "NumSort" });

        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition { Name = "Score", DataType = FieldDataType.Number });

        var recSvc = CreateRecordService();
        await recSvc.CreateAsync(entity.Id, """{"Score":2}""");
        await recSvc.CreateAsync(entity.Id, """{"Score":10}""");
        await recSvc.CreateAsync(entity.Id, """{"Score":1}""");

        var page = await recSvc.GetAllAsync(entity.Id, sortField: "Score", sortDir: "asc");
        var scores = page.Records.Select(r =>
        {
            using var doc = JsonDocument.Parse(r.DataJson);
            return doc.RootElement.GetProperty("Score").GetInt32();
        }).ToList();

        Assert.Equal(new[] { 1, 2, 10 }, scores);
    }
}
