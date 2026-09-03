# Newsroom and News Feed — design

Date: 2026-09-03
Status: approved for planning

## Goal

Give the site one central store for news and announcements. Each item is
its own page with its own URL. Any page can show a feed of the latest
items. The home page "Recent News" block reads from this store instead of
a hand-kept list.

## Background

The site has two overlapping, unfinished pieces:

- `newsWidget` / `newsItem`: a manual Block List of news cards. One block
  sits on the home page inside `site.widgets`.
- `pressReleases` / `pressReleaseItem` / `BlockListPressReleases`: an
  in-progress Block List of press release cards. Untracked, unused.

Both keep news as blocks pasted onto a page. Neither gives a news item its
own URL. This design replaces that model.

## Decisions

| Question | Decision |
|---|---|
| Storage | Each news item is its own content page. |
| Names | Container doctype `newsroom`. Item doctype `newsArticle`. |
| Home block | Auto feed with manual pin. Newest fill the rest. |
| Keywords field | Dropped. The `<meta name="keywords">` tag does nothing for search now. |
| Old `newsWidget` | Kept so existing usages still render. Removed from the block picker later if unused. |
| In-progress `pressReleases` | Field definitions reused for `newsArticle`. The block files are deleted. |

## Content structure

- **Newsroom** (`newsroom`), a page doctype, allowed only under Home
  (`site`). Expected to be a single node, for example at `/newsroom`. Not
  enforced in code.
  - Fields: `pageTitle` (textbox), `introduction` (textarea).
  - Allowed child: `newsArticle` only.
  - Template: `Newsroom`.
- **News Article** (`newsArticle`), a page doctype, allowed only under
  `newsroom`. URL example `/newsroom/board-meeting-recap`.
  - Template: `NewsArticle`.
  - Fields below.

This uses dedicated doctypes rather than the site's usual "`page` doctype
plus a named template" convention. Dedicated types are needed here to
restrict children and to give each field a real editor.

### News Article fields

Reuse the existing datatype definitions (the GUIDs already in
`pressReleaseItem`), so no new field editors are created.

| Field | Alias | Type | Datatype GUID | Notes |
|---|---|---|---|---|
| Banner image | `bannerImage` | Media Picker | `ad9f0cf2-bda2-45d5-9ea1-a63cfc873fd3` | Optional. Article header and card image. |
| Title | `title` | Textbox | `0cc0eba1-9960-42c9-bf9b-60e150b429ae` | Optional. Falls back to node name. |
| Introduction | `introduction` | Textarea | `c6bac0dd-4ab9-45b1-8e30-e4b619ee5da3` | Optional. Card summary and article lead. |
| Body | `content` | Rich Text | `ca90c950-0aff-4e72-b976-a30b1ac57dad` | Optional. Full article. |
| Tags | `tags` | Tags | `b6b73142-b9c1-4bf8-a16d-e1c23320b549` | Optional. Reader-facing chips. |
| Published date | `publishedDate` | Date | `5046194e-4237-453c-a547-15db3a07c4e1` | Optional. Sort key. Falls back to `CreateDate`. |

## Newsroom listing page

`Views/Newsroom.cshtml`, layout `Master.cshtml`.

- Reads `Model.Children` of type `newsArticle`, published only.
- Sort: `publishedDate` descending, `CreateDate` descending as fallback.
- Renders the page title and intro, then one row per article: banner,
  title, formatted date, intro text, link to the article.
- No pagination in this version. Revisit when the list passes ~30 items.
- Empty state: a short "No news yet" line.

## News Article page

`Views/NewsArticle.cshtml`, layout `Master.cshtml`.

- Header: banner image if set, title, formatted published date.
- Body: the `content` rich text.
- Tags: chips under the body if any.
- "Back to Newsroom" link to the parent.
- `ViewBag.Title` set to `"{title} | GGUSD News"`.
- Sets `<meta name="description">` from `introduction` if present.

## News Feed widget

New block widget, usable on any page and on the home page.

### Element types

- `newsFeed` (content), composes `baseContent` for heading, preheading,
  text.
  - `count` (number, default 3): how many articles to show.
  - `pinnedArticles` (the new picker below): articles to show first.
  - `actionLink` (Multi URL Picker, single): the "view all" link.
- `newsFeedSettings` (settings), composes `baseAdvancedSettings` and
  `baseAppearanceSettings`, matching `newsWidgetSettings`.

### New datatype

