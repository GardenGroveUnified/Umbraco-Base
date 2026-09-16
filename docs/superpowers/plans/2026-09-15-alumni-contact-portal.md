# Alumni Contact Portal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let alumni browse a public directory of other alumni and send them a message without either side seeing the other's email address, backed by a one-time bulk import of the spreadsheet already copied from the live site and a public self-service signup form.

**Architecture:** Alumni records live as Umbraco Members (`alumniMember` Member Type) — a database-only store, never uSync-exported. A narrow `IAlumniMemberStore` interface hides all `IMemberService` plumbing behind three operations (`CreateSignup`, `Browse`, `FindContactTarget`), so the pure business logic (name/grad-year filtering, honeypot/validation) is unit-testable without faking Umbraco's large service interfaces. A `AlumniSurfaceController` exposes `SignUp` and `SendMessage`; the contact relay uses Umbraco's own built-in `IEmailSender` (already registered by the framework, already no-ops safely when no SMTP is configured) rather than a custom abstraction — this satisfies the spec's "swap in real SMTP later without touching relay logic" requirement for free. A dev-and-admin-gated `AlumniImportController` drives the one-time bulk import from the spreadsheet, reusing the same store.

**Tech Stack:** Umbraco 17 (.NET 10), xUnit (no mocking library — hand-rolled fakes, matching `tests/UmbracoBase.Tests/Calendar/CalendarFeedServiceTests.cs`), ASP.NET Core's built-in rate limiter, `ExcelDataReader` for the legacy `.xls` import.

**Spec:** `docs/superpowers/specs/2026-09-15-alumni-contact-portal-design.md`

## Global Constraints

- Alumni personal data (email, phone, address, gender, `legacyRecId`) never enters uSync-exported files or git — it lives only in Umbraco Members (database).
- The public directory and contact form require no login of any kind.
- Spam guard on both public forms: a honeypot field plus per-IP rate limiting (5 contact messages / hour, 3 signups / hour).
- `emailingOk = false` hides the "Send a message" button for that person, both in the widget and re-checked server-side in the controller (never trust the client).
- The contact-relay email's `To` is the target Member's stored email; `Reply-To` is the sender's own email; `From` is a fixed no-reply address. The target's email must never appear in an HTTP response, page HTML, or log line.
- No custom `IEmailSender` abstraction — use Umbraco's own `Umbraco.Cms.Core.Mail.IEmailSender` (confirmed present via `Umbraco.Cms.Core.Models.Email.EmailMessage` and `IEmailSender.SendAsync(EmailMessage, string)` / `CanSendRequiredEmail()`), so real SMTP settings slot into `appsettings.json` later with zero code changes.
- `IMember` custom properties are read/written via `member.Properties["alias"]` (`IProperty.SetValue(object value, string? culture, string? segment)` / `.GetValue(string? culture, string? segment, bool published)`) — there is no simpler `SetValue(member, alias, value)` extension method in this Umbraco version (confirmed by reflecting the installed `Umbraco.Cms` 17.3.5 package).
- The bulk import cannot run as a bare console app without re-implementing Umbraco's entire host bootstrap (`IMemberService` needs the full DI container). It instead runs as a `Development`-environment-only, backoffice-admin-gated controller action inside the already-running site, so it reuses the live DI container for free.

---

## Task 1: `alumniMember` Member Type

**Files:**
- Create: `uSync/v17/MemberTypes/alumnimember.config`

**Interfaces:**
- Produces: Member Type alias `alumniMember` with properties `firstName`, `lastName`, `formerLastName`, `gradYear` (Numeric), `industry`, `profession`, `update` (Textarea), `homepageUrl`, `phone`, `address` (Textarea), `gender`, `emailingOk` (True/false), `legacyRecId` — all on a `profile` tab. Built-in `Email`, `Username`, `Name`, `IsApproved` come from the base Member Type, no config needed.

- [ ] **Step 1: Write the Member Type config**

Reuses existing DataType keys already in this repo (no new DataTypes needed): Textstring `0cc0eba1-9960-42c9-bf9b-60e150b429ae`, Textarea `c6bac0dd-4ab9-45b1-8e30-e4b619ee5da3`, True/false `92897bc6-a5f3-4ffe-ae27-f2e7e33dda49`, Numeric `2e6d3631-066e-44b8-aec4-96f09099b2b5`.

```xml
<?xml version="1.0" encoding="utf-8"?>
<MemberType Key="1f598903-9661-43eb-84d6-9f2d1e8ccc56" Alias="alumniMember" Level="1">
  <Info>
    <Name>Alumni Member</Name>
    <Icon>icon-user color-blue</Icon>
    <Thumbnail>icon-user</Thumbnail>
    <Description>An alumnus in the Alumni Contact Portal directory. Email/phone/address/gender/legacyRecId are private - never rendered publicly.</Description>
    <AllowAtRoot>False</AllowAtRoot>
    <ListView>00000000-0000-0000-0000-000000000000</ListView>
    <Variations>Nothing</Variations>
    <IsElement>false</IsElement>
    <Compositions />
  </Info>
  <GenericProperties>
    <GenericProperty>
      <Key>2d0e5df4-a851-4859-91f8-7f79fbad36ff</Key>
      <Name>First Name</Name>
      <Alias>firstName</Alias>
      <Definition>0cc0eba1-9960-42c9-bf9b-60e150b429ae</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>true</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[]]></Description>
      <SortOrder>0</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>false</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>8a91f358-0046-4d40-aef9-af03f9716323</Key>
      <Name>Last Name</Name>
      <Alias>lastName</Alias>
      <Definition>0cc0eba1-9960-42c9-bf9b-60e150b429ae</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>true</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[]]></Description>
      <SortOrder>1</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>false</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>87dc6643-4828-48e6-b1f0-b4f22464da78</Key>
      <Name>Former Last Name</Name>
      <Alias>formerLastName</Alias>
      <Definition>0cc0eba1-9960-42c9-bf9b-60e150b429ae</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Maiden/prior name. Public.]]></Description>
      <SortOrder>2</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>false</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>91f4e171-9d8f-4886-a308-98f2986f23d7</Key>
      <Name>Grad Year</Name>
      <Alias>gradYear</Alias>
      <Definition>2e6d3631-066e-44b8-aec4-96f09099b2b5</Definition>
      <Type>Umbraco.Integer</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[]]></Description>
      <SortOrder>3</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>false</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>2ccb125c-a678-49e5-b29d-6fa7555bde35</Key>
      <Name>Industry</Name>
      <Alias>industry</Alias>
      <Definition>0cc0eba1-9960-42c9-bf9b-60e150b429ae</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[]]></Description>
      <SortOrder>4</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>false</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>d74d8b60-8192-4fa7-a890-34a9d097f8ea</Key>
      <Name>Profession</Name>
      <Alias>profession</Alias>
      <Definition>0cc0eba1-9960-42c9-bf9b-60e150b429ae</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[]]></Description>
      <SortOrder>5</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>false</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>b51c5ef0-1847-43e1-ae84-963bead0e7d5</Key>
      <Name>Update</Name>
      <Alias>update</Alias>
      <Definition>c6bac0dd-4ab9-45b1-8e30-e4b619ee5da3</Definition>
      <Type>Umbraco.TextArea</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[A short public update/bio, e.g. "Other information" from the legacy directory.]]></Description>
      <SortOrder>6</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>false</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>110bb322-4adf-4e41-985e-9b67178ac732</Key>
      <Name>Homepage URL</Name>
      <Alias>homepageUrl</Alias>
      <Definition>0cc0eba1-9960-42c9-bf9b-60e150b429ae</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[]]></Description>
      <SortOrder>7</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>false</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>d73037ce-9ce2-462a-91a6-29fa0d71dec5</Key>
      <Name>Phone</Name>
      <Alias>phone</Alias>
      <Definition>0cc0eba1-9960-42c9-bf9b-60e150b429ae</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Private. Never rendered publicly.]]></Description>
      <SortOrder>8</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>true</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>2c70fd91-bc5a-432a-a029-1df6f9802a5b</Key>
      <Name>Address</Name>
      <Alias>address</Alias>
      <Definition>c6bac0dd-4ab9-45b1-8e30-e4b619ee5da3</Definition>
      <Type>Umbraco.TextArea</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Private. Combined Street/City/State/Zip/Country on import.]]></Description>
      <SortOrder>9</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>true</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>00f88e36-395b-4f6d-9d20-82e01ffd9ed5</Key>
      <Name>Gender</Name>
      <Alias>gender</Alias>
      <Definition>0cc0eba1-9960-42c9-bf9b-60e150b429ae</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Private. Never rendered publicly.]]></Description>
      <SortOrder>10</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>true</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>2419e82e-7a78-40bd-b1f0-2476fd2e6335</Key>
      <Name>Emailing OK</Name>
      <Alias>emailingOk</Alias>
      <Definition>92897bc6-a5f3-4ffe-ae27-f2e7e33dda49</Definition>
      <Type>Umbraco.TrueFalse</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[When off, this person still appears in the directory but the "Send a message" button is hidden.]]></Description>
      <SortOrder>11</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>false</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>7d13f90b-3507-46a5-b0a2-e4fca50fb59d</Key>
      <Name>Legacy Rec Id</Name>
      <Alias>legacyRecId</Alias>
      <Definition>0cc0eba1-9960-42c9-bf9b-60e150b429ae</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Spreadsheet REC_ID, for idempotent re-import. Not set for self-service signups.]]></Description>
      <SortOrder>12</SortOrder>
      <Tab Alias="profile">Profile</Tab>
      <CanEdit>false</CanEdit>
      <CanView>false</CanView>
      <IsSensitive>true</IsSensitive>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
  </GenericProperties>
  <Structure />
  <Tabs>
    <Tab>
      <Key>2bb094a5-4e12-40c0-a9f6-b11062864c03</Key>
      <Caption>Profile</Caption>
      <Alias>profile</Alias>
      <Type>Group</Type>
      <SortOrder>1</SortOrder>
    </Tab>
  </Tabs>
</MemberType>
```

- [ ] **Step 2: Validate the XML is well-formed**

Run: `python3 -c "import xml.dom.minidom as m; m.parse('uSync/v17/MemberTypes/alumnimember.config')"`
Expected: no output, exit code 0.

- [ ] **Step 3: Manually verify in the backoffice**

