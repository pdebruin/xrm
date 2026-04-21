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

        // Server-side filter on JSON content
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(r => r.DataJson.Contains(filter));
        }

        var total = await query.CountAsync();

        // If sorting by a field, we need to materialize and sort client-side
        // since values are stored inside JSON. For default/no field, sort by CreatedAt.
        if (string.IsNullOrEmpty(sortField))
        {
            query = sortDir == "desc"
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt);

            var records = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new RecordPage(records, total, page, pageSize);
        }
        else
        {
            // Materialize all matching records, sort by JSON field value, then page
            var all = await query.ToListAsync();
            var sorted = SortByJsonField(all, sortField, sortDir);
            var paged = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new RecordPage(paged, total, page, pageSize);
        }
    }

    private static List<Record> SortByJsonField(List<Record> records, string fieldName, string dir)
    {
        return (dir == "desc"
            ? records.OrderByDescending(r => ExtractSortKey(r.DataJson, fieldName))
            : records.OrderBy(r => ExtractSortKey(r.DataJson, fieldName))
        ).ToList();
    }

    private static IComparable ExtractSortKey(string dataJson, string fieldName)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(dataJson);
            if (doc.RootElement.TryGetProperty(fieldName, out var val))
            {
                return val.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.Number => val.GetDouble(),
                    System.Text.Json.JsonValueKind.True => 1,
                    System.Text.Json.JsonValueKind.False => 0,
                    System.Text.Json.JsonValueKind.Null => "",
                    _ => val.ToString() ?? ""
                };
            }
        }
        catch { }
        return "";
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
        await ValidateRecordData(db, entityId, dataJson);

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

        await ValidateRecordData(db, entityId, dataJson);
        record.DataJson = dataJson;
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task ValidateRecordData(XrmDbContext db, Guid entityId, string dataJson)
    {
        var fields = await db.FieldDefinitions
            .Where(f => f.EntityDefinitionId == entityId)
            .ToListAsync();

        if (fields.Count == 0) return;

        Dictionary<string, System.Text.Json.JsonElement> data;
        try
        {
            data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(dataJson) ?? new();
        }
        catch
        {
            throw new InvalidOperationException("Invalid JSON data");
        }

        var errors = new List<string>();

        foreach (var field in fields)
        {
            var hasValue = data.TryGetValue(field.Name, out var val)
                && val.ValueKind != System.Text.Json.JsonValueKind.Null
                && !(val.ValueKind == System.Text.Json.JsonValueKind.String && string.IsNullOrWhiteSpace(val.GetString()));

            if (field.IsRequired && !hasValue)
            {
                errors.Add($"'{field.DisplayName ?? field.Name}' is required");
                continue;
            }

            if (!hasValue) continue;

            var strVal = val.ToString();

            if (field.MaxLength.HasValue && strVal.Length > field.MaxLength.Value)
                errors.Add($"'{field.DisplayName ?? field.Name}' exceeds max length of {field.MaxLength.Value}");

            if ((field.MinValue.HasValue || field.MaxValue.HasValue) && double.TryParse(strVal, out var numVal))
            {
                if (field.MinValue.HasValue && numVal < field.MinValue.Value)
                    errors.Add($"'{field.DisplayName ?? field.Name}' must be ≥ {field.MinValue.Value}");
                if (field.MaxValue.HasValue && numVal > field.MaxValue.Value)
                    errors.Add($"'{field.DisplayName ?? field.Name}' must be ≤ {field.MaxValue.Value}");
            }

            if (!string.IsNullOrEmpty(field.Pattern))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(strVal, field.Pattern))
                    errors.Add($"'{field.DisplayName ?? field.Name}' does not match required pattern");
            }

            if (field.DataType == FieldDataType.Choice && !string.IsNullOrEmpty(field.OptionsJson))
            {
                var options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(field.OptionsJson) ?? new();
                if (options.Count > 0 && !options.Contains(strVal))
                    errors.Add($"'{field.DisplayName ?? field.Name}' must be one of: {string.Join(", ", options)}");
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("; ", errors));
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

        // Validate relationship exists
        var rel = await db.RelationshipDefinitions.FindAsync(relationshipId)
            ?? throw new InvalidOperationException($"Relationship {relationshipId} not found");

        // Validate source record belongs to the relationship's source entity
        var sourceRecord = await db.Records.FirstOrDefaultAsync(r => r.Id == recordId)
            ?? throw new InvalidOperationException($"Source record {recordId} not found");
        if (sourceRecord.EntityDefinitionId != rel.SourceEntityId)
            throw new InvalidOperationException($"Source record does not belong to entity {rel.SourceEntityId}");

        // Validate target record belongs to the relationship's target entity
        var targetRecord = await db.Records.FirstOrDefaultAsync(r => r.Id == targetRecordId)
            ?? throw new InvalidOperationException($"Target record {targetRecordId} not found");
        if (targetRecord.EntityDefinitionId != rel.TargetEntityId)
            throw new InvalidOperationException($"Target record does not belong to entity {rel.TargetEntityId}");

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
