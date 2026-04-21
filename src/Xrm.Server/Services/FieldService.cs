using Microsoft.EntityFrameworkCore;
using Xrm.Server.Data;
using Xrm.Server.Models;

namespace Xrm.Server.Services;

public class FieldService : IFieldService
{
    private readonly IDbContextFactory<XrmDbContext> _dbFactory;

    public FieldService(IDbContextFactory<XrmDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<FieldDefinition>> GetAllAsync(Guid entityId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FieldDefinitions
            .Where(f => f.EntityDefinitionId == entityId)
            .OrderBy(f => f.SortOrder)
            .ToListAsync();
    }

    public async Task<FieldDefinition?> GetByIdAsync(Guid entityId, Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FieldDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.EntityDefinitionId == entityId);
    }

    public async Task<FieldDefinition> CreateAsync(Guid entityId, FieldDefinition field)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Validate entity exists
        var entityExists = await db.EntityDefinitions.AnyAsync(e => e.Id == entityId);
        if (!entityExists)
            throw new InvalidOperationException($"Entity {entityId} not found");

        field.Id = Guid.NewGuid();
        field.EntityDefinitionId = entityId;
        db.FieldDefinitions.Add(field);
        await db.SaveChangesAsync();
        return field;
    }

    public async Task<FieldDefinition?> UpdateAsync(Guid entityId, Guid id, FieldDefinition field)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.FieldDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.EntityDefinitionId == entityId);
        if (existing is null) return null;

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

        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid entityId, Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var field = await db.FieldDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.EntityDefinitionId == entityId);
        if (field is null) return false;

        db.FieldDefinitions.Remove(field);
        await db.SaveChangesAsync();
        return true;
    }
}
