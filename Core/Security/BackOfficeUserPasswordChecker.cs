// using Microsoft.Extensions.Configuration;
// using System;
// using System.DirectoryServices.AccountManagement;
// using System.Threading.Tasks;
// using System.Runtime.InteropServices;
// using Umbraco.Cms.Core.Security;
// using Microsoft.Extensions.Logging;

// namespace UmbracoBase.Core.Security
// {
//     //https://our.umbraco.com/documentation/reference/security/custom-password-checker
//     public class BackOfficeUserPasswordChecker : IBackOfficeUserPasswordChecker
//     {
//         private readonly IConfiguration _config;
//         private readonly ILogger<BackOfficeUserPasswordChecker> _logger;
        
//         public BackOfficeUserPasswordChecker(IConfiguration configuration, ILogger<BackOfficeUserPasswordChecker> logger)
//         {
//             _config = configuration;
//             _logger = logger;
//         }
//         public Task<BackOfficeUserPasswordCheckerResult> CheckPasswordAsync(BackOfficeIdentityUser user, string password)
//         {
//             //NOTE: if the username entered in the login screen does not exist in Umbraco then ActiveDirectoryPasswordChecker() does not run, instead Umbraco will immediately fall back to its internal checks (default Umbraco behavior).
//             var result = IsValidADUser(user.UserName ?? "", password)
//                 ? Task.FromResult(BackOfficeUserPasswordCheckerResult.ValidCredentials)
//                 : Task.FromResult(BackOfficeUserPasswordCheckerResult.InvalidCredentials);
//             return result;
//         }

//         [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
//         private bool IsValidADUser(string userName, string password)
//         {
//             // Check if running on Windows - AD integration only works on Windows
//             if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//             {
//                 _logger.LogWarning("Active Directory authentication is only supported on Windows. Running on {OS}.", RuntimeInformation.OSDescription);
//                 return false; // Fall back to Umbraco's built-in authentication
//             }

//             try
//             {
//                 var domain = _config.GetValue<string>("ActiveDirectory:Domain");  //get active directory domain from appsettings 

//                 if (string.IsNullOrEmpty(domain))
//                 {
//                     _logger.LogWarning("ActiveDirectory:Domain not configured in appsettings.json");
//                     return false;
//                 }

//                 using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, domain))
//                 {
//                     var valid = pc.ValidateCredentials(userName, password);
//                     _logger.LogInformation("AD authentication for user {Username}: {Result}", userName, valid ? "Success" : "Failed");
//                     return valid;
//                 }
//             }
//             catch (PlatformNotSupportedException ex)
//             {
//                 _logger.LogWarning(ex, "Active Directory authentication not supported on this platform.");
//                 return false;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error during Active Directory authentication for user {Username}", userName);
//                 return false;
//             }
//         }
//     }
// }
