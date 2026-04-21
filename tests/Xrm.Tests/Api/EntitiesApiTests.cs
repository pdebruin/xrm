using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xrm.Server.Models;
using Xrm.Tests.Infrastructure;

namespace Xrm.Tests.Api;

public class EntitiesApiTests : IClassFixture<XrmWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public EntitiesApiTests(XrmWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetEntities_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/entities");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEntity_Returns201()
    {
        var entity = new { Name = "ApiTest", DisplayName = "API Test" };
        var response = await _client.PostAsJsonAsync("/api/entities", entity);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<EntityDefinition>(JsonOpts);
        Assert.NotNull(created);
        Assert.Equal("ApiTest", created!.Name);
    }

    [Fact]
    public async Task GetEntity_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/entities/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEntity_NotFound_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/entities/{Guid.NewGuid()}", new { Name = "Ghost" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEntity_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/entities/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRelationships_ReturnsOk()
    {
        var createRes = await _client.PostAsJsonAsync("/api/entities", new { Name = "RelEntity" });
        var entity = await createRes.Content.ReadFromJsonAsync<EntityDefinition>(JsonOpts);

        var response = await _client.GetAsync($"/api/entities/{entity!.Id}/relationships");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SeedDemo_ReturnsOk()
    {
        var response = await _client.PostAsync("/api/entities/seed-demo", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEntity_WithIsHomeEntity_ClearsOthers()
    {
        var first = await _client.PostAsJsonAsync("/api/entities", new { Name = "Home1", IsHomeEntity = true });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/entities", new { Name = "Home2", IsHomeEntity = true });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var firstEntity = await first.Content.ReadFromJsonAsync<EntityDefinition>(JsonOpts);
        var refreshed = await _client.GetAsync($"/api/entities/{firstEntity!.Id}");
        var refreshedEntity = await refreshed.Content.ReadFromJsonAsync<EntityDefinition>(JsonOpts);
        Assert.False(refreshedEntity!.IsHomeEntity);
    }

    [Fact]
    public async Task UpdateEntity_SetIsHomeEntity_ClearsOthers()
    {
        var first = await _client.PostAsJsonAsync("/api/entities", new { Name = "HomeUpd1", IsHomeEntity = true });
        var firstEntity = await first.Content.ReadFromJsonAsync<EntityDefinition>(JsonOpts);

        var second = await _client.PostAsJsonAsync("/api/entities", new { Name = "HomeUpd2" });
        var secondEntity = await second.Content.ReadFromJsonAsync<EntityDefinition>(JsonOpts);

        await _client.PutAsJsonAsync($"/api/entities/{secondEntity!.Id}", new { Name = "HomeUpd2", IsHomeEntity = true });

        var refreshed = await _client.GetAsync($"/api/entities/{firstEntity!.Id}");
        var refreshedEntity = await refreshed.Content.ReadFromJsonAsync<EntityDefinition>(JsonOpts);
        Assert.False(refreshedEntity!.IsHomeEntity);
    }

    [Fact]
    public async Task Crud_FullLifecycle()
    {
        // Create
        var createResponse = await _client.PostAsJsonAsync("/api/entities", new { Name = "Lifecycle" });
        var entity = await createResponse.Content.ReadFromJsonAsync<EntityDefinition>(JsonOpts);

        // Read
        var getResponse = await _client.GetAsync($"/api/entities/{entity!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Update
        var putResponse = await _client.PutAsJsonAsync($"/api/entities/{entity.Id}", new { Name = "Updated" });
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/entities/{entity.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify gone
        var gone = await _client.GetAsync($"/api/entities/{entity.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }
}