Start the site (or run the uSync import if it's already running), open **Members → Member Types**, confirm "Alumni Member" appears with all 13 properties on a "Profile" tab, `phone`/`address`/`gender`/`legacyRecId` marked sensitive.

- [ ] **Step 4: Commit**

```bash
git add uSync/v17/MemberTypes/alumnimember.config
git commit -m "Add alumniMember Member Type for the Alumni Contact Portal"
```

---

## Task 2: Browse filtering (`AlumniBrowseFilter`) and summary models

**Files:**
- Create: `Core/Models/AlumniMemberSummary.cs`
- Create: `Core/Models/AlumniBrowseResult.cs`
- Create: `Core/Alumni/AlumniBrowseFilter.cs`
- Test: `tests/UmbracoBase.Tests/Alumni/AlumniBrowseFilterTests.cs`

**Interfaces:**
- Produces: `AlumniMemberSummary` (public fields only), `AlumniBrowseResult`, `AlumniBrowseFilter.Apply(IEnumerable<AlumniMemberSummary> all, string? query, int? gradYear, int page, int pageSize) -> AlumniBrowseResult`.
- Consumed by: Task 4 (`UmbracoAlumniMemberStore.Browse`), Task 9 (the browse widget).

- [ ] **Step 1: Write the failing test**

```csharp
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
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniBrowseFilterTests`
Expected: build FAILS — `AlumniMemberSummary`, `AlumniBrowseResult`, `AlumniBrowseFilter` don't exist yet.

- [ ] **Step 3: Write the models**

```csharp
// Core/Models/AlumniMemberSummary.cs
namespace UmbracoBase.Core.Models;

/// <summary>
/// Public-facing alumni directory card data. Contains only fields the
/// Alumni Contact Portal spec marks public - never email, phone, address
/// or gender. See docs/superpowers/specs/2026-09-15-alumni-contact-portal-design.md.
/// </summary>
public sealed record AlumniMemberSummary(
    Guid Id,
    string FirstName,
    string LastName,
    string? FormerLastName,
    int? GradYear,
    string? Industry,
    string? Profession,
    string? Update,
    string? HomepageUrl,
    bool EmailingOk);
```

```csharp
// Core/Models/AlumniBrowseResult.cs
namespace UmbracoBase.Core.Models;

public sealed record AlumniBrowseResult(IReadOnlyList<AlumniMemberSummary> Items, int TotalCount);
```

- [ ] **Step 4: Write the filter**

```csharp
// Core/Alumni/AlumniBrowseFilter.cs
using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Alumni;

/// <summary>
/// Pure name/grad-year filtering and pagination over an in-memory list of
/// already-approved alumni. Kept free of MemberService so it can be unit
/// tested directly - the Umbraco-facing store (UmbracoAlumniMemberStore)
/// loads the full approved list once, then delegates here.
/// </summary>
public static class AlumniBrowseFilter
{
    public static AlumniBrowseResult Apply(
        IEnumerable<AlumniMemberSummary> all, string? query, int? gradYear, int page, int pageSize)
    {
        IEnumerable<AlumniMemberSummary> filtered = all;

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(a =>
                a.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                a.LastName.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (gradYear.HasValue)
        {
            filtered = filtered.Where(a => a.GradYear == gradYear.Value);
        }

        var matched = filtered.ToList();
        var pageItems = matched
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new AlumniBrowseResult(pageItems, matched.Count);
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniBrowseFilterTests`
Expected: PASS, 6 tests.

- [ ] **Step 6: Commit**

```bash
git add Core/Models/AlumniMemberSummary.cs Core/Models/AlumniBrowseResult.cs Core/Alumni/AlumniBrowseFilter.cs tests/UmbracoBase.Tests/Alumni/AlumniBrowseFilterTests.cs
git commit -m "Add alumni directory browse filter and summary models"
```

---

## Task 3: Honeypot and form validation (`AlumniFormGuard`)

**Files:**
- Create: `Core/Alumni/AlumniFormGuard.cs`
- Test: `tests/UmbracoBase.Tests/Alumni/AlumniFormGuardTests.cs`

**Interfaces:**
- Produces: `AlumniFormGuard.IsHoneypotTripped(string? honeypotValue) -> bool`, `AlumniFormGuard.IsValidEmail(string? email) -> bool`, `AlumniFormGuard.IsValidMessage(string? message, int maxLength) -> bool`.
- Consumed by: Task 7 (`SignUp`), Task 8 (`SendMessage`).

- [ ] **Step 1: Write the failing test**

```csharp
using UmbracoBase.Core.Alumni;
using Xunit;

namespace UmbracoBase.Tests.Alumni;

public class AlumniFormGuardTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("I am a bot", true)]
    public void IsHoneypotTripped_is_true_only_when_the_hidden_field_has_content(string? value, bool expected)
    {
        Assert.Equal(expected, AlumniFormGuard.IsHoneypotTripped(value));
    }

    [Theory]
    [InlineData("alumni@example.com", true)]
    [InlineData("first.last+tag@sub.example.org", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("not-an-email", false)]
    [InlineData("missing@domain", false)]
    [InlineData("@nolocal.com", false)]
    public void IsValidEmail_matches_a_plausible_email_shape(string? email, bool expected)
    {
        Assert.Equal(expected, AlumniFormGuard.IsValidEmail(email));
    }

    [Fact]
    public void IsValidMessage_rejects_null_or_whitespace()
    {
        Assert.False(AlumniFormGuard.IsValidMessage(null, maxLength: 2000));
        Assert.False(AlumniFormGuard.IsValidMessage("   ", maxLength: 2000));
    }

    [Fact]
    public void IsValidMessage_rejects_a_message_longer_than_the_max_length()
    {
        var tooLong = new string('a', 2001);
        Assert.False(AlumniFormGuard.IsValidMessage(tooLong, maxLength: 2000));
    }

    [Fact]
    public void IsValidMessage_accepts_a_message_within_the_max_length()
    {
        Assert.True(AlumniFormGuard.IsValidMessage("Hi, great to hear from you!", maxLength: 2000));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniFormGuardTests`
Expected: build FAILS — `AlumniFormGuard` doesn't exist yet.

- [ ] **Step 3: Write the guard**

```csharp
// Core/Alumni/AlumniFormGuard.cs
using System.Text.RegularExpressions;

namespace UmbracoBase.Core.Alumni;

/// <summary>
/// Shared spam/validation checks for the alumni signup and contact-relay
/// forms. A tripped honeypot or failed validation must never reveal *why*
/// to the caller - callers should return the same generic rejection either
/// way, per the spec's error-handling section.
/// </summary>
public static partial class AlumniFormGuard
{
    private const int MaxEmailLength = 254;

    public static bool IsHoneypotTripped(string? honeypotValue) => !string.IsNullOrWhiteSpace(honeypotValue);

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > MaxEmailLength) { return false; }
        return EmailPattern().IsMatch(email);
    }

    public static bool IsValidMessage(string? message, int maxLength)
        => !string.IsNullOrWhiteSpace(message) && message.Length <= maxLength;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniFormGuardTests`
Expected: PASS, 11 tests.

- [ ] **Step 5: Commit**

```bash
git add Core/Alumni/AlumniFormGuard.cs tests/UmbracoBase.Tests/Alumni/AlumniFormGuardTests.cs
git commit -m "Add honeypot and validation guard for alumni forms"
```

---

## Task 4: `IAlumniMemberStore` and its Umbraco-backed implementation

**Files:**
- Create: `Core/Models/AlumniSignupInput.cs`
- Create: `Core/Models/AlumniContactTarget.cs`
- Create: `Core/Alumni/IAlumniMemberStore.cs`
- Create: `Core/Alumni/UmbracoAlumniMemberStore.cs`

**Interfaces:**
- Consumes: `AlumniBrowseFilter.Apply` (Task 2), `IMemberService`/`IMemberTypeService` (Umbraco core).
- Produces: `IAlumniMemberStore` with `CreateSignup(AlumniSignupInput) -> AlumniMemberSummary`, `Browse(string? query, int? gradYear, int page, int pageSize) -> AlumniBrowseResult`, `FindContactTarget(Guid memberId) -> AlumniContactTarget?`. Consumed by Task 5 (DI), Task 7/8 (controller), Task 9 (browse widget), Task 14 (import controller, via a fourth method added in that task).

This task's own implementation is thin glue over `IMemberService` and is not independently unit-testable without a live Umbraco host (same reasoning as `UmbracoAlumniMemberStore` in the spec) - it is verified manually in Step 5. The parts worth unit testing (filtering, validation) are already covered in Tasks 2-3; this task's job is to wire them to real data.

- [ ] **Step 1: Write the domain models**

```csharp
// Core/Models/AlumniSignupInput.cs
namespace UmbracoBase.Core.Models;

public sealed record AlumniSignupInput(
    string FirstName,
    string LastName,
    string? FormerLastName,
    int? GradYear,
    string? Industry,
    string? Profession,
    string? Update,
    string? HomepageUrl,
    string Email,
    string? Phone,
    string? Address,
    string? Gender);
```

```csharp
// Core/Models/AlumniContactTarget.cs
namespace UmbracoBase.Core.Models;

/// <summary>
/// Server-side-only view of a Member used to send the contact-relay
/// email. Never returned from a public controller action or rendered to
/// a page - Email is exactly the address the spec says must never reach
/// the browser.
/// </summary>
public sealed record AlumniContactTarget(Guid Id, string DisplayName, string Email, bool EmailingOk);
```

- [ ] **Step 2: Write the interface**

```csharp
// Core/Alumni/IAlumniMemberStore.cs
using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Alumni;

/// <summary>
/// The only thing that touches Umbraco's IMemberService for the Alumni
/// Contact Portal. Kept narrow (three operations) so controllers and
/// widgets can be tested against a hand-written fake instead of the much
/// larger IMemberService interface.
/// </summary>
public interface IAlumniMemberStore
{
    AlumniMemberSummary CreateSignup(AlumniSignupInput input);

    AlumniBrowseResult Browse(string? query, int? gradYear, int page, int pageSize);

    AlumniContactTarget? FindContactTarget(Guid memberId);
}
```

- [ ] **Step 3: Write the Umbraco-backed implementation**

```csharp
// Core/Alumni/UmbracoAlumniMemberStore.cs
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Alumni;

public sealed class UmbracoAlumniMemberStore : IAlumniMemberStore
{
    public const string MemberTypeAlias = "alumniMember";
    private const int DefaultPageSize = 24;

    private readonly IMemberService _memberService;

    public UmbracoAlumniMemberStore(IMemberService memberService)
    {
        _memberService = memberService;
    }

    public AlumniMemberSummary CreateSignup(AlumniSignupInput input)
    {
        var displayName = $"{input.FirstName} {input.LastName}".Trim();
        var member = _memberService.CreateMember(input.Email, input.Email, displayName, MemberTypeAlias);

        SetProfile(member, input);
        member.IsApproved = false; // staff moderates in the backoffice Members section

        _memberService.Save(member, Constants.Security.SuperUserId);

        return ToSummary(member);
    }

    public AlumniBrowseResult Browse(string? query, int? gradYear, int page, int pageSize)
    {
        var approved = _memberService
            .GetMembersByMemberType(MemberTypeAlias)
            .Where(m => m.IsApproved)
            .Select(ToSummary);

        return AlumniBrowseFilter.Apply(approved, query, gradYear, page, pageSize <= 0 ? DefaultPageSize : pageSize);
    }

    public AlumniContactTarget? FindContactTarget(Guid memberId)
    {
        var member = _memberService
            .GetMembersByMemberType(MemberTypeAlias)
            .FirstOrDefault(m => m.Key == memberId && m.IsApproved);

        if (member is null) { return null; }

        var displayName = $"{member.Name}".Trim();
        var emailingOk = GetBool(member, "emailingOk");

        return new AlumniContactTarget(member.Key, displayName, member.Email, emailingOk);
    }

    private static void SetProfile(IMember member, AlumniSignupInput input)
    {
        SetString(member, "firstName", input.FirstName);
        SetString(member, "lastName", input.LastName);
        SetString(member, "formerLastName", input.FormerLastName);
        SetInt(member, "gradYear", input.GradYear);
        SetString(member, "industry", input.Industry);
        SetString(member, "profession", input.Profession);
        SetString(member, "update", input.Update);
        SetString(member, "homepageUrl", input.HomepageUrl);
        SetString(member, "phone", input.Phone);
        SetString(member, "address", input.Address);
        SetString(member, "gender", input.Gender);
        SetBool(member, "emailingOk", true); // new signups opt in by default
    }

    private static AlumniMemberSummary ToSummary(IMember member) => new(
        Id: member.Key,
        FirstName: GetString(member, "firstName") ?? string.Empty,
        LastName: GetString(member, "lastName") ?? string.Empty,
        FormerLastName: GetString(member, "formerLastName"),
        GradYear: GetInt(member, "gradYear"),
        Industry: GetString(member, "industry"),
        Profession: GetString(member, "profession"),
        Update: GetString(member, "update"),
        HomepageUrl: GetString(member, "homepageUrl"),
        EmailingOk: GetBool(member, "emailingOk"));

    private static void SetString(IMember member, string alias, string? value)
        => member.Properties[alias]?.SetValue(value, null, null);

    private static void SetInt(IMember member, string alias, int? value)
        => member.Properties[alias]?.SetValue(value, null, null);

    private static void SetBool(IMember member, string alias, bool value)
        => member.Properties[alias]?.SetValue(value, null, null);

    private static string? GetString(IMember member, string alias)
        => member.Properties[alias]?.GetValue(null, null, false) as string;

    private static int? GetInt(IMember member, string alias)
        => member.Properties[alias]?.GetValue(null, null, false) switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => null
        };

    private static bool GetBool(IMember member, string alias)
        => member.Properties[alias]?.GetValue(null, null, false) switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            _ => false
        };
}
```

- [ ] **Step 4: Build the project**

Run: `dotnet build UmbracoBase.csproj`
Expected: 0 errors. (`Constants.Security.SuperUserId` and `IMemberService`/`IMember` are part of `Umbraco.Cms.Core`, already referenced.)

- [ ] **Step 5: Manually verify against the running site**

With the site running and Task 1's Member Type imported: temporarily call `CreateSignup` from a scratch route or the Umbraco C# REPL if available, or defer this check to Task 7's manual test (the `SignUp` controller action exercises this same code path end-to-end). Confirm a Member appears in **Members** with `IsApproved` off and the profile fields populated.

- [ ] **Step 6: Commit**

```bash
git add Core/Models/AlumniSignupInput.cs Core/Models/AlumniContactTarget.cs Core/Alumni/IAlumniMemberStore.cs Core/Alumni/UmbracoAlumniMemberStore.cs
git commit -m "Add IAlumniMemberStore and its Umbraco Member-backed implementation"
```

---

## Task 5: `AlumniComposer` (DI registration)

**Files:**
- Create: `Core/Composers/AlumniComposer.cs`

**Interfaces:**
- Consumes: `IAlumniMemberStore` / `UmbracoAlumniMemberStore` (Task 4).
- Produces: `IAlumniMemberStore` resolvable via constructor injection anywhere in the app (Task 7, 8, 9, 10, 14).

- [ ] **Step 1: Write the composer**

`IComposer` implementations are auto-discovered by `builder.CreateUmbracoBuilder().AddComposers()` in `Program.cs` - no manual registration needed, matching `Core/Composers/CalendarFeedComposer.cs`.

```csharp
// Core/Composers/AlumniComposer.cs
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using UmbracoBase.Core.Alumni;

namespace UmbracoBase.Core.Composers
{
    /// <summary>
    /// Registers the Alumni Contact Portal's data-access layer. Everything
    /// else in the feature (controllers, widgets) depends only on
    /// IAlumniMemberStore, never on IMemberService directly.
    /// </summary>
    public class AlumniComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddScoped<IAlumniMemberStore, UmbracoAlumniMemberStore>();
        }
    }
}
```

- [ ] **Step 2: Build the project**

Run: `dotnet build UmbracoBase.csproj`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Core/Composers/AlumniComposer.cs
git commit -m "Register IAlumniMemberStore via a composer"
```

---

## Task 6: Rate limiting for the alumni forms

**Files:**
- Modify: `Program.cs`

**Interfaces:**
- Produces: two named rate-limit policies, `"alumni-signup"` (3/hour per IP) and `"alumni-contact"` (5/hour per IP), applied via `[EnableRateLimiting("...")]` in Task 7 and Task 8.

- [ ] **Step 1: Add the rate limiter services and middleware**

```csharp
// Program.cs
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using UmbracoBase.Core.Bundling;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

// Add MVC services for custom controllers
builder.Services.AddControllersWithViews();

// Combine and minify the front-end stylesheets into one fingerprinted bundle.
builder.Services.AddSiteStylesheetBundle(builder.Environment);

// Per-IP caps on the public alumni signup and contact-relay forms, per the
// Alumni Contact Portal spec's spam-guard requirements.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("alumni-signup", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0
        }));

    options.AddPolicy("alumni-contact", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0
        }));
});

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

