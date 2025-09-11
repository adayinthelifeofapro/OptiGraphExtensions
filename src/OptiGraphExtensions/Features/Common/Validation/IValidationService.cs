namespace OptiGraphExtensions.Features.Common.Validation;

public interface IValidationService<T>
{
    ValidationResult Validate(T model);
}