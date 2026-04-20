using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xrm.Server.Data;
using Xrm.Server.Models;

namespace Xrm.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntitiesController : ControllerBase
{
    private readonly XrmDbContext _db;

    public EntitiesController(XrmDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<EntityDefinition>>> GetAll()
    {
        return await _db.EntityDefinitions
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Name)
            .ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EntityDefinition>> Get(Guid id)
    {
        var entity = await _db.EntityDefinitions
            .Include(e => e.Fields.OrderBy(f => f.SortOrder))
            .Include(e => e.SourceRelationships)
            .Include(e => e.TargetRelationships)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null) return NotFound();
        return entity;
    }

    [HttpPost]
    public async Task<ActionResult<EntityDefinition>> Create(EntityDefinition entity)
    {
        entity.Id = Guid.NewGuid();
        _db.EntityDefinitions.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, EntityDefinition entity)
    {
        if (id != entity.Id) return BadRequest();
        var existing = await _db.EntityDefinitions.FindAsync(id);
        if (existing is null) return NotFound();

        existing.Name = entity.Name;
        existing.DisplayName = entity.DisplayName;
        existing.PluralName = entity.PluralName;
        existing.Description = entity.Description;
        existing.Icon = entity.Icon;
        existing.IsHomeEntity = entity.IsHomeEntity;
        existing.SortOrder = entity.SortOrder;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.EntityDefinitions.FindAsync(id);
        if (entity is null) return NotFound();

        _db.EntityDefinitions.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("seed-demo")]
    public async Task<IActionResult> SeedDemo()
    {
        await DemoDataSeeder.SeedAsync(_db);
        return Ok(new { message = "Demo data loaded" });
    }
}
