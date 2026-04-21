using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xrm.Server.Models;
using Xrm.Tests.Infrastructure;

namespace Xrm.Tests.Api;

public class RecordsApiTests : IClassFixture<XrmWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public RecordsApiTests(XrmWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateEntityAsync(string name = "RecordTestEntity")
    {
        var response = await _client.PostAsJsonAsync("/api/entities", new { Name = name });
        var entity = await response.Content.ReadFromJsonAsync<EntityDefinition>(JsonOpts);
        return entity!.Id;
    }

    [Fact]
    public async Task CreateRecord_Returns201()
    {
        var entityId = await CreateEntityAsync();
        var response = await _client.PostAsJsonAsync(
            $"/api/entities/{entityId}/records",
            new { DataJson = """{"Name":"Test"}""" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetRecords_ReturnsPaginatedList()
    {
        var entityId = await CreateEntityAsync("PaginatedEntity");
        for (int i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync(
                $"/api/entities/{entityId}/records",
                new { DataJson = $"{{\"Name\":\"Rec{i}\"}}" });
        }

        var response = await _client.GetAsync($"/api/entities/{entityId}/records?page=1&pageSize=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var total = doc.RootElement.GetProperty("total").GetInt32();
        var records = doc.RootElement.GetProperty("records");
        Assert.Equal(3, total);
        Assert.Equal(2, records.GetArrayLength());
    }

    [Fact]
    public async Task UpdateRecord_ReturnsNoContent()
    {
        var entityId = await CreateEntityAsync("UpdateEntity");
        var createRes = await _client.PostAsJsonAsync(
            $"/api/entities/{entityId}/records",
            new { DataJson = """{"Name":"Old"}""" });
        var record = await createRes.Content.ReadFromJsonAsync<JsonElement>();
        var recordId = record.GetProperty("id").GetString();

        var updateRes = await _client.PutAsJsonAsync(
            $"/api/entities/{entityId}/records/{recordId}",
            new { DataJson = """{"Name":"New"}""" });
        Assert.Equal(HttpStatusCode.NoContent, updateRes.StatusCode);
    }

    [Fact]
    public async Task DeleteRecord_ReturnsNoContent()
    {
        var entityId = await CreateEntityAsync("DeleteEntity");
        var createRes = await _client.PostAsJsonAsync(
            $"/api/entities/{entityId}/records",
            new { DataJson = """{"Name":"ToDelete"}""" });
        var record = await createRes.Content.ReadFromJsonAsync<JsonElement>();
        var recordId = record.GetProperty("id").GetString();

        var deleteRes = await _client.DeleteAsync($"/api/entities/{entityId}/records/{recordId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);
    }
}
