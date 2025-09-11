namespace OptiGraphExtensions.Features.Common.Validation;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public IList<string> ErrorMessages { get; set; } = new List<string>();
    
    public static ValidationResult Success() => new() { IsValid = true };
    
    public static ValidationResult Failure(params string[] errors) => new() 
    { 
        IsValid = false, 
        ErrorMessages = errors.ToList() 
    };
    
    public static ValidationResult Failure(IEnumerable<string> errors) => new() 
    { 
        IsValid = false, 
        ErrorMessages = errors.ToList() 
    };
}