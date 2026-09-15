using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using UmbracoBase.Core.Alumni;
using UmbracoBase.Core.Models;
using UmbracoBase.Core.ViewModels;

namespace UmbracoBase.Core.Controllers
{
    public class AlumniSurfaceController : SurfaceController
    {
        private const int MaxMessageLength = 2000;

        private readonly IAlumniMemberStore _store;

        public AlumniSurfaceController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IAlumniMemberStore store)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _store = store;
        }

        [HttpPost]
        [EnableRateLimiting("alumni-signup")]
        public IActionResult SignUp(AlumniSignupFormModel model)
        {
            var (success, message) = ProcessSignUp(_store, model);
            return Json(new { success, message });
        }

        /// <summary>
        /// The actual signup decision logic, separated from the HTTP action so
        /// it can be unit tested without standing up a full SurfaceController.
        /// </summary>
        internal static (bool Success, string Message) ProcessSignUp(IAlumniMemberStore store, AlumniSignupFormModel model)
        {
            const string genericRejection = "We couldn't process that submission. Please check your details and try again.";

            if (AlumniFormGuard.IsHoneypotTripped(model.Website)) { return (false, genericRejection); }
            if (string.IsNullOrWhiteSpace(model.FirstName)) { return (false, genericRejection); }
            if (string.IsNullOrWhiteSpace(model.LastName)) { return (false, genericRejection); }
            if (!AlumniFormGuard.IsValidEmail(model.Email)) { return (false, genericRejection); }

            store.CreateSignup(new AlumniSignupInput(
                FirstName: model.FirstName.Trim(),
                LastName: model.LastName.Trim(),
                FormerLastName: model.FormerLastName,
                GradYear: model.GradYear,
                Industry: model.Industry,
                Profession: model.Profession,
                Update: model.Update,
                HomepageUrl: model.HomepageUrl,
                Email: model.Email.Trim(),
                Phone: model.Phone,
                Address: model.Address,
                Gender: model.Gender));

            return (true, "Thanks! Your submission is pending review and will appear in the directory once approved.");
        }
    }
}
