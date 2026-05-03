namespace Xrm.Core.Models;

/// <summary>
/// A cross-field validation rule evaluated on save.
/// Stored as JSON array in EntityDefinition.ValidationRulesJson.
/// </summary>
public class ValidationRule
{
    /// <summary>
    /// Rule type: "compare" (field vs field) or "required_if" (conditional required).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The target field name being validated.
    /// For "compare": the left-hand field.
    /// For "required_if": the field that becomes required.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Comparison operator: gt, gte, lt, lte, eq, neq.
    /// For "compare": compares Field against OtherField.
    /// For "required_if": compares WhenField against Value.
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// For "compare": the right-hand field to compare against.
    /// </summary>
    public string? OtherField { get; set; }

    /// <summary>
    /// For "required_if": the field whose value triggers the requirement.
    /// </summary>
    public string? WhenField { get; set; }

    /// <summary>
    /// For "required_if": the value to compare WhenField against.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Error message shown when the rule is violated.
    /// </summary>
    public string Message { get; set; } = "Validation failed";
}
