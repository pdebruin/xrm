using Xrm.Server.Models;
using Xrm.Tests.Infrastructure;

namespace Xrm.Tests.Services;

public class EntityServiceTests : ServiceTestBase
{
    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        var svc = CreateEntityService();
        var result = await svc.GetAllAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task Create_ValidEntity_ReturnsWithId()
    {
        var svc = CreateEntityService();
        var entity = new EntityDefinition { Name = "Contact", DisplayName = "Contact", PluralName = "Contacts" };

        var created = await svc.CreateAsync(entity);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Contact", created.Name);
    }

    [Fact]
    public async Task GetById_ExistingEntity_ReturnsWithFields()
    {
        var svc = CreateEntityService();
        var created = await svc.CreateAsync(new EntityDefinition { Name = "Company" });

        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(created.Id, new FieldDefinition { Name = "Name", DataType = FieldDataType.Text });

        var result = await svc.GetByIdAsync(created.Id);
        Assert.NotNull(result);
        Assert.Single(result!.Fields);
    }

    [Fact]
    public async Task Update_ChangesName()
    {
        var svc = CreateEntityService();
        var created = await svc.CreateAsync(new EntityDefinition { Name = "OldName" });

        var updated = await svc.UpdateAsync(created.Id, new EntityDefinition { Name = "NewName", DisplayName = "New" });

        Assert.NotNull(updated);
        Assert.Equal("NewName", updated!.Name);
    }

    [Fact]
    public async Task Delete_ExistingEntity_ReturnsTrue()
    {
        var svc = CreateEntityService();
        var created = await svc.CreateAsync(new EntityDefinition { Name = "Temp" });

        var deleted = await svc.DeleteAsync(created.Id);
        Assert.True(deleted);

        var result = await svc.GetByIdAsync(created.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsFalse()
    {
        var svc = CreateEntityService();
        var deleted = await svc.DeleteAsync(Guid.NewGuid());
        Assert.False(deleted);
    }

    [Fact]
    public async Task SeedDemo_CreatesEntities()
    {
        var svc = CreateEntityService();
        await svc.SeedDemoAsync();

        var entities = await svc.GetAllAsync();
        Assert.True(entities.Count >= 5, $"Expected at least 5 demo entities, got {entities.Count}");
    }

    [Fact]
    public async Task Create_SetsAuditFields()
    {
        var svc = CreateEntityService();
        var before = DateTime.UtcNow;
        var created = await svc.CreateAsync(new EntityDefinition { Name = "Audited" });

        Assert.True(created.CreatedAt >= before);
        Assert.Equal("system", created.CreatedBy);
    }
}
