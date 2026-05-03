using Microsoft.EntityFrameworkCore;
using Xrm.Core.Data;
using Xrm.Core.Models;

namespace Xrm.Core.Services;

public class RelationshipService : IRelationshipService
{
    private readonly IDbContextFactory<XrmDbContext> _dbFactory;

    public RelationshipService(IDbContextFactory<XrmDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<RelationshipDefinition>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.RelationshipDefinitions
            .Include(r => r.ParentEntity)
            .Include(r => r.ChildEntity)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<RelationshipDefinition?> GetByIdAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.RelationshipDefinitions
            .Include(r => r.ParentEntity)
            .Include(r => r.ChildEntity)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<(List<RelationshipDefinition> AsParent, List<RelationshipDefinition> AsChild)> GetForEntityAsync(Guid entityId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var asParent = await db.RelationshipDefinitions
            .Where(r => r.ParentEntityId == entityId)
            .ToListAsync();
        var asChild = await db.RelationshipDefinitions
            .Where(r => r.ChildEntityId == entityId)
            .ToListAsync();
        return (asParent, asChild);
    }

    public async Task<RelationshipDefinition> CreateAsync(RelationshipDefinition rel)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Validate both entities exist
        var parentExists = await db.EntityDefinitions.AnyAsync(e => e.Id == rel.ParentEntityId);
        var childExists = await db.EntityDefinitions.AnyAsync(e => e.Id == rel.ChildEntityId);
        if (!parentExists || !childExists)
            throw new InvalidOperationException("Parent or child entity not found");

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
        existing.ParentEntityId = rel.ParentEntityId;
        existing.ChildEntityId = rel.ChildEntityId;
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
