using Microsoft.AspNetCore.Mvc;
using Xrm.Server.Models;
using Xrm.Server.Services;

namespace Xrm.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntitiesController : ControllerBase
{
    private readonly IEntityService _entities;
    private readonly IRelationshipService _relationships;

    public EntitiesController(IEntityService entities, IRelationshipService relationships)
    {
        _entities = entities;
        _relationships = relationships;
    }

    [HttpGet]
    public async Task<ActionResult<List<EntityDefinition>>> GetAll()
        => await _entities.GetAllAsync();

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EntityDefinition>> Get(Guid id)
    {
        var entity = await _entities.GetByIdAsync(id);
        if (entity is null) return NotFound();
        return entity;
    }

    [HttpGet("{id:guid}/relationships")]
    public async Task<ActionResult<EntityRelationshipsDto>> GetRelationships(Guid id)
    {
        var (source, target) = await _relationships.GetForEntityAsync(id);
        return new EntityRelationshipsDto { SourceRelationships = source, TargetRelationships = target };
    }

    [HttpPost]
    public async Task<ActionResult<EntityDefinition>> Create(EntityDefinition entity)
    {
        var created = await _entities.CreateAsync(entity);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, EntityDefinition entity)
    {
        var updated = await _entities.UpdateAsync(id, entity);
        if (updated is null) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _entities.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("seed-demo")]
    public async Task<IActionResult> SeedDemo()
    {
        await _entities.SeedDemoAsync();
        return Ok(new { message = "Demo data loaded" });
    }
}

public class EntityRelationshipsDto
{
    public List<RelationshipDefinition> SourceRelationships { get; set; } = new();
    public List<RelationshipDefinition> TargetRelationships { get; set; } = new();
}

