using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SpecificationPairsAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IEnumerable<ProductSpecificationDto> specifications)
        {
            return ValidationResult.Success;
        }

        if (specifications.Any(specification => string.IsNullOrWhiteSpace(specification.Key)))
        {
            return new ValidationResult("Specification keys cannot be empty.", [validationContext.MemberName!]);
        }

        if (specifications
            .Where(specification => !string.IsNullOrWhiteSpace(specification.Key))
            .GroupBy(specification => specification.Key.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            return new ValidationResult("Specification keys must be unique, ignoring case.", [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}