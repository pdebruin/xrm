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

    [Fact]
    public async Task GetRecord_NotFound_Returns404()
    {
        var entityId = await CreateEntityAsync("GetNotFoundEntity");
        var response = await _client.GetAsync($"/api/entities/{entityId}/records/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRecord_NotFound_Returns404()
    {
        var entityId = await CreateEntityAsync("UpdateNFEntity");
        var response = await _client.PutAsJsonAsync(
            $"/api/entities/{entityId}/records/{Guid.NewGuid()}",
            new { DataJson = """{"Name":"Ghost"}""" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRecord_NotFound_Returns404()
    {
        var entityId = await CreateEntityAsync("DeleteNFEntity");
        var response = await _client.DeleteAsync($"/api/entities/{entityId}/records/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRecords_WithSortField_ReturnsSorted()
    {
        var entityId = await CreateEntityAsync("SortEntity");
        await _client.PostAsJsonAsync($"/api/entities/{entityId}/records", new { DataJson = """{"Name":"Banana"}""" });
        await _client.PostAsJsonAsync($"/api/entities/{entityId}/records", new { DataJson = """{"Name":"Apple"}""" });

        var response = await _client.GetAsync($"/api/entities/{entityId}/records?sortField=Name&sortDir=asc");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRecords_WithSortFieldDesc_ReturnsSorted()
    {
        var entityId = await CreateEntityAsync("SortDescEntity");
        await _client.PostAsJsonAsync($"/api/entities/{entityId}/records", new { DataJson = """{"Name":"Alpha"}""" });
        await _client.PostAsJsonAsync($"/api/entities/{entityId}/records", new { DataJson = """{"Name":"Zeta"}""" });

        var response = await _client.GetAsync($"/api/entities/{entityId}/records?sortField=Name&sortDir=desc");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRecords_WithFilter_FiltersResults()
    {
        var entityId = await CreateEntityAsync("FilterEntity");
        await _client.PostAsJsonAsync($"/api/entities/{entityId}/records", new { DataJson = """{"Name":"Match"}""" });
        await _client.PostAsJsonAsync($"/api/entities/{entityId}/records", new { DataJson = """{"Name":"Other"}""" });

        var response = await _client.GetAsync($"/api/entities/{entityId}/records?filter=Match");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task GetRecords_DescSort_ReturnsResults()
    {
        var entityId = await CreateEntityAsync("DescSortEntity");
        await _client.PostAsJsonAsync($"/api/entities/{entityId}/records", new { DataJson = """{"Name":"A"}""" });
        await _client.PostAsJsonAsync($"/api/entities/{entityId}/records", new { DataJson = """{"Name":"B"}""" });

        var response = await _client.GetAsync($"/api/entities/{entityId}/records?sortDir=desc");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RecordLinks_FullLifecycle()
    {
        var sourceEntityId = await CreateEntityAsync("LinkSourceEntity");
        var targetEntityId = await CreateEntityAsync("LinkTargetEntity");

        // Create a relationship
        var relRes = await _client.PostAsJsonAsync("/api/relationships", new
        {
            Name = "LinkTestRel",
            SourceEntityId = sourceEntityId,
            TargetEntityId = targetEntityId,
            RelationshipType = "OneToMany",
            CascadeBehavior = "None"
        });
        var rel = await relRes.Content.ReadFromJsonAsync<JsonElement>();
        var relId = rel.GetProperty("id").GetString();

        // Create records
        var srcRecRes = await _client.PostAsJsonAsync($"/api/entities/{sourceEntityId}/records", new { DataJson = """{"Name":"Source"}""" });
        var srcRec = await srcRecRes.Content.ReadFromJsonAsync<JsonElement>();
        var srcRecId = srcRec.GetProperty("id").GetString();

        var tgtRecRes = await _client.PostAsJsonAsync($"/api/entities/{targetEntityId}/records", new { DataJson = """{"Name":"Target"}""" });
        var tgtRec = await tgtRecRes.Content.ReadFromJsonAsync<JsonElement>();
        var tgtRecId = tgtRec.GetProperty("id").GetString();

        // Create link
        var linkRes = await _client.PostAsJsonAsync(
            $"/api/entities/{sourceEntityId}/records/{srcRecId}/links",
            new { RelationshipDefinitionId = relId, TargetRecordId = tgtRecId });
        Assert.Equal(HttpStatusCode.OK, linkRes.StatusCode);

        // Get links
        var getLinksRes = await _client.GetAsync($"/api/entities/{sourceEntityId}/records/{srcRecId}/links");
        Assert.Equal(HttpStatusCode.OK, getLinksRes.StatusCode);
        var links = await getLinksRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(links.GetArrayLength() >= 1);
        var linkId = links[0].GetProperty("id").GetString();

        // Delete link
        var deleteLinkRes = await _client.DeleteAsync($"/api/entities/{sourceEntityId}/records/{srcRecId}/links/{linkId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteLinkRes.StatusCode);
    }

    [Fact]
    public async Task DeleteLink_NotFound_Returns404()
    {
        var entityId = await CreateEntityAsync("DeleteLinkNFEntity");
        var recRes = await _client.PostAsJsonAsync($"/api/entities/{entityId}/records", new { DataJson = """{"Name":"X"}""" });
        var rec = await recRes.Content.ReadFromJsonAsync<JsonElement>();
        var recId = rec.GetProperty("id").GetString();

        var response = await _client.DeleteAsync($"/api/entities/{entityId}/records/{recId}/links/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
