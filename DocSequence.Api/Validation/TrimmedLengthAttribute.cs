using System.ComponentModel.DataAnnotations;

namespace DocSequence.Api.Validation;

// FR-001/FR-002: length is measured after trimming; whitespace-only fails
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class TrimmedLengthAttribute(int minimum, int maximum) : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is string s && s.Trim().Length is var length && length >= minimum && length <= maximum;

    public override string FormatErrorMessage(string name) =>
        $"{name} must be {minimum}-{maximum} characters after trimming.";
}