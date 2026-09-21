using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Security;
using UmbracoBase.Core.Security;
using Xunit;

namespace UmbracoBase.Tests.Security;

public class ActiveDirectoryPasswordEvaluatorTests
{
    private static readonly NullLogger Logger = NullLogger.Instance;

    [Fact]
    public void Evaluate_returns_valid_credentials_when_ad_confirms_the_password()
    {
        var result = ActiveDirectoryPasswordEvaluator.Evaluate(
            isWindowsPlatform: true,
            domain: "santiagohs.local",
            validateCredentials: (_, _, _) => true,
            userName: "jdoe",
            password: "correct-password",
            logger: Logger);

        Assert.Equal(BackOfficeUserPasswordCheckerResult.ValidCredentials, result);
    }

    [Fact]
    public void Evaluate_returns_invalid_credentials_when_ad_rejects_the_password()
    {
        var result = ActiveDirectoryPasswordEvaluator.Evaluate(
            isWindowsPlatform: true,
            domain: "santiagohs.local",
            validateCredentials: (_, _, _) => false,
            userName: "jdoe",
            password: "wrong-password",
            logger: Logger);

        Assert.Equal(BackOfficeUserPasswordCheckerResult.InvalidCredentials, result);
    }

    [Fact]
    public void Evaluate_falls_back_to_the_default_checker_when_not_on_windows()
    {
        var result = ActiveDirectoryPasswordEvaluator.Evaluate(
            isWindowsPlatform: false,
            domain: "santiagohs.local",
            validateCredentials: (_, _, _) => throw new InvalidOperationException("should not be called"),
            userName: "jdoe",
            password: "any-password",
            logger: Logger);

        Assert.Equal(BackOfficeUserPasswordCheckerResult.FallbackToDefaultChecker, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Evaluate_falls_back_to_the_default_checker_when_domain_is_not_configured(string? domain)
    {
        var result = ActiveDirectoryPasswordEvaluator.Evaluate(
            isWindowsPlatform: true,
            domain: domain,
            validateCredentials: (_, _, _) => throw new InvalidOperationException("should not be called"),
            userName: "jdoe",
            password: "any-password",
            logger: Logger);

        Assert.Equal(BackOfficeUserPasswordCheckerResult.FallbackToDefaultChecker, result);
    }

    [Fact]
    public void Evaluate_falls_back_to_the_default_checker_when_the_domain_controller_is_unreachable()
    {
        var result = ActiveDirectoryPasswordEvaluator.Evaluate(
            isWindowsPlatform: true,
            domain: "santiagohs.local",
            validateCredentials: (_, _, _) => throw new TimeoutException("DC unreachable"),
            userName: "jdoe",
            password: "any-password",
            logger: Logger);

        Assert.Equal(BackOfficeUserPasswordCheckerResult.FallbackToDefaultChecker, result);
    }
}
