using UmbracoBase.Core.Alumni;
using UmbracoBase.Core.Models;
using Xunit;

namespace UmbracoBase.Tests.Alumni;

public class AlumniBrowseFilterTests
{
    private static AlumniMemberSummary Alum(Guid id, string first, string last, int gradYear)
        => new(
            Id: id,
            FirstName: first,
            LastName: last,
            FormerLastName: null,
            GradYear: gradYear,
            Industry: "Education",
            Profession: "Teacher",
            Update: null,
            HomepageUrl: null,
            EmailingOk: true);

    private static readonly AlumniMemberSummary Alice = Alum(Guid.NewGuid(), "Alice", "Nguyen", 2010);
    private static readonly AlumniMemberSummary Bob = Alum(Guid.NewGuid(), "Bob", "Alvarez", 2012);
    private static readonly AlumniMemberSummary Carol = Alum(Guid.NewGuid(), "Carol", "Nguyen", 2012);

    private static readonly AlumniMemberSummary[] All = { Alice, Bob, Carol };

    [Fact]
    public void Apply_with_no_filters_returns_everyone_and_the_total_count()
    {
        var result = AlumniBrowseFilter.Apply(All, query: null, gradYear: null, page: 1, pageSize: 10);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public void Apply_filters_by_name_query_case_insensitively_against_first_or_last_name()
    {
        var result = AlumniBrowseFilter.Apply(All, query: "nguyen", gradYear: null, page: 1, pageSize: 10);

        Assert.Equal(new[] { Alice, Carol }, result.Items);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public void Apply_filters_by_grad_year()
    {
        var result = AlumniBrowseFilter.Apply(All, query: null, gradYear: 2012, page: 1, pageSize: 10);

        Assert.Equal(new[] { Bob, Carol }, result.Items);
    }

    [Fact]
    public void Apply_combines_query_and_grad_year_filters()
    {
        var result = AlumniBrowseFilter.Apply(All, query: "nguyen", gradYear: 2012, page: 1, pageSize: 10);

        Assert.Equal(new[] { Carol }, result.Items);
    }

    [Fact]
    public void Apply_paginates_results_and_reports_the_unpaginated_total()
    {
        var result = AlumniBrowseFilter.Apply(All, query: null, gradYear: null, page: 1, pageSize: 2);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.TotalCount);

        var secondPage = AlumniBrowseFilter.Apply(All, query: null, gradYear: null, page: 2, pageSize: 2);
        Assert.Single(secondPage.Items);
    }

    [Fact]
    public void Apply_returns_empty_items_for_a_page_past_the_end()
    {
        var result = AlumniBrowseFilter.Apply(All, query: null, gradYear: null, page: 99, pageSize: 2);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.TotalCount);
    }
}