// Must run before static files so /css/site.bundle.css is served by WebOptimizer.
app.UseWebOptimizer();

app.UseRateLimiter();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

// Configure custom controller routes
app.MapControllerRoute(
    name: "admin",
    pattern: "admin",
    defaults: new { controller = "Admin", action = "Index" });

// Add a catch-all route for our custom controllers
app.MapControllerRoute(
    name: "customControllers",
    pattern: "{controller}/{action=Index}/{id?}");

await app.RunAsync();
```

- [ ] **Step 2: Build the project**

Run: `dotnet build UmbracoBase.csproj`
Expected: 0 errors.

- [ ] **Step 3: Manual verification (deferred)**

There is nothing to exercise the policies against yet - re-verify at the end of Task 7 by posting to `SignUp` 4 times in under an hour and confirming the 4th returns HTTP 429.

- [ ] **Step 4: Commit**

```bash
git add Program.cs
git commit -m "Add per-IP rate limiting for the alumni signup and contact forms"
```

---

## Task 7: `AlumniSurfaceController.SignUp`

**Files:**
- Create: `Core/ViewModels/AlumniSignupFormModel.cs`
- Create: `Core/Controllers/AlumniSurfaceController.cs`
- Test: `tests/UmbracoBase.Tests/Alumni/AlumniSurfaceControllerSignUpTests.cs`
- Test fake: `tests/UmbracoBase.Tests/Alumni/Fakes/FakeAlumniMemberStore.cs`

**Interfaces:**
- Consumes: `IAlumniMemberStore` (Task 4), `AlumniFormGuard` (Task 3).
- Produces: `POST /umbraco/surface/AlumniSurface/SignUp`, returning `{ success: bool, message: string }` as JSON. Consumed by Task 11's client-side JS.

- [ ] **Step 1: Write the form model**

```csharp
// Core/ViewModels/AlumniSignupFormModel.cs
namespace UmbracoBase.Core.ViewModels
{
    public class AlumniSignupFormModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? FormerLastName { get; set; }
        public int? GradYear { get; set; }
        public string? Industry { get; set; }
        public string? Profession { get; set; }
        public string? Update { get; set; }
        public string? HomepageUrl { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }

        /// <summary>Hidden field a real user never fills in. Non-empty = bot.</summary>
        public string? Website { get; set; }
    }
}
```

- [ ] **Step 2: Write the failing test**

```csharp
// tests/UmbracoBase.Tests/Alumni/Fakes/FakeAlumniMemberStore.cs
using UmbracoBase.Core.Alumni;
using UmbracoBase.Core.Models;

namespace UmbracoBase.Tests.Alumni.Fakes;

public sealed class FakeAlumniMemberStore : IAlumniMemberStore
{
    public List<AlumniSignupInput> Signups { get; } = new();
    public Dictionary<Guid, AlumniContactTarget> Targets { get; } = new();

    public AlumniMemberSummary CreateSignup(AlumniSignupInput input)
    {
        Signups.Add(input);
        return new AlumniMemberSummary(
            Guid.NewGuid(), input.FirstName, input.LastName, input.FormerLastName,
            input.GradYear, input.Industry, input.Profession, input.Update, input.HomepageUrl,
            EmailingOk: true);
    }

    public AlumniBrowseResult Browse(string? query, int? gradYear, int page, int pageSize)
        => new(Array.Empty<AlumniMemberSummary>(), 0);

    public AlumniContactTarget? FindContactTarget(Guid memberId)
        => Targets.TryGetValue(memberId, out var target) ? target : null;
}
```

```csharp
// tests/UmbracoBase.Tests/Alumni/AlumniSurfaceControllerSignUpTests.cs
using UmbracoBase.Core.Alumni;
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
```

The controller's actual `[HttpPost]` action is a thin ASP.NET Core wrapper the test harness can't easily instantiate (it needs `SurfaceController`'s six-argument base constructor wired to a live Umbraco context). So the decision logic is split into a `public static` method, `ProcessSignUp`, that the action calls - this mirrors `AlumniBrowseFilter`/`AlumniFormGuard`'s pure-logic-first approach and keeps the actual `[HttpPost]` action a one-line wrapper.

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniSurfaceControllerSignUpTests`
Expected: build FAILS — `AlumniSurfaceController` doesn't exist yet.

- [ ] **Step 4: Write the controller**

```csharp
// Core/Controllers/AlumniSurfaceController.cs
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
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniSurfaceControllerSignUpTests`
Expected: PASS, 4 tests.

- [ ] **Step 6: Build the whole solution**

Run: `dotnet build UmbracoBase.csproj`
Expected: 0 errors.

- [ ] **Step 7: Manually verify rate limiting end-to-end**

With the site running, POST to `/umbraco/surface/AlumniSurface/SignUp` with a valid form body 4 times within a minute (e.g. via `curl` or the browser once the widget from Task 10 exists). Expected: the first 3 succeed, the 4th returns HTTP 429.

- [ ] **Step 8: Commit**

```bash
git add Core/ViewModels/AlumniSignupFormModel.cs Core/Controllers/AlumniSurfaceController.cs tests/UmbracoBase.Tests/Alumni/AlumniSurfaceControllerSignUpTests.cs tests/UmbracoBase.Tests/Alumni/Fakes/FakeAlumniMemberStore.cs
git commit -m "Add AlumniSurfaceController.SignUp with honeypot and validation"
```

---

## Task 8: `AlumniSurfaceController.SendMessage` (the contact relay)

**Files:**
- Create: `Core/ViewModels/AlumniContactFormModel.cs`
- Modify: `Core/Controllers/AlumniSurfaceController.cs`
- Test: `tests/UmbracoBase.Tests/Alumni/AlumniSurfaceControllerSendMessageTests.cs`
- Test fake: `tests/UmbracoBase.Tests/Alumni/Fakes/FakeEmailSender.cs`

**Interfaces:**
- Consumes: `IAlumniMemberStore.FindContactTarget` (Task 4), `AlumniFormGuard` (Task 3), Umbraco's `Umbraco.Cms.Core.Mail.IEmailSender`.
- Produces: `POST /umbraco/surface/AlumniSurface/SendMessage`, same JSON shape as `SignUp`. Consumed by Task 11's JS.

- [ ] **Step 1: Write the form model**

```csharp
// Core/ViewModels/AlumniContactFormModel.cs
namespace UmbracoBase.Core.ViewModels
{
    public class AlumniContactFormModel
    {
        public Guid MemberId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        /// <summary>Hidden field a real user never fills in. Non-empty = bot.</summary>
        public string? Website { get; set; }
    }
}
```

- [ ] **Step 2: Write the failing test**

```csharp
// tests/UmbracoBase.Tests/Alumni/Fakes/FakeEmailSender.cs
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;

namespace UmbracoBase.Tests.Alumni.Fakes;

public sealed class FakeEmailSender : IEmailSender
{
    public List<(EmailMessage Message, string EmailType)> Sent { get; } = new();
    public bool CanSend { get; set; } = true;

    public Task SendAsync(EmailMessage message, string emailType)
    {
        Sent.Add((message, emailType));
        return Task.CompletedTask;
    }

    public Task SendAsync(EmailMessage message, string emailType, bool enableNotification)
        => SendAsync(message, emailType);

    public Task SendAsync(EmailMessage message, string emailType, bool enableNotification, DateTime? expires)
        => SendAsync(message, emailType);

    public bool CanSendRequiredEmail() => CanSend;
}
```

