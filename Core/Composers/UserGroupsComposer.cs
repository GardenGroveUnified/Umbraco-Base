using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using UmbracoBase.Core.Notifications;

namespace UmbracoBase.Core.Composers
{
    /// <summary>
    /// Backoffice user groups that live in code because uSync doesn't sync them.
    /// </summary>
    public class UserGroupsComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, EnsureSchoolAdminsUserGroupHandler>();
        }
    }
}
