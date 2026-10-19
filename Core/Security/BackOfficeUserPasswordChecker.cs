using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Security;
using UmbracoBase.Core.Constants;

namespace UmbracoBase.Core.Security;

// https://our.umbraco.com/documentation/reference/security/custom-password-checker
public class BackOfficeUserPasswordChecker : IBackOfficeUserPasswordChecker
{
    private readonly IConfiguration _config;
    private readonly ILogger<BackOfficeUserPasswordChecker> _logger;

    public BackOfficeUserPasswordChecker(IConfiguration configuration, ILogger<BackOfficeUserPasswordChecker> logger)
    {
        _config = configuration;
        _logger = logger;
    }

    // NOTE: if the username entered in the login screen does not exist in Umbraco, Umbraco never calls
    // this checker at all and falls straight back to its own stored-password check.
    public Task<BackOfficeUserPasswordCheckerResult> CheckPasswordAsync(BackOfficeIdentityUser user, string password)
    {
        var domain = _config.GetValue<string>(AppSettings.ActiveDirectoryDomain);

        var result = ActiveDirectoryPasswordEvaluator.Evaluate(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            domain,
            ValidateAgainstDomain,
            user.UserName ?? string.Empty,
            password,
            _logger);

        return Task.FromResult(result);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Only reached when ActiveDirectoryPasswordEvaluator has confirmed OSPlatform.Windows.")]
    private static bool ValidateAgainstDomain(string domain, string userName, string password)
    {
        using var context = new PrincipalContext(ContextType.Domain, domain);
        return context.ValidateCredentials(userName, password);
    }
}
