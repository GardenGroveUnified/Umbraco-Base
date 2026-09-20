using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Notifications;
using UmbracoBase.Core.Alumni;

namespace UmbracoBase.Core.Notifications;

/// <summary>
/// Sends the alumnus a confirmation email the moment staff flip IsApproved
/// to true in the backoffice Members section. Detected via WasPropertyDirty,
/// the standard Umbraco way to see a property just changed in this save.
/// </summary>
public sealed class AlumniApprovalEmailHandler : INotificationAsyncHandler<MemberSavedNotification>
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AlumniApprovalEmailHandler> _logger;

    public AlumniApprovalEmailHandler(IEmailSender emailSender, ILogger<AlumniApprovalEmailHandler> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task HandleAsync(MemberSavedNotification notification, CancellationToken cancellationToken)
    {
        foreach (var member in notification.SavedEntities.Where(IsNewlyApprovedAlumniMember))
        {
            await SendApprovalEmail(member);
        }
    }

    private static bool IsNewlyApprovedAlumniMember(IMember member)
        => member.ContentTypeAlias == UmbracoAlumniMemberStore.MemberTypeAlias
           && member.IsApproved
           && member.WasPropertyDirty(nameof(IMember.IsApproved));

    private async Task SendApprovalEmail(IMember member)
    {
        if (string.IsNullOrWhiteSpace(member.Email)) { return; }

        try
        {
            if (!_emailSender.CanSendRequiredEmail())
            {
                _logger.LogWarning("Alumni approval email for {Email} skipped: email sender is not configured.", member.Email);
                return;
            }

            var email = new EmailMessage(
                from: "alumnishs53@gmail.com",
                to: new[] { member.Email },
                cc: null,
                bcc: null,
                replyTo: null,
                subject: "Your Santiago High School Alumni Directory listing is approved",
                body: $"Hi {member.Name},\n\nYour Alumni Directory submission has been approved and is now visible in the directory. Thanks for staying connected with Santiago High School!",
                isBodyHtml: false,
                attachments: null);

            await _emailSender.SendAsync(email, "AlumniApproval");
            _logger.LogInformation("Alumni approval email sent to {Email}.", member.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Alumni approval email for {Email} failed.", member.Email);
        }
    }
}
