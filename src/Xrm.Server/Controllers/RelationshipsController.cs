using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xrm.Server.Data;
using Xrm.Server.Models;

namespace Xrm.Server.Controllers;

[ApiController]
[Route("api/relationships")]
public class RelationshipsController : ControllerBase
{
    private readonly XrmDbContext _db;

    public RelationshipsController(XrmDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<RelationshipDefinition>>> GetAll()
    {
        return await _db.RelationshipDefinitions
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RelationshipDefinition>> Get(Guid id)
    {
        var rel = await _db.RelationshipDefinitions
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rel is null) return NotFound();
        return rel;
    }

    [HttpPost]
    public async Task<ActionResult<RelationshipDefinition>> Create(RelationshipDefinition rel)
    {
        rel.Id = Guid.NewGuid();
        _db.RelationshipDefinitions.Add(rel);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = rel.Id }, rel);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, RelationshipDefinition rel)
    {
        if (id != rel.Id) return BadRequest();
        var existing = await _db.RelationshipDefinitions.FindAsync(id);
        if (existing is null) return NotFound();

        existing.Name = rel.Name;
        existing.DisplayName = rel.DisplayName;
        existing.SourceEntityId = rel.SourceEntityId;
        existing.TargetEntityId = rel.TargetEntityId;
        existing.RelationshipType = rel.RelationshipType;
        existing.CascadeBehavior = rel.CascadeBehavior;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var rel = await _db.RelationshipDefinitions.FindAsync(id);
        if (rel is null) return NotFound();

        _db.RelationshipDefinitions.Remove(rel);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
