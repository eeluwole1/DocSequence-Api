using System.ComponentModel.DataAnnotations;

namespace DocSequence.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is Guid g && g != Guid.Empty;

    public override string FormatErrorMessage(string name) => $"{name} must be a non-empty UUID.";
}