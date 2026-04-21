using Microsoft.EntityFrameworkCore;
using Xrm.Server.Data;
using Xrm.Server.Models;

namespace Xrm.Server.Services;

public class RecordService : IRecordService
{
    private readonly IDbContextFactory<XrmDbContext> _dbFactory;

    public RecordService(IDbContextFactory<XrmDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<RecordPage> GetAllAsync(Guid entityId, int page = 1, int pageSize = 25, string? sortField = null, string sortDir = "asc", string? filter = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Records.Where(r => r.EntityDefinitionId == entityId);

        // Client-side filter on JSON content
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(r => r.DataJson.Contains(filter));
        }

        var total = await query.CountAsync();

        // Sort by creation date (JSON field sorting would need computed columns)
        query = sortDir == "desc"
            ? query.OrderByDescending(r => r.CreatedAt)
            : query.OrderBy(r => r.CreatedAt);

        var records = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new RecordPage(records, total, page, pageSize);
    }

    public async Task<Record?> GetByIdAsync(Guid entityId, Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Records
            .FirstOrDefaultAsync(r => r.Id == id && r.EntityDefinitionId == entityId);
    }

    public async Task<Record> CreateAsync(Guid entityId, string dataJson)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var record = new Record
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entityId,
            DataJson = dataJson
        };
        db.Records.Add(record);
        await db.SaveChangesAsync();
        return record;
    }

    public async Task<bool> UpdateAsync(Guid entityId, Guid id, string dataJson)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var record = await db.Records
            .FirstOrDefaultAsync(r => r.Id == id && r.EntityDefinitionId == entityId);
        if (record is null) return false;

        record.DataJson = dataJson;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid entityId, Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var record = await db.Records
            .FirstOrDefaultAsync(r => r.Id == id && r.EntityDefinitionId == entityId);
        if (record is null) return false;

        // Remove associated links
        var links = await db.RecordLinks
            .Where(l => l.SourceRecordId == id || l.TargetRecordId == id)
            .ToListAsync();
        db.RecordLinks.RemoveRange(links);
        db.Records.Remove(record);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<RecordLinkInfo>> GetLinksAsync(Guid entityId, Guid recordId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.RecordLinks
            .Include(l => l.RelationshipDefinition)
            .Where(l => l.SourceRecordId == recordId || l.TargetRecordId == recordId)
            .Select(l => new RecordLinkInfo(
                l.Id,
                l.RelationshipDefinitionId,
                l.RelationshipDefinition!.DisplayName ?? l.RelationshipDefinition.Name,
                l.SourceRecordId,
                l.TargetRecordId,
                l.SourceRecordId == recordId ? "outgoing" : "incoming"
            ))
            .ToListAsync();
    }

    public async Task<RecordLink> CreateLinkAsync(Guid recordId, Guid relationshipId, Guid targetRecordId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var link = new RecordLink
        {
            Id = Guid.NewGuid(),
            RelationshipDefinitionId = relationshipId,
            SourceRecordId = recordId,
            TargetRecordId = targetRecordId
        };
        db.RecordLinks.Add(link);
        await db.SaveChangesAsync();
        return link;
    }

    public async Task<bool> DeleteLinkAsync(Guid linkId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var link = await db.RecordLinks.FindAsync(linkId);
        if (link is null) return false;

        db.RecordLinks.Remove(link);
        await db.SaveChangesAsync();
        return true;
    }
}
