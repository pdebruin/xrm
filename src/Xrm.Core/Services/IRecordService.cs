using System.Text.Json;
using Xrm.Core.Models;

namespace Xrm.Core.Services;

public record RecordPage(List<Record> Records, int Total, int Page, int PageSize);

public record RecordLinkInfo(Guid Id, Guid RelationshipId, string RelationshipName, Guid ParentRecordId, Guid ChildRecordId, string Direction);

public record SaveResult(bool Success, Record? Record = null, List<string>? Warnings = null)
{
    public bool HasWarnings => Warnings is { Count: > 0 };
}

public interface IRecordService
{
    Task<RecordPage> GetAllAsync(Guid entityId, int page = 1, int pageSize = 25, string? sortField = null, string sortDir = "asc", string? filter = null, List<ViewFilter>? viewFilters = null);
    Task<Record?> GetByIdAsync(Guid entityId, Guid id);
    Task<SaveResult> CreateAsync(Guid entityId, string dataJson);
    Task<SaveResult> UpdateAsync(Guid entityId, Guid id, string dataJson);
    Task<bool> DeleteAsync(Guid entityId, Guid id);
    Task<List<RecordLinkInfo>> GetLinksAsync(Guid entityId, Guid recordId);
    Task<RecordLink> CreateLinkAsync(Guid recordId, Guid relationshipId, Guid childRecordId);
    Task<bool> DeleteLinkAsync(Guid linkId);
}
