using UmbracoBase.Core.Alumni;
using Xunit;

namespace UmbracoBase.Tests.Alumni;

public class AlumniFormGuardTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("I am a bot", true)]
    public void IsHoneypotTripped_is_true_only_when_the_hidden_field_has_content(string? value, bool expected)
    {
        Assert.Equal(expected, AlumniFormGuard.IsHoneypotTripped(value));
    }

    [Theory]
    [InlineData("alumni@example.com", true)]
    [InlineData("first.last+tag@sub.example.org", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("not-an-email", false)]
    [InlineData("missing@domain", false)]
    [InlineData("@nolocal.com", false)]
    public void IsValidEmail_matches_a_plausible_email_shape(string? email, bool expected)
    {
        Assert.Equal(expected, AlumniFormGuard.IsValidEmail(email));
    }

    [Fact]
    public void IsValidMessage_rejects_null_or_whitespace()
    {
        Assert.False(AlumniFormGuard.IsValidMessage(null, maxLength: 2000));
        Assert.False(AlumniFormGuard.IsValidMessage("   ", maxLength: 2000));
    }

    [Fact]
    public void IsValidMessage_rejects_a_message_longer_than_the_max_length()
    {
        var tooLong = new string('a', 2001);
        Assert.False(AlumniFormGuard.IsValidMessage(tooLong, maxLength: 2000));
    }

    [Fact]
    public void IsValidMessage_accepts_a_message_within_the_max_length()
    {
        Assert.True(AlumniFormGuard.IsValidMessage("Hi, great to hear from you!", maxLength: 2000));
    }
}
