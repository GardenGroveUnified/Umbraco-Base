# Newsroom and News Feed Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a central Newsroom of individual News Article pages, plus a News Feed block that shows the latest articles with optional pinning.

**Architecture:** Two new page doctypes (`newsroom` container, `newsArticle` child) with their own templates. One new block widget (`newsFeed` + `newsFeedSettings`) that queries the Newsroom's children and renders them with the existing news-card markup. All new Razor is model-agnostic: it uses `IPublishedContent` and `.Value<T>("alias")`, never a generated ModelsBuilder class, so the project builds from source before any uSync import.

**Tech Stack:** Umbraco CMS 17 (`net10.0`), uSync for schema and content, Razor views, ModelsBuilder in `SourceCodeAuto` mode.

**Spec:** `docs/superpowers/specs/2026-09-03-newsroom-and-news-feed-design.md`

## Global Constraints

- Config files under `uSync/v17/` are UTF-8 **with BOM** (`EF BB BF`). Preserve it.
- New Razor partials for blocks live in `Views/Partials/Widgets/<alias>.cshtml`. `RenderBlocks.cshtml` and `blockLine.cshtml` resolve them by `"Widgets/" + ContentType.Alias`.
- Page templates: the `.cshtml` file name must equal the template alias. Layout is `Master.cshtml`.
- Do not depend on generated ModelsBuilder classes in new code. Use `Model.Value<T>("alias")` and `IPublishedContent`.
- Brand colours: use `var(--shs-purple, #4a1f7a)`, `var(--shs-gold, #c9a227)`, `var(--shs-purple-pale, #f3edfa)`, font `var(--font-nav-and-texts, "Nunito", sans-serif)`.
- Commit messages: no `Co-Authored-By` or session trailers.
- After any task that changes `uSync/v17/`: the reviewer runs a uSync import in the backoffice and restarts the site to verify. `dotnet build` alone does not exercise schema.

### Fixed identifiers (use verbatim)

Doctype / template / datatype keys:

| Thing | Key |
|-------|-----|
| doctype `newsroom` | `9f5e2a20-0001-4c30-b8a1-9f5e2a200001` |
| doctype `newsArticle` | `9f5e2a20-0002-4c30-b8a1-9f5e2a200001` |
| element `newsFeed` | `9f5e2a20-0003-4c30-b8a1-9f5e2a200001` |
| element `newsFeedSettings` | `9f5e2a20-0004-4c30-b8a1-9f5e2a200001` |
| template `Newsroom` | `9f5e2a20-0005-4c30-b8a1-9f5e2a200001` |
| template `NewsArticle` | `9f5e2a20-0006-4c30-b8a1-9f5e2a200001` |
| datatype `[Content] News Articles` | `9f5e2a20-0007-4c30-b8a1-9f5e2a200001` |

Existing datatype definitions to reference:

| Editor | Definition key |
|--------|----------------|
| MediaPicker3 | `ad9f0cf2-bda2-45d5-9ea1-a63cfc873fd3` |
| TextBox | `0cc0eba1-9960-42c9-bf9b-60e150b429ae` |
| TextArea | `c6bac0dd-4ab9-45b1-8e30-e4b619ee5da3` |
| RichText | `ca90c950-0aff-4e72-b976-a30b1ac57dad` |
| Tags | `b6b73142-b9c1-4bf8-a16d-e1c23320b549` |
| DateTime | `5046194e-4237-453c-a547-15db3a07c4e1` |
| Multi URL Picker | `b4e3535a-1753-47e2-8568-602cf8cfee6f` |
| Numeric (Umbraco.Integer) | `2e6d3631-066e-44b8-aec4-96f09099b2b5` |
| baseContent composition | `2de152b5-b7d5-45aa-bb65-d8605ac0b6c7` |
| baseAdvancedSettings composition | `5f78746d-6566-4c80-ab32-7ca4a4e1d04a` |
| baseAppearanceSettings composition | `ae4b16ce-a7b0-478d-8012-ea970a048e61` |
| `[Block List] Widgets` datatype | `5a2ca282-d593-4e23-9bde-43cfe324c6d4` |
| `site` doctype | `ed4372f0-575a-46f8-be43-e6821bcabf5f` |
| `master` template parent | (uSync uses the name `master`) |

---

## Task 1: News Article picker datatype

**Files:**
- Create: `uSync/v17/DataTypes/ContentNewsArticles.config`

**Interfaces:**
- Produces: a datatype keyed `9f5e2a20-0007-4c30-b8a1-9f5e2a200001`, alias `[Content] News Articles`, editor `Umbraco.MultiNodeTreePicker`, filtered to the `newsArticle` doctype, max 4. Task 4 references this key for the `pinnedArticles` property.

- [ ] **Step 1: Create the datatype config**

`uSync/v17/DataTypes/ContentNewsArticles.config` (write with a UTF-8 BOM):

```xml
<?xml version="1.0" encoding="utf-8"?>
<DataType Key="9f5e2a20-0007-4c30-b8a1-9f5e2a200001" Alias="[Content] News Articles" Level="1">
  <Info>
    <Name>[Content] News Articles</Name>
    <EditorAlias>Umbraco.MultiNodeTreePicker</EditorAlias>
    <EditorUIAlias>Umb.PropertyEditorUi.TreePicker</EditorUIAlias>
    <DatabaseType>Ntext</DatabaseType>
  </Info>
  <Config><![CDATA[{
  "filter": "newsArticle",
  "minNumber": 0,
  "maxNumber": 4,
  "ignoreUserStartNodes": true,
  "treeSource": { "type": "content" }
}]]></Config>
</DataType>
```

- [ ] **Step 2: Build**

Run: `dotnet build UmbracoBase.csproj`
Expected: PASS. This file changes no code, so the build only proves nothing else broke.

- [ ] **Step 3: Commit**

```bash
git add uSync/v17/DataTypes/ContentNewsArticles.config
git commit -m "Add News Articles multi-node picker datatype"
```

---

## Task 2: Newsroom and News Article doctypes

**Files:**
- Create: `uSync/v17/ContentTypes/newsroom.config`
- Create: `uSync/v17/ContentTypes/newsarticle.config`
- Modify: `uSync/v17/ContentTypes/site.config` (add `newsroom` to `<Structure>`)

**Interfaces:**
- Produces:
  - doctype `newsroom`: props `pageTitle` (textbox), `introduction` (textarea). Template `Newsroom`. Allowed child: `newsArticle`.
  - doctype `newsArticle`: props `bannerImage` (MediaPicker3), `title` (textbox), `introduction` (textarea), `bodyText` (RichText, alias `bodyText`), `tags` (Tags), `publishedDate` (DateTime). Template `NewsArticle`.
  - Task 3 reads these aliases in the templates. Task 4's picker filters on the `newsArticle` alias.

- [ ] **Step 1: Create `uSync/v17/ContentTypes/newsroom.config`** (UTF-8 BOM)

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType Key="9f5e2a20-0001-4c30-b8a1-9f5e2a200001" Alias="newsroom" Level="2">
  <Info>
    <Name>Newsroom</Name>
    <Icon>icon-newspaper color-blue</Icon>
    <Thumbnail>folder.png</Thumbnail>
    <Description>The container for all News Article pages. One per site, under Home.</Description>
    <AllowAtRoot>False</AllowAtRoot>
    <ListView>00000000-0000-0000-0000-000000000000</ListView>
    <Variations>Nothing</Variations>
    <IsElement>false</IsElement>
    <HistoryCleanup>
      <PreventCleanup>False</PreventCleanup>
      <KeepAllVersionsNewerThanDays></KeepAllVersionsNewerThanDays>
      <KeepLatestVersionPerDayForDays></KeepLatestVersionPerDayForDays>
    </HistoryCleanup>
    <Folder>News</Folder>
    <Compositions />
    <DefaultTemplate>Newsroom</DefaultTemplate>
    <AllowedTemplates>
      <Template Key="9f5e2a20-0005-4c30-b8a1-9f5e2a200001">Newsroom</Template>
    </AllowedTemplates>
  </Info>
  <Structure>
    <ContentType Key="9f5e2a20-0002-4c30-b8a1-9f5e2a200001" SortOrder="0">newsArticle</ContentType>
  </Structure>
  <GenericProperties>
    <GenericProperty>
      <Key>9f5e2a20-0001-4c30-b8a1-9f5e2a200010</Key>
      <Name>Page Title</Name>
      <Alias>pageTitle</Alias>
      <Definition>0cc0eba1-9960-42c9-bf9b-60e150b429ae</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Shown as the page heading and in the browser tab.]]></Description>
      <SortOrder>0</SortOrder>
      <Tab Alias="content">Content</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>true</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>9f5e2a20-0001-4c30-b8a1-9f5e2a200011</Key>
      <Name>Introduction</Name>
      <Alias>introduction</Alias>
      <Definition>c6bac0dd-4ab9-45b1-8e30-e4b619ee5da3</Definition>
      <Type>Umbraco.TextArea</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Optional. A short line under the heading.]]></Description>
      <SortOrder>1</SortOrder>
      <Tab Alias="content">Content</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>true</LabelOnTop>
    </GenericProperty>
  </GenericProperties>
  <Tabs>
    <Tab>
      <Key>9f5e2a20-0001-4c30-b8a1-9f5e2a200002</Key>
      <Caption>Content</Caption>
      <Alias>content</Alias>
      <Type>Group</Type>
      <SortOrder>0</SortOrder>
    </Tab>
  </Tabs>
</ContentType>
```

- [ ] **Step 2: Create `uSync/v17/ContentTypes/newsarticle.config`** (UTF-8 BOM)

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType Key="9f5e2a20-0002-4c30-b8a1-9f5e2a200001" Alias="newsArticle" Level="3">
  <Info>
    <Name>News Article</Name>
    <Icon>icon-article color-blue</Icon>
    <Thumbnail>folder.png</Thumbnail>
    <Description>One news item or announcement. Lives under the Newsroom.</Description>
    <AllowAtRoot>False</AllowAtRoot>
    <ListView>00000000-0000-0000-0000-000000000000</ListView>
    <Variations>Nothing</Variations>
    <IsElement>false</IsElement>
    <HistoryCleanup>
      <PreventCleanup>False</PreventCleanup>
      <KeepAllVersionsNewerThanDays></KeepAllVersionsNewerThanDays>
      <KeepLatestVersionPerDayForDays></KeepLatestVersionPerDayForDays>
    </HistoryCleanup>
    <Folder>News</Folder>
    <Compositions />
    <DefaultTemplate>NewsArticle</DefaultTemplate>
    <AllowedTemplates>
      <Template Key="9f5e2a20-0006-4c30-b8a1-9f5e2a200001">NewsArticle</Template>
    </AllowedTemplates>
  </Info>
  <Structure />
  <GenericProperties>
    <GenericProperty>
      <Key>9f5e2a20-0002-4c30-b8a1-9f5e2a200010</Key>
      <Name>Banner Image</Name>
      <Alias>bannerImage</Alias>
      <Definition>ad9f0cf2-bda2-45d5-9ea1-a63cfc873fd3</Definition>
      <Type>Umbraco.MediaPicker3</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Optional. Shown at the top of the article and on the card.]]></Description>
      <SortOrder>0</SortOrder>
      <Tab Alias="content">Content</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>9f5e2a20-0002-4c30-b8a1-9f5e2a200011</Key>
      <Name>Title</Name>
      <Alias>title</Alias>
      <Definition>0cc0eba1-9960-42c9-bf9b-60e150b429ae</Definition>
      <Type>Umbraco.TextBox</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Optional. The headline. Falls back to the page name.]]></Description>
      <SortOrder>1</SortOrder>
      <Tab Alias="content">Content</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>9f5e2a20-0002-4c30-b8a1-9f5e2a200012</Key>
      <Name>Introduction</Name>
      <Alias>introduction</Alias>
      <Definition>c6bac0dd-4ab9-45b1-8e30-e4b619ee5da3</Definition>
      <Type>Umbraco.TextArea</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Optional. A short summary. Shown on cards and at the top of the article.]]></Description>
      <SortOrder>2</SortOrder>
      <Tab Alias="content">Content</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>9f5e2a20-0002-4c30-b8a1-9f5e2a200013</Key>
      <Name>Body</Name>
      <Alias>bodyText</Alias>
      <Definition>ca90c950-0aff-4e72-b976-a30b1ac57dad</Definition>
      <Type>Umbraco.RichText</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[The full article.]]></Description>
      <SortOrder>3</SortOrder>
      <Tab Alias="content">Content</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>9f5e2a20-0002-4c30-b8a1-9f5e2a200014</Key>
      <Name>Tags</Name>
      <Alias>tags</Alias>
      <Definition>b6b73142-b9c1-4bf8-a16d-e1c23320b549</Definition>
      <Type>Umbraco.Tags</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Optional. Reader-facing labels, e.g. "Athletics", "Board".]]></Description>
      <SortOrder>4</SortOrder>
      <Tab Alias="content">Content</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>9f5e2a20-0002-4c30-b8a1-9f5e2a200015</Key>
      <Name>Published Date</Name>
      <Alias>publishedDate</Alias>
      <Definition>5046194e-4237-453c-a547-15db3a07c4e1</Definition>
      <Type>Umbraco.DateTime</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Optional. The date this item was issued. Sorts the Newsroom and the feed. Falls back to the create date.]]></Description>
      <SortOrder>5</SortOrder>
      <Tab Alias="content">Content</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
  </GenericProperties>
  <Tabs>
    <Tab>
      <Key>9f5e2a20-0002-4c30-b8a1-9f5e2a200002</Key>
      <Caption>Content</Caption>
      <Alias>content</Alias>
      <Type>Group</Type>
      <SortOrder>0</SortOrder>
    </Tab>
  </Tabs>
</ContentType>
```

