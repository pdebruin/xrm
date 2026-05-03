using Xrm.Core.Models;

namespace Xrm.Core.Services;

public interface IRelationshipService
{
    Task<List<RelationshipDefinition>> GetAllAsync();
    Task<RelationshipDefinition?> GetByIdAsync(Guid id);
    Task<(List<RelationshipDefinition> AsParent, List<RelationshipDefinition> AsChild)> GetForEntityAsync(Guid entityId);
    Task<RelationshipDefinition> CreateAsync(RelationshipDefinition rel);
    Task<RelationshipDefinition?> UpdateAsync(Guid id, RelationshipDefinition rel);
    Task<bool> DeleteAsync(Guid id);
}
