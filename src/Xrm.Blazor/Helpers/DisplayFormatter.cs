using System.Globalization;
using System.Text.Json;
using Xrm.Core.Models;

namespace Xrm.Blazor.Helpers;

/// <summary>
/// Formats raw field values for display using the current thread culture.
/// </summary>
public static class DisplayFormatter
{
    public static string FormatValue(string raw, FieldDataType dataType)
    {
        var culture = CultureInfo.CurrentCulture;
        return dataType switch
        {
            FieldDataType.Boolean => raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw == "True" ? "Yes" : "No",
            FieldDataType.Date => DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d.ToString("d", culture) : raw,
            FieldDataType.DateTime => DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                ? dt.ToString("g", culture) : raw,
            FieldDataType.Number => long.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var n)
                ? n.ToString("N0", culture) : raw,
            FieldDataType.Decimal => decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec)
                ? dec.ToString("N2", culture) : raw,
            FieldDataType.MultiChoice => FormatMultiChoice(raw),
            _ => raw
        };
    }

    private static string FormatMultiChoice(string raw)
    {
        if (string.IsNullOrEmpty(raw) || !raw.StartsWith('['))
            return raw;
        try
        {
            var items = JsonSerializer.Deserialize<string[]>(raw);
            return items is not null ? string.Join(", ", items) : raw;
        }
        catch
        {
            return raw;
        }
    }
}