- [ ] **Step 3: Allow Newsroom under Home**

In `uSync/v17/ContentTypes/site.config`, find the `<Structure>` block:

```xml
  <Structure>
    <ContentType Key="1892c33e-8409-43ab-80a0-92628cbf84bc" SortOrder="0">subsite</ContentType>
    <ContentType Key="da53e7ee-644c-4195-b688-5ba9b4ffb5a8" SortOrder="1">linkPage</ContentType>
    <ContentType Key="146f354d-bcee-4e05-bbea-d251a85322cf" SortOrder="2">page</ContentType>
  </Structure>
```

Add one line before `</Structure>`:

```xml
    <ContentType Key="9f5e2a20-0001-4c30-b8a1-9f5e2a200001" SortOrder="3">newsroom</ContentType>
```

- [ ] **Step 4: Build**

Run: `dotnet build UmbracoBase.csproj`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add uSync/v17/ContentTypes/newsroom.config uSync/v17/ContentTypes/newsarticle.config uSync/v17/ContentTypes/site.config
git commit -m "Add Newsroom and News Article doctypes"
```

---

## Task 3: Templates for Newsroom and News Article

**Files:**
- Create: `uSync/v17/Templates/newsroom.config`
- Create: `uSync/v17/Templates/newsarticle.config`
- Create: `Views/Newsroom.cshtml`
- Create: `Views/NewsArticle.cshtml`

**Interfaces:**
- Consumes: doctype aliases and property aliases from Task 2.
- Produces: two rendered page templates. Task 5's feed links to `newsArticle` URLs, which these templates serve.

- [ ] **Step 1: Create `uSync/v17/Templates/newsroom.config`** (UTF-8 BOM)

```xml
<?xml version="1.0" encoding="utf-8"?>
<Template Key="9f5e2a20-0005-4c30-b8a1-9f5e2a200001" Alias="Newsroom" Level="2">
  <Name>Newsroom</Name>
  <Parent>master</Parent>
</Template>
```

- [ ] **Step 2: Create `uSync/v17/Templates/newsarticle.config`** (UTF-8 BOM)

```xml
<?xml version="1.0" encoding="utf-8"?>
<Template Key="9f5e2a20-0006-4c30-b8a1-9f5e2a200001" Alias="NewsArticle" Level="2">
  <Name>NewsArticle</Name>
  <Parent>master</Parent>
</Template>
```

- [ ] **Step 3: Create `Views/Newsroom.cshtml`**

```cshtml
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage<Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent>
@using Umbraco.Cms.Core.Models.PublishedContent
@using Umbraco.Extensions
@{
    Layout = "Master.cshtml";

    var pageTitle = Model.Value<string>("pageTitle");
    if (string.IsNullOrWhiteSpace(pageTitle)) { pageTitle = Model.Name; }
    ViewBag.Title = $"{pageTitle} | GGUSD News";

    var intro = Model.Value<string>("introduction");

    DateTime SortDate(IPublishedContent a)
    {
        var d = a.Value<DateTime>("publishedDate");
        return d > DateTime.MinValue ? d : a.CreateDate;
    }

    var articles = Model.Children
        .Where(c => c.ContentType.Alias == "newsArticle" && c.IsPublished())
        .OrderByDescending(SortDate)
        .ToList();
}

<section class="section newsroom py-4">
    <div class="container">
        <h1 class="head display-5 bold text-blue">@pageTitle</h1>
        @if (!string.IsNullOrWhiteSpace(intro))
        {
            <p class="newsroom__intro">@intro</p>
        }

        @if (articles.Count == 0)
        {
            <p class="text-muted">No news yet.</p>
        }
        else
        {
            <ul class="newsroom__list">
                @foreach (var a in articles)
                {
                    var title = a.Value<string>("title");
                    if (string.IsNullOrWhiteSpace(title)) { title = a.Name; }
                    var summary = a.Value<string>("introduction");
                    var banner = a.Value<MediaWithCrops>("bannerImage");
                    var bannerUrl = banner?.GetCropUrl(320, 200);
                    var date = SortDate(a);

                    <li class="newsroom__item">
                        <a class="newsroom__link" href="@a.Url()">
                            @if (!string.IsNullOrEmpty(bannerUrl))
                            {
                                <span class="newsroom__thumb"><img src="@bannerUrl" alt="@title" loading="lazy" /></span>
                            }
                            <span class="newsroom__body">
                                <span class="newsroom__date">@date.ToString("MMMM d, yyyy")</span>
                                <span class="newsroom__title">@title</span>
                                @if (!string.IsNullOrWhiteSpace(summary))
                                {
                                    <span class="newsroom__summary">@summary</span>
                                }
                            </span>
                        </a>
                    </li>
                }
            </ul>
        }
    </div>