```csharp
// tests/UmbracoBase.Tests/Alumni/AlumniSurfaceControllerSendMessageTests.cs
using UmbracoBase.Core.Alumni;
using UmbracoBase.Core.Models;
using UmbracoBase.Core.ViewModels;
using UmbracoBase.Tests.Alumni.Fakes;
using Xunit;

namespace UmbracoBase.Tests.Alumni;

public class AlumniSurfaceControllerSendMessageTests
{
    private static readonly Guid TargetId = Guid.NewGuid();

    private static FakeAlumniMemberStore StoreWithTarget(bool emailingOk = true)
    {
        var store = new FakeAlumniMemberStore();
        store.Targets[TargetId] = new AlumniContactTarget(TargetId, "Jordan Lee", "jordan.lee@example.com", emailingOk);
        return store;
    }

    private static AlumniContactFormModel ValidModel() => new()
    {
        MemberId = TargetId,
        SenderName = "Casey Kim",
        SenderEmail = "casey.kim@example.com",
        Message = "Hey, small world - I think we were in the same class!"
    };

    [Fact]
    public async Task ProcessSendMessage_emails_the_target_with_reply_to_set_to_the_sender()
    {
        var store = StoreWithTarget();
        var emailSender = new FakeEmailSender();

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, ValidModel());

        Assert.True(success);
        Assert.Single(emailSender.Sent);
        var (message, _) = emailSender.Sent[0];
        Assert.Contains("jordan.lee@example.com", message.To);
        Assert.NotNull(message.ReplyTo);
        Assert.Contains("casey.kim@example.com", message.ReplyTo!);
    }

    [Fact]
    public async Task ProcessSendMessage_never_puts_the_targets_email_in_the_returned_message()
    {
        var store = StoreWithTarget();
        var emailSender = new FakeEmailSender();

        var (_, message) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, ValidModel());

        Assert.DoesNotContain("jordan.lee@example.com", message);
    }

    [Fact]
    public async Task ProcessSendMessage_rejects_when_the_target_has_emailing_off()
    {
        var store = StoreWithTarget(emailingOk: false);
        var emailSender = new FakeEmailSender();

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, ValidModel());

        Assert.False(success);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSendMessage_rejects_an_unknown_member_id()
    {
        var store = new FakeAlumniMemberStore(); // no targets registered
        var emailSender = new FakeEmailSender();

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, ValidModel());

        Assert.False(success);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSendMessage_silently_rejects_a_tripped_honeypot()
    {
        var store = StoreWithTarget();
        var emailSender = new FakeEmailSender();
        var model = ValidModel();
        model.Website = "https://spam.example";

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, model);

        Assert.False(success);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSendMessage_rejects_an_invalid_sender_email()
    {
        var store = StoreWithTarget();
        var emailSender = new FakeEmailSender();
        var model = ValidModel();
        model.SenderEmail = "not-an-email";

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, model);

        Assert.False(success);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSendMessage_rejects_a_blank_message()
    {
        var store = StoreWithTarget();
        var emailSender = new FakeEmailSender();
        var model = ValidModel();
        model.Message = "   ";

        var (success, _) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, model);

        Assert.False(success);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task ProcessSendMessage_returns_a_generic_message_when_sending_fails()
    {
        var store = StoreWithTarget();
        var emailSender = new ThrowingEmailSender();

        var (success, message) = await AlumniSurfaceController.ProcessSendMessage(store, emailSender, ValidModel());

        Assert.False(success);
        Assert.DoesNotContain("Exception", message);
        Assert.DoesNotContain("SMTP", message, StringComparison.OrdinalIgnoreCase);
    }
}
```

```csharp
// Append to tests/UmbracoBase.Tests/Alumni/Fakes/FakeEmailSender.cs
namespace UmbracoBase.Tests.Alumni.Fakes;

public sealed class ThrowingEmailSender : Umbraco.Cms.Core.Mail.IEmailSender
{
    public Task SendAsync(Umbraco.Cms.Core.Models.Email.EmailMessage message, string emailType)
        => throw new InvalidOperationException("SMTP relay unreachable");

    public Task SendAsync(Umbraco.Cms.Core.Models.Email.EmailMessage message, string emailType, bool enableNotification)
        => SendAsync(message, emailType);

    public Task SendAsync(Umbraco.Cms.Core.Models.Email.EmailMessage message, string emailType, bool enableNotification, DateTime? expires)
        => SendAsync(message, emailType);

    public bool CanSendRequiredEmail() => true;
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniSurfaceControllerSendMessageTests`
Expected: build FAILS — `ProcessSendMessage` doesn't exist yet.

- [ ] **Step 4: Add `SendMessage` to the controller**

```csharp
// Add these usings to Core/Controllers/AlumniSurfaceController.cs
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
```

```csharp
// Add this field, constructor parameter, action, and static method to
// the existing AlumniSurfaceController class from Task 7.

private readonly IEmailSender _emailSender;
private readonly ILogger<AlumniSurfaceController> _logger;

// Extend the constructor signature with two more parameters:
//   IEmailSender emailSender, ILogger<AlumniSurfaceController> logger
// and assign them: _emailSender = emailSender; _logger = logger;

[HttpPost]
[EnableRateLimiting("alumni-contact")]
public async Task<IActionResult> SendMessage(AlumniContactFormModel model)
{
    var (success, message) = await ProcessSendMessage(_store, _emailSender, model, _logger);
    return Json(new { success, message });
}

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
```

