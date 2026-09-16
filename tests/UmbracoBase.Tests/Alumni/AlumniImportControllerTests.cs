using UmbracoBase.Core.Alumni;
using UmbracoBase.Core.Controllers;
using UmbracoBase.Core.Models;
using Xunit;

namespace UmbracoBase.Tests.Alumni;

public class AlumniImportControllerTests
{
    private sealed class FakeImportStore : IAlumniMemberStore
    {
        public HashSet<string> ImportedRecIds { get; } = new();

        public bool ImportLegacyRow(AlumniImportRow row) => ImportedRecIds.Add(row.LegacyRecId);

        public AlumniMemberSummary CreateSignup(AlumniSignupInput input) => throw new NotSupportedException();
        public AlumniBrowseResult Browse(string? query, int? gradYear, int page, int pageSize) => throw new NotSupportedException();
        public AlumniContactTarget? FindContactTarget(Guid memberId) => throw new NotSupportedException();
    }

    private static AlumniImportRow Row(string recId, string email = "a@example.com") => new(
        LegacyRecId: recId, FirstName: "A", LastName: "B", FormerLastName: null, GradYear: 2000,
        Gender: null, Email: email, HomepageUrl: null, Phone: null, Address: null,
        Industry: null, Profession: null, Update: null, EmailingOk: true);

    [Fact]
    public void RunImport_counts_a_freshly_imported_row_as_imported()
    {
        var store = new FakeImportStore();

        var summary = AlumniImportController.RunImport(store, new[] { Row("1") });

        Assert.Equal(1, summary.Imported);
        Assert.Equal(0, summary.SkippedExisting);
        Assert.Equal(0, summary.SkippedNoEmail);
    }

    [Fact]
    public void RunImport_counts_an_already_imported_rec_id_as_skipped()
    {
        var store = new FakeImportStore();
        store.ImportedRecIds.Add("1"); // simulate a prior run

        var summary = AlumniImportController.RunImport(store, new[] { Row("1") });

        Assert.Equal(0, summary.Imported);
        Assert.Equal(1, summary.SkippedExisting);
    }

    [Fact]
    public void RunImport_processes_multiple_rows_independently()
    {
        var store = new FakeImportStore();
        store.ImportedRecIds.Add("2"); // already there

        var summary = AlumniImportController.RunImport(store, new[] { Row("1"), Row("2"), Row("3") });

        Assert.Equal(2, summary.Imported);
        Assert.Equal(1, summary.SkippedExisting);
    }
}
