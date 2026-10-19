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
    public void ProcessSubmitMemoir_creates_a_pending_memoir_for_a_valid_submission()
    {
        var store = new FakeMemoirStore();

        var (success, _) = AlumniSurfaceController.ProcessSubmitMemoir(store, ValidModel());

        Assert.True(success);
        Assert.Single(store.Submissions);
        Assert.Equal("alex.rivera@example.com", store.Submissions[0].Email);
    }

    [Fact]
    public void ProcessSubmitMemoir_silently_rejects_a_tripped_honeypot_without_creating_a_submission()
    {
        var store = new FakeMemoirStore();
        var model = ValidModel();
        model.Website = "https://spam.example";

        var (success, _) = AlumniSurfaceController.ProcessSubmitMemoir(store, model);

        Assert.False(success);
        Assert.Empty(store.Submissions);
    }

    [Fact]
    public void ProcessSubmitMemoir_rejects_an_invalid_email_without_creating_a_submission()
    {
        var store = new FakeMemoirStore();
        var model = ValidModel();
        model.Email = "not-an-email";

        var (success, _) = AlumniSurfaceController.ProcessSubmitMemoir(store, model);

        Assert.False(success);
        Assert.Empty(store.Submissions);
    }

    [Fact]
    public void ProcessSubmitMemoir_rejects_a_blank_author_name()
    {
        var store = new FakeMemoirStore();
        var model = ValidModel();
        model.AuthorName = "  ";

        var (success, _) = AlumniSurfaceController.ProcessSubmitMemoir(store, model);

        Assert.False(success);
        Assert.Empty(store.Submissions);
    }

    [Fact]
    public void ProcessSubmitMemoir_rejects_blank_memoir_text()
    {
        var store = new FakeMemoirStore();
        var model = ValidModel();
        model.MemoirText = "   ";

        var (success, _) = AlumniSurfaceController.ProcessSubmitMemoir(store, model);

        Assert.False(success);
        Assert.Empty(store.Submissions);
    }

    [Fact]
    public void ProcessSubmitMemoir_rejects_memoir_text_over_the_max_length()
    {
        var store = new FakeMemoirStore();
        var model = ValidModel();
        model.MemoirText = new string('a', 5001);

        var (success, _) = AlumniSurfaceController.ProcessSubmitMemoir(store, model);

        Assert.False(success);
        Assert.Empty(store.Submissions);
    }
}
