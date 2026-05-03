using Microsoft.EntityFrameworkCore;
using Xrm.Core.Data;
using Xrm.Core.Models;

namespace Xrm.Core.Services;

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

        // Generate AutoNumber values
        dataJson = await ApplyAutoNumbers(db, entityId, dataJson);

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
            // AutoNumber fields are system-generated; skip validation
            if (field.DataType == FieldDataType.AutoNumber) continue;

            var hasValue = data.TryGetValue(field.Name, out var val)
                && val.ValueKind != System.Text.Json.JsonValueKind.Null
                && !(val.ValueKind == System.Text.Json.JsonValueKind.String && string.IsNullOrWhiteSpace(val.GetString()))
                && !(val.ValueKind == System.Text.Json.JsonValueKind.Array && val.GetArrayLength() == 0);

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

            if (field.DataType == FieldDataType.MultiChoice && !string.IsNullOrEmpty(field.OptionsJson))
            {
                var options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(field.OptionsJson) ?? new();
                if (options.Count > 0 && val.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var selected = val.EnumerateArray()
                        .Select(e => e.GetString() ?? "").Where(s => s != "").ToList();
                    var invalid = selected.Where(s => !options.Contains(s)).ToList();
                    if (invalid.Count > 0)
                        errors.Add($"'{field.DisplayName ?? field.Name}' contains invalid values: {string.Join(", ", invalid)}");
                }
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
            .Where(l => l.ParentRecordId == id || l.ChildRecordId == id)
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
            .Where(l => l.ParentRecordId == recordId || l.ChildRecordId == recordId)
            .Select(l => new RecordLinkInfo(
                l.Id,
                l.RelationshipDefinitionId,
                l.RelationshipDefinition!.DisplayName ?? l.RelationshipDefinition.Name,
                l.ParentRecordId,
                l.ChildRecordId,
                l.ParentRecordId == recordId ? "outgoing" : "incoming"
            ))
            .ToListAsync();
    }

    public async Task<RecordLink> CreateLinkAsync(Guid recordId, Guid relationshipId, Guid childRecordId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Validate relationship exists
        var rel = await db.RelationshipDefinitions.FindAsync(relationshipId)
            ?? throw new InvalidOperationException($"Relationship {relationshipId} not found");

        // Validate parent record belongs to the relationship's parent entity
        var parentRecord = await db.Records.FirstOrDefaultAsync(r => r.Id == recordId)
            ?? throw new InvalidOperationException($"Parent record {recordId} not found");
        if (parentRecord.EntityDefinitionId != rel.ParentEntityId)
            throw new InvalidOperationException($"Parent record does not belong to entity {rel.ParentEntityId}");

        // Validate child record belongs to the relationship's child entity
        var childRecord = await db.Records.FirstOrDefaultAsync(r => r.Id == childRecordId)
            ?? throw new InvalidOperationException($"Child record {childRecordId} not found");
        if (childRecord.EntityDefinitionId != rel.ChildEntityId)
            throw new InvalidOperationException($"Child record does not belong to entity {rel.ChildEntityId}");

        var link = new RecordLink
        {
            Id = Guid.NewGuid(),
            RelationshipDefinitionId = relationshipId,
            ParentRecordId = recordId,
            ChildRecordId = childRecordId
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

    private async Task<string> ApplyAutoNumbers(XrmDbContext db, Guid entityId, string dataJson)
    {
        var autoFields = await db.FieldDefinitions
            .Where(f => f.EntityDefinitionId == entityId && f.DataType == FieldDataType.AutoNumber)
            .ToListAsync();

        if (autoFields.Count == 0) return dataJson;

        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(dataJson) ?? new();

        foreach (var field in autoFields)
        {
            var config = ParseAutoNumberConfig(field.DefaultValue);

            // Get or create sequence
            var seq = await db.AutoNumberSequences
                .FirstOrDefaultAsync(s => s.FieldDefinitionId == field.Id);
            if (seq is null)
            {
                seq = new AutoNumberSequence { Id = Guid.NewGuid(), FieldDefinitionId = field.Id, NextValue = 1 };
                db.AutoNumberSequences.Add(seq);
            }

            // Generate formatted value
            var number = seq.NextValue.ToString().PadLeft(config.Width, '0');
            var value = string.IsNullOrEmpty(config.Prefix) ? number : $"{config.Prefix}-{number}";

            data[field.Name] = value;
            seq.NextValue++;
        }

        return System.Text.Json.JsonSerializer.Serialize(data);
    }

    private static AutoNumberConfig ParseAutoNumberConfig(string? defaultValue)
    {
        if (string.IsNullOrEmpty(defaultValue))
            return new AutoNumberConfig("", 4);

        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(defaultValue);
            var root = doc.RootElement;
            var prefix = root.TryGetProperty("prefix", out var p) ? p.GetString() ?? "" : "";
            var width = root.TryGetProperty("width", out var w) ? w.GetInt32() : 4;
            return new AutoNumberConfig(prefix, width);
        }
        catch
        {
            return new AutoNumberConfig("", 4);
        }
    }

    private record AutoNumberConfig(string Prefix, int Width);
}
