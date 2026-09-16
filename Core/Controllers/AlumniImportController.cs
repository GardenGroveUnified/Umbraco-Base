using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Security;
using UmbracoBase.Core.Alumni;
using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Controllers
{
    public sealed record ImportSummary(int Imported, int SkippedExisting, int SkippedNoEmail);

    /// <summary>
    /// One-time bulk import of the legacy AlumniDirectory.xls export into
    /// the alumniMember Member Type (see the Alumni Contact Portal spec's
    /// Bulk Import section). Gated to Development environment and a
    /// logged-in, approved backoffice user - never reachable in
    /// production even if this code ships. Delete this controller and its
    /// view once the real import has run, or leave it: it's inert outside
    /// Development.
    /// </summary>
    [AllowAnonymous]
    [Route("admin/alumni-import")]
    public class AlumniImportController : Controller
    {
        private readonly IAlumniMemberStore _store;
        private readonly IBackOfficeSecurity _backOfficeSecurity;
        private readonly IWebHostEnvironment _environment;

        public AlumniImportController(
            IAlumniMemberStore store, IBackOfficeSecurity backOfficeSecurity, IWebHostEnvironment environment)
        {
            _store = store;
            _backOfficeSecurity = backOfficeSecurity;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var denied = DenyUnlessDevAdmin();
            if (denied != null) { return denied; }

            return View("~/Views/Alumni/Import.cshtml");
        }

        [HttpPost]
        public IActionResult Import(IFormFile file)
        {
            var denied = DenyUnlessDevAdmin();
            if (denied != null) { return denied; }

            if (file is null || file.Length == 0)
            {
                ViewBag.Error = "Choose the AlumniDirectory.xls file first.";
                return View("~/Views/Alumni/Import.cshtml");
            }

            using var stream = file.OpenReadStream();
            var rawRows = AlumniSpreadsheetReader.ReadRows(stream);
            var mappedRows = rawRows.Select(AlumniRowMapper.Map).Where(r => r is not null).Select(r => r!);

            ViewBag.Summary = RunImport(_store, mappedRows);
            ViewBag.SkippedNoEmailFromReading = rawRows.Count - mappedRows.Count();
            return View("~/Views/Alumni/Import.cshtml");
        }

        internal static ImportSummary RunImport(IAlumniMemberStore store, IEnumerable<AlumniImportRow> rows)
        {
            var imported = 0;
            var skippedExisting = 0;

            foreach (var row in rows)
            {
                if (store.ImportLegacyRow(row)) { imported++; } else { skippedExisting++; }
            }

            return new ImportSummary(imported, skippedExisting, SkippedNoEmail: 0);
        }

        private IActionResult? DenyUnlessDevAdmin()
        {
            if (!_environment.IsDevelopment()) { return NotFound(); }

            var user = _backOfficeSecurity.CurrentUser;
            if (user is null || !user.IsApproved) { return NotFound(); }

            return null;
        }
    }
}
