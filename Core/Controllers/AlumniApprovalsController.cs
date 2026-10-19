using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Security;
using UmbracoBase.Core.Alumni;
using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Controllers
{
    /// <summary>
    /// Staff-only list of alumni signups still awaiting approval, since the
    /// backoffice Members grid has no way to filter or sort by the Approved
    /// toggle. Gated on the backoffice login, same check AdminController and
    /// CustomBackOfficeLoginController use.
    /// </summary>
    [AllowAnonymous]
    public class AlumniApprovalsController : Controller
    {
        private readonly IAlumniMemberStore _store;
        private readonly IMemoirStore _memoirStore;
        private readonly IBackOfficeSecurity _backOfficeSecurity;

        public AlumniApprovalsController(IAlumniMemberStore store, IMemoirStore memoirStore, IBackOfficeSecurity backOfficeSecurity)
        {
            _store = store;
            _memoirStore = memoirStore;
            _backOfficeSecurity = backOfficeSecurity;
        }

        [HttpGet]
        [Route("admin/alumni-approvals")]
        public IActionResult Index()
        {
            var currentUser = _backOfficeSecurity.CurrentUser;
            if (currentUser is null || !currentUser.IsApproved)
            {
                return RedirectToAction("Login", "CustomBackOfficeLogin", new { returnUrl = "/admin/alumni-approvals" });
            }

            var model = new AlumniApprovalsPageModel(_store.GetPendingApprovals(), _memoirStore.GetPendingApprovals());
            return View(model);
        }
    }
}
