using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Strings;
using UmbracoBase.Core.Notifications;
using Xunit;

namespace UmbracoBase.Tests.Security;

public class SchoolAdminsUserGroupTests
{
    private static readonly IShortStringHelper ShortStringHelper =
        new DefaultShortStringHelper(Options.Create(new RequestHandlerSettings()));

    [Fact]
    public void Build_uses_the_school_admins_alias_and_name()
    {
        var group = EnsureSchoolAdminsUserGroupHandler.Build(ShortStringHelper);

        Assert.Equal("schoolAdmins", group.Alias);
        Assert.Equal("School Admins", group.Name);
    }

    [Fact]
    public void Build_allows_only_the_content_media_and_member_sections()
    {
        var group = EnsureSchoolAdminsUserGroupHandler.Build(ShortStringHelper);

        Assert.Equal(new[] { "content", "media", "member" }, group.AllowedSections.OrderBy(s => s));
    }

    [Fact]
    public void Build_grants_publishing_but_not_permission_management()
    {
        var group = EnsureSchoolAdminsUserGroupHandler.Build(ShortStringHelper);

        Assert.Contains("Umb.Document.Publish", group.Permissions);
        Assert.Contains("Umb.Document.Rollback", group.Permissions);
        Assert.DoesNotContain("Umb.Document.Permissions", group.Permissions);
        Assert.Equal(10, group.Permissions.Count);
    }
}
