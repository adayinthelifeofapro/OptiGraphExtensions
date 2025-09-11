using System.ComponentModel.DataAnnotations;
using OptiGraphExtensions.Features.Common.Validation;

namespace OptiGraphExtensions.Tests.Features.Common.Validation;

[TestFixture]
public class AttributeValidationServiceTests
{
    private AttributeValidationService<TestModel> _validationService;

    [SetUp]
    public void Setup()
    {
        _validationService = new AttributeValidationService<TestModel>();
    }

    [Test]
    public void Validate_WithValidModel_ReturnsSuccessResult()
    {
        // Arrange
        var model = new TestModel
        {
            RequiredProperty = "Valid Value",
            EmailProperty = "test@example.com",
            RangeProperty = 5
        };

        // Act
        var result = _validationService.Validate(model);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessages, Is.Empty);
    }

    [Test]
    public void Validate_WithInvalidModel_ReturnsFailureResult()
    {
        // Arrange
        var model = new TestModel
        {
            RequiredProperty = "", // Invalid - required
            EmailProperty = "invalid-email", // Invalid - not email format
            RangeProperty = 15 // Invalid - outside range
        };

        // Act
        var result = _validationService.Validate(model);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessages.Count, Is.GreaterThan(0));
        Assert.That(result.ErrorMessages, Does.Contain("Required property is required"));
        Assert.That(result.ErrorMessages, Does.Contain("Email property must be a valid email address"));
        Assert.That(result.ErrorMessages, Does.Contain("Range property must be between 1 and 10"));
    }

    [Test]
    public void Validate_WithNullModel_ReturnsFailureResult()
    {
        // Arrange
        TestModel model = null!;

        // Act
        var result = _validationService.Validate(model);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessages.Count, Is.EqualTo(1));
        Assert.That(result.ErrorMessages[0], Is.EqualTo("Model cannot be null"));
    }

    [Test]
    public void Validate_WithPartiallyValidModel_ReturnsSpecificErrors()
    {
        // Arrange
        var model = new TestModel
        {
            RequiredProperty = "Valid Value", // Valid
            EmailProperty = "invalid-email", // Invalid
            RangeProperty = 5 // Valid
        };

        // Act
        var result = _validationService.Validate(model);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessages.Count, Is.EqualTo(1));
        Assert.That(result.ErrorMessages[0], Is.EqualTo("Email property must be a valid email address"));
    }

    private class TestModel
    {
        [Required(ErrorMessage = "Required property is required")]
        public string RequiredProperty { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email property must be a valid email address")]
        public string EmailProperty { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(1, 10, ErrorMessage = "Range property must be between 1 and 10")]
        public int RangeProperty { get; set; }
    }
}