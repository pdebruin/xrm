using Microsoft.EntityFrameworkCore;
using Xrm.Core.Data;
using Xrm.Core.Models;

namespace Xrm.Core.Services;

public class AuditService : IAuditService
{
    private readonly IDbContextFactory<XrmDbContext> _dbFactory;

    public AuditService(IDbContextFactory<XrmDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<AuditEntry>> GetHistoryAsync(Guid recordId, int limit = 50)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AuditEntries
            .Where(a => a.RecordId == recordId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AuditEntry>> GetEntityHistoryAsync(Guid entityDefinitionId, int limit = 100)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AuditEntries
            .Where(a => a.EntityDefinitionId == entityDefinitionId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}
