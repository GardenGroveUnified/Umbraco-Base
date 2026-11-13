using UmbracoBase.Core.Controllers;
using UmbracoBase.Core.ViewModels;
using UmbracoBase.Tests.Alumni.Fakes;
using Xunit;

namespace UmbracoBase.Tests.Alumni;

public class AlumniSurfaceControllerSubmitMemoirTests
{
    private static MemoirSubmissionFormModel ValidModel() => new()
    {
        AuthorName = "Alex Rivera",
        Email = "alex.rivera@example.com",
        GradYear = 2015,
        MemoirText = "Santiago High gave me some of my best memories."
    };

    [Fact]
    public async Task ProcessSubmitMemoir_creates_a_pending_memoir_for_a_valid_submission()
    {
        var store = new FakeMemoirStore();
        var emailSender = new FakeEmailSender();

        var (success, _) = await AlumniSurfaceController.ProcessSubmitMemoir(store, emailSender, ValidModel());

        Assert.True(success);
        Assert.Single(store.Submissions);
        Assert.Equal("alex.rivera@example.com", store.Submissions[0].Email);
    }

    [Fact]
    public async Task ProcessSubmitMemoir_silently_rejects_a_tripped_honeypot_without_creating_a_submission()
    {
        var store = new FakeMemoirStore();
        var emailSender = new FakeEmailSender();
        var model = ValidModel();
        model.Website = "https://spam.example";

        var (success, _) = await AlumniSurfaceController.ProcessSubmitMemoir(store, emailSender, model);

        Assert.False(success);
        Assert.Empty(store.Submissions);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSubmitMemoir_rejects_an_invalid_email_without_creating_a_submission()
    {
        var store = new FakeMemoirStore();
        var emailSender = new FakeEmailSender();
        var model = ValidModel();
        model.Email = "not-an-email";

        var (success, _) = await AlumniSurfaceController.ProcessSubmitMemoir(store, emailSender, model);

        Assert.False(success);
        Assert.Empty(store.Submissions);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSubmitMemoir_rejects_a_blank_author_name()
    {
        var store = new FakeMemoirStore();
        var emailSender = new FakeEmailSender();
        var model = ValidModel();
        model.AuthorName = "  ";

        var (success, _) = await AlumniSurfaceController.ProcessSubmitMemoir(store, emailSender, model);

        Assert.False(success);
        Assert.Empty(store.Submissions);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSubmitMemoir_rejects_blank_memoir_text()
    {
        var store = new FakeMemoirStore();
        var emailSender = new FakeEmailSender();
        var model = ValidModel();
        model.MemoirText = "   ";

        var (success, _) = await AlumniSurfaceController.ProcessSubmitMemoir(store, emailSender, model);

        Assert.False(success);
        Assert.Empty(store.Submissions);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSubmitMemoir_rejects_memoir_text_over_the_max_length()
    {
        var store = new FakeMemoirStore();
        var emailSender = new FakeEmailSender();
        var model = ValidModel();
        model.MemoirText = new string('a', 5001);

        var (success, _) = await AlumniSurfaceController.ProcessSubmitMemoir(store, emailSender, model);

        Assert.False(success);
        Assert.Empty(store.Submissions);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSubmitMemoir_emails_a_review_notice_to_the_school_inbox()
    {
        var store = new FakeMemoirStore();
        var emailSender = new FakeEmailSender();

        var (success, _) = await AlumniSurfaceController.ProcessSubmitMemoir(store, emailSender, ValidModel());

        Assert.True(success);
        var (message, _) = Assert.Single(emailSender.Sent);
        Assert.Contains("shs@ggusd.us", message.To);
        Assert.Contains("Alex Rivera", message.Subject);
    }

    [Fact]
    public async Task ProcessSubmitMemoir_still_succeeds_when_the_notice_email_fails()
    {
        var store = new FakeMemoirStore();

        var (success, _) = await AlumniSurfaceController.ProcessSubmitMemoir(store, new ThrowingEmailSender(), ValidModel());

        Assert.True(success);
        Assert.Single(store.Submissions);
    }

    [Fact]
    public async Task ProcessSubmitMemoir_skips_the_notice_when_email_is_not_configured()
    {
        var store = new FakeMemoirStore();
        var emailSender = new FakeEmailSender { CanSend = false };

        var (success, _) = await AlumniSurfaceController.ProcessSubmitMemoir(store, emailSender, ValidModel());

        Assert.True(success);
        Assert.Single(store.Submissions);
        Assert.Empty(emailSender.Sent);
    }
}