The `from` address is a placeholder (`no-reply@santiagohs.org`) - confirm the real no-reply address with the district alongside the SMTP credentials (spec's open question); it is a single string constant, trivial to change later.

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniSurfaceControllerSendMessageTests`
Expected: PASS, 8 tests.

- [ ] **Step 6: Build and run the full test suite**

Run: `dotnet build UmbracoBase.csproj && dotnet test tests/UmbracoBase.Tests`
Expected: 0 build errors, all tests pass.

- [ ] **Step 7: Commit**

```bash
git add Core/ViewModels/AlumniContactFormModel.cs Core/Controllers/AlumniSurfaceController.cs tests/UmbracoBase.Tests/Alumni/AlumniSurfaceControllerSendMessageTests.cs tests/UmbracoBase.Tests/Alumni/Fakes/FakeEmailSender.cs
git commit -m "Add AlumniSurfaceController.SendMessage contact relay"
```

---

## Task 9: Browse widget (`AlumniDirectory`)

**Files:**
- Create: `uSync/v17/ContentTypes/alumnidirectorywidget.config`
- Modify: `uSync/v17/DataTypes/BlockListWidgets.config`
- Create: `Views/Partials/Widgets/AlumniDirectory.cshtml`
- Create: `wwwroot/css/Widgets_CSS/Alumni.css`
- Modify: `Core/Bundling/StylesheetBundle.cs`

**Interfaces:**
- Consumes: `IAlumniMemberStore.Browse` (Task 4), `AlumniMemberSummary` (Task 2).
- Produces: a `alumniDirectoryWidget` block type, addable to any page's `Blocks` property; consumed by Task 15 (the Browse page).

- [ ] **Step 1: Add the widget content type**

Composes `baseContent` only (heading/preheading/text for an intro blurb above the grid), no own properties - same shape as the existing `sidebarText` type.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType Key="6978a64d-1fb1-465e-8f98-0f4ba0c8b911" Alias="alumniDirectoryWidget" Level="3">
  <Info>
    <Name>Alumni Directory</Name>
    <Icon>icon-users color-blue</Icon>
    <Thumbnail>folder.png</Thumbnail>
    <Description>Public, searchable grid of approved alumni. Data comes from Umbraco Members, not block content.</Description>
    <AllowAtRoot>False</AllowAtRoot>
    <ListView>00000000-0000-0000-0000-000000000000</ListView>
    <Variations>Nothing</Variations>
    <IsElement>true</IsElement>
    <HistoryCleanup>
      <PreventCleanup>False</PreventCleanup>
      <KeepAllVersionsNewerThanDays></KeepAllVersionsNewerThanDays>
      <KeepLatestVersionPerDayForDays></KeepLatestVersionPerDayForDays>
    </HistoryCleanup>
    <Folder>Layout+Blocks/Alumni</Folder>
    <Compositions>
      <Composition Key="2de152b5-b7d5-45aa-bb65-d8605ac0b6c7">baseContent</Composition>
    </Compositions>
    <DefaultTemplate></DefaultTemplate>
    <AllowedTemplates />
  </Info>
  <Structure />
  <GenericProperties />
  <Tabs />
</ContentType>
```

- [ ] **Step 2: Register it in the block picker**

Reuses `quickLinksBarSettings` (`b48fdab0-c6b0-47ab-99e4-48902872bfd5`) for anchor/background/spacing, same as `Achievements` and `Link Cards`.

Add to the `blocks` array in `uSync/v17/DataTypes/BlockListWidgets.config` (before the closing `]`):

```json
    {
      "contentTypeKey": "6978a64d-1fb1-465e-8f98-0f4ba0c8b911",
      "settingsElementTypeKey": "b48fdab0-c6b0-47ab-99e4-48902872bfd5",
      "editorSize": "full",
      "label": "Alumni Directory : ${ heading || $settings.alias }"
    }
```

- [ ] **Step 3: Write the widget partial**

```cshtml
@inherits UmbracoViewPage<Umbraco.Cms.Core.Models.Blocks.BlockListItem>
@using Umbraco.Cms.Core.Models.Blocks
@using UmbracoBase.Core.Alumni
@using UmbracoBase.Core.Models
@inject IAlumniMemberStore AlumniStore
@{
    // Model-agnostic access so the view compiles before ModelsBuilder regenerates.
    var content = Model.Content;
    var settings = Model.Settings;
    if (settings?.Value<bool?>("blockVisibility") == false) { return; }

    var heading = content.Value<string>("heading");
    var preheading = content.Value<string>("preheading");
    var text = content.Value<Umbraco.Cms.Core.Strings.IHtmlEncodedString>("text");

    var anchorId = settings?.Value<string>("anchorId");
    var backgroundColorClass = string.IsNullOrWhiteSpace(settings?.Value<string>("backgroundColor")) ? "bg-transparent" : "";
    var backgroundColorStyle = string.IsNullOrWhiteSpace(settings?.Value<string>("backgroundColor")) ? "" : $"background-color:{settings!.Value<string>("backgroundColor")};";
    var verticalSpacingClass = settings?.Value<string>("verticalSpacing");
    if (string.IsNullOrWhiteSpace(verticalSpacingClass)) { verticalSpacingClass = "py-3"; }
    var classNames = string.Join(" ",
        (settings?.Value<IEnumerable<string>>("classnames") ?? Enumerable.Empty<string>())
        .Where(s => !string.IsNullOrWhiteSpace(s)));

    const int pageSize = 24;
    var query = Context.Request.Query["q"].ToString();
    var yearRaw = Context.Request.Query["year"].ToString();
    int? gradYear = int.TryParse(yearRaw, out var parsedYear) ? parsedYear : null;
    var page = int.TryParse(Context.Request.Query["page"].ToString(), out var parsedPage) && parsedPage > 0 ? parsedPage : 1;

    var result = AlumniStore.Browse(string.IsNullOrWhiteSpace(query) ? null : query, gradYear, page, pageSize);
    var totalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize);
}

<section class="section block-alumni-directory @backgroundColorClass @verticalSpacingClass @classNames" style="@backgroundColorStyle"
@if (!string.IsNullOrEmpty(anchorId))
{
    <text>id="@anchorId"</text>
}>
    <div class="container">
        @if (!string.IsNullOrEmpty(heading) || !string.IsNullOrEmpty(preheading) || !string.IsNullOrEmpty(text?.ToString()))
        {
            <div class="mb-4">
                @if (!string.IsNullOrEmpty(preheading))
                {
                    <div class="pre-head">@preheading</div>
                }
                @if (!string.IsNullOrEmpty(heading))
                {
                    <h2 class="head display-6 bold">@heading</h2>
                }
                @if (!string.IsNullOrEmpty(text?.ToString()))
                {
                    <p>@text</p>
                }
            </div>
        }

        <form method="get" class="alumni-directory__search" role="search">
            <label for="alumni-search-q">Search by name</label>
            <input type="search" id="alumni-search-q" name="q" value="@query" placeholder="Search by name" />
            <label for="alumni-search-year">Grad year</label>
            <input type="number" id="alumni-search-year" name="year" value="@(gradYear?.ToString() ?? "")" placeholder="Grad year" />
            <button type="submit">Search</button>
        </form>

        @if (result.Items.Any())
        {
            <div class="alumni-directory__grid">
                @foreach (var alum in result.Items)
                {
                    <div class="alumni-directory__card">
                        <div class="alumni-directory__name">
                            @alum.FirstName @alum.LastName
                            @if (!string.IsNullOrEmpty(alum.FormerLastName))
                            {
                                <span class="alumni-directory__former">(@alum.FormerLastName)</span>
                            }
                        </div>
                        @if (alum.GradYear.HasValue)
                        {
                            <div class="alumni-directory__meta">Class of @alum.GradYear</div>
                        }
                        @if (!string.IsNullOrEmpty(alum.Profession) || !string.IsNullOrEmpty(alum.Industry))
                        {
                            <div class="alumni-directory__meta">@alum.Profession@(!string.IsNullOrEmpty(alum.Profession) && !string.IsNullOrEmpty(alum.Industry) ? " \u00b7 " : "")@alum.Industry</div>
                        }
                        @if (!string.IsNullOrEmpty(alum.Update))
                        {
                            <p class="alumni-directory__update">@alum.Update</p>
                        }
                        @if (!string.IsNullOrEmpty(alum.HomepageUrl))
                        {
                            <a href="@alum.HomepageUrl" target="_blank" rel="noopener" class="alumni-directory__homepage">Visit homepage &rarr;</a>
                        }
                        @if (alum.EmailingOk)
                        {
                            <button type="button" class="alumni-directory__contact" data-alumni-contact-trigger data-member-id="@alum.Id" data-member-name="@alum.FirstName @alum.LastName">Send a message</button>
                        }
                    </div>
                }
            </div>

            @if (totalPages > 1)
            {
                <nav class="alumni-directory__pagination" aria-label="Directory pages">
                    @for (var p = 1; p <= totalPages; p++)
                    {
                        <a href="?q=@query&amp;year=@gradYear&amp;page=@p" class="alumni-directory__page @(p == page ? "alumni-directory__page--active" : "")">@p</a>
                    }
                </nav>
            }
        }
        else
        {
            <p class="text-muted mb-0">No alumni match your search.</p>
        }
    </div>
</section>
```

- [ ] **Step 4: Add the widget CSS**

```css
/* wwwroot/css/Widgets_CSS/Alumni.css */
.alumni-directory__search {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    align-items: end;
    margin-bottom: 20px;
}

.alumni-directory__search label {
    display: block;
    font-size: 0.8rem;
    font-weight: 600;
    margin-bottom: 4px;
}

.alumni-directory__grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
    gap: 16px;
}

.alumni-directory__card {
    border: 1px solid #eee;
    border-radius: 8px;
    padding: 16px;
    background: #fff;
}

.alumni-directory__name {
    font-weight: 700;
    color: var(--shs-purple, #4a1f7a);
}

.alumni-directory__former {
    font-weight: 400;
    color: #666;
    font-size: 0.85rem;
}

.alumni-directory__meta {
    font-size: 0.85rem;
    color: #666;
    margin-top: 2px;
}

.alumni-directory__update {
    font-size: 0.85rem;
    margin-top: 8px;
}

.alumni-directory__homepage,
.alumni-directory__contact {
    display: inline-block;
    margin-top: 10px;
    font-size: 0.85rem;
}

.alumni-directory__contact {
    background: var(--shs-purple, #4a1f7a);
    color: #fff;
    border: 0;
    border-radius: 4px;
    padding: 6px 14px;
    cursor: pointer;
}

.alumni-directory__pagination {
    display: flex;
    gap: 6px;
    margin-top: 20px;
    flex-wrap: wrap;
}

.alumni-directory__page {
    padding: 4px 10px;
    border: 1px solid #ddd;
    border-radius: 4px;
    text-decoration: none;
    color: #333;
}

.alumni-directory__page--active {
    background: var(--shs-purple, #4a1f7a);
    color: #fff;
    border-color: var(--shs-purple, #4a1f7a);
}

/* Honeypot field on the alumni signup/contact forms (Tasks 7-8, wired up
   in Task 11): off-screen for sighted users, aria-hidden and unfocusable
   for assistive tech, but present in the DOM for bots that fill every
   field. Confirmed pattern with existing accessibility-audit conventions
   in this codebase. */
.alumni-form__hp {
    position: absolute;
    left: -9999px;
    width: 1px;
    height: 1px;
    overflow: hidden;
}
```

- [ ] **Step 5: Register the CSS in the bundle**

Add `"css/Widgets_CSS/Alumni.css",` to the `SourceFiles` array in `Core/Bundling/StylesheetBundle.cs`, after the last existing `Widgets_CSS` entry.

- [ ] **Step 6: Build the project**

Run: `dotnet build UmbracoBase.csproj`
Expected: 0 errors.

- [ ] **Step 7: Manual verification**

Import Task 1 and this task's uSync files in the backoffice, add an "Alumni Directory" block to any page's Blocks, publish, and load that page. Expected: an empty-state "No alumni match your search" message (no Members exist yet until Task 14's import runs).

- [ ] **Step 8: Commit**

```bash
git add uSync/v17/ContentTypes/alumnidirectorywidget.config uSync/v17/DataTypes/BlockListWidgets.config Views/Partials/Widgets/AlumniDirectory.cshtml wwwroot/css/Widgets_CSS/Alumni.css Core/Bundling/StylesheetBundle.cs
git commit -m "Add the Alumni Directory browse/search widget"
```

---

## Task 10: Sign Up widget (`AlumniSignup`)

**Files:**
- Create: `uSync/v17/ContentTypes/alumnisignupwidget.config`
- Modify: `uSync/v17/DataTypes/BlockListWidgets.config`
- Create: `Views/Partials/Widgets/AlumniSignup.cshtml`

**Interfaces:**
- Produces: an `alumniSignupWidget` block type, posting to `AlumniSurfaceController.SignUp` (Task 7). Consumed by Task 15 (the Sign Up page) and Task 11 (JS).

- [ ] **Step 1: Add the widget content type**

Same shape as Task 9's widget - composes `baseContent` only, form fields are fixed in the partial rather than backoffice-configurable.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType Key="04839df1-c104-4c65-903f-87e68bea9471" Alias="alumniSignupWidget" Level="3">
  <Info>
    <Name>Alumni Sign Up</Name>
    <Icon>icon-add color-blue</Icon>
    <Thumbnail>folder.png</Thumbnail>
    <Description>Public self-service form for alumni to join the directory. Creates an unapproved Member for staff review.</Description>
    <AllowAtRoot>False</AllowAtRoot>
    <ListView>00000000-0000-0000-0000-000000000000</ListView>
    <Variations>Nothing</Variations>
    <IsElement>true</IsElement>
    <HistoryCleanup>
      <PreventCleanup>False</PreventCleanup>
      <KeepAllVersionsNewerThanDays></KeepAllVersionsNewerThanDays>
      <KeepLatestVersionPerDayForDays></KeepLatestVersionPerDayForDays>
    </HistoryCleanup>
    <Folder>Layout+Blocks/Alumni</Folder>
    <Compositions>
      <Composition Key="2de152b5-b7d5-45aa-bb65-d8605ac0b6c7">baseContent</Composition>
    </Compositions>
    <DefaultTemplate></DefaultTemplate>
    <AllowedTemplates />
  </Info>
  <Structure />
  <GenericProperties />
  <Tabs />
</ContentType>
```

- [ ] **Step 2: Register it in the block picker**

Add to the `blocks` array in `uSync/v17/DataTypes/BlockListWidgets.config`:

```json
    {
      "contentTypeKey": "04839df1-c104-4c65-903f-87e68bea9471",
      "settingsElementTypeKey": "b48fdab0-c6b0-47ab-99e4-48902872bfd5",
      "editorSize": "full",
      "label": "Alumni Sign Up : ${ heading || $settings.alias }"
    }
```

- [ ] **Step 3: Write the widget partial**

```cshtml
@inherits UmbracoViewPage<Umbraco.Cms.Core.Models.Blocks.BlockListItem>
@using Umbraco.Cms.Core.Models.Blocks
@{
    var content = Model.Content;
    var settings = Model.Settings;
    if (settings?.Value<bool?>("blockVisibility") == false) { return; }

    var heading = content.Value<string>("heading");
    var preheading = content.Value<string>("preheading");
    var text = content.Value<Umbraco.Cms.Core.Strings.IHtmlEncodedString>("text");

    var anchorId = settings?.Value<string>("anchorId");
    var backgroundColorClass = string.IsNullOrWhiteSpace(settings?.Value<string>("backgroundColor")) ? "bg-transparent" : "";
    var backgroundColorStyle = string.IsNullOrWhiteSpace(settings?.Value<string>("backgroundColor")) ? "" : $"background-color:{settings!.Value<string>("backgroundColor")};";
    var verticalSpacingClass = settings?.Value<string>("verticalSpacing");
    if (string.IsNullOrWhiteSpace(verticalSpacingClass)) { verticalSpacingClass = "py-3"; }
    var classNames = string.Join(" ",
        (settings?.Value<IEnumerable<string>>("classnames") ?? Enumerable.Empty<string>())
        .Where(s => !string.IsNullOrWhiteSpace(s)));
}

<section class="section block-alumni-signup @backgroundColorClass @verticalSpacingClass @classNames" style="@backgroundColorStyle"
@if (!string.IsNullOrEmpty(anchorId))
{
    <text>id="@anchorId"</text>
}>
    <div class="container container-md">
        @if (!string.IsNullOrEmpty(heading) || !string.IsNullOrEmpty(preheading) || !string.IsNullOrEmpty(text?.ToString()))
        {
            <div class="mb-4">
                @if (!string.IsNullOrEmpty(preheading))
                {
                    <div class="pre-head">@preheading</div>
                }
                @if (!string.IsNullOrEmpty(heading))
                {
                    <h2 class="head display-6 bold">@heading</h2>
                }
                @if (!string.IsNullOrEmpty(text?.ToString()))
                {
                    <p>@text</p>
                }
            </div>
        }

        <form data-alumni-signup-form class="alumni-signup__form">
            <div class="alumni-signup__row">
                <label for="alumni-signup-firstName">First name</label>
                <input type="text" id="alumni-signup-firstName" name="firstName" required />
            </div>
            <div class="alumni-signup__row">
                <label for="alumni-signup-lastName">Last name</label>
                <input type="text" id="alumni-signup-lastName" name="lastName" required />
            </div>
            <div class="alumni-signup__row">
                <label for="alumni-signup-formerLastName">Former last name (optional)</label>
                <input type="text" id="alumni-signup-formerLastName" name="formerLastName" />
            </div>
            <div class="alumni-signup__row">
                <label for="alumni-signup-gradYear">Grad year</label>
                <input type="number" id="alumni-signup-gradYear" name="gradYear" min="1950" max="2100" />
            </div>
            <div class="alumni-signup__row">
                <label for="alumni-signup-industry">Industry</label>
                <input type="text" id="alumni-signup-industry" name="industry" />
            </div>
            <div class="alumni-signup__row">
                <label for="alumni-signup-profession">Profession</label>
                <input type="text" id="alumni-signup-profession" name="profession" />
            </div>
            <div class="alumni-signup__row">
                <label for="alumni-signup-update">Share a brief update (optional)</label>
                <textarea id="alumni-signup-update" name="update" rows="3"></textarea>
            </div>
            <div class="alumni-signup__row">
                <label for="alumni-signup-homepageUrl">Homepage URL (optional)</label>
                <input type="url" id="alumni-signup-homepageUrl" name="homepageUrl" />
            </div>
            <div class="alumni-signup__row">
                <label for="alumni-signup-email">Email (kept private - never shown publicly)</label>
                <input type="email" id="alumni-signup-email" name="email" required />
            </div>
            <div class="alumni-signup__row">
                <label for="alumni-signup-phone">Phone (optional, kept private)</label>
                <input type="tel" id="alumni-signup-phone" name="phone" />
            </div>
            <div class="alumni-signup__row">
                <label for="alumni-signup-address">Address (optional, kept private)</label>
                <textarea id="alumni-signup-address" name="address" rows="2"></textarea>
            </div>
            <div class="alumni-signup__row">
                <label for="alumni-signup-gender">Gender (optional, kept private)</label>
                <input type="text" id="alumni-signup-gender" name="gender" />
            </div>

            <div class="alumni-form__hp" aria-hidden="true">
                <label for="alumni-signup-website">Leave this field blank</label>
                <input type="text" id="alumni-signup-website" name="website" tabindex="-1" autocomplete="off" />
            </div>

            <button type="submit">Join the directory</button>
            <p class="alumni-signup__result" data-alumni-form-result role="status" aria-live="polite"></p>
        </form>
    </div>
</section>
```

- [ ] **Step 4: Build the project**

Run: `dotnet build UmbracoBase.csproj`
Expected: 0 errors.

- [ ] **Step 5: Manual verification**

Add an "Alumni Sign Up" block to a page, publish, load the page - confirm the form renders with all fields. Submission is wired up in Task 11.

- [ ] **Step 6: Commit**

```bash
git add uSync/v17/ContentTypes/alumnisignupwidget.config uSync/v17/DataTypes/BlockListWidgets.config Views/Partials/Widgets/AlumniSignup.cshtml
git commit -m "Add the Alumni Sign Up widget"
```

---

## Task 11: Client-side JS for the signup and contact forms

**Files:**
- Create: `wwwroot/js/alumniForms.js`
- Modify: `Views/Master.cshtml`
- Modify: `Views/Partials/Widgets/AlumniDirectory.cshtml` (add the contact modal markup)

**Interfaces:**
- Consumes: `POST /umbraco/surface/AlumniSurface/SignUp` and `.../SendMessage` (Tasks 7-8), the `data-alumni-signup-form` / `data-alumni-contact-trigger` hooks (Tasks 9-10).

- [ ] **Step 1: Add the contact modal markup to the Browse widget**

Append this just before the closing `</section>` in `Views/Partials/Widgets/AlumniDirectory.cshtml` (from Task 9):

```cshtml
    <div class="alumni-contact-modal" data-alumni-contact-modal hidden>
        <div class="alumni-contact-modal__panel" role="dialog" aria-modal="true" aria-labelledby="alumni-contact-modal-title">
            <button type="button" class="alumni-contact-modal__close" data-alumni-contact-close aria-label="Close">&times;</button>
            <h3 id="alumni-contact-modal-title" data-alumni-contact-title>Send a message</h3>
            <form data-alumni-contact-form>
                <input type="hidden" name="memberId" data-alumni-contact-member-id />
                <div class="alumni-signup__row">
                    <label for="alumni-contact-senderName">Your name</label>
                    <input type="text" id="alumni-contact-senderName" name="senderName" required />
                </div>
                <div class="alumni-signup__row">
                    <label for="alumni-contact-senderEmail">Your email</label>
                    <input type="email" id="alumni-contact-senderEmail" name="senderEmail" required />
                </div>
                <div class="alumni-signup__row">
                    <label for="alumni-contact-message">Message</label>
                    <textarea id="alumni-contact-message" name="message" rows="4" required></textarea>
                </div>
                <div class="alumni-form__hp" aria-hidden="true">
                    <label for="alumni-contact-website">Leave this field blank</label>
                    <input type="text" id="alumni-contact-website" name="website" tabindex="-1" autocomplete="off" />
                </div>
                <button type="submit">Send</button>
                <p class="alumni-signup__result" data-alumni-form-result role="status" aria-live="polite"></p>
            </form>
        </div>
    </div>
```

- [ ] **Step 2: Write the JS**

```javascript
// wwwroot/js/alumniForms.js
// Handles the Alumni Sign Up form and the Alumni Directory's per-card
// "Send a message" contact form, both posted via fetch to
// AlumniSurfaceController (see Core/Controllers/AlumniSurfaceController.cs).
(function () {
    function showResult(form, message, isError) {
        var result = form.querySelector('[data-alumni-form-result]');
        if (!result) { return; }
        result.textContent = message;
        result.classList.toggle('alumni-signup__result--error', !!isError);
    }

    function submitForm(form, url, extraFields) {
        var formData = new FormData(form);
        if (extraFields) {
            Object.keys(extraFields).forEach(function (key) {
                formData.set(key, extraFields[key]);
            });
        }

        var submitButton = form.querySelector('button[type="submit"]');
        if (submitButton) { submitButton.disabled = true; }

        fetch(url, { method: 'POST', body: formData })
            .then(function (response) { return response.json(); })
            .then(function (data) {
                showResult(form, data.message, !data.success);
                if (data.success) { form.reset(); }
            })
            .catch(function () {
                showResult(form, 'Something went wrong. Please try again later.', true);
            })
            .finally(function () {
                if (submitButton) { submitButton.disabled = false; }
            });
    }

    function initSignupForm() {
        var form = document.querySelector('[data-alumni-signup-form]');
        if (!form) { return; }

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            submitForm(form, '/umbraco/surface/AlumniSurface/SignUp');
        });
    }

    function initContactModal() {
        var modal = document.querySelector('[data-alumni-contact-modal]');
        if (!modal) { return; }

        var form = modal.querySelector('[data-alumni-contact-form]');
        var memberIdField = modal.querySelector('[data-alumni-contact-member-id]');
        var titleEl = modal.querySelector('[data-alumni-contact-title]');

        function open(memberId, memberName) {
            memberIdField.value = memberId;
            titleEl.textContent = 'Send a message to ' + memberName;
            modal.hidden = false;
        }

        function close() {
            modal.hidden = true;
            form.reset();
        }

        document.addEventListener('click', function (event) {
            var trigger = event.target.closest('[data-alumni-contact-trigger]');
            if (trigger) {
                open(trigger.getAttribute('data-member-id'), trigger.getAttribute('data-member-name') || 'this alumnus');
                return;
            }

            if (event.target.closest('[data-alumni-contact-close]') || event.target === modal) {
                close();
            }
        });

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            submitForm(form, '/umbraco/surface/AlumniSurface/SendMessage');
        });
    }

    initSignupForm();
    initContactModal();
})();
```

- [ ] **Step 3: Add a modal CSS block to `wwwroot/css/Widgets_CSS/Alumni.css`**

```css
.alumni-signup__row {
    margin-bottom: 12px;
}

