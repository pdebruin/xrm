namespace Xrm.Core.Services;

/// <summary>
/// Holds a deferred field renderer registration, applied when the registry is first accessed.
/// </summary>
public record FieldRendererRegistration(string EntityName, string FieldName, Type ComponentType, bool Replace);