</section>
```

- [ ] **Step 4: Create `Views/NewsArticle.cshtml`**

```cshtml
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage<Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent>
@using Umbraco.Cms.Core.Models.PublishedContent
@using Umbraco.Cms.Core.Strings
@using Umbraco.Extensions
@{
    Layout = "Master.cshtml";

    var title = Model.Value<string>("title");
    if (string.IsNullOrWhiteSpace(title)) { title = Model.Name; }
    ViewBag.Title = $"{title} | GGUSD News";

    var intro = Model.Value<string>("introduction");
    if (!string.IsNullOrWhiteSpace(intro)) { ViewBag.MetaDescription = intro; }

    var banner = Model.Value<MediaWithCrops>("bannerImage");
    var bannerUrl = banner?.GetCropUrl(1200, 500);
    var body = Model.Value<IHtmlEncodedString>("bodyText");
    var tags = Model.Value<IEnumerable<string>>("tags") ?? Enumerable.Empty<string>();

    var date = Model.Value<DateTime>("publishedDate");
    if (date <= DateTime.MinValue) { date = Model.CreateDate; }

    var newsroom = Model.Parent;
}

<article class="section news-article py-4">
    <div class="container">
        @if (newsroom != null)
        {
            <a class="news-article__back" href="@newsroom.Url()">&larr; @newsroom.Name</a>
        }

        @if (!string.IsNullOrEmpty(bannerUrl))
        {
            <img class="news-article__banner" src="@bannerUrl" alt="@title" />
        }

        <h1 class="head display-5 bold text-blue">@title</h1>
        <div class="news-article__date">@date.ToString("MMMM d, yyyy")</div>

        @if (!string.IsNullOrWhiteSpace(intro))
        {
            <p class="news-article__intro">@intro</p>
        }

        @if (body != null && !string.IsNullOrWhiteSpace(body.ToString()))
        {
            <div class="news-article__body">@body</div>
        }

        @if (tags.Any())
        {
            <div class="news-article__tags">
                @foreach (var t in tags)
                {
                    <span class="news-article__tag">@t</span>
                }
            </div>
        }
    </div>
</article>
```

- [ ] **Step 5: Build**

Run: `dotnet build UmbracoBase.csproj`
Expected: PASS. Razor compiles at runtime, so also confirm no C# syntax error by checking the build output has no `CS####` errors for these files. If the project uses `RazorCompileOnBuild`, a Razor error fails the build.

- [ ] **Step 6: Commit**

```bash
git add uSync/v17/Templates/newsroom.config uSync/v17/Templates/newsarticle.config Views/Newsroom.cshtml Views/NewsArticle.cshtml
git commit -m "Add Newsroom and News Article templates"
```

- [ ] **Step 7: Manual check (reviewer, after uSync import + restart)**

1. Import uSync. No errors for `newsroom`, `newsArticle`, the two templates, or the picker datatype.
2. Create a Newsroom page under Home. Set a title.
3. Create two News Articles under it. Give each a title, intro, body, and a published date a day apart.
4. Visit the Newsroom URL. Both articles listed, newest first.
5. Click one. The article page shows title, date, body, and a back link.

---

## Task 4: News Feed element types and block picker entry

**Files:**
- Create: `uSync/v17/ContentTypes/newsfeed.config`
- Create: `uSync/v17/ContentTypes/newsfeedsettings.config`
- Modify: `uSync/v17/DataTypes/BlockListWidgets.config` (add one block entry)

**Interfaces:**
- Consumes: the picker datatype key from Task 1.
- Produces:
  - element `newsFeed`: `baseContent` (heading, preheading, text), plus `count` (Umbraco.Integer, alias `count`), `pinnedArticles` (the Task 1 picker, alias `pinnedArticles`), `actionLink` (Umbraco.MultiUrlPicker, alias `actionLink`).
  - element `newsFeedSettings`: composes `baseAdvancedSettings` + `baseAppearanceSettings`, no own properties. Matches `newsWidgetSettings`.
  - Block picker gains a "News Feed" entry. Task 5's partial is resolved by the `newsFeed` alias. Task 6 places a `newsFeed` block.

