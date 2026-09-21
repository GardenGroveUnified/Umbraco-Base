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
        private const int MaxMemoirLength = 5000;

        // Placeholder recipient for the staff moderation-notice email sent on
        // signup (see the Alumni Contact Portal spec's Signup flow, step 4).
        // Needs a real value confirmed with the district later.
        private const string StaffNotificationEmail = "alumni-signups@santiagohs.org";

        private readonly IAlumniMemberStore _store;
        private readonly IMemoirStore _memoirStore;
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
            IMemoirStore memoirStore,
            IEmailSender emailSender,
            ILogger<AlumniSurfaceController> logger)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _store = store;
            _memoirStore = memoirStore;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpPost]
        [EnableRateLimiting("alumni-signup")]
        public async Task<IActionResult> SignUp(AlumniSignupFormModel model)
        {
            var (success, message) = await ProcessSignUp(_store, _emailSender, model, _logger);
            return Json(new { success, message });
        }

        /// <summary>
        /// The actual signup decision logic, separated from the HTTP action so
        /// it can be unit tested without standing up a full SurfaceController.
        /// </summary>
        internal static async Task<(bool Success, string Message)> ProcessSignUp(
            IAlumniMemberStore store, IEmailSender emailSender, AlumniSignupFormModel model, ILogger? logger = null)
        {
            const string genericRejection = "We couldn't process that submission. Please check your details and try again.";

            if (AlumniFormGuard.IsHoneypotTripped(model.Website)) { return (false, genericRejection); }
            if (string.IsNullOrWhiteSpace(model.FirstName)) { return (false, genericRejection); }
            if (string.IsNullOrWhiteSpace(model.LastName)) { return (false, genericRejection); }
            if (!AlumniFormGuard.IsValidEmail(model.Email)) { return (false, genericRejection); }

            var firstName = model.FirstName.Trim();
            var lastName = model.LastName.Trim();

            store.CreateSignup(new AlumniSignupInput(
                FirstName: firstName,
                LastName: lastName,
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

            // Best-effort staff moderation notice (spec: Signup flow, step 4).
            // If this fails, the Member is still created - staff pick it up on
            // their next regular check of the Members section.
            try
            {
                if (emailSender.CanSendRequiredEmail())
                {
                    var notice = new EmailMessage(
                        from: "alumnishs53@gmail.com",
                        to: new[] { StaffNotificationEmail },
                        cc: null,
                        bcc: null,
                        replyTo: null,
                        subject: $"New Alumni Directory signup pending review: {firstName} {lastName}",
                        body: $"{firstName} {lastName} ({model.Email.Trim()}) submitted a new Alumni Directory signup and is awaiting review in the backoffice Members section.",
                        isBodyHtml: false,
                        attachments: null);

                    await emailSender.SendAsync(notice, "AlumniSignupNotice");
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Alumni staff moderation-notice email for {Email} failed.", model.Email);
            }

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

            if (!emailSender.CanSendRequiredEmail()) { return (false, sendFailure); }

            var email = new EmailMessage(
                from: "alumnishs53@gmail.com",
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

        [HttpPost]
        [EnableRateLimiting("alumni-memoir")]
        public IActionResult SubmitMemoir(MemoirSubmissionFormModel model)
        {
            var (success, message) = ProcessSubmitMemoir(_memoirStore, model);
            return Json(new { success, message });
        }

        /// <summary>
        /// The actual memoir-submission decision logic, separated from the HTTP
        /// action so it can be unit tested without standing up a full
        /// SurfaceController.
        /// </summary>
        internal static (bool Success, string Message) ProcessSubmitMemoir(IMemoirStore store, MemoirSubmissionFormModel model)
        {
            const string genericRejection = "We couldn't process that submission. Please check your details and try again.";

            if (AlumniFormGuard.IsHoneypotTripped(model.Website)) { return (false, genericRejection); }
            if (string.IsNullOrWhiteSpace(model.AuthorName)) { return (false, genericRejection); }
            if (!AlumniFormGuard.IsValidEmail(model.Email)) { return (false, genericRejection); }
            if (!AlumniFormGuard.IsValidMessage(model.MemoirText, MaxMemoirLength)) { return (false, genericRejection); }

            store.CreateSubmission(new MemoirSubmissionInput(
                AuthorName: model.AuthorName.Trim(),
                GradYear: model.GradYear,
                Email: model.Email.Trim(),
                MemoirText: model.MemoirText.Trim()));

            return (true, "Thanks! Your memoir is pending review and will appear in Read Memoirs once approved.");
        }
    }
}
