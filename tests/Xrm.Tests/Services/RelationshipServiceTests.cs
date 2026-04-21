using Xrm.Server.Models;
using Xrm.Tests.Infrastructure;

namespace Xrm.Tests.Services;

public class RelationshipServiceTests : ServiceTestBase
{
    private async Task<(Guid Source, Guid Target)> CreateTwoEntitiesAsync()
    {
        var svc = CreateEntityService();
        var source = await svc.CreateAsync(new EntityDefinition { Name = "Company" });
        var target = await svc.CreateAsync(new EntityDefinition { Name = "Contact" });
        return (source.Id, target.Id);
    }

    [Fact]
    public async Task Create_ValidRelationship_Saved()
    {
        var (src, tgt) = await CreateTwoEntitiesAsync();
        var svc = CreateRelationshipService();

        var rel = await svc.CreateAsync(new RelationshipDefinition
        {
            Name = "CompanyContacts",
            SourceEntityId = src,
            TargetEntityId = tgt,
            RelationshipType = RelationshipType.OneToMany
        });

        Assert.NotEqual(Guid.Empty, rel.Id);
    }

    [Fact]
    public async Task GetForEntity_ReturnsBothDirections()
    {
        var (src, tgt) = await CreateTwoEntitiesAsync();
        var svc = CreateRelationshipService();

        await svc.CreateAsync(new RelationshipDefinition
        {
            Name = "CompanyContacts",
            SourceEntityId = src,
            TargetEntityId = tgt,
            RelationshipType = RelationshipType.OneToMany
        });

        var (sourceRels, _) = await svc.GetForEntityAsync(src);
        Assert.Single(sourceRels);

        var (_, targetRels) = await svc.GetForEntityAsync(tgt);
        Assert.Single(targetRels);
    }

    [Fact]
    public async Task Delete_RemovesRelationship()
    {
        var (src, tgt) = await CreateTwoEntitiesAsync();
        var svc = CreateRelationshipService();
        var rel = await svc.CreateAsync(new RelationshipDefinition
        {
            Name = "Temp",
            SourceEntityId = src,
            TargetEntityId = tgt,
            RelationshipType = RelationshipType.OneToMany
        });

        Assert.True(await svc.DeleteAsync(rel.Id));
        Assert.Empty(await svc.GetAllAsync());
    }
}
