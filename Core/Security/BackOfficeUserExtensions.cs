using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Extensions;

namespace UmbracoBase.Core.Security;

public static class BackOfficeUserExtensions
{
    /// <summary>
    /// Returns the signed-in, approved backoffice user, or null.
    /// Umbraco only runs the backoffice cookie scheme on /umbraco requests, so on
    /// our own routes (/admin/...) IBackOfficeSecurity.CurrentUser stays null even
    /// with a valid login cookie - which sent staff round the login page forever.
    /// Authenticating the backoffice scheme explicitly first fixes that.
    /// </summary>
    public static async Task<IUser?> GetApprovedBackOfficeUserAsync(
        this ControllerBase controller, IBackOfficeSecurity backOfficeSecurity)
    {
        var result = await controller.AuthenticateBackOfficeAsync();
        if (result.Succeeded && result.Principal is not null)
        {
            controller.HttpContext.User = result.Principal;
        }

        var user = backOfficeSecurity.CurrentUser;
        return user is { IsApproved: true } ? user : null;
    }
}
