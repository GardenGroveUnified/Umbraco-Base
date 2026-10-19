using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;

namespace UmbracoBase.Tests.Alumni.Fakes;

public sealed class FakeEmailSender : IEmailSender
{
    public List<(EmailMessage Message, string EmailType)> Sent { get; } = new();
    public bool CanSend { get; set; } = true;

    public Task SendAsync(EmailMessage message, string emailType)
    {
        Sent.Add((message, emailType));
        return Task.CompletedTask;
    }

    public Task SendAsync(EmailMessage message, string emailType, bool enableNotification)
        => SendAsync(message, emailType);

    public Task SendAsync(EmailMessage message, string emailType, bool enableNotification, DateTime? expires)
        => SendAsync(message, emailType);

    public bool CanSendRequiredEmail() => CanSend;
}

public sealed class ThrowingEmailSender : Umbraco.Cms.Core.Mail.IEmailSender
{
    public Task SendAsync(Umbraco.Cms.Core.Models.Email.EmailMessage message, string emailType)
        => throw new InvalidOperationException("SMTP relay unreachable");

    public Task SendAsync(Umbraco.Cms.Core.Models.Email.EmailMessage message, string emailType, bool enableNotification)
        => SendAsync(message, emailType);

    public Task SendAsync(Umbraco.Cms.Core.Models.Email.EmailMessage message, string emailType, bool enableNotification, DateTime? expires)
        => SendAsync(message, emailType);

    public bool CanSendRequiredEmail() => true;
}
