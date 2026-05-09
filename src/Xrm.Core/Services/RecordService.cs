using Microsoft.EntityFrameworkCore;
using Xrm.Core.Data;
using Xrm.Core.Models;

namespace Xrm.Core.Services;

public class RecordService : IRecordService
{
    private readonly IDbContextFactory<XrmDbContext> _dbFactory;
    private readonly IEnumerable<IRecordLifecycleHandler> _lifecycleHandlers;
    private readonly ICurrentUser _currentUser;

    public RecordService(IDbContextFactory<XrmDbContext> dbFactory, IEnumerable<IRecordLifecycleHandler> lifecycleHandlers, ICurrentUser currentUser)
    {
        _dbFactory = dbFactory;
        _lifecycleHandlers = lifecycleHandlers;
        _currentUser = currentUser;
    }

    private async Task<string?> GetEntityDomain(XrmDbContext db, Guid entityId)
    {
        var entity = await db.EntityDefinitions.FindAsync(entityId);
        return entity?.Domain;
    }

    private async Task EnsureReadAccess(XrmDbContext db, Guid entityId)
    {
        var domain = await GetEntityDomain(db, entityId);
        if (!_currentUser.CanRead(domain))
            throw new UnauthorizedAccessException($"Access denied: cannot read in domain '{domain}'");
    }

    private async Task EnsureWriteAccess(XrmDbContext db, Guid entityId)
    {
        var domain = await GetEntityDomain(db, entityId);
        if (!_currentUser.CanWrite(domain))
            throw new UnauthorizedAccessException($"Access denied: cannot write in domain '{domain}'");
    }

    public async Task<RecordPage> GetAllAsync(Guid entityId, int page = 1, int pageSize = 25, string? sortField = null, string sortDir = "asc", string? filter = null, List<ViewFilter>? viewFilters = null, bool? isActive = true)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await EnsureReadAccess(db, entityId);
        var query = db.Records.Where(r => r.EntityDefinitionId == entityId);

        // Filter by active status: true = active only, false = deactivated only, null = all
        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        var computedFields = await db.FieldDefinitions
            .Where(f => f.EntityDefinitionId == entityId && f.DataType == FieldDataType.Computed)
            .ToListAsync();

        var hasViewFilters = viewFilters is { Count: > 0 };
        var hasTextFilter = !string.IsNullOrWhiteSpace(filter);

        // View filters and/or text filter require client-side evaluation on JSON
        if (hasViewFilters || hasTextFilter)
        {
            var all = await query.ToListAsync();
            if (computedFields.Count > 0)
                foreach (var r in all) r.DataJson = await ApplyComputedFieldsAsync(r.DataJson, computedFields, db, r.Id);

            IEnumerable<Record> filtered = all;
            if (hasViewFilters)
                filtered = filtered.Where(r => MatchesViewFilters(r.DataJson, viewFilters!));
            if (hasTextFilter)
                filtered = filtered.Where(r => MatchesFilter(r.DataJson, filter!));

            var filteredList = filtered.ToList();
            var total = filteredList.Count;
            var sorted = string.IsNullOrEmpty(sortField)
                ? (sortDir == "desc" ? filteredList.OrderByDescending(r => r.CreatedAt) : filteredList.OrderBy(r => r.CreatedAt)).ToList()
                : SortByJsonField(filteredList, sortField, sortDir);
            var paged = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new RecordPage(paged, total, page, pageSize);
        }

