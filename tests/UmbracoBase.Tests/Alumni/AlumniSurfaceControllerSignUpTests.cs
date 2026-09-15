using UmbracoBase.Core.Alumni;
using UmbracoBase.Core.Controllers;
using UmbracoBase.Core.ViewModels;
using UmbracoBase.Tests.Alumni.Fakes;
using Xunit;

namespace UmbracoBase.Tests.Alumni;

public class AlumniSurfaceControllerSignUpTests
{
    private static AlumniSignupFormModel ValidModel() => new()
    {
        FirstName = "Alex",
        LastName = "Rivera",
        Email = "alex.rivera@example.com",
        GradYear = 2015
    };

    [Fact]
    public void ProcessSignUp_creates_a_member_for_a_valid_submission()
    {
        var store = new FakeAlumniMemberStore();

        var (success, _) = AlumniSurfaceController.ProcessSignUp(store, ValidModel());

        Assert.True(success);
        Assert.Single(store.Signups);
        Assert.Equal("alex.rivera@example.com", store.Signups[0].Email);
    }

    [Fact]
    public void ProcessSignUp_silently_rejects_a_tripped_honeypot_without_creating_a_member()
    {
        var store = new FakeAlumniMemberStore();
        var model = ValidModel();
        model.Website = "https://spam.example";

        var (success, _) = AlumniSurfaceController.ProcessSignUp(store, model);

        Assert.False(success);
        Assert.Empty(store.Signups);
    }

    [Fact]
    public void ProcessSignUp_rejects_an_invalid_email_without_creating_a_member()
    {
        var store = new FakeAlumniMemberStore();
        var model = ValidModel();
        model.Email = "not-an-email";

        var (success, _) = AlumniSurfaceController.ProcessSignUp(store, model);

        Assert.False(success);
        Assert.Empty(store.Signups);
    }

    [Fact]
    public void ProcessSignUp_rejects_a_blank_first_or_last_name()
    {
        var store = new FakeAlumniMemberStore();
        var model = ValidModel();
        model.FirstName = "  ";

        var (success, _) = AlumniSurfaceController.ProcessSignUp(store, model);

        Assert.False(success);
        Assert.Empty(store.Signups);
    }
}
