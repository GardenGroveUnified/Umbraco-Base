using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
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
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AlumniSurfaceController> _logger;

        public AlumniSurfaceController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IAlumniMemberStore store,
            IEmailSender emailSender,
            ILogger<AlumniSurfaceController> logger)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _store = store;
            _emailSender = emailSender;
            _logger = logger;
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

        [HttpPost]
        [EnableRateLimiting("alumni-contact")]
        public async Task<IActionResult> SendMessage(AlumniContactFormModel model)
        {
            var (success, message) = await ProcessSendMessage(_store, _emailSender, model, _logger);
            return Json(new { success, message });
        }

        /// <summary>
        /// The actual contact-relay decision logic, separated from the HTTP action so
        /// it can be unit tested without standing up a full SurfaceController.
        /// </summary>
        internal static async Task<(bool Success, string Message)> ProcessSendMessage(
            IAlumniMemberStore store, IEmailSender emailSender, AlumniContactFormModel model, ILogger? logger = null)
        {
            const string genericRejection = "We couldn't send that message. Please check your details and try again.";
            const string sendFailure = "Your message couldn't be sent right now. Please try again later.";

            if (AlumniFormGuard.IsHoneypotTripped(model.Website)) { return (false, genericRejection); }
            if (string.IsNullOrWhiteSpace(model.SenderName)) { return (false, genericRejection); }
            if (!AlumniFormGuard.IsValidEmail(model.SenderEmail)) { return (false, genericRejection); }
            if (!AlumniFormGuard.IsValidMessage(model.Message, MaxMessageLength)) { return (false, genericRejection); }

            var target = store.FindContactTarget(model.MemberId);
            if (target is null || !target.EmailingOk) { return (false, genericRejection); }

            var email = new EmailMessage(
                from: "no-reply@santiagohs.org",
                to: new[] { target.Email },
                cc: null,
                bcc: null,
                replyTo: new[] { model.SenderEmail },
                subject: $"Message from {model.SenderName} via the Alumni Directory",
                body: model.Message,
                isBodyHtml: false,
                attachments: null);

            try
            {
                await emailSender.SendAsync(email, "AlumniContact");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Alumni contact-relay email to member {MemberId} failed.", model.MemberId);
                return (false, sendFailure);
            }

            return (true, "Message sent!");
        }
    }
}
