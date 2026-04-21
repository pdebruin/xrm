using Microsoft.AspNetCore.Mvc;
using Xrm.Server.Models;
using Xrm.Server.Services;

namespace Xrm.Server.Controllers;

[ApiController]
[Route("api/relationships")]
public class RelationshipsController : ControllerBase
{
    private readonly IRelationshipService _relationships;

    public RelationshipsController(IRelationshipService relationships) => _relationships = relationships;

    [HttpGet]
    public async Task<ActionResult<List<RelationshipDefinition>>> GetAll()
        => await _relationships.GetAllAsync();

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RelationshipDefinition>> Get(Guid id)
    {
        var rel = await _relationships.GetByIdAsync(id);
        if (rel is null) return NotFound();
        return rel;
    }

    [HttpPost]
    public async Task<ActionResult<RelationshipDefinition>> Create(RelationshipDefinition rel)
    {
        var created = await _relationships.CreateAsync(rel);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, RelationshipDefinition rel)
    {
        var updated = await _relationships.UpdateAsync(id, rel);
        if (updated is null) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _relationships.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}

