using Microsoft.EntityFrameworkCore;
using Xrm.Core.Data;
using Xrm.Core.Models;

namespace Xrm.Core.Services;

public class EntityService : IEntityService
{
    private readonly IDbContextFactory<XrmDbContext> _dbFactory;
    private readonly IDataSeeder _seeder;

    public EntityService(IDbContextFactory<XrmDbContext> dbFactory, IDataSeeder seeder)
    {
        _dbFactory = dbFactory;
        _seeder = seeder;
    }

    public async Task<List<EntityDefinition>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.EntityDefinitions
            .OrderBy(e => e.Domain ?? "")
            .ThenBy(e => e.DomainSortOrder ?? e.SortOrder)
            .ThenBy(e => e.SortOrder)
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
        existing.Domain = entity.Domain;
        existing.DomainSortOrder = entity.DomainSortOrder;
        existing.PrimaryFieldId = entity.PrimaryFieldId;

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
        await _seeder.SeedAsync(db);
    }
}
