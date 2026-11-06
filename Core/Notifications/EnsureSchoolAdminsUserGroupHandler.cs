using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace UmbracoBase.Core.Notifications;

/// <summary>
/// Creates the "School Admins" backoffice user group on startup if it is
/// missing. uSync (free) does not sync user groups, so this keeps the group
/// in git and recreates it on any environment. An existing group with the
/// same alias is left alone, so edits made in the backoffice are kept.
/// </summary>
public sealed class EnsureSchoolAdminsUserGroupHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public const string Alias = "schoolAdmins";

    private static readonly string[] Sections =
    {
        Umbraco.Cms.Core.Constants.Applications.Content,
        Umbraco.Cms.Core.Constants.Applications.Media,
        Umbraco.Cms.Core.Constants.Applications.Members,
    };

    private static readonly string[] DocumentPermissions =
    {
        "Umb.Document.Read",
        "Umb.Document.Create",
        "Umb.Document.Update",
        "Umb.Document.Delete",
        "Umb.Document.Publish",
        "Umb.Document.Unpublish",
        "Umb.Document.Move",
        "Umb.Document.Sort",
        "Umb.Document.Duplicate",
        "Umb.Document.Rollback",
    };

    private readonly IRuntimeState _runtimeState;
    private readonly IUserGroupService _userGroupService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<EnsureSchoolAdminsUserGroupHandler> _logger;

    public EnsureSchoolAdminsUserGroupHandler(
        IRuntimeState runtimeState,
        IUserGroupService userGroupService,
        IShortStringHelper shortStringHelper,
        ILogger<EnsureSchoolAdminsUserGroupHandler> logger)
    {
        _runtimeState = runtimeState;
        _userGroupService = userGroupService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        // Skip during install/upgrade, when the database isn't ready for service calls.
        if (_runtimeState.Level != RuntimeLevel.Run) { return; }

        try
        {
            if (await _userGroupService.GetAsync(Alias) is not null) { return; }

            var result = await _userGroupService.CreateAsync(Build(_shortStringHelper), Umbraco.Cms.Core.Constants.Security.SuperUserKey);
            if (result.Success)
            {
                _logger.LogInformation("Created the {Alias} user group.", Alias);
            }
            else
            {
                _logger.LogWarning("Could not create the {Alias} user group: {Status}.", Alias, result.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create the {Alias} user group.", Alias);
        }
    }

    public static IUserGroup Build(IShortStringHelper shortStringHelper)
    {
        var group = new UserGroup(shortStringHelper)
        {
            Alias = Alias,
            Name = "School Admins",
            Icon = "icon-users",
            HasAccessToAllLanguages = false,
            Permissions = new HashSet<string>(DocumentPermissions),
        };

        foreach (var section in Sections)
        {
            group.AddAllowedSection(section);
        }

        return group;
    }
}