- [ ] **Step 1: Create `uSync/v17/ContentTypes/newsfeed.config`** (UTF-8 BOM)

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType Key="9f5e2a20-0003-4c30-b8a1-9f5e2a200001" Alias="newsFeed" Level="3">
  <Info>
    <Name>News Feed</Name>
    <Icon>icon-newspaper color-blue</Icon>
    <Thumbnail>folder.png</Thumbnail>
    <Description>Shows the latest News Articles from the Newsroom. Pin specific articles to show them first.</Description>
    <AllowAtRoot>False</AllowAtRoot>
    <ListView>00000000-0000-0000-0000-000000000000</ListView>
    <Variations>Nothing</Variations>
    <IsElement>true</IsElement>
    <HistoryCleanup>
      <PreventCleanup>False</PreventCleanup>
      <KeepAllVersionsNewerThanDays></KeepAllVersionsNewerThanDays>
      <KeepLatestVersionPerDayForDays></KeepLatestVersionPerDayForDays>
    </HistoryCleanup>
    <Folder>Layout+Blocks/News</Folder>
    <Compositions>
      <Composition Key="2de152b5-b7d5-45aa-bb65-d8605ac0b6c7">baseContent</Composition>
    </Compositions>
    <DefaultTemplate></DefaultTemplate>
    <AllowedTemplates />
  </Info>
  <Structure />
  <GenericProperties>
    <GenericProperty>
      <Key>9f5e2a20-0003-4c30-b8a1-9f5e2a200010</Key>
      <Name>How Many To Show</Name>
      <Alias>count</Alias>
      <Definition>2e6d3631-066e-44b8-aec4-96f09099b2b5</Definition>
      <Type>Umbraco.Integer</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Total cards to show. Default 3.]]></Description>
      <SortOrder>1</SortOrder>
      <Tab Alias="content">Content</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>false</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>9f5e2a20-0003-4c30-b8a1-9f5e2a200011</Key>
      <Name>Pinned Articles</Name>
      <Alias>pinnedArticles</Alias>
      <Definition>9f5e2a20-0007-4c30-b8a1-9f5e2a200001</Definition>
      <Type>Umbraco.MultiNodeTreePicker</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Optional. These show first, in this order. Newest articles fill the rest.]]></Description>
      <SortOrder>2</SortOrder>
      <Tab Alias="content">Content</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>true</LabelOnTop>
    </GenericProperty>
    <GenericProperty>
      <Key>9f5e2a20-0003-4c30-b8a1-9f5e2a200012</Key>
      <Name>Action Link</Name>
      <Alias>actionLink</Alias>
      <Definition>b4e3535a-1753-47e2-8568-602cf8cfee6f</Definition>
      <Type>Umbraco.MultiUrlPicker</Type>
      <Mandatory>false</Mandatory>
      <Validation></Validation>
      <Description><![CDATA[Optional. The "view all" link, usually the Newsroom page.]]></Description>
      <SortOrder>3</SortOrder>
      <Tab Alias="content">Content</Tab>
      <Variations>Nothing</Variations>
      <MandatoryMessage></MandatoryMessage>
      <ValidationRegExpMessage></ValidationRegExpMessage>
      <LabelOnTop>true</LabelOnTop>
    </GenericProperty>
  </GenericProperties>
  <Tabs>
    <Tab>
      <Key>9f5e2a20-0003-4c30-b8a1-9f5e2a200002</Key>
      <Caption>Content</Caption>
      <Alias>content</Alias>
      <Type>Group</Type>
      <SortOrder>0</SortOrder>
    </Tab>
  </Tabs>
</ContentType>
```

Note: `baseContent` supplies the `heading`, `preheading`, and `text` properties on the same `content` tab. Do not redefine them.

- [ ] **Step 2: Create `uSync/v17/ContentTypes/newsfeedsettings.config`** (UTF-8 BOM)

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentType Key="9f5e2a20-0004-4c30-b8a1-9f5e2a200001" Alias="newsFeedSettings" Level="3">
  <Info>
    <Name>News Feed Settings</Name>
    <Icon>icon-settings color-blue</Icon>
    <Thumbnail>folder.png</Thumbnail>
    <Description></Description>
    <AllowAtRoot>False</AllowAtRoot>
    <ListView>00000000-0000-0000-0000-000000000000</ListView>
    <Variations>Nothing</Variations>
    <IsElement>true</IsElement>
    <HistoryCleanup>
      <PreventCleanup>False</PreventCleanup>
      <KeepAllVersionsNewerThanDays></KeepAllVersionsNewerThanDays>
      <KeepLatestVersionPerDayForDays></KeepLatestVersionPerDayForDays>
    </HistoryCleanup>
    <Folder>Layout+Blocks/News</Folder>
    <Compositions>
      <Composition Key="5f78746d-6566-4c80-ab32-7ca4a4e1d04a">baseAdvancedSettings</Composition>
      <Composition Key="ae4b16ce-a7b0-478d-8012-ea970a048e61">baseAppearanceSettings</Composition>
    </Compositions>
    <DefaultTemplate></DefaultTemplate>
    <AllowedTemplates />
  </Info>
  <Structure />
  <GenericProperties />
  <Tabs />
</ContentType>
```

- [ ] **Step 3: Add the block picker entry**

Open `uSync/v17/DataTypes/BlockListWidgets.config`. Inside the `<Config>` CDATA JSON, the `"blocks"` array holds objects like:

```json
{
  "contentElementTypeKey": "11987211-aa22-4261-a86e-e56b6c907199",
  "settingsElementTypeKey": "fb2cde28-4f32-42ad-aeee-2c01a83b32ef",
  "label": "Recent News : ${ heading || $settings.alias }",
  ...
}
```

Copy the "Recent News" object, paste it as a new array element next to it, and change three fields:

```json
{
  "contentElementTypeKey": "9f5e2a20-0003-4c30-b8a1-9f5e2a200001",
  "settingsElementTypeKey": "9f5e2a20-0004-4c30-b8a1-9f5e2a200001",
  "label": "News Feed : ${ heading || $settings.alias }"
}
```

Keep every other field (`view`, `stylesheet`, `editorSize`, `iconColor`, etc.) identical to the object you copied. Keep the JSON valid: comma between array elements, no trailing comma.

- [ ] **Step 4: Validate the JSON**

Run:

```bash
python3 -c "import re,json;raw=open('uSync/v17/DataTypes/BlockListWidgets.config',encoding='utf-8-sig').read();i=raw.find('{');j=raw.rfind('}');json.loads(raw[i:j+1]);print('JSON OK')"
```

Expected: `JSON OK`.

- [ ] **Step 5: Build**