.alumni-signup__row label {
    display: block;
    font-size: 0.85rem;
    font-weight: 600;
    margin-bottom: 4px;
}

.alumni-signup__row input,
.alumni-signup__row textarea {
    width: 100%;
    padding: 8px 10px;
    border: 1px solid #ccc;
    border-radius: 4px;
}

.alumni-signup__result {
    margin-top: 10px;
    font-size: 0.85rem;
}

.alumni-signup__result--error {
    color: #b3261e;
}

.alumni-contact-modal {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
}

.alumni-contact-modal__panel {
    background: #fff;
    border-radius: 8px;
    padding: 24px;
    max-width: 480px;
    width: calc(100% - 32px);
    position: relative;
}

.alumni-contact-modal__close {
    position: absolute;
    top: 10px;
    right: 14px;
    background: none;
    border: 0;
    font-size: 1.5rem;
    cursor: pointer;
    line-height: 1;
}
```

- [ ] **Step 4: Load the script from `Views/Master.cshtml`**

Add after the existing `<script src="/js/slider.js" defer></script>` line:

```html
<script src="/js/alumniForms.js" defer></script>
```

- [ ] **Step 5: Manual verification**

With both widgets on published pages: submit the Sign Up form with valid data, confirm the success message appears and a new unapproved Member shows in the backoffice. Click "Send a message" on a directory card, confirm the modal opens, submit it, confirm a success/failure message appears without a page reload.

- [ ] **Step 6: Commit**

```bash
git add wwwroot/js/alumniForms.js Views/Master.cshtml Views/Partials/Widgets/AlumniDirectory.cshtml wwwroot/css/Widgets_CSS/Alumni.css
git commit -m "Wire up client-side JS for the alumni signup and contact forms"
```

---

## Task 12: Spreadsheet row mapping (`AlumniRowMapper`)

**Files:**
- Create: `Core/Models/AlumniImportRow.cs`
- Create: `Core/Alumni/AlumniRowMapper.cs`
- Test: `tests/UmbracoBase.Tests/Alumni/AlumniRowMapperTests.cs`

**Interfaces:**
- Produces: `AlumniImportRow`, `AlumniRowMapper.Map(IReadOnlyDictionary<string, string> rawRow) -> AlumniImportRow?` (returns `null` for a row with no usable email). Consumed by Task 14.

Operates on plain `IReadOnlyDictionary<string,string>` rows (header name → cell text), not directly on `ExcelDataReader`, so this is testable with in-memory dictionaries - no binary spreadsheet fixture needed. Task 13 is the thin, untested glue that turns an actual `.xls` file into these dictionaries.

- [ ] **Step 1: Write the failing test**

```csharp
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
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniRowMapperTests`
Expected: build FAILS — `AlumniImportRow`, `AlumniRowMapper` don't exist yet.

- [ ] **Step 3: Write the model and mapper**

```csharp
// Core/Models/AlumniImportRow.cs
namespace UmbracoBase.Core.Models;

/// <summary>
/// One row of the legacy AlumniDirectory.xls, after column mapping.
/// Title, Middle Name and Record Date are intentionally not carried
/// over - out of scope per the design spec.
/// </summary>
public sealed record AlumniImportRow(
    string LegacyRecId,
    string FirstName,
    string LastName,
    string? FormerLastName,
    int? GradYear,
    string? Gender,
    string Email,
    string? HomepageUrl,
    string? Phone,
    string? Address,
    string? Industry,
    string? Profession,
    string? Update,
    bool EmailingOk);
```

```csharp
// Core/Alumni/AlumniRowMapper.cs
namespace UmbracoBase.Core.Alumni;

using UmbracoBase.Core.Models;

