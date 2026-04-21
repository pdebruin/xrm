using Microsoft.AspNetCore.Mvc;
using Xrm.Server.Models;
using Xrm.Server.Services;

namespace Xrm.Server.Controllers;

[ApiController]
[Route("api/entities/{entityId:guid}/fields")]
public class FieldsController : ControllerBase
{
    private readonly IFieldService _fields;

    public FieldsController(IFieldService fields) => _fields = fields;

    [HttpGet]
    public async Task<ActionResult<List<FieldDefinition>>> GetAll(Guid entityId)
        => await _fields.GetAllAsync(entityId);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FieldDefinition>> Get(Guid entityId, Guid id)
    {
        var field = await _fields.GetByIdAsync(entityId, id);
        if (field is null) return NotFound();
        return field;
    }

    [HttpPost]
    public async Task<ActionResult<FieldDefinition>> Create(Guid entityId, FieldDefinition field)
    {
        var created = await _fields.CreateAsync(entityId, field);
        return CreatedAtAction(nameof(Get), new { entityId, id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid entityId, Guid id, FieldDefinition field)
    {
        var updated = await _fields.UpdateAsync(entityId, id, field);
        if (updated is null) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid entityId, Guid id)
    {
        var deleted = await _fields.DeleteAsync(entityId, id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}