- `[Content Picker] News Articles`: a Multi Node Tree Picker limited to
  the `newsArticle` doctype. Used by `pinnedArticles`. One config file in
  `uSync/v17/DataTypes/`.

### Block picker entry

Add one entry to `uSync/v17/DataTypes/BlockListWidgets.config`:
`contentElementTypeKey` = `newsFeed`, `settingsElementTypeKey` =
`newsFeedSettings`, label `News Feed : ${ heading || $settings.alias }`.

### Rendering

`Views/Partials/Widgets/newsFeed.cshtml`:

1. Find the Newsroom node. Strategy: first published `newsroom` under the
   site root. Cache the lookup per request.
2. Build the list: pinned articles first, in picked order, then newest
   articles by `publishedDate` that are not already pinned, until `count`
   is reached.
3. Render with the current `.block-recent-news` card markup and classes,
   so the home page keeps its present look.
4. If the Newsroom node is missing or empty, render nothing.

`wwwroot/css/Widgets_CSS/newsFeed.css`, linked from `Views/Master.cshtml`.
Start by copying the news card rules the home page uses now. Keep the
class names so existing CSS still applies.

## Home page change

- In the Home content node (`uSync/v17/Content/home.config`), replace the
  `newsWidget` block (`contentTypeKey` `11987211-...`) inside `widgets`
  with a `newsFeed` block. Set its heading to the current one and
  `count` to 3.
- Leave the `newsWidget` doctype, partial, and CSS in place.

## Files

New:

- `uSync/v17/ContentTypes/newsroom.config`
- `uSync/v17/ContentTypes/newsarticle.config`
- `uSync/v17/ContentTypes/newsfeed.config`
- `uSync/v17/ContentTypes/newsfeedsettings.config`
- `uSync/v17/DataTypes/ContentPickerNewsArticles.config`
- `uSync/v17/Templates/newsroom.config`
- `uSync/v17/Templates/newsarticle.config`
- `Views/Newsroom.cshtml`
- `Views/NewsArticle.cshtml`
- `Views/Partials/Widgets/newsFeed.cshtml`
- `wwwroot/css/Widgets_CSS/newsFeed.css`
- `umbraco/models/Newsroom.generated.cs`
- `umbraco/models/NewsArticle.generated.cs`
- `umbraco/models/NewsFeed.generated.cs`
- `umbraco/models/NewsFeedSettings.generated.cs`

Changed:

- `uSync/v17/ContentTypes/site.config`: allow `newsroom` as a child.
- `uSync/v17/DataTypes/BlockListWidgets.config`: add the News Feed entry.
- `Views/Master.cshtml`: link `newsFeed.css`.
- `uSync/v17/Content/home.config`: swap the news block.

Deleted (untracked, never committed):

- `uSync/v17/ContentTypes/pressreleases.config`
- `uSync/v17/ContentTypes/pressreleaseitem.config`
- `uSync/v17/DataTypes/BlockListPressReleases.config`
- `Views/Partials/Widgets/pressReleases.cshtml`
- `wwwroot/css/Widgets_CSS/pressReleases.css`
- `umbraco/models/PressReleases.generated.cs`
- `umbraco/models/PressReleaseItem.generated.cs`

## Build and deploy notes

- ModelsBuilder mode is `SourceCodeAuto`. Hand-write the four
  `*.generated.cs` models so the project builds before the first run.
  ModelsBuilder rewrites them with the same output on the next run.
- After merge: run a uSync import in the backoffice, let ModelsBuilder
  regenerate, restart the site.
- Then create the Newsroom node, add one or two News Articles, and point
  the home page block's heading and count.

## Testing

Manual, in a local run:

1. uSync import succeeds with no errors for the new types.
2. Newsroom node can be created under Home. No other parent allows it.
3. News Article can be created only under Newsroom.
4. The Newsroom page lists articles newest first.
5. A News Article page renders banner, title, date, body, tags.
6. The News Feed block on the home page shows the newest 3 articles.
7. Pinning two articles shows them first, then one newest article fills
   the third slot.
8. Deleting all articles makes the block render nothing, not an error.
9. The old News Widget still renders on any page that still uses it.

## Out of scope

- Pagination or infinite scroll on the Newsroom page.
- Tag filter pages or an RSS feed.
- Migrating existing `newsItem` blocks into News Articles.
- Author or byline fields.
- Scheduled publishing beyond Umbraco's built-in schedule.