        var totalCount = await query.CountAsync();

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
            if (computedFields.Count > 0)
                foreach (var r in records) r.DataJson = await ApplyComputedFieldsAsync(r.DataJson, computedFields, db, r.Id);
            return new RecordPage(records, totalCount, page, pageSize);
        }
        else
        {
            // Materialize all matching records, sort by JSON field value, then page
            var all = await query.ToListAsync();
            if (computedFields.Count > 0)
                foreach (var r in all) r.DataJson = await ApplyComputedFieldsAsync(r.DataJson, computedFields, db, r.Id);
            var sorted = SortByJsonField(all, sortField, sortDir);
            var paged = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new RecordPage(paged, totalCount, page, pageSize);
        }
    }

    private static async Task<string> ApplyComputedFieldsAsync(string dataJson, List<FieldDefinition> computedFields, XrmDbContext? db, Guid recordId)
    {
        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(dataJson) ?? new();
            var writable = new Dictionary<string, object?>();
            foreach (var kvp in data) writable[kvp.Key] = kvp.Value;

            foreach (var field in computedFields)
            {
                if (string.IsNullOrEmpty(field.Expression)) continue;

                var expression = field.Expression;

                // Resolve aggregate functions (COUNT, SUM) if database context available
                if (db is not null && (expression.Contains("COUNT(") || expression.Contains("SUM(")))
                    expression = await ResolveAggregates(expression, db, recordId);

                var result = ExpressionEvaluator.Evaluate(expression, data);
                if (result.HasValue)
                    writable[field.Name] = Math.Round(result.Value, 10);
            }

            return System.Text.Json.JsonSerializer.Serialize(writable);
        }
        catch
        {
            return dataJson;
        }
    }

    private static async Task<string> ResolveAggregates(string expression, XrmDbContext db, Guid recordId)
    {
        // Pattern: COUNT(EntityName) or SUM(EntityName.FieldName)
        var result = expression;

        // Resolve COUNT(EntityName)
        var countPattern = new System.Text.RegularExpressions.Regex(@"COUNT\((\w+)\)");
        foreach (System.Text.RegularExpressions.Match match in countPattern.Matches(expression))
        {
            var entityName = match.Groups[1].Value;
            var count = await GetChildRecordCount(db, recordId, entityName);
            result = result.Replace(match.Value, count.ToString());
        }

        // Resolve SUM(EntityName.FieldName)
        var sumPattern = new System.Text.RegularExpressions.Regex(@"SUM\((\w+)\.(\w+)\)");
        foreach (System.Text.RegularExpressions.Match match in sumPattern.Matches(expression))
        {
            var entityName = match.Groups[1].Value;
            var fieldName = match.Groups[2].Value;
            var sum = await GetChildRecordSum(db, recordId, entityName, fieldName);
            result = result.Replace(match.Value, sum.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return result;
    }

    private static async Task<int> GetChildRecordCount(XrmDbContext db, Guid parentRecordId, string childEntityName)
    {
        var childEntity = await db.EntityDefinitions.FirstOrDefaultAsync(e => e.Name == childEntityName);
        if (childEntity is null) return 0;

        var relationship = await db.RelationshipDefinitions
            .FirstOrDefaultAsync(r => r.ChildEntityId == childEntity.Id);
        if (relationship is null) return 0;

        return await db.RecordLinks
            .Where(rl => rl.RelationshipDefinitionId == relationship.Id && rl.ParentRecordId == parentRecordId)
            .CountAsync();
    }

    private static async Task<double> GetChildRecordSum(XrmDbContext db, Guid parentRecordId, string childEntityName, string fieldName)
    {
        var childEntity = await db.EntityDefinitions.FirstOrDefaultAsync(e => e.Name == childEntityName);
        if (childEntity is null) return 0;

        var relationship = await db.RelationshipDefinitions
            .FirstOrDefaultAsync(r => r.ChildEntityId == childEntity.Id);
        if (relationship is null) return 0;

        var childRecordIds = await db.RecordLinks
            .Where(rl => rl.RelationshipDefinitionId == relationship.Id && rl.ParentRecordId == parentRecordId)
            .Select(rl => rl.ChildRecordId)
            .ToListAsync();

        if (childRecordIds.Count == 0) return 0;

        var records = await db.Records
            .Where(r => childRecordIds.Contains(r.Id))
            .ToListAsync();

        double sum = 0;
        foreach (var record in records)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(record.DataJson);
                if (doc.RootElement.TryGetProperty(fieldName, out var val))
                {
                    if (val.ValueKind == System.Text.Json.JsonValueKind.Number)
                        sum += val.GetDouble();
                    else if (val.ValueKind == System.Text.Json.JsonValueKind.String &&
                             double.TryParse(val.GetString(), System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture, out var n))
                        sum += n;
                }
            }
            catch { }
        }
        return sum;
    }

    private static List<Record> SortByJsonField(List<Record> records, string fieldName, string dir)
    {
        return (dir == "desc"
            ? records.OrderByDescending(r => ExtractSortKey(r.DataJson, fieldName))
            : records.OrderBy(r => ExtractSortKey(r.DataJson, fieldName))
        ).ToList();
    }

    private static bool MatchesFilter(string dataJson, string filter)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(dataJson);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var value = prop.Value.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => prop.Value.GetString(),
                    System.Text.Json.JsonValueKind.Number => prop.Value.GetRawText(),
                    System.Text.Json.JsonValueKind.True => "true",
                    System.Text.Json.JsonValueKind.False => "false",
                    System.Text.Json.JsonValueKind.Array => prop.Value.GetRawText(),
                    _ => null
                };
                if (value != null && value.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Checks if a record's JSON data matches all view filter conditions (AND logic).
    /// </summary>
    private static bool MatchesViewFilters(string dataJson, List<ViewFilter> filters)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(dataJson);
            foreach (var f in filters)
            {
                var fieldValue = "";
                if (doc.RootElement.TryGetProperty(f.Field, out var val))
                {
                    fieldValue = val.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => val.GetString() ?? "",
                        System.Text.Json.JsonValueKind.Number => val.GetRawText(),
                        System.Text.Json.JsonValueKind.True => "true",
                        System.Text.Json.JsonValueKind.False => "false",
                        _ => ""
                    };
                }

                if (!EvaluateFilter(fieldValue, f.Operator, f.Value))
                    return false;
            }
            return true;
        }
        catch { }
        return false;
    }

    private static bool EvaluateFilter(string fieldValue, string op, string filterValue)
    {
        return op switch
        {
            "eq" => string.Equals(fieldValue, filterValue, StringComparison.OrdinalIgnoreCase),
            "neq" => !string.Equals(fieldValue, filterValue, StringComparison.OrdinalIgnoreCase),
            "contains" => fieldValue.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
            "gt" => CompareNumeric(fieldValue, filterValue) > 0,
            "lt" => CompareNumeric(fieldValue, filterValue) < 0,
            "gte" => CompareNumeric(fieldValue, filterValue) >= 0,
            "lte" => CompareNumeric(fieldValue, filterValue) <= 0,
            _ => false
        };
    }

    private static int CompareNumeric(string a, string b)
    {
        if (double.TryParse(a, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var na)
            && double.TryParse(b, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var nb))
            return na.CompareTo(nb);
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
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
        await EnsureReadAccess(db, entityId);
        var record = await db.Records
            .FirstOrDefaultAsync(r => r.Id == id && r.EntityDefinitionId == entityId);
        if (record is null) return null;

        var computedFields = await db.FieldDefinitions
            .Where(f => f.EntityDefinitionId == entityId && f.DataType == FieldDataType.Computed)
            .ToListAsync();
        if (computedFields.Count > 0)
            record.DataJson = await ApplyComputedFieldsAsync(record.DataJson, computedFields, db, record.Id);

        return record;
    }

    public async Task<SaveResult> CreateAsync(Guid entityId, string dataJson)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await EnsureWriteAccess(db, entityId);
        var entity = await db.EntityDefinitions.FindAsync(entityId)
            ?? throw new InvalidOperationException($"Entity {entityId} not found");

        await ValidateRecordData(db, entityId, dataJson);

        // Generate AutoNumber values
        dataJson = await ApplyAutoNumbers(db, entityId, dataJson);

        // Pre-save hook
        foreach (var handler in _lifecycleHandlers)
            dataJson = await handler.OnCreatingAsync(entityId, dataJson, entity);

        var record = new Record
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entityId,
            DataJson = dataJson
        };
        db.Records.Add(record);

        db.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entityId,
            RecordId = record.Id,
            Action = "Created",
            Timestamp = DateTime.UtcNow,
            NewDataJson = dataJson
        });

        await db.SaveChangesAsync();

        // Post-save hooks — collect warnings instead of failing
        var warnings = new List<string>();
        foreach (var handler in _lifecycleHandlers)
        {
            try { await handler.OnCreatedAsync(record, entity); }
            catch (Exception ex) { warnings.Add(ex.Message); }
        }

        return new SaveResult(true, record, warnings);
    }

    public async Task<SaveResult> UpdateAsync(Guid entityId, Guid id, string dataJson)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await EnsureWriteAccess(db, entityId);
        var record = await db.Records
            .FirstOrDefaultAsync(r => r.Id == id && r.EntityDefinitionId == entityId);
        if (record is null) return new SaveResult(false);

        var entity = await db.EntityDefinitions.FindAsync(entityId)
            ?? throw new InvalidOperationException($"Entity {entityId} not found");

        await ValidateRecordData(db, entityId, dataJson, record.DataJson);
        var oldDataJson = record.DataJson;

        // Pre-save hook
        foreach (var handler in _lifecycleHandlers)
            dataJson = await handler.OnUpdatingAsync(record, dataJson, entity);

        record.DataJson = dataJson;

        db.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entityId,
            RecordId = id,
            Action = "Updated",
            Timestamp = DateTime.UtcNow,
            OldDataJson = oldDataJson,
            NewDataJson = dataJson
        });

        await db.SaveChangesAsync();

        // Post-save hooks — collect warnings instead of failing
        var warnings = new List<string>();
        foreach (var handler in _lifecycleHandlers)
        {
            try { await handler.OnUpdatedAsync(record, oldDataJson, entity); }
            catch (Exception ex) { warnings.Add(ex.Message); }
        }

        return new SaveResult(true, record, warnings);
    }

    private static Task ValidateRecordData(XrmDbContext db, Guid entityId, string dataJson)
        => ValidateRecordData(db, entityId, dataJson, oldDataJson: null);

    private static async Task ValidateRecordData(XrmDbContext db, Guid entityId, string dataJson, string? oldDataJson)
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
            // AutoNumber and Computed fields are system-generated; skip validation
            if (field.DataType == FieldDataType.AutoNumber || field.DataType == FieldDataType.Computed) continue;

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

            if (field.MinLength.HasValue && strVal.Length < field.MinLength.Value)
                errors.Add($"'{field.DisplayName ?? field.Name}' must be at least {field.MinLength.Value} characters");

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

            // State machine: validate transition is allowed
            if (field.DataType == FieldDataType.Choice && !string.IsNullOrEmpty(field.TransitionsJson) && oldDataJson is not null)
            {
                var oldValue = GetFieldValueFromJson(oldDataJson, field.Name);
                if (!string.IsNullOrEmpty(oldValue) && oldValue != strVal)
                {
                    var transitions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(field.TransitionsJson) ?? new();
                    if (transitions.TryGetValue(oldValue, out var allowedNext))
                    {
                        if (!allowedNext.Contains(strVal))
                            errors.Add($"'{field.DisplayName ?? field.Name}' cannot transition from '{oldValue}' to '{strVal}'. Allowed: {string.Join(", ", allowedNext)}");
                    }
                    else
                    {
                        // State has no outgoing transitions defined — it's a terminal state
                        errors.Add($"'{field.DisplayName ?? field.Name}' cannot transition from '{oldValue}' (terminal state)");
                    }
                }
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

        // Cross-field validation rules
        var entity = await db.EntityDefinitions.FindAsync(entityId);
        if (entity?.ValidationRulesJson is not null)
        {
            try
            {
                var rules = System.Text.Json.JsonSerializer.Deserialize<List<Models.ValidationRule>>(
                    entity.ValidationRulesJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                foreach (var rule in rules)
                {
                    switch (rule.Type.ToLowerInvariant())
                    {
                        case "compare":
                            ValidateCompareRule(rule, data, errors);
                            break;
                        case "required_if":
                            ValidateRequiredIfRule(rule, data, errors);
                            break;
                    }
                }
            }
            catch (System.Text.Json.JsonException) { }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("; ", errors));
    }

    private static void ValidateCompareRule(Models.ValidationRule rule, Dictionary<string, System.Text.Json.JsonElement> data, List<string> errors)
    {
        if (string.IsNullOrEmpty(rule.OtherField)) return;

        if (!data.TryGetValue(rule.Field, out var leftEl) || leftEl.ValueKind == System.Text.Json.JsonValueKind.Null) return;
        if (!data.TryGetValue(rule.OtherField, out var rightEl) || rightEl.ValueKind == System.Text.Json.JsonValueKind.Null) return;

        var leftStr = leftEl.ToString();
        var rightStr = rightEl.ToString();

        // Try date comparison first, then numeric
        if (DateTime.TryParse(leftStr, out var leftDate) && DateTime.TryParse(rightStr, out var rightDate))
        {
            if (!CompareValues(leftDate.CompareTo(rightDate), rule.Operator))
                errors.Add(rule.Message);
        }
        else if (double.TryParse(leftStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var leftNum)
              && double.TryParse(rightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var rightNum))
        {
            if (!CompareValues(leftNum.CompareTo(rightNum), rule.Operator))
                errors.Add(rule.Message);
        }
        else
        {
            if (!CompareValues(string.Compare(leftStr, rightStr, StringComparison.Ordinal), rule.Operator))
                errors.Add(rule.Message);
        }
    }

    private static void ValidateRequiredIfRule(Models.ValidationRule rule, Dictionary<string, System.Text.Json.JsonElement> data, List<string> errors)
    {
        if (string.IsNullOrEmpty(rule.WhenField)) return;

        // Check if the condition is met
        if (!data.TryGetValue(rule.WhenField, out var whenEl)) return;
        var whenStr = whenEl.ValueKind == System.Text.Json.JsonValueKind.Null ? "" : whenEl.ToString();
        var conditionValue = rule.Value ?? "";

        var conditionMet = rule.Operator.ToLowerInvariant() switch
        {
            "eq" => string.Equals(whenStr, conditionValue, StringComparison.OrdinalIgnoreCase),
            "neq" => !string.Equals(whenStr, conditionValue, StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        if (!conditionMet) return;

        // Condition met — check that the target field has a value
        var hasValue = data.TryGetValue(rule.Field, out var targetEl)
            && targetEl.ValueKind != System.Text.Json.JsonValueKind.Null
            && !(targetEl.ValueKind == System.Text.Json.JsonValueKind.String && string.IsNullOrWhiteSpace(targetEl.GetString()));

        if (!hasValue)
            errors.Add(rule.Message);
    }

    private static bool CompareValues(int comparison, string op) => op.ToLowerInvariant() switch
    {
        "gt" => comparison > 0,
        "gte" => comparison >= 0,
        "lt" => comparison < 0,
        "lte" => comparison <= 0,
        "eq" => comparison == 0,
        "neq" => comparison != 0,
        _ => true
    };

    private static string? GetFieldValueFromJson(string json, string fieldName)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(fieldName, out var prop) &&
                prop.ValueKind == System.Text.Json.JsonValueKind.String)
                return prop.GetString();
        }
        catch { }
        return null;
    }

    public async Task<bool> DeleteAsync(Guid entityId, Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await EnsureWriteAccess(db, entityId);
        var record = await db.Records
            .FirstOrDefaultAsync(r => r.Id == id && r.EntityDefinitionId == entityId);
        if (record is null) return false;

        var entity = await db.EntityDefinitions.FindAsync(entityId)
            ?? throw new InvalidOperationException($"Entity {entityId} not found");

        // Pre-delete hook
        foreach (var handler in _lifecycleHandlers)
            await handler.OnDeletingAsync(record, entity);

        db.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entityId,
            RecordId = id,
            Action = "Deactivated",
            Timestamp = DateTime.UtcNow,
            OldDataJson = record.DataJson
        });

        record.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReactivateAsync(Guid entityId, Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await EnsureWriteAccess(db, entityId);
        var record = await db.Records
            .FirstOrDefaultAsync(r => r.Id == id && r.EntityDefinitionId == entityId && !r.IsActive);
        if (record is null) return false;

        db.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entityId,
            RecordId = id,
            Action = "Reactivated",
            Timestamp = DateTime.UtcNow,
            NewDataJson = record.DataJson
        });

        record.IsActive = true;
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
