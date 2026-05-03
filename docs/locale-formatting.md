# Locale-Aware Display Formatting

XRM formats values for display using `CultureInfo.CurrentCulture`. The host application controls which locale is active — XRM respects it automatically.

## What gets formatted

| Field Type | Format | Example (nl-NL) | Example (en-US) |
|-----------|--------|-----------------|-----------------|
| Date | Short date | 03-05-2026 | 5/3/2026 |
| DateTime | Short date + time | 03-05-2026 15:02 | 5/3/2026 3:02 PM |
| Number | Integer with grouping | 1.234 | 1,234 |
| Decimal | 2 decimal places | 1.234,56 | 1,234.56 |
| Boolean | Yes/No | Yes | Yes |

## How to configure the host

XRM uses `CultureInfo.CurrentCulture` at render time. The host sets this via standard .NET culture configuration.

### Option 1: Set a fixed culture in Program.cs

```csharp
using System.Globalization;

var culture = new CultureInfo("nl-NL");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
```

### Option 2: Use ASP.NET Core request localization

```csharp
builder.Services.AddLocalization();

// After building the app:
var supportedCultures = new[] { "nl-NL", "en-US" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("nl-NL")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));
```

This picks up the culture from the browser's `Accept-Language` header, a cookie, or a query string parameter.

### Option 3: Blazor Server with per-circuit culture

```csharp
// In _Imports.razor or a layout component:
@using System.Globalization

// The culture follows the server-side setting or can be set per-user
```

## Data storage

Values are always stored in **invariant format** in the JSON data:
- Dates: ISO 8601 (`2026-05-03`)
- Numbers: no grouping, dot decimal (`1234.56`)

The `DisplayFormatter` parses from invariant and formats for the current culture at display time. This means the same data renders correctly regardless of which user is viewing it.

## No XRM configuration needed

There is no XRM-specific locale setting. The consumer controls culture through standard .NET mechanisms, and XRM's UI components automatically format values accordingly.
