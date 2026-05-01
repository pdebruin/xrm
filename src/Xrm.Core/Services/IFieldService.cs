using Xrm.Core.Models;

namespace Xrm.Core.Services;

public interface IFieldService
{
    Task<List<FieldDefinition>> GetAllAsync(Guid entityId);
    Task<FieldDefinition?> GetByIdAsync(Guid entityId, Guid id);
    Task<FieldDefinition> CreateAsync(Guid entityId, FieldDefinition field);
    Task<FieldDefinition?> UpdateAsync(Guid entityId, Guid id, FieldDefinition field);
    Task<bool> DeleteAsync(Guid entityId, Guid id);
}
