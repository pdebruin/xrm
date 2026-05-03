using Xrm.Core.Models;
using Xrm.Tests.Infrastructure;

namespace Xrm.Tests.Services;

public class AuditServiceTests : ServiceTestBase
{
    [Fact]
    public async Task Create_LogsCreatedEntry()
    {
        var entitySvc = CreateEntityService();
        var entity = await entitySvc.CreateAsync(new EntityDefinition { Name = "AudCreate" });

        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition { Name = "Title", DataType = FieldDataType.Text });

        var recSvc = CreateRecordService();
        var record = await recSvc.CreateAsync(entity.Id, """{"Title":"Hello"}""");

        var auditSvc = CreateAuditService();
        var history = await auditSvc.GetHistoryAsync(record.Id);

        Assert.Single(history);
        Assert.Equal("Created", history[0].Action);
        Assert.Null(history[0].OldDataJson);
        Assert.Contains("Hello", history[0].NewDataJson!);
        Assert.Equal(entity.Id, history[0].EntityDefinitionId);
    }

    [Fact]
    public async Task Update_LogsUpdatedWithOldAndNew()
    {
        var entitySvc = CreateEntityService();
        var entity = await entitySvc.CreateAsync(new EntityDefinition { Name = "AudUpdate" });

        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition { Name = "Status", DataType = FieldDataType.Text });

        var recSvc = CreateRecordService();
        var record = await recSvc.CreateAsync(entity.Id, """{"Status":"Open"}""");
        await recSvc.UpdateAsync(entity.Id, record.Id, """{"Status":"Closed"}""");

        var auditSvc = CreateAuditService();
        var history = await auditSvc.GetHistoryAsync(record.Id);

        Assert.Equal(2, history.Count);
        var updateEntry = history.First(h => h.Action == "Updated");
        Assert.Contains("Open", updateEntry.OldDataJson!);
        Assert.Contains("Closed", updateEntry.NewDataJson!);
    }

    [Fact]
    public async Task Delete_LogsDeletedWithOldData()
    {
        var entitySvc = CreateEntityService();
        var entity = await entitySvc.CreateAsync(new EntityDefinition { Name = "AudDelete" });

        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition { Name = "Name", DataType = FieldDataType.Text });

        var recSvc = CreateRecordService();
        var record = await recSvc.CreateAsync(entity.Id, """{"Name":"ToDelete"}""");
        await recSvc.DeleteAsync(entity.Id, record.Id);

        var auditSvc = CreateAuditService();
        var history = await auditSvc.GetHistoryAsync(record.Id);

        Assert.Equal(2, history.Count);
        var deleteEntry = history.First(h => h.Action == "Deleted");
        Assert.Contains("ToDelete", deleteEntry.OldDataJson!);
        Assert.Null(deleteEntry.NewDataJson);
    }

    [Fact]
    public async Task GetEntityHistory_ReturnsAllRecordEntries()
    {
        var entitySvc = CreateEntityService();
        var entity = await entitySvc.CreateAsync(new EntityDefinition { Name = "AudEntity" });

        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition { Name = "X", DataType = FieldDataType.Text });

        var recSvc = CreateRecordService();
        await recSvc.CreateAsync(entity.Id, """{"X":"1"}""");
        await recSvc.CreateAsync(entity.Id, """{"X":"2"}""");

        var auditSvc = CreateAuditService();
        var history = await auditSvc.GetEntityHistoryAsync(entity.Id);

        Assert.Equal(2, history.Count);
        Assert.All(history, h => Assert.Equal("Created", h.Action));
    }
}