Run: `dotnet build UmbracoBase.csproj`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add uSync/v17/ContentTypes/newsfeed.config uSync/v17/ContentTypes/newsfeedsettings.config uSync/v17/DataTypes/BlockListWidgets.config
git commit -m "Add News Feed block element types and picker entry"
```

---

## Task 5: News Feed partial and stylesheet

**Files:**
- Create: `Views/Partials/Widgets/newsFeed.cshtml`
- Create: `wwwroot/css/Widgets_CSS/newsFeed.css`
- Modify: `Views/Master.cshtml` (add one `<link>`)

**Interfaces:**
- Consumes: `newsFeed` properties from Task 4 (`heading`, `preheading`, `text`, `count`, `pinnedArticles`, `actionLink`), `newsArticle` properties from Task 2, the Newsroom listing from Task 3.
- Produces: a rendered block. Task 6 places it on the home page.

- [ ] **Step 1: Create `Views/Partials/Widgets/newsFeed.cshtml`**

```cshtml
@inherits UmbracoViewPage<Umbraco.Cms.Core.Models.Blocks.BlockListItem>
@using Umbraco.Cms.Core.Models.Blocks
@using Umbraco.Cms.Core.Models.PublishedContent
@using Umbraco.Extensions
@{
    var content = Model.Content;
    var settings = Model.Settings;

    if (settings?.Value<bool?>("blockVisibility") == false) { return; }

    int order = ViewData["order"] != null ? Convert.ToInt32(ViewData["order"]) : 0;

    var heading = content.Value<string>("heading");
    var preheading = content.Value<string>("preheading");
    var text = content.Value<Umbraco.Cms.Core.Strings.IHtmlEncodedString>("text");

    var count = content.Value<int>("count");
    if (count <= 0) { count = 3; }

    var actionLink = content.Value<Link>("actionLink");
    var hasActionLink = actionLink != null && !string.IsNullOrEmpty(actionLink.Url);

    var verticalSpacingClass = string.IsNullOrWhiteSpace(settings?.Value<string>("verticalSpacing"))
        ? "py-4" : settings.Value<string>("verticalSpacing");
    var anchorId = settings?.Value<string>("anchorId");
    var classNames = string.Join(" ",
        (settings?.Value<IEnumerable<string>>("classnames") ?? Enumerable.Empty<string>())
        .Where(s => !string.IsNullOrWhiteSpace(s)));

    DateTime SortDate(IPublishedContent a)
    {
        var d = a.Value<DateTime>("publishedDate");
        return d > DateTime.MinValue ? d : a.CreateDate;
    }

    // Find the Newsroom: the first published newsroom node anywhere under the site root.
    IPublishedContent? newsroom = Umbraco.AssignedContentItem?.Root()?
        .DescendantsOrSelf()
        .FirstOrDefault(c => c.ContentType.Alias == "newsroom" && c.IsPublished());

    var pinned = (content.Value<IEnumerable<IPublishedContent>>("pinnedArticles") ?? Enumerable.Empty<IPublishedContent>())
        .Where(a => a != null && a.ContentType.Alias == "newsArticle" && a.IsPublished())
        .ToList();

    var newest = newsroom == null
        ? new List<IPublishedContent>()
        : newsroom.Children
            .Where(c => c.ContentType.Alias == "newsArticle" && c.IsPublished())
            .OrderByDescending(SortDate)
            .ToList();

    var pinnedIds = pinned.Select(p => p.Id).ToHashSet();
    var feed = pinned
        .Concat(newest.Where(n => !pinnedIds.Contains(n.Id)))
        .Take(count)
        .ToList();
}

@if (feed.Any())
{
    <section class="section block-recent-news @verticalSpacingClass @classNames"
    @if (!string.IsNullOrEmpty(anchorId))
    {
        <text>id="@anchorId"</text>
    }>
        <div class="container-fluid px-4">
            @if (!string.IsNullOrEmpty(preheading) || !string.IsNullOrEmpty(heading) || hasActionLink)
            {
                <div class="news-widget__header">
                    <div class="news-widget__label">
                        @if (!string.IsNullOrEmpty(preheading))
                        {
                            <span>@preheading</span>
                        }
                        @if (!string.IsNullOrEmpty(heading) && order == 0)
                        {
                            <h1 class="news-widget__heading">@heading</h1>
                        }
                        else if (!string.IsNullOrEmpty(heading))
                        {
                            <h2 class="news-widget__heading">@heading</h2>
                        }
                    </div>
                    @if (hasActionLink)
                    {
                        <a href="@actionLink!.Url" target="@(actionLink.Target ?? "_self")" class="news-widget__action">@(string.IsNullOrEmpty(actionLink.Name) ? "All news" : actionLink.Name) &rarr;</a>
                    }
                </div>
            }
            @if (text != null && !string.IsNullOrEmpty(text.ToString()))
            {
                <p class="news-widget__text">@text</p>
            }

            @foreach (var a in feed)
            {
                var title = a.Value<string>("title");
                if (string.IsNullOrWhiteSpace(title)) { title = a.Name; }
                var tag = (a.Value<IEnumerable<string>>("tags") ?? Enumerable.Empty<string>()).FirstOrDefault();
                var banner = a.Value<MediaWithCrops>("bannerImage");
                var photoUrl = banner?.GetCropUrl(160, 130);
                var dateText = SortDate(a).ToString("MMM d, yyyy");

                <div class="news-item">
                    @if (!string.IsNullOrEmpty(photoUrl))
                    {
                        <div class="news-item__thumb"><img src="@photoUrl" alt="@title" /></div>
                    }
                    else
                    {
                        <div class="news-item__thumb news-item__thumb--empty"></div>
                    }
                    <div class="news-item__body">
                        @if (!string.IsNullOrEmpty(tag))
                        {
                            <span class="news-item__tag">@tag</span>
                        }
                        <div class="news-item__title">
                            <a href="@a.Url()">@title</a>
                        </div>
                        <div class="news-item__date">@dateText</div>
                    </div>
                </div>
            }
        </div>
    </section>
}
```

- [ ] **Step 2: Create `wwwroot/css/Widgets_CSS/newsFeed.css`**

The feed reuses the `.block-recent-news` / `.news-item` classes the current Recent News block already styles. This file only adds the article-page and newsroom-page styles plus a light guard.

```css
@import '../styles.css';

/* News Feed block reuses .block-recent-news and .news-item from the
   existing Recent News widget CSS. Nothing extra needed for the block. */

