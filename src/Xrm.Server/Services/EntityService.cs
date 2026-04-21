using Microsoft.EntityFrameworkCore;
using Xrm.Server.Data;
using Xrm.Server.Models;

namespace Xrm.Server.Services;

public class EntityService : IEntityService
{
    private readonly IDbContextFactory<XrmDbContext> _dbFactory;

    public EntityService(IDbContextFactory<XrmDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<EntityDefinition>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.EntityDefinitions
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<EntityDefinition?> GetByIdAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.EntityDefinitions
            .Include(e => e.Fields.OrderBy(f => f.SortOrder))
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<EntityDefinition> CreateAsync(EntityDefinition entity)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        entity.Id = Guid.NewGuid();

        // If marked as home, clear others
        if (entity.IsHomeEntity)
        {
            await db.EntityDefinitions
                .Where(e => e.IsHomeEntity)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsHomeEntity, false));
        }

        db.EntityDefinitions.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<EntityDefinition?> UpdateAsync(Guid id, EntityDefinition entity)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.EntityDefinitions.FindAsync(id);
        if (existing is null) return null;

        // If marking as home, clear others
        if (entity.IsHomeEntity && !existing.IsHomeEntity)
        {
            await db.EntityDefinitions
                .Where(e => e.IsHomeEntity && e.Id != id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsHomeEntity, false));
        }

        existing.Name = entity.Name;
        existing.DisplayName = entity.DisplayName;
        existing.PluralName = entity.PluralName;
        existing.Description = entity.Description;
        existing.Icon = entity.Icon;
        existing.IsHomeEntity = entity.IsHomeEntity;
        existing.SortOrder = entity.SortOrder;

        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.EntityDefinitions.FindAsync(id);
        if (entity is null) return false;

        db.EntityDefinitions.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task SeedDemoAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await DemoDataSeeder.SeedAsync(db);
    }
}
