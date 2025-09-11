using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.Common.Validation;

public class AttributeValidationService<T> : IValidationService<T>
{
    public ValidationResult Validate(T model)
    {
        if (model == null)
            return ValidationResult.Failure("Model cannot be null");

        var context = new ValidationContext(model);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true))
            return ValidationResult.Success();
            
        var errorMessages = validationResults
            .Where(r => r.ErrorMessage != null)
            .Select(r => r.ErrorMessage!)
            .ToList();
            
        return ValidationResult.Failure(errorMessages);
    }
}