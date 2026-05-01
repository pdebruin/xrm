using Xrm.Core.Models;

namespace Xrm.Core.Services;

public interface IEntityService
{
    Task<List<EntityDefinition>> GetAllAsync();
    Task<EntityDefinition?> GetByIdAsync(Guid id);
    Task<EntityDefinition> CreateAsync(EntityDefinition entity);
    Task<EntityDefinition?> UpdateAsync(Guid id, EntityDefinition entity);
    Task<bool> DeleteAsync(Guid id);
    Task SeedDemoAsync();
}
