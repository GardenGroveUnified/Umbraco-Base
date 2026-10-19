using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Security;

namespace UmbracoBase.Core.Security;

internal static class ActiveDirectoryPasswordEvaluator
{
    internal static BackOfficeUserPasswordCheckerResult Evaluate(
        bool isWindowsPlatform,
        string? domain,
        Func<string, string, string, bool> validateCredentials,
        string userName,
        string password,
        ILogger logger)
    {
        if (!isWindowsPlatform)
        {
            logger.LogWarning("Active Directory authentication is only supported on Windows. Falling back to Umbraco's stored password for {Username}.", userName);
            return BackOfficeUserPasswordCheckerResult.FallbackToDefaultChecker;
        }

        if (string.IsNullOrEmpty(domain))
        {
            logger.LogWarning("ActiveDirectory:Domain not configured in appsettings.json. Falling back to Umbraco's stored password for {Username}.", userName);
            return BackOfficeUserPasswordCheckerResult.FallbackToDefaultChecker;
        }

        try
        {
            var valid = validateCredentials(domain, userName, password);
            logger.LogInformation("AD authentication for user {Username}: {Result}", userName, valid ? "Success" : "Failed");
            return valid ? BackOfficeUserPasswordCheckerResult.ValidCredentials : BackOfficeUserPasswordCheckerResult.InvalidCredentials;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error contacting Active Directory for user {Username}. Falling back to Umbraco's stored password.", userName);
            return BackOfficeUserPasswordCheckerResult.FallbackToDefaultChecker;
        }
    }
}
