using System.Text.Json;
using Xrm.Server.Models;

namespace Xrm.Server.Services;

public record RecordPage(List<Record> Records, int Total, int Page, int PageSize);

public record RecordLinkInfo(Guid Id, Guid RelationshipId, string RelationshipName, Guid SourceRecordId, Guid TargetRecordId, string Direction);

public interface IRecordService
{
    Task<RecordPage> GetAllAsync(Guid entityId, int page = 1, int pageSize = 25, string? sortField = null, string sortDir = "asc", string? filter = null);
    Task<Record?> GetByIdAsync(Guid entityId, Guid id);
    Task<Record> CreateAsync(Guid entityId, string dataJson);
    Task<bool> UpdateAsync(Guid entityId, Guid id, string dataJson);
    Task<bool> DeleteAsync(Guid entityId, Guid id);
    Task<List<RecordLinkInfo>> GetLinksAsync(Guid entityId, Guid recordId);
    Task<RecordLink> CreateLinkAsync(Guid recordId, Guid relationshipId, Guid targetRecordId);
    Task<bool> DeleteLinkAsync(Guid linkId);
}
