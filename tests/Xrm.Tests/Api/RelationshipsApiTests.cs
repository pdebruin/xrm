using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xrm.Core.Models;
using Xrm.Tests.Infrastructure;

namespace Xrm.Tests.Api;

public class RelationshipsApiTests : IClassFixture<XrmWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public RelationshipsApiTests(XrmWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateEntityAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/entities", new { Name = name });
        var entity = await response.Content.ReadFromJsonAsync<EntityDefinition>(JsonOpts);
        return entity!.Id;
    }

    [Fact]
    public async Task GetRelationships_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/relationships");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateRelationship_Returns201()
    {
        var parentId = await CreateEntityAsync("RelSource1");
        var childId = await CreateEntityAsync("RelTarget1");

        var response = await _client.PostAsJsonAsync("/api/relationships", new
        {
            Name = "TestRel",
            ParentEntityId = parentId,
            ChildEntityId = childId,
            RelationshipType = "OneToMany",
            CascadeBehavior = "None"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<RelationshipDefinition>(JsonOpts);
        Assert.NotNull(created);
        Assert.Equal("TestRel", created!.Name);
    }

    [Fact]
    public async Task GetRelationship_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/relationships/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRelationship_ReturnsNoContent()
    {
        var parentId = await CreateEntityAsync("RelUpdateSource");
        var childId = await CreateEntityAsync("RelUpdateTarget");

        var createRes = await _client.PostAsJsonAsync("/api/relationships", new
        {
            Name = "OriginalRel",
            ParentEntityId = parentId,
            ChildEntityId = childId,
            RelationshipType = "OneToMany",
            CascadeBehavior = "None"
        });
        var rel = await createRes.Content.ReadFromJsonAsync<RelationshipDefinition>(JsonOpts);

        var updateRes = await _client.PutAsJsonAsync($"/api/relationships/{rel!.Id}", new
        {
            Name = "RenamedRel",
            ParentEntityId = parentId,
            ChildEntityId = childId,
            RelationshipType = "ManyToMany",
            CascadeBehavior = "Cascade"
        });
        Assert.Equal(HttpStatusCode.NoContent, updateRes.StatusCode);
    }

    [Fact]
    public async Task UpdateRelationship_NotFound_Returns404()
    {
        var parentId = await CreateEntityAsync("RelUpdateNFSource");
        var childId = await CreateEntityAsync("RelUpdateNFTarget");

        var response = await _client.PutAsJsonAsync($"/api/relationships/{Guid.NewGuid()}", new
        {
            Name = "Ghost",
            ParentEntityId = parentId,
            ChildEntityId = childId,
            RelationshipType = "OneToMany",
            CascadeBehavior = "None"
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRelationship_ReturnsNoContent()
    {
        var parentId = await CreateEntityAsync("RelDeleteSource");
        var childId = await CreateEntityAsync("RelDeleteTarget");

        var createRes = await _client.PostAsJsonAsync("/api/relationships", new
        {
            Name = "ToDeleteRel",
            ParentEntityId = parentId,
            ChildEntityId = childId,
            RelationshipType = "OneToMany",
            CascadeBehavior = "None"
        });
        var rel = await createRes.Content.ReadFromJsonAsync<RelationshipDefinition>(JsonOpts);

        var deleteRes = await _client.DeleteAsync($"/api/relationships/{rel!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);
    }

    [Fact]
    public async Task DeleteRelationship_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/relationships/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Crud_FullLifecycle()
    {
        var parentId = await CreateEntityAsync("RelLifecycleSource");
        var childId = await CreateEntityAsync("RelLifecycleTarget");

        // Create
        var createRes = await _client.PostAsJsonAsync("/api/relationships", new
        {
            Name = "LifecycleRel",
            ParentEntityId = parentId,
            ChildEntityId = childId,
            RelationshipType = "OneToMany",
            CascadeBehavior = "RemoveLink"
        });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var rel = await createRes.Content.ReadFromJsonAsync<RelationshipDefinition>(JsonOpts);

        // Read
        var getRes = await _client.GetAsync($"/api/relationships/{rel!.Id}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

        // Update
        var putRes = await _client.PutAsJsonAsync($"/api/relationships/{rel.Id}", new
        {
            Name = "UpdatedRel",
            ParentEntityId = parentId,
            ChildEntityId = childId,
            RelationshipType = "ManyToMany",
            CascadeBehavior = "Cascade"
        });
        Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);

        // Delete
        var deleteRes = await _client.DeleteAsync($"/api/relationships/{rel.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // Verify gone
        var gone = await _client.GetAsync($"/api/relationships/{rel.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }
}
