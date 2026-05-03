using Xrm.Core.Models;

namespace Xrm.Core.Services;

public interface IAuditService
{
    Task<List<AuditEntry>> GetHistoryAsync(Guid recordId, int limit = 50);
    Task<List<AuditEntry>> GetEntityHistoryAsync(Guid entityDefinitionId, int limit = 100);
}
