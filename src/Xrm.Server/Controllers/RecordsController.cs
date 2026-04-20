using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xrm.Server.Data;
using Xrm.Server.Models;

namespace Xrm.Server.Controllers;

[ApiController]
[Route("api/entities/{entityId:guid}/records")]
public class RecordsController : ControllerBase
{
    private readonly XrmDbContext _db;

    public RecordsController(XrmDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<RecordListResponse>> GetAll(
        Guid entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortField = null,
        [FromQuery] string sortDir = "asc",
        [FromQuery] string? filter = null)
    {
        var query = _db.Records.Where(r => r.EntityDefinitionId == entityId);

        var total = await query.CountAsync();

        // Basic sorting by CreatedAt if no field specified
        query = sortField is null
            ? query.OrderByDescending(r => r.CreatedAt)
            : (sortDir == "desc"
                ? query.OrderByDescending(r => r.DataJson)
                : query.OrderBy(r => r.DataJson));

        var records = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // If filter is specified, do client-side filtering on JSON data
        if (!string.IsNullOrWhiteSpace(filter))
        {
            records = records
                .Where(r => r.DataJson.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return new RecordListResponse
        {
            Records = records,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Record>> Get(Guid entityId, Guid id)
    {
        var record = await _db.Records
            .FirstOrDefaultAsync(r => r.Id == id && r.EntityDefinitionId == entityId);
        if (record is null) return NotFound();
        return record;
    }

    [HttpPost]
    public async Task<ActionResult<Record>> Create(Guid entityId, [FromBody] JsonElement data)
    {
        var record = new Record
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entityId,
            DataJson = data.ToString()
        };
        _db.Records.Add(record);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { entityId, id = record.Id }, record);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid entityId, Guid id, [FromBody] JsonElement data)
    {
        var record = await _db.Records
            .FirstOrDefaultAsync(r => r.Id == id && r.EntityDefinitionId == entityId);
        if (record is null) return NotFound();

        record.DataJson = data.ToString();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid entityId, Guid id)
    {
        var record = await _db.Records
            .FirstOrDefaultAsync(r => r.Id == id && r.EntityDefinitionId == entityId);
        if (record is null) return NotFound();

        // Handle cascade deletes on links
        var links = await _db.RecordLinks
            .Where(l => l.SourceRecordId == id || l.TargetRecordId == id)
            .ToListAsync();
        _db.RecordLinks.RemoveRange(links);

        _db.Records.Remove(record);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/links")]
    public async Task<ActionResult<List<RecordLinkDto>>> GetLinks(Guid entityId, Guid id)
    {
        var links = await _db.RecordLinks
            .Include(l => l.RelationshipDefinition)
            .Where(l => l.SourceRecordId == id || l.TargetRecordId == id)
            .Select(l => new RecordLinkDto
            {
                Id = l.Id,
                RelationshipId = l.RelationshipDefinitionId,
                RelationshipName = l.RelationshipDefinition.DisplayName ?? l.RelationshipDefinition.Name,
                SourceRecordId = l.SourceRecordId,
                TargetRecordId = l.TargetRecordId,
                Direction = l.SourceRecordId == id ? "outgoing" : "incoming"
            })
            .ToListAsync();

        return links;
    }

    [HttpPost("{id:guid}/links")]
    public async Task<IActionResult> CreateLink(Guid entityId, Guid id, [FromBody] CreateLinkRequest request)
    {
        var link = new RecordLink
        {
            Id = Guid.NewGuid(),
            RelationshipDefinitionId = request.RelationshipDefinitionId,
            SourceRecordId = id,
            TargetRecordId = request.TargetRecordId
        };
        _db.RecordLinks.Add(link);
        await _db.SaveChangesAsync();
        return Ok(link);
    }

    [HttpDelete("{id:guid}/links/{linkId:guid}")]
    public async Task<IActionResult> DeleteLink(Guid entityId, Guid id, Guid linkId)
    {
        var link = await _db.RecordLinks.FindAsync(linkId);
        if (link is null) return NotFound();

        _db.RecordLinks.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class RecordListResponse
{
    public List<Record> Records { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class RecordLinkDto
{
    public Guid Id { get; set; }
    public Guid RelationshipId { get; set; }
    public string RelationshipName { get; set; } = string.Empty;
    public Guid SourceRecordId { get; set; }
    public Guid TargetRecordId { get; set; }
    public string Direction { get; set; } = string.Empty;
}

public class CreateLinkRequest
{
    public Guid RelationshipDefinitionId { get; set; }
    public Guid TargetRecordId { get; set; }
}
