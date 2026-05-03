using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Xrm.Core.Models;
using Xrm.Core.Services;

namespace Xrm.Core.Controllers;

[ApiController]
[Route("api/entities/{entityId:guid}/records")]
public class RecordsController : ControllerBase
{
    private readonly IRecordService _records;
    private readonly IAuditService _audit;

    public RecordsController(IRecordService records, IAuditService audit)
    {
        _records = records;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<RecordPageDto>> GetAll(
        Guid entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortField = null,
        [FromQuery] string sortDir = "asc",
        [FromQuery] string? filter = null)
    {
        var result = await _records.GetAllAsync(entityId, page, pageSize, sortField, sortDir, filter);
        return new RecordPageDto
        {
            Records = result.Records,
            Total = result.Total,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Record>> Get(Guid entityId, Guid id)
    {
        var record = await _records.GetByIdAsync(entityId, id);
        if (record is null) return NotFound();
        return record;
    }

    [HttpPost]
    public async Task<ActionResult<Record>> Create(Guid entityId, [FromBody] JsonElement data)
    {
        var result = await _records.CreateAsync(entityId, data.GetRawText());
        if (!result.Success) return BadRequest();
        var response = CreatedAtAction(nameof(Get), new { entityId, id = result.Record!.Id }, result.Record);
        if (result.HasWarnings)
            Response.Headers["X-Warnings"] = string.Join("; ", result.Warnings!);
        return response;
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid entityId, Guid id, [FromBody] JsonElement data)
    {
        var result = await _records.UpdateAsync(entityId, id, data.GetRawText());
        if (!result.Success) return NotFound();
        if (result.HasWarnings)
            Response.Headers["X-Warnings"] = string.Join("; ", result.Warnings!);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid entityId, Guid id)
    {
        var deleted = await _records.DeleteAsync(entityId, id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("{id:guid}/links")]
    public async Task<ActionResult<List<RecordLinkDto>>> GetLinks(Guid entityId, Guid id)
    {
        var links = await _records.GetLinksAsync(entityId, id);
        return links.Select(l => new RecordLinkDto
        {
            Id = l.Id,
            RelationshipId = l.RelationshipId,
            RelationshipName = l.RelationshipName,
            ParentRecordId = l.ParentRecordId,
            ChildRecordId = l.ChildRecordId,
            Direction = l.Direction
        }).ToList();
    }

    [HttpPost("{id:guid}/links")]
    public async Task<IActionResult> CreateLink(Guid entityId, Guid id, [FromBody] CreateLinkRequest request)
    {
        var link = await _records.CreateLinkAsync(id, request.RelationshipDefinitionId, request.ChildRecordId);
        return Ok(link);
    }

    [HttpDelete("{id:guid}/links/{linkId:guid}")]
    public async Task<IActionResult> DeleteLink(Guid entityId, Guid id, Guid linkId)
    {
        var deleted = await _records.DeleteLinkAsync(linkId);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<List<AuditEntry>>> GetHistory(Guid entityId, Guid id, [FromQuery] int limit = 50)
    {
        var entries = await _audit.GetHistoryAsync(id, limit);
        return entries;
    }
}

public class RecordPageDto
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
    public Guid ParentRecordId { get; set; }
    public Guid ChildRecordId { get; set; }
    public string Direction { get; set; } = string.Empty;
}

public class CreateLinkRequest
{
    public Guid RelationshipDefinitionId { get; set; }
    public Guid ChildRecordId { get; set; }
}