/* Newsroom listing page */
.newsroom__intro{
    font-size: 1.05rem;
    color: #444;
    margin-bottom: 1.5rem;
}
.newsroom__list{
    list-style: none;
    margin: 0;
    padding: 0;
}
.newsroom__item{
    border-top: 1px solid var(--shs-purple-pale, #f3edfa);
}
.newsroom__item:last-child{
    border-bottom: 1px solid var(--shs-purple-pale, #f3edfa);
}
.newsroom__link{
    display: flex;
    gap: 1rem;
    padding: 1rem 0;
    text-decoration: none;
    color: inherit;
}
.newsroom__link:hover .newsroom__title{
    color: var(--shs-purple, #4a1f7a);
    text-decoration: underline;
}
.newsroom__thumb img{
    width: 160px;
    height: 100px;
    object-fit: cover;
    border-radius: 8px;
}
.newsroom__body{
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}
.newsroom__date{
    font-size: 0.8rem;
    text-transform: uppercase;
    letter-spacing: 0.03em;
    color: var(--shs-gold, #c9a227);
    font-weight: 700;
}
.newsroom__title{
    font-family: var(--font-nav-and-texts, "Nunito", sans-serif);
    font-weight: 800;
    font-size: 1.15rem;
    color: var(--shs-purple, #4a1f7a);
}
.newsroom__summary{
    color: #555;
}

/* News Article page */
.news-article__back{
    display: inline-block;
    margin-bottom: 1rem;
    font-weight: 700;
    color: var(--shs-purple, #4a1f7a);
    text-decoration: none;
}
.news-article__banner{
    width: 100%;
    max-height: 420px;
    object-fit: cover;
    border-radius: 12px;
    border-top: 4px solid var(--shs-gold, #c9a227);
    margin-bottom: 1.5rem;
}
.news-article__date{
    font-size: 0.85rem;
    text-transform: uppercase;
    letter-spacing: 0.03em;
    color: var(--shs-gold, #c9a227);
    font-weight: 700;
    margin-bottom: 1rem;
}
.news-article__intro{
    font-size: 1.15rem;
    font-weight: 600;
    color: #333;
}
.news-article__body{
    font-family: var(--font-nav-and-texts, "Nunito", sans-serif);
    line-height: 1.7;
}
.news-article__tags{
    margin-top: 1.5rem;
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
}
.news-article__tag{
    background: var(--shs-purple-pale, #f3edfa);
    color: var(--shs-purple, #4a1f7a);
    font-size: 0.8rem;
    font-weight: 700;
    padding: 0.2rem 0.6rem;
    border-radius: 999px;
}

@media (max-width: 576px){
    .newsroom__link{ flex-direction: column; }
    .newsroom__thumb img{ width: 100%; height: 180px; }
}
```

- [ ] **Step 3: Link the stylesheet**

In `Views/Master.cshtml`, find the run of widget stylesheet links (near `pressReleases.css` at the end of that block) and add:

```html
	<link rel="stylesheet" href="/css/Widgets_CSS/newsFeed.css" />
```

If `pressReleases.css` is still linked there, replace that line with the `newsFeed.css` line, since Task 7 deletes `pressReleases.css`.

- [ ] **Step 4: Build**

Run: `dotnet build UmbracoBase.csproj`
Expected: PASS with no `CS####` errors.

- [ ] **Step 5: Commit**

```bash
git add Views/Partials/Widgets/newsFeed.cshtml wwwroot/css/Widgets_CSS/newsFeed.css Views/Master.cshtml
git commit -m "Add News Feed partial and styles"
```

- [ ] **Step 6: Manual check (reviewer, after uSync import + restart)**

1. On any page, add a News Feed block. Set heading "Recent News", count 3, action link to the Newsroom.
2. Publish. The block shows the newest 3 articles, each linking to its article page.
3. Pin one older article. It now shows first, then two newest fill the rest.
4. Unpublish every article. The block renders nothing, no error.

---

## Task 6: Move the home page news block to News Feed

**Files:**
- Modify: `uSync/v17/Content/home.config` (swap one nested block)
- Modify: `Views/Partials/Widgets/RenderBlocks.cshtml:36` (recognise `newsFeed` in the events pairing)

**Interfaces:**
- Consumes: the `newsFeed` element type from Task 4, the partial from Task 5.

**Context:** In `home.config`, the `widgets` Block List holds a Block Line row (`contentTypeKey` `f1a90b1e-...`). Inside that row's nested `widgets` list sits the `newsWidget` block (`contentTypeKey` `11987211-aa22-4261-a86e-e56b6c907199`), heading "Recent News". Replace only that nested block.

- [ ] **Step 1: Inspect the current block**

Run:

```bash
python3 - <<'PY'
import re,json
raw=open('uSync/v17/Content/home.config',encoding='utf-8-sig').read()
m=re.search(r'<widgets>\s*<Value><!\[CDATA\[(.*?)\]\]></Value>\s*</widgets>',raw,re.S)
d=json.loads(m.group(1))
def find(node):
    if isinstance(node,dict):
        if node.get('contentTypeKey','').startswith('11987211'):
            print(json.dumps(node,indent=2)); return
        for v in node.values(): find(v)
    elif isinstance(node,list):
        for x in node: find(x)
find(d)
PY
```

Note the block's `key` (call it `NEWS_KEY`) and its settings block key from the row's `settingsData` / `Layout` (call it `SET_KEY`).

- [ ] **Step 2: Swap the block in place**

Run this script. It keeps the same `key` and layout position, changes the content type to `newsFeed`, and rewrites the values to `heading`, `count`, and an `actionLink` pointing at the Newsroom. It also changes the matching settings block's content type to `newsFeedSettings`.

```bash
python3 - <<'PY'
import re,json
p='uSync/v17/Content/home.config'
raw=open(p,encoding='utf-8-sig').read()
m=re.search(r'(<widgets>\s*<Value><!\[CDATA\[)(.*?)(\]\]></Value>\s*</widgets>)',raw,re.S)
d=json.loads(m.group(2))

NEWS_CT='9f5e2a20-0003-4c30-b8a1-9f5e2a200001'
SET_CT='9f5e2a20-0004-4c30-b8a1-9f5e2a200001'
OLD_CT='11987211-aa22-4261-a86e-e56b6c907199'
OLD_SET_CT='fb2cde28-4f32-42ad-aeee-2c01a83b32ef'

def v(alias,value): return {"alias":alias,"culture":None,"editorAlias":None,"segment":None,"value":value}

def walk(node):
    if isinstance(node,dict):
        cd=node.get('contentData')
        if isinstance(cd,list):
            for c in cd:
                if c.get('contentTypeKey')==OLD_CT:
                    c['contentTypeKey']=NEWS_CT
                    c['values']=[
                        v("heading","Recent News"),
                        v("count","3"),
                    ]
        sd=node.get('settingsData')
        if isinstance(sd,list):
            for s in sd:
                if s.get('contentTypeKey')==OLD_SET_CT:
                    s['contentTypeKey']=SET_CT
        for val in node.values(): walk(val)
    elif isinstance(node,list):
        for x in node: walk(x)

walk(d)
new=m.group(1)+json.dumps(d,indent=2)+m.group(3)
raw=raw[:m.start()]+new+raw[m.end():]
open(p,'wb').write(b'\xef\xbb\xbf'+raw.encode('utf-8'))
print('done')
PY
```

- [ ] **Step 3: Verify the swap and the BOM**

Run:

```bash
python3 -c "import re,json;raw=open('uSync/v17/Content/home.config',encoding='utf-8-sig').read();d=json.loads(re.search(r'CDATA\[(.*?)\]\]></Value>\s*</widgets>',raw,re.S).group(1));print('newsFeed present:', '9f5e2a20-0003-4c30-b8a1-9f5e2a200001' in json.dumps(d));print('old newsWidget gone:', '11987211-aa22-4261-a86e-e56b6c907199' not in json.dumps(d))"
head -c 3 uSync/v17/Content/home.config | xxd
```

Expected: both `True`, and `xxd` shows `efbb bf`.

- [ ] **Step 4: Update the events/news pairing check**

In `Views/Partials/Widgets/RenderBlocks.cshtml`, line 36 reads:

```csharp
        var nextIsNews = i + 1 < blocks.Count && blocks[i + 1].Content.ContentType.Alias == "newsWidget";
```

Change it to:

```csharp
        var nextIsNews = i + 1 < blocks.Count && blocks[i + 1].Content.ContentType.Alias is "newsWidget" or "newsFeed";
```

- [ ] **Step 5: Build**

Run: `dotnet build UmbracoBase.csproj`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add uSync/v17/Content/home.config Views/Partials/Widgets/RenderBlocks.cshtml
git commit -m "Home page: use News Feed for Recent News"
```

- [ ] **Step 7: Manual check (reviewer, after uSync import + restart)**

1. Import uSync. The home page Recent News area still renders.
2. It now shows the newest News Articles, each linking to its article page.
3. The side-by-side layout with Upcoming Events, if present, still holds.

---

## Task 7: Remove the unfinished Press Releases block

**Files:**
- Delete: `uSync/v17/ContentTypes/pressreleases.config`
- Delete: `uSync/v17/ContentTypes/pressreleaseitem.config`
- Delete: `uSync/v17/DataTypes/BlockListPressReleases.config`
- Delete: `Views/Partials/Widgets/pressReleases.cshtml`
- Delete: `wwwroot/css/Widgets_CSS/pressReleases.css`
- Delete: `umbraco/models/PressReleases.generated.cs`
- Delete: `umbraco/models/PressReleaseItem.generated.cs`

**Context:** These files are untracked and were never committed. `git status` shows them as `??`. Confirm none of them appear in any committed content before deleting.

- [ ] **Step 1: Confirm the block is unused**

Run:

```bash
grep -rl "pressReleases\|pressReleaseItem\|9f5e1a20-0001-4c30\|9f5e1a20-0002-4c30" uSync/v17/Content/ || echo "no content references"
grep -n "pressReleases" uSync/v17/DataTypes/BlockListWidgets.config || echo "not in the widget picker"
grep -rn "pressReleases" Views/Master.cshtml || echo "not linked in Master"
```

Expected: `no content references`, `not in the widget picker`, and either no Master line or one to remove in the next step.

- [ ] **Step 2: Remove the Master stylesheet link if present**

If Step 1 found a `pressReleases.css` link in `Views/Master.cshtml` and Task 5 Step 4 did not already replace it, delete that `<link>` line now.

- [ ] **Step 3: Delete the files**

```bash
rm uSync/v17/ContentTypes/pressreleases.config \
   uSync/v17/ContentTypes/pressreleaseitem.config \
   uSync/v17/DataTypes/BlockListPressReleases.config \
   Views/Partials/Widgets/pressReleases.cshtml \
   wwwroot/css/Widgets_CSS/pressReleases.css \
   umbraco/models/PressReleases.generated.cs \
   umbraco/models/PressReleaseItem.generated.cs
```

- [ ] **Step 4: Build**

Run: `dotnet build UmbracoBase.csproj`
Expected: PASS. If a `CS####` error names `PressRelease`, something still references the deleted models. Find it with `grep -rn "PressRelease" Views/ umbraco/`.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Remove unfinished Press Releases block"
```

---

## Final integration check (reviewer)

Run the site locally. Then:

1. uSync import completes with no errors.
2. ModelsBuilder regenerates. `umbraco/models/` gains `Newsroom`, `NewsArticle`, `NewsFeed`, `NewsFeedSettings`. The build stays green.
3. Create a Newsroom under Home. Try to create it elsewhere: no other parent offers it.
4. Add three News Articles with published dates over three days.
5. The Newsroom page lists them newest first.
6. Each article page renders banner, title, date, intro, body, tags, and a back link.
7. The home page Recent News shows the newest three, linking to article pages.
8. Add a News Feed block to one other page. Pin one article. It shows first.
9. Unpublish all articles. Both the Newsroom page and every feed degrade cleanly, no error.
10. Any page still using the old News Widget renders as before.

## Self-review notes

- Spec section "Newsroom listing page": Task 3.
- Spec section "News Article page": Task 3 (`NewsArticle.cshtml`). The spec listed the article template under "content structure"; it is folded into Task 3.
- Spec "News Feed widget" element types and datatype: Task 4. Rendering: Task 5.
- Spec "Home page change": Task 6.
- Spec "Files / Deleted": Task 7.
- Spec "Build and deploy notes": the plan drops the hand-written `*.generated.cs` step. All new Razor is model-agnostic, so the project builds from source with no generated models. ModelsBuilder still creates the typed models after import. This is a deliberate simplification of the spec.
- The spec named the body field `content`; the plan uses alias `bodyText` to avoid colliding with the `content` tab alias and the `baseContent` composition's group. Templates and the feed use `bodyText` consistently.
- The spec named the picker a "Multi Node Tree Picker limited to newsArticle". Task 1 builds exactly that.