/// <summary>
/// Pure mapping from one AlumniDirectory.xls row (as a header-name-keyed
/// dictionary) to an AlumniImportRow. Kept independent of the Excel
/// library so it's testable with plain in-memory dictionaries - see
/// AlumniSpreadsheetReader for the part that actually reads the file.
/// </summary>
public static class AlumniRowMapper
{
    public static AlumniImportRow? Map(IReadOnlyDictionary<string, string> row)
    {
        var email = Get(row, "Email 1");
        if (string.IsNullOrWhiteSpace(email)) { return null; }

        var firstName = Get(row, "First Name");
        var lastName = Get(row, "Last Name");

        return new AlumniImportRow(
            LegacyRecId: Get(row, "REC_ID"),
            FirstName: firstName,
            LastName: lastName,
            FormerLastName: NullIfEmpty(Get(row, "Former Last Name")),
            GradYear: int.TryParse(Get(row, "Graduation Year"), out var year) ? year : null,
            Gender: NullIfEmpty(Get(row, "Gender")),
            Email: email,
            HomepageUrl: NullIfEmpty(Get(row, "Homepage")),
            Phone: NullIfEmpty(Get(row, "Phone 1")),
            Address: CombineAddress(row),
            Industry: NullIfEmpty(Get(row, "Industry")),
            Profession: NullIfEmpty(Get(row, "Profession")),
            Update: NullIfEmpty(Get(row, "Other information")),
            EmailingOk: Get(row, "Emailing is OK") == "1");
    }

    private static string CombineAddress(IReadOnlyDictionary<string, string> row)
    {
        var street = Get(row, "Street");
        var city = Get(row, "City");
        var state = Get(row, "State");
        var zip = Get(row, "Zip");
        var country = Get(row, "Country");

        var cityStateZip = string.Join(" ", new[] { $"{city},", state, zip }
            .Where(s => !string.IsNullOrWhiteSpace(s.TrimEnd(','))))
            .Trim();

        var parts = new[] { street, cityStateZip, country }.Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(", ", parts);
    }

    private static string Get(IReadOnlyDictionary<string, string> row, string key)
        => row.TryGetValue(key, out var value) ? value.Trim() : string.Empty;

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniRowMapperTests`
Expected: PASS, 6 tests. (If the address-combining assertion doesn't match exactly, adjust `CombineAddress`'s joining until `"P.O. Box 1546, North Beach, Maryland 20714, USA"` is produced - the exact punctuation is a judgment call, not a hard requirement from the spec.)

- [ ] **Step 5: Commit**

```bash
git add Core/Models/AlumniImportRow.cs Core/Alumni/AlumniRowMapper.cs tests/UmbracoBase.Tests/Alumni/AlumniRowMapperTests.cs
git commit -m "Add AlumniImportRow and the spreadsheet row mapper"
```

---

## Task 13: Spreadsheet reading (`AlumniSpreadsheetReader`)

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `UmbracoBase.csproj`
- Create: `Core/Alumni/AlumniSpreadsheetReader.cs`
- Modify: `Core/Composers/AlumniComposer.cs`

**Interfaces:**
- Consumes: `ExcelDataReader` (new dependency).
- Produces: `AlumniSpreadsheetReader.ReadRows(Stream xlsStream) -> IReadOnlyList<IReadOnlyDictionary<string, string>>`, one dictionary per data row keyed by header text from row 1. Consumed by Task 14.

Not unit tested directly - it is a thin wrapper around `ExcelDataReader`'s own well-tested parsing, verified manually against the real file in Task 14.

- [ ] **Step 1: Add the package**

```xml
<!-- Add to Directory.Packages.props, in the main ItemGroup -->
<PackageVersion Include="ExcelDataReader" Version="3.7.0" />
```

If `3.7.0` fails to restore, check the latest stable 3.x release on nuget.org and use that version instead - the API used here (`ExcelReaderFactory.CreateReader`) has been stable across the 3.x line.

```xml
<!-- Add to UmbracoBase.csproj, in the main PackageReference ItemGroup -->
<PackageReference Include="ExcelDataReader" />
```

- [ ] **Step 2: Register the legacy encoding provider**

`.xls` (pre-2007 binary format) files can use code-page text encodings `System.Text.Encoding` doesn't know about by default - `ExcelDataReader`'s README calls out registering `CodePagesEncodingProvider` before first use. Add this to `AlumniComposer` from Task 5:

```csharp
// Core/Composers/AlumniComposer.cs
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using UmbracoBase.Core.Alumni;

namespace UmbracoBase.Core.Composers
{
    public class AlumniComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddScoped<IAlumniMemberStore, UmbracoAlumniMemberStore>();

            // ExcelDataReader needs this for legacy .xls code-page text encodings.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
```

- [ ] **Step 3: Write the reader**

```csharp
// Core/Alumni/AlumniSpreadsheetReader.cs
using ExcelDataReader;

namespace UmbracoBase.Core.Alumni;

/// <summary>
/// Reads an AlumniDirectory.xls/.xlsx export into one header-keyed
/// dictionary per data row. Thin wrapper around ExcelDataReader - the
/// actual column mapping logic lives in AlumniRowMapper, which is unit
/// tested independently of this class.
/// </summary>
public static class AlumniSpreadsheetReader
{
    public static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadRows(Stream xlsStream)
    {
        using var reader = ExcelReaderFactory.CreateReader(xlsStream);

        if (!reader.Read()) { return Array.Empty<IReadOnlyDictionary<string, string>>(); } // no header row

        var headers = new string[reader.FieldCount];
        for (var i = 0; i < reader.FieldCount; i++)
        {
            headers[i] = reader.GetValue(i)?.ToString()?.Trim() ?? string.Empty;
        }

        var rows = new List<IReadOnlyDictionary<string, string>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, string>();
            for (var i = 0; i < reader.FieldCount && i < headers.Length; i++)
            {
                if (string.IsNullOrEmpty(headers[i])) { continue; }
                row[headers[i]] = reader.GetValue(i)?.ToString()?.Trim() ?? string.Empty;
            }
            rows.Add(row);
        }

        return rows;
    }
}
```

- [ ] **Step 4: Build the project**

Run: `dotnet build UmbracoBase.csproj`
Expected: 0 errors. If `ExcelDataReader` fails to restore at the pinned version, adjust per Step 1's fallback note and re-run.

- [ ] **Step 5: Commit**

```bash
git add Directory.Packages.props UmbracoBase.csproj Core/Alumni/AlumniSpreadsheetReader.cs Core/Composers/AlumniComposer.cs
git commit -m "Add ExcelDataReader-backed alumni spreadsheet reader"
```

---

## Task 14: One-time bulk import (`AlumniImportController`)

**Files:**
- Modify: `Core/Alumni/IAlumniMemberStore.cs`
- Modify: `Core/Alumni/UmbracoAlumniMemberStore.cs`
- Create: `Core/Controllers/AlumniImportController.cs`
- Create: `Views/Alumni/Import.cshtml`
- Test: `tests/UmbracoBase.Tests/Alumni/AlumniImportControllerTests.cs`

**Interfaces:**
- Consumes: `AlumniSpreadsheetReader.ReadRows` (Task 13), `AlumniRowMapper.Map` (Task 12), `IAlumniMemberStore` (Task 4, extended here).
- Produces: `GET /admin/alumni-import` (upload form), `POST /admin/alumni-import` (runs the import), gated to `Development` environment and a logged-in, approved backoffice user.

- [ ] **Step 1: Extend the store interface with an idempotent import method**

```csharp
// Add to Core/Alumni/IAlumniMemberStore.cs, inside the interface:
    /// <summary>
    /// Creates an approved Member for one legacy row, unless a Member with
    /// the same LegacyRecId already exists (re-running the import is then
    /// a no-op for that row). Returns true if a Member was created.
    /// </summary>
    bool ImportLegacyRow(AlumniImportRow row);
```

```csharp
// Add "using UmbracoBase.Core.Models;" is already present in
// UmbracoAlumniMemberStore.cs. Add this method to the class:

    public bool ImportLegacyRow(AlumniImportRow row)
    {
        var alreadyImported = _memberService
            .GetMembersByMemberType(MemberTypeAlias)
            .Any(m => GetString(m, "legacyRecId") == row.LegacyRecId);
        if (alreadyImported) { return false; }

        var displayName = $"{row.FirstName} {row.LastName}".Trim();
        var member = _memberService.CreateMember(row.Email, row.Email, displayName, MemberTypeAlias);

        SetString(member, "firstName", row.FirstName);
        SetString(member, "lastName", row.LastName);
        SetString(member, "formerLastName", row.FormerLastName);
        SetInt(member, "gradYear", row.GradYear);
        SetString(member, "industry", row.Industry);
        SetString(member, "profession", row.Profession);
        SetString(member, "update", row.Update);
        SetString(member, "homepageUrl", row.HomepageUrl);
        SetString(member, "phone", row.Phone);
        SetString(member, "address", row.Address);
        SetString(member, "gender", row.Gender);
        SetBool(member, "emailingOk", row.EmailingOk);
        SetString(member, "legacyRecId", row.LegacyRecId);
        member.IsApproved = true; // already-known real members, not a public submission

        _memberService.Save(member, Constants.Security.SuperUserId);
        return true;
    }
```

- [ ] **Step 2: Write the failing test for the import summary logic**

The controller action itself (file upload, environment/auth gating) isn't practical to unit test without a live host; the row-by-row counting logic is, so it's split out the same way `ProcessSignUp`/`ProcessSendMessage` were.

```csharp
using UmbracoBase.Core.Alumni;
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
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniImportControllerTests`
Expected: build FAILS — `AlumniImportController` doesn't exist yet.

- [ ] **Step 4: Write the controller and view**

```csharp
// Core/Controllers/AlumniImportController.cs
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

