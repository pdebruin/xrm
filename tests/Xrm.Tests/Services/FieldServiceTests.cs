using Xrm.Server.Models;
using Xrm.Tests.Infrastructure;

namespace Xrm.Tests.Services;

public class FieldServiceTests : ServiceTestBase
{
    private async Task<Guid> CreateEntityAsync()
    {
        var svc = CreateEntityService();
        var entity = await svc.CreateAsync(new EntityDefinition { Name = "TestEntity" });
        return entity.Id;
    }

    [Fact]
    public async Task Create_ValidField_Saved()
    {
        var entityId = await CreateEntityAsync();
        var svc = CreateFieldService();

        var field = await svc.CreateAsync(entityId, new FieldDefinition
        {
            Name = "Email",
            DataType = FieldDataType.Email,
            IsRequired = true
        });

        Assert.NotEqual(Guid.Empty, field.Id);
        Assert.Equal(entityId, field.EntityDefinitionId);
    }

    [Fact]
    public async Task GetAll_ReturnsFieldsForEntity()
    {
        var entityId = await CreateEntityAsync();
        var svc = CreateFieldService();
        await svc.CreateAsync(entityId, new FieldDefinition { Name = "First", DataType = FieldDataType.Text });
        await svc.CreateAsync(entityId, new FieldDefinition { Name = "Second", DataType = FieldDataType.Number });

        var fields = await svc.GetAllAsync(entityId);
        Assert.Equal(2, fields.Count);
    }

    [Fact]
    public async Task Update_ChangesDataType()
    {
        var entityId = await CreateEntityAsync();
        var svc = CreateFieldService();
        var field = await svc.CreateAsync(entityId, new FieldDefinition { Name = "Score", DataType = FieldDataType.Number });

        var updated = await svc.UpdateAsync(entityId, field.Id, new FieldDefinition
        {
            Name = "Score",
            DataType = FieldDataType.Decimal,
            MaxLength = 10
        });

        Assert.NotNull(updated);
        Assert.Equal(FieldDataType.Decimal, updated!.DataType);
    }

    [Fact]
    public async Task Delete_RemovesField()
    {
        var entityId = await CreateEntityAsync();
        var svc = CreateFieldService();
        var field = await svc.CreateAsync(entityId, new FieldDefinition { Name = "Temp", DataType = FieldDataType.Text });

        Assert.True(await svc.DeleteAsync(entityId, field.Id));
        Assert.Empty(await svc.GetAllAsync(entityId));
    }

    [Fact]
    public async Task Create_ChoiceField_OptionsPreserved()
    {
        var entityId = await CreateEntityAsync();
        var svc = CreateFieldService();
        var field = await svc.CreateAsync(entityId, new FieldDefinition
        {
            Name = "Priority",
            DataType = FieldDataType.Choice,
            OptionsJson = "[\"Low\",\"Medium\",\"High\"]"
        });

        var loaded = await svc.GetByIdAsync(entityId, field.Id);
        Assert.NotNull(loaded);
        Assert.Contains("Medium", loaded!.OptionsJson!);
    }
}
