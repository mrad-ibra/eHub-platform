using eHub.Domain.Exceptions;

namespace eHub.UnitTests.Domain;

public sealed class AppExceptionTests
{
    [Fact]
    public void NotFoundException_HasExpectedStatusAndCode()
    {
        var exception = new NotFoundException("Asset was not found.");

        exception.StatusCode.Should().Be(404);
        exception.Code.Should().Be("not_found");
        exception.Message.Should().Be("Asset was not found.");
    }

    [Fact]
    public void ValidationFailedException_IncludesDetails()
    {
        var details = new Dictionary<string, object?> { ["field"] = "name" };
        var exception = new ValidationFailedException("Invalid payload.", details);

        exception.StatusCode.Should().Be(400);
        exception.Code.Should().Be("validation_failed");
        exception.Details.Should().ContainKey("field");
    }

    [Fact]
    public void ConflictException_HasConflictStatus()
    {
        var exception = new ConflictException("Already exists.");

        exception.StatusCode.Should().Be(409);
        exception.Code.Should().Be("conflict");
    }
}