            return View();
        }

        [HttpPost]
        public IActionResult Import(IFormFile file)
        {
            var denied = DenyUnlessDevAdmin();
            if (denied != null) { return denied; }

            if (file is null || file.Length == 0)
            {
                ViewBag.Error = "Choose the AlumniDirectory.xls file first.";
                return View("Index");
            }

            using var stream = file.OpenReadStream();
            var rawRows = AlumniSpreadsheetReader.ReadRows(stream);
            var mappedRows = rawRows.Select(AlumniRowMapper.Map).Where(r => r is not null).Select(r => r!);

            ViewBag.Summary = RunImport(_store, mappedRows);
            ViewBag.SkippedNoEmailFromReading = rawRows.Count - mappedRows.Count();
            return View("Index");
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
```

```cshtml
@* Views/Alumni/Import.cshtml *@
@using UmbracoBase.Core.Controllers
@{
    Layout = null;
    var summary = ViewBag.Summary as ImportSummary;
}
<!DOCTYPE html>
<html>
<head><title>Alumni Import (dev only)</title></head>
<body style="font-family: sans-serif; max-width: 600px; margin: 40px auto;">
    <h1>Alumni Directory Import</h1>
    <p>Development-only, one-time import of AlumniDirectory.xls into the alumniMember Member Type. Safe to re-run - rows already imported (by REC_ID) are skipped.</p>

    @if (ViewBag.Error != null)
    {
        <p style="color:#b3261e;">@ViewBag.Error</p>
    }

    @if (summary != null)
    {
        <p>
            Imported: @summary.Imported<br />
            Skipped (already imported): @summary.SkippedExisting<br />
            Skipped (no email in the spreadsheet): @ViewBag.SkippedNoEmailFromReading
        </p>
    }

    <form method="post" enctype="multipart/form-data">
        <input type="file" name="file" accept=".xls,.xlsx" required />
        <button type="submit">Run import</button>
    </form>
</body>
</html>
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests/UmbracoBase.Tests --filter AlumniImportControllerTests`
Expected: PASS, 3 tests.

- [ ] **Step 6: Build the whole solution and run the full test suite**

Run: `dotnet build UmbracoBase.csproj && dotnet test tests/UmbracoBase.Tests`
Expected: 0 build errors, all tests pass.

- [ ] **Step 7: Run the real import (manual, once)**

With the site running in `Development`, logged into the backoffice: visit `/admin/alumni-import`, upload `C:\Users\ckobayashi2\OneDrive - Garden Grove Unified School District\Desktop\AlumniDirectory.xls`, confirm the summary shows roughly 800 imported and 0 skipped. Spot-check a handful of the new Members in the backoffice for correct data. Re-run once more to confirm the second run reports 0 imported / ~800 skipped (idempotency).

- [ ] **Step 8: Commit**

```bash
git add Core/Alumni/IAlumniMemberStore.cs Core/Alumni/UmbracoAlumniMemberStore.cs Core/Controllers/AlumniImportController.cs Views/Alumni/Import.cshtml tests/UmbracoBase.Tests/Alumni/AlumniImportControllerTests.cs
git commit -m "Add the one-time dev-gated alumni spreadsheet import"
```

---

## Task 15: Wire up the Browse and Sign Up pages

**Files:**
- Create: `uSync/v17/Content/alumni-directory-browse.config`
- Create: `uSync/v17/Content/alumni-directory-signup.config`
- Modify: `uSync/v17/Content/alumni-directory.config`

**Interfaces:**
- Consumes: `alumniDirectoryWidget` (Task 9), `alumniSignupWidget` (Task 10), the existing `page` doctype and `studentLinks`/`quickLinkItem` Link Cards already on the Alumni Directory page.

This task is XML/uSync content only - no C# code, so it's verified manually (build a page, load it, click through) rather than with an automated test, matching Task 1's approach.

- [ ] **Step 1: Read the current Alumni Directory page and Link Cards block**

Read `uSync/v17/Content/alumni-directory.config` in full before editing - confirm the current structure of the `contentTypeKey: "5100d1a5-1c1a-4d5e-9a11-11c1a5d10001"` (Link Cards) block, and note its two placeholder `quickLinkItem` entries labelled "Alumni Directory" and "Register Yourself" (both currently `url: "#"`).

- [ ] **Step 2: Create the Browse child page**

Use `page.config`'s doctype (`page`, Template Key `177e59b6-ba34-4c6c-94a6-4c216db3ed77`, matching how Guidance was pointed at the standard Page template) as the shape. Parent Key is the Alumni Directory page's own Key (`8022c1fa-bc27-4a51-8b2a-fd2c437d68ac`). Content Key for this new page: `fc22cb5d-5719-43f8-9365-94ecf5fd88fb`.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Content Key="fc22cb5d-5719-43f8-9365-94ecf5fd88fb" Alias="Alumni Directory Browse" Level="4">
  <Info>
    <Parent Key="8022c1fa-bc27-4a51-8b2a-fd2c437d68ac">Alumni Directory</Parent>
    <Path>/Home/Contact/AlumniDirectory/AlumniDirectoryBrowse</Path>
    <Trashed>false</Trashed>
    <ContentType>page</ContentType>
    <CreateDate>2026-09-15T00:00:00</CreateDate>
    <NodeName Default="Browse Alumni" />
    <SortOrder>0</SortOrder>
    <Published Default="true" />
    <Schedule />
    <Template Key="177e59b6-ba34-4c6c-94a6-4c216db3ed77">Page</Template>
  </Info>
  <Properties>
    <blocks>
      <Value><![CDATA[{
  "contentData": [
    {
      "contentTypeKey": "6978a64d-1fb1-465e-8f98-0f4ba0c8b911",
      "key": "a1b2c3d4-0001-4001-8001-000000000001",
      "values": [
        {
          "alias": "heading",
          "culture": null,
          "editorAlias": null,
          "segment": null,
          "value": "Browse the Alumni Directory"
        },
        {
          "alias": "preheading",
          "culture": null,
          "editorAlias": null,
          "segment": null,
          "value": ""
        },
        {
          "alias": "text",
          "culture": null,
          "editorAlias": null,
          "segment": null,
          "value": ""
        }
      ]
    }
  ],
  "settingsData": [
    {
      "contentTypeKey": "b48fdab0-c6b0-47ab-99e4-48902872bfd5",
      "key": "a1b2c3d4-0002-4002-8002-000000000002",
      "values": [
        {
          "alias": "blockVisibility",
          "culture": null,
          "editorAlias": null,
          "segment": null,
          "value": "1"
        }
      ]
    }
  ],
  "expose": [
    {
      "contentKey": "a1b2c3d4-0001-4001-8001-000000000001",
      "culture": null,
      "segment": null
    }
  ],
  "Layout": {
    "Umbraco.BlockList": [
      {
        "contentKey": "a1b2c3d4-0001-4001-8001-000000000001",
        "contentUdi": null,
        "settingsKey": "a1b2c3d4-0002-4002-8002-000000000002",
        "settingsUdi": null
      }
    ]
  }
}]]></Value>
    </blocks>
    <headerBlocks>
      <Value><![CDATA[]]></Value>
    </headerBlocks>
    <metaDescription>
      <Value><![CDATA[]]></Value>
    </metaDescription>
    <pageTitle>
      <Value><![CDATA[Browse Alumni]]></Value>
    </pageTitle>
    <showParent>
      <Value><![CDATA[0]]></Value>
    </showParent>
  </Properties>
</Content>
```

- [ ] **Step 3: Create the Sign Up child page**

Content Key for this new page: `63ff8f0a-c65d-45f0-87f1-696dcff0e335`.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Content Key="63ff8f0a-c65d-45f0-87f1-696dcff0e335" Alias="Alumni Directory Sign Up" Level="4">
  <Info>
    <Parent Key="8022c1fa-bc27-4a51-8b2a-fd2c437d68ac">Alumni Directory</Parent>
    <Path>/Home/Contact/AlumniDirectory/AlumniDirectorySignUp</Path>
    <Trashed>false</Trashed>
    <ContentType>page</ContentType>
    <CreateDate>2026-09-15T00:00:00</CreateDate>
    <NodeName Default="Join the Alumni Directory" />
    <SortOrder>1</SortOrder>
    <Published Default="true" />
    <Schedule />
    <Template Key="177e59b6-ba34-4c6c-94a6-4c216db3ed77">Page</Template>
  </Info>
  <Properties>
    <blocks>
      <Value><![CDATA[{
  "contentData": [
    {
      "contentTypeKey": "04839df1-c104-4c65-903f-87e68bea9471",
      "key": "a1b2c3d4-0003-4003-8003-000000000003",
      "values": [
        {
          "alias": "heading",
          "culture": null,
          "editorAlias": null,
          "segment": null,
          "value": "Join the Alumni Directory"
        },
        {
          "alias": "preheading",
          "culture": null,
          "editorAlias": null,
          "segment": null,
          "value": ""
        },
        {
          "alias": "text",
          "culture": null,
          "editorAlias": null,
          "segment": null,
          "value": "Your email, phone, address and gender stay private - only your name, grad year and profile info shown below are ever public."
        }
      ]
    }
  ],
  "settingsData": [
    {
      "contentTypeKey": "b48fdab0-c6b0-47ab-99e4-48902872bfd5",
      "key": "a1b2c3d4-0004-4004-8004-000000000004",
      "values": [
        {
          "alias": "blockVisibility",
          "culture": null,
          "editorAlias": null,
          "segment": null,
          "value": "1"
        }
      ]
    }
  ],
  "expose": [
    {
      "contentKey": "a1b2c3d4-0003-4003-8003-000000000003",
      "culture": null,
      "segment": null
    }
  ],
  "Layout": {
    "Umbraco.BlockList": [
      {
        "contentKey": "a1b2c3d4-0003-4003-8003-000000000003",
        "contentUdi": null,
        "settingsKey": "a1b2c3d4-0004-4004-8004-000000000004",
        "settingsUdi": null
      }
    ]
  }
}]]></Value>
    </blocks>
    <headerBlocks>
      <Value><![CDATA[]]></Value>
    </headerBlocks>
    <metaDescription>
      <Value><![CDATA[]]></Value>
    </metaDescription>
    <pageTitle>
      <Value><![CDATA[Join the Alumni Directory]]></Value>
    </pageTitle>
    <showParent>
      <Value><![CDATA[0]]></Value>
    </showParent>
  </Properties>
</Content>
```

- [ ] **Step 4: Validate both new files are well-formed**

Run: `python3 -c "import xml.dom.minidom as m; m.parse('uSync/v17/Content/alumni-directory-browse.config'); m.parse('uSync/v17/Content/alumni-directory-signup.config')"`
Expected: no output, exit code 0.

Run the same nested-JSON validation used earlier in this project on both files' `blocks` CDATA (extract with a small Python script, `json.loads` it) - confirm each parses and each block's `Layout.Umbraco.BlockList` entry count matches its `contentData`/`expose` entry counts (this project has already hit the "missing nested Layout" bug once - see `[[usync-and-nav-gotchas]]`).

- [ ] **Step 5: Point the existing placeholder Link Cards at the real pages**

In `uSync/v17/Content/alumni-directory.config`, find the two `quickLinkItem` entries under the Link Cards block (`contentTypeKey: "5100d1a5-1c1a-4d5e-9a11-11c1a5d10001"`) labelled "Alumni Directory" and "Register Yourself". Change each one's `link` value from the `url: "#"` placeholder to an internal page link, matching the `udi: "umb://document/<key-without-dashes>"` shape used elsewhere in this file:

- "Alumni Directory" → `udi: "umb://document/fc22cb5d571943f8936594ecf5fd88fb"` (the Browse page)
- "Register Yourself" → `udi: "umb://document/63ff8f0ac65d45f087f1696dcff0e335"` (the Sign Up page)

- [ ] **Step 6: Manually verify the whole flow end-to-end**

Import all of this task's uSync content files (and every prior task's uSync/C# changes) in the backoffice, or restart the site if it auto-imports. Load `/contact/alumni-directory/`, click "Alumni Directory" and "Register Yourself" - confirm they land on the new Browse and Sign Up pages instead of `#`. On Browse, confirm the ~800 imported alumni appear and search/grad-year filtering works. On Sign Up, submit a test entry and confirm it shows up unapproved in the backoffice. From Browse, click "Send a message" on an imported alumnus with `emailingOk` true, confirm the modal works; confirm the button is absent for one imported with `emailingOk` false.

- [ ] **Step 7: Commit**

```bash
git add uSync/v17/Content/alumni-directory-browse.config uSync/v17/Content/alumni-directory-signup.config uSync/v17/Content/alumni-directory.config
git commit -m "Add Alumni Directory Browse and Sign Up pages, link them from the landing page"
```

---

## Self-Review Notes

- **Spec coverage:** Member Type/data model → Task 1. Signup flow → Tasks 7, 9-11. Bulk import → Tasks 12-14. Browse/search widget → Task 9. Contact-relay flow → Task 8. Spam guards → Tasks 3, 6. Page placement → Task 15. The one deliberate deviation from the spec (a custom `IEmailSender` abstraction) is called out in the plan header and Task 8 - Umbraco's own `IEmailSender` already satisfies the same requirement.
- **Type consistency:** `AlumniMemberSummary`/`AlumniBrowseResult` (Task 2) are used unchanged by `UmbracoAlumniMemberStore` (Task 4), the browse widget (Task 9), and referenced in tests throughout. `IAlumniMemberStore`'s three original methods (Task 4) plus `ImportLegacyRow` (Task 14) match every call site. `AlumniImportRow` (Task 12) is used identically by `AlumniSpreadsheetReader`'s caller (Task 14) and `UmbracoAlumniMemberStore.ImportLegacyRow` (Task 14).
- **District SMTP credentials** remain an open item per the spec - nothing in this plan blocks on them; `EmailMessage`'s `from` address is a single named constant in Task 8, trivial to update once the district confirms the real no-reply address alongside the SMTP settings.
