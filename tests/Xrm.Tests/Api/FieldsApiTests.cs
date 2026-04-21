using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xrm.Server.Models;
using Xrm.Tests.Infrastructure;

namespace Xrm.Tests.Api;

public class FieldsApiTests : IClassFixture<XrmWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public FieldsApiTests(XrmWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateEntityAsync(string name = "FieldTestEntity")
    {
        var response = await _client.PostAsJsonAsync("/api/entities", new { Name = name });
        var entity = await response.Content.ReadFromJsonAsync<EntityDefinition>(JsonOpts);
        return entity!.Id;
    }

    [Fact]
    public async Task GetFields_ReturnsOk()
    {
        var entityId = await CreateEntityAsync();
        var response = await _client.GetAsync($"/api/entities/{entityId}/fields");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateField_Returns201()
    {
        var entityId = await CreateEntityAsync("CreateFieldEntity");
        var response = await _client.PostAsJsonAsync(
            $"/api/entities/{entityId}/fields",
            new { Name = "TestField", DataType = "Text" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<FieldDefinition>(JsonOpts);
        Assert.NotNull(created);
        Assert.Equal("TestField", created!.Name);
    }

    [Fact]
    public async Task GetField_NotFound_Returns404()
    {
        var entityId = await CreateEntityAsync("NotFoundFieldEntity");
        var response = await _client.GetAsync($"/api/entities/{entityId}/fields/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateField_ReturnsNoContent()
    {
        var entityId = await CreateEntityAsync("UpdateFieldEntity");
        var createRes = await _client.PostAsJsonAsync(
            $"/api/entities/{entityId}/fields",
            new { Name = "Original", DataType = "Text" });
        var field = await createRes.Content.ReadFromJsonAsync<FieldDefinition>(JsonOpts);

        var updateRes = await _client.PutAsJsonAsync(
            $"/api/entities/{entityId}/fields/{field!.Id}",
            new { Name = "Renamed", DataType = "Text" });
        Assert.Equal(HttpStatusCode.NoContent, updateRes.StatusCode);
    }

    [Fact]
    public async Task UpdateField_NotFound_Returns404()
    {
        var entityId = await CreateEntityAsync("UpdateNotFoundEntity");
        var response = await _client.PutAsJsonAsync(
            $"/api/entities/{entityId}/fields/{Guid.NewGuid()}",
            new { Name = "Ghost", DataType = "Text" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteField_ReturnsNoContent()
    {
        var entityId = await CreateEntityAsync("DeleteFieldEntity");
        var createRes = await _client.PostAsJsonAsync(
            $"/api/entities/{entityId}/fields",
            new { Name = "ToDelete", DataType = "Text" });
        var field = await createRes.Content.ReadFromJsonAsync<FieldDefinition>(JsonOpts);

        var deleteRes = await _client.DeleteAsync($"/api/entities/{entityId}/fields/{field!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);
    }

    [Fact]
    public async Task DeleteField_NotFound_Returns404()
    {
        var entityId = await CreateEntityAsync("DeleteNotFoundEntity");
        var response = await _client.DeleteAsync($"/api/entities/{entityId}/fields/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Crud_FullLifecycle()
    {
        var entityId = await CreateEntityAsync("LifecycleFieldEntity");

        // Create
        var createRes = await _client.PostAsJsonAsync(
            $"/api/entities/{entityId}/fields",
            new { Name = "Lifecycle", DataType = "Number", IsRequired = true });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var field = await createRes.Content.ReadFromJsonAsync<FieldDefinition>(JsonOpts);

        // Read
        var getRes = await _client.GetAsync($"/api/entities/{entityId}/fields/{field!.Id}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

        // Update
        var putRes = await _client.PutAsJsonAsync(
            $"/api/entities/{entityId}/fields/{field.Id}",
            new { Name = "Updated", DataType = "Text" });
        Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);

        // Delete
        var deleteRes = await _client.DeleteAsync($"/api/entities/{entityId}/fields/{field.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // Verify gone
        var gone = await _client.GetAsync($"/api/entities/{entityId}/fields/{field.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }
}
