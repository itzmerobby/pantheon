using Pantheon.Common;
using Xunit;

namespace Pantheon.Common.Tests;

public sealed class PantheonPackageExceptionTests
{
    [Fact]
    public void Constructor_StoresMessageAndCodeWithoutAnInnerException()
    {
        var exception = new PantheonPackageException("A package error occurred.", "MEKHANE000001");

        Assert.IsAssignableFrom<Exception>(exception);
        Assert.Equal("A package error occurred.", exception.Message);
        Assert.Equal("MEKHANE000001", exception.Code);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_PreservesAnOptionalInnerException()
    {
        var originalException = new InvalidOperationException("The user callback failed.");

        var exception = new PantheonPackageException(
            "A user callback failed.",
            "MEKHANE000002",
            originalException);

        Assert.Same(originalException, exception.InnerException);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB000001")]
    [InlineData("ABCDEFGHIJKLM000001")]
    [InlineData("MEKHANE0")]
    [InlineData("MEKHANE1000000")]
    [InlineData("MEKHANE00000X")]
    [InlineData("MEKHANE+1")]
    [InlineData("MEKHANE 1")]
    public void Constructor_RejectsInvalidCodes(string code)
    {
        Assert.Throws<ArgumentException>(() => new PantheonPackageException("A package error occurred.", code));
    }
}
