using UmbracoBase.Core.Security;
using Xunit;

namespace UmbracoBase.Tests.Security;

public class LoginNameNormalizerTests
{
    [Fact]
    public void Adds_the_suffix_to_a_bare_username()
        => Assert.Equal("jsmith@ggusd.us", LoginNameNormalizer.Normalize("jsmith", "@ggusd.us"));

    [Fact]
    public void Leaves_a_full_email_unchanged()
        => Assert.Equal("jsmith@ggusd.net", LoginNameNormalizer.Normalize("jsmith@ggusd.net", "@ggusd.us"));

    [Fact]
    public void Trims_whitespace_before_adding_the_suffix()
        => Assert.Equal("jsmith@ggusd.us", LoginNameNormalizer.Normalize("  jsmith ", "@ggusd.us"));

    [Fact]
    public void Accepts_a_suffix_written_without_the_at_sign()
        => Assert.Equal("jsmith@ggusd.us", LoginNameNormalizer.Normalize("jsmith", "ggusd.us"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Falls_back_to_the_default_suffix_when_none_is_configured(string? suffix)
        => Assert.Equal("jsmith" + LoginNameNormalizer.DefaultSuffix, LoginNameNormalizer.Normalize("jsmith", suffix));

    [Fact]
    public void Strips_a_windows_domain_prefix()
        => Assert.Equal("jsmith@ggusd.us", LoginNameNormalizer.Normalize(@"GGUSD\jsmith", "@ggusd.us"));

    [Fact]
    public void Leaves_blank_input_blank()
        => Assert.Equal(string.Empty, LoginNameNormalizer.Normalize("   ", "@ggusd.us"));
}
