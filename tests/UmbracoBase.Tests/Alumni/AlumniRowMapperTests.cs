using UmbracoBase.Core.Alumni;
using Xunit;

namespace UmbracoBase.Tests.Alumni;

public class AlumniRowMapperTests
{
    private static Dictionary<string, string> FullRow() => new()
    {
        ["REC_ID"] = "558332",
        ["Record Date"] = "9/14/2026",
        ["Graduation Year"] = "1977",
        ["Title"] = "Mrs",
        ["First Name"] = "Pat",
        ["Middle Name"] = "Elaine",
        ["Last Name"] = " Example",
        ["Former Last Name"] = "Sample",
        ["Gender"] = "F",
        ["Email 1"] = "pat.example@example.com",
        ["Email 2"] = "",
        ["Homepage"] = "https://example.com/pat",
        ["Phone 1"] = "443 955-9264",
        ["Phone 2"] = "",
        ["Street"] = "P.O. Box 1546",
        ["City"] = "North Beach",
        ["State"] = "Maryland",
        ["Zip"] = "20714",
        ["Country"] = "USA",
        ["Industry"] = "Medical",
        ["Profession"] = "Nurse",
        ["Other information"] = "Loved my time at Santiago!",
        ["Emailing is OK"] = "1",
    };

    [Fact]
    public void Map_reads_every_column_into_the_matching_field()
    {
        var row = AlumniRowMapper.Map(FullRow());

        Assert.NotNull(row);
        Assert.Equal("558332", row!.LegacyRecId);
        Assert.Equal(1977, row.GradYear);
        Assert.Equal("Pat", row.FirstName);
        Assert.Equal("Example", row.LastName); // trimmed
        Assert.Equal("Sample", row.FormerLastName);
        Assert.Equal("F", row.Gender);
        Assert.Equal("pat.example@example.com", row.Email);
        Assert.Equal("https://example.com/pat", row.HomepageUrl);
        Assert.Equal("443 955-9264", row.Phone);
        Assert.Equal("Medical", row.Industry);
        Assert.Equal("Nurse", row.Profession);
        Assert.Equal("Loved my time at Santiago!", row.Update);
        Assert.True(row.EmailingOk);
    }

    [Fact]
    public void Map_combines_street_city_state_zip_country_into_one_address()
    {
        var row = AlumniRowMapper.Map(FullRow());

        Assert.Equal("P.O. Box 1546, North Beach, Maryland 20714, USA", row!.Address);
    }

    [Fact]
    public void Map_treats_zero_in_emailing_is_ok_as_false()
    {
        var raw = FullRow();
        raw["Emailing is OK"] = "0";

        var row = AlumniRowMapper.Map(raw);

        Assert.False(row!.EmailingOk);
    }

    [Fact]
    public void Map_returns_null_when_email_1_is_blank()
    {
        var raw = FullRow();
        raw["Email 1"] = "";

        var row = AlumniRowMapper.Map(raw);

        Assert.Null(row);
    }

    [Fact]
    public void Map_tolerates_a_non_numeric_graduation_year()
    {
        var raw = FullRow();
        raw["Graduation Year"] = "";

        var row = AlumniRowMapper.Map(raw);

        Assert.NotNull(row);
        Assert.Null(row!.GradYear);
    }

    [Fact]
    public void Map_drops_title_middle_name_and_record_date()
    {
        // No assertion needed beyond Map_reads_every_column_into_the_matching_field
        // not exposing them - AlumniImportRow simply has no properties for them.
        var row = AlumniRowMapper.Map(FullRow());
        Assert.NotNull(row);
    }
}
