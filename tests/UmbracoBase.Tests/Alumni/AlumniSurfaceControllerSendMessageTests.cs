using UmbracoBase.Core.Alumni;
using UmbracoBase.Core.Controllers;
using UmbracoBase.Core.Models;
using UmbracoBase.Core.ViewModels;
using UmbracoBase.Tests.Alumni.Fakes;
using Xunit;

namespace UmbracoBase.Tests.Alumni;

public class AlumniSurfaceControllerSendMessageTests
{
    private static readonly Guid TargetId = Guid.NewGuid();

    private static FakeAlumniMemberStore StoreWithTarget(bool emailingOk = true)
    {
        var store = new FakeAlumniMemberStore();
        store.Targets[TargetId] = new AlumniContactTarget(TargetId, "Jordan Lee", "jordan.lee@example.com", emailingOk);
        return store;
    }

    private static AlumniContactFormModel ValidModel() => new()
    {
        MemberId = TargetId,
        SenderName = "Casey Kim",
        SenderEmail = "casey.kim@example.com",
        Message = "Hey, small world - I think we were in the same class!"
    };

    [Fact]
    public async Task ProcessSendMessage_emails_the_target_with_reply_to_set_to_the_sender()
    {
        var store = StoreWithTarget();
        var emailSender = new FakeEmailSender();

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, ValidModel());

        Assert.True(success);
        Assert.Single(emailSender.Sent);
        var (message, _) = emailSender.Sent[0];
        Assert.Contains("jordan.lee@example.com", message.To);
        Assert.NotNull(message.ReplyTo);
        Assert.Contains("casey.kim@example.com", message.ReplyTo!);
    }

    [Fact]
    public async Task ProcessSendMessage_never_puts_the_targets_email_in_the_returned_message()
    {
        var store = StoreWithTarget();
        var emailSender = new FakeEmailSender();

        var (_, message) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, ValidModel());

        Assert.DoesNotContain("jordan.lee@example.com", message);
    }

    [Fact]
    public async Task ProcessSendMessage_rejects_when_the_target_has_emailing_off()
    {
        var store = StoreWithTarget(emailingOk: false);
        var emailSender = new FakeEmailSender();

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, ValidModel());

        Assert.False(success);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSendMessage_rejects_an_unknown_member_id()
    {
        var store = new FakeAlumniMemberStore(); // no targets registered
        var emailSender = new FakeEmailSender();

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, ValidModel());

        Assert.False(success);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSendMessage_silently_rejects_a_tripped_honeypot()
    {
        var store = StoreWithTarget();
        var emailSender = new FakeEmailSender();
        var model = ValidModel();
        model.Website = "https://spam.example";

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, model);

        Assert.False(success);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSendMessage_rejects_an_invalid_sender_email()
    {
        var store = StoreWithTarget();
        var emailSender = new FakeEmailSender();
        var model = ValidModel();
        model.SenderEmail = "not-an-email";

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, model);

        Assert.False(success);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSendMessage_rejects_a_blank_message()
    {
        var store = StoreWithTarget();
        var emailSender = new FakeEmailSender();
        var model = ValidModel();
        model.Message = "   ";

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, model);

        Assert.False(success);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSendMessage_returns_a_generic_message_when_sending_fails()
    {
        var store = StoreWithTarget();
        var emailSender = new ThrowingEmailSender();

        var (success, message) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, ValidModel());

        Assert.False(success);
        Assert.DoesNotContain("Exception", message);
        Assert.DoesNotContain("SMTP", message, StringComparison.OrdinalIgnoreCase);
    }
}
