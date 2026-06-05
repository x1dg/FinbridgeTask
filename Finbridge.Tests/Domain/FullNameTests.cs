using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Tests.Domain;

public class FullNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Of_ShouldThrow_OnEmptyOrNull(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => FullName.Of(value!));
    }

    [Fact]
    public void Of_ShouldTrimWhitespace()
    {
        var name = FullName.Of("  Иван Иванов  ");
        Assert.Equal("Иван Иванов", name.Value);
    }

    [Fact]
    public void Of_ShouldThrow_WhenExceedsMaxLength()
    {
        var tooLong = new string('a', FullName.MaxLength + 1);
        Assert.Throws<ArgumentException>(() => FullName.Of(tooLong));
    }
}
