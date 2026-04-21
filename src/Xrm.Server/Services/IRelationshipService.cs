using Xrm.Server.Models;

namespace Xrm.Server.Services;

public interface IRelationshipService
{
    Task<List<RelationshipDefinition>> GetAllAsync();
    Task<RelationshipDefinition?> GetByIdAsync(Guid id);
    Task<(List<RelationshipDefinition> Source, List<RelationshipDefinition> Target)> GetForEntityAsync(Guid entityId);
    Task<RelationshipDefinition> CreateAsync(RelationshipDefinition rel);
    Task<RelationshipDefinition?> UpdateAsync(Guid id, RelationshipDefinition rel);
    Task<bool> DeleteAsync(Guid id);
}
