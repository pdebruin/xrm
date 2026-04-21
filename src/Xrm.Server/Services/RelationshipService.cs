using Microsoft.EntityFrameworkCore;
using Xrm.Server.Data;
using Xrm.Server.Models;

namespace Xrm.Server.Services;

public class RelationshipService : IRelationshipService
{
    private readonly IDbContextFactory<XrmDbContext> _dbFactory;

    public RelationshipService(IDbContextFactory<XrmDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<RelationshipDefinition>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.RelationshipDefinitions
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<RelationshipDefinition?> GetByIdAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.RelationshipDefinitions
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<(List<RelationshipDefinition> Source, List<RelationshipDefinition> Target)> GetForEntityAsync(Guid entityId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var source = await db.RelationshipDefinitions
            .Where(r => r.SourceEntityId == entityId)
            .ToListAsync();
        var target = await db.RelationshipDefinitions
            .Where(r => r.TargetEntityId == entityId)
            .ToListAsync();
        return (source, target);
    }

    public async Task<RelationshipDefinition> CreateAsync(RelationshipDefinition rel)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Validate both entities exist
        var sourceExists = await db.EntityDefinitions.AnyAsync(e => e.Id == rel.SourceEntityId);
        var targetExists = await db.EntityDefinitions.AnyAsync(e => e.Id == rel.TargetEntityId);
        if (!sourceExists || !targetExists)
            throw new InvalidOperationException("Source or target entity not found");

        rel.Id = Guid.NewGuid();
        db.RelationshipDefinitions.Add(rel);
        await db.SaveChangesAsync();
        return rel;
    }

    public async Task<RelationshipDefinition?> UpdateAsync(Guid id, RelationshipDefinition rel)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.RelationshipDefinitions.FindAsync(id);
        if (existing is null) return null;

        existing.Name = rel.Name;
        existing.DisplayName = rel.DisplayName;
        existing.SourceEntityId = rel.SourceEntityId;
        existing.TargetEntityId = rel.TargetEntityId;
        existing.RelationshipType = rel.RelationshipType;
        existing.CascadeBehavior = rel.CascadeBehavior;

        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rel = await db.RelationshipDefinitions.FindAsync(id);
        if (rel is null) return false;

        db.RelationshipDefinitions.Remove(rel);
        await db.SaveChangesAsync();
        return true;
    }
}
