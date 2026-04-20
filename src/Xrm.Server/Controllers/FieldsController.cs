using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xrm.Server.Data;
using Xrm.Server.Models;

namespace Xrm.Server.Controllers;

[ApiController]
[Route("api/entities/{entityId:guid}/fields")]
public class FieldsController : ControllerBase
{
    private readonly XrmDbContext _db;

    public FieldsController(XrmDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<FieldDefinition>>> GetAll(Guid entityId)
    {
        return await _db.FieldDefinitions
            .Where(f => f.EntityDefinitionId == entityId)
            .OrderBy(f => f.SortOrder)
            .ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FieldDefinition>> Get(Guid entityId, Guid id)
    {
        var field = await _db.FieldDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.EntityDefinitionId == entityId);
        if (field is null) return NotFound();
        return field;
    }

    [HttpPost]
    public async Task<ActionResult<FieldDefinition>> Create(Guid entityId, FieldDefinition field)
    {
        field.Id = Guid.NewGuid();
        field.EntityDefinitionId = entityId;
        _db.FieldDefinitions.Add(field);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { entityId, id = field.Id }, field);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid entityId, Guid id, FieldDefinition field)
    {
        if (id != field.Id) return BadRequest();
        var existing = await _db.FieldDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.EntityDefinitionId == entityId);
        if (existing is null) return NotFound();

        existing.Name = field.Name;
        existing.DisplayName = field.DisplayName;
        existing.DataType = field.DataType;
        existing.IsRequired = field.IsRequired;
        existing.DefaultValue = field.DefaultValue;
        existing.SortOrder = field.SortOrder;
        existing.MaxLength = field.MaxLength;
        existing.MinValue = field.MinValue;
        existing.MaxValue = field.MaxValue;
        existing.Pattern = field.Pattern;
        existing.OptionsJson = field.OptionsJson;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid entityId, Guid id)
    {
        var field = await _db.FieldDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.EntityDefinitionId == entityId);
        if (field is null) return NotFound();

        _db.FieldDefinitions.Remove(field);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
