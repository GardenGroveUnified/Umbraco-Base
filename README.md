# Umbraco Base (school website)

An Umbraco 17 site built on .NET 10. It currently runs the Santiago High School website, but it is meant to be a base that other schools can clone.

- **CMS:** Umbraco 17.3.5 (block-list widgets, ModelsBuilder in `SourceCodeAuto` mode)
- **Database:** SQL Server (SQL Server Express locally)
- **Content and schema in git:** uSync 17.3.1 (`uSync/v17/`)
- **Other packages:** see `Directory.Packages.props` (SeoToolkit, Contentment, Full Text Search, Skybrud Redirects, Accessibility Toolkit, WebOptimizer, Ical.Net, ExcelDataReader)

---

## 1. Getting the project running locally

### Prerequisites

| Tool | Notes |
|------|-------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | `dotnet --version` should report 10.x |
| SQL Server Express (or any SQL Server 2019+) | Default instance name used here is `localhost\SQLEXPRESS` |
| SQL Server Management Studio (SSMS) | To create the database and run the setup script below |
| Visual Studio 2022 (17.14+) / VS 2026, Rider, or VS Code | Any of these work. No Node.js build step is needed. |

### Step by step

1. **Clone the repo.**
   ```bash
   git clone <repo-url>
   cd Umbraco-Base
   ```

2. **Create an empty database** in SSMS (for example `UmbracoBase`). Then run this against it. Without it, SQL Express defaults cause deadlocks and login errors with Umbraco:
   ```sql
   ALTER DATABASE [UmbracoBase] SET ALLOW_SNAPSHOT_ISOLATION ON;
   ALTER DATABASE [UmbracoBase] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
   ALTER DATABASE [UmbracoBase] SET AUTO_CLOSE OFF;

   -- Check: should return 1 and 0
   SELECT is_read_committed_snapshot_on, is_auto_close_on FROM sys.databases WHERE name = 'UmbracoBase';
   ```

3. **Create `appsettings.json` and `appsettings.Development.json`** in the project root. Both files are **gitignored**, so they are not in the clone. See [Configuration](#2-setup-and-configuration) for a template.

4. **Copy the media files.** `wwwroot/media/` is also **gitignored** (uploaded images and PDFs, about 40 MB). uSync only stores the media *records*, not the files. Copy `wwwroot/media/` from another developer's machine or from the server. Without it, the site runs but every image is broken.

5. **Run the site.**
   ```bash
   dotnet run --launch-profile "Umbraco.Web.UI"
   ```
   Or press F5 in Visual Studio with the `Umbraco.Web.UI` profile. On first boot, the unattended install creates the Umbraco tables and the admin user from your `appsettings.json`.

6. **Log in to the backoffice** at **https://localhost:44389/umbraco**. Use the `https` port: the backoffice login rejects plain `http://localhost:18040`. The public site works on either port.

7. **Import the content with uSync.** A new database is empty. Go to **Settings → uSync** and click **Import**. This brings in document types, data types, templates, media records and all pages. See [uSync](#4-usync) for details.

8. **Restart the site** once after the first import so ModelsBuilder and the caches pick everything up. Then browse to https://localhost:44389/.

### Running the tests

```bash
dotnet test tests/UmbracoBase.Tests/UmbracoBase.Tests.csproj
```

### Day-to-day tips

- **Razor views compile at runtime** (`RazorCompileOnBuild` is `false`). A `.cshtml` change needs no rebuild, just a browser refresh. A Razor error only shows when you load the page that uses it, not at build time.
- **C# changes need a restart.** Stop the running site first, or `dotnet build` fails with a "file is locked by UmbracoBase" error. In PowerShell: `Get-Process -Name UmbracoBase | Stop-Process -Force`.
- **Only one copy of the site can run at a time**, because the ports (44389/18040) are fixed in `Properties/launchSettings.json`.
- **CSS is bundled.** All front-end stylesheets are combined into `/css/site.bundle.css` by WebOptimizer. To add a stylesheet, add its path to `SourceFiles` in `Core/Bundling/StylesheetBundle.cs`. Do not add a `<link>` tag to `Master.cshtml`. Order in that list matters.
- **JavaScript** lives in `wwwroot/js/*.js`, loaded from `Views/Master.cshtml` with `defer`.

---

## 2. Setup and configuration

### `appsettings.json` template

Create this file in the project root and fill in the `<...>` values. Never commit real secrets.

```jsonc
{
  "$schema": "appsettings-schema.json",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    }
  },
  "Umbraco": {
    "CMS": {
      "Global": {
        "Id": "<new GUID>",
        "SanitizeTinyMce": true,
        "Smtp": {
          "From": "<no-reply address>",
          "Host": "<smtp host, e.g. smtp.gmail.com>",
          "Port": 587,
          "SecureSocketOptions": "StartTlsWhenAvailable",
          "Username": "<smtp username>",
          "Password": "<smtp password or app password>"
        }
      },
      "Content": {
        "AllowEditInvariantFromNonDefault": true,
        "ContentVersionCleanupPolicy": { "EnableCleanup": true }
      },
      "ModelsBuilder": {
        "ModelsMode": "SourceCodeAuto",
        "ModelsNamespace": "Umbraco.Cms.Web.Common.PublishedModels",
        "ModelsDirectory": "~/umbraco/models"
      },
      "Unattended": {
        "InstallUnattended": true,
        "UpgradeUnattended": true,
        "UnattendedUserName": "Administrator",
        "UnattendedUserEmail": "<your email>",
        "UnattendedUserPassword": "<a strong password, 10+ characters>"
      },
      "Security": { "AllowConcurrentLogins": true },
      "Imaging": { "HMACSecretKey": "<long random string>" }
    }
  },
  "ConnectionStrings": {
    "umbracoDbDSN": "Server=localhost\\SQLEXPRESS;Database=UmbracoBase;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    "umbracoDbDSN_ProviderName": "Microsoft.Data.SqlClient"
  },
  "ActiveDirectory": {
    "Domain": ""
  }
}
```

### `appsettings.Development.json` template

```json
{
  "$schema": "appsettings-schema.json",
  "Serilog": {
    "MinimumLevel": { "Default": "Information" },
    "WriteTo": [ { "Name": "Async", "Args": { "configure": [ { "Name": "Console" } ] } } ]
  },
  "Umbraco": {
    "CMS": {
      "Content": { "MacroErrors": "Throw" },
      "Hosting": { "Debug": true }
    }
  }
}
```

### What each setting is for

| Setting | Purpose |
|---------|---------|
| `ConnectionStrings:umbracoDbDSN` | Database connection. The example uses Windows authentication against SQL Express. |
| `Umbraco:CMS:Unattended` | Creates the first admin user on a blank database. You can remove the user/email/password values after the first boot. |
| `Umbraco:CMS:Global:Smtp` | Outgoing email (alumni sign-up, memoir and approval emails). If it is missing, Umbraco silently skips sending. It does not throw an error. |
| `Umbraco:CMS:Imaging:HMACSecretKey` | Signs image-crop URLs. Any long random string. |
| `ActiveDirectory:Domain` | Optional. The on-premises AD domain for backoffice password checks. If it is empty, or AD is unreachable, login falls back to the normal Umbraco password. Local machines normally leave it empty. |

**Production:** prefer environment variables over plain-text secrets on disk, for example `Umbraco__CMS__Global__Smtp__Password=...` and `ConnectionStrings__umbracoDbDSN=...`.

---

## 3. CMS content and cloning this site for another school

### How the site is structured

- **Home node** (document type `site`) is the root of the site. It also holds the site-wide settings: `pageTitle`, `metaDescription`, `siteLogo`, `socialShareImage`, `socialLinks`, `topBarLinks`, `showTopBar`, `additionalNavBarLinks`, `footerText`, plus its own `widgets`.
- **Sections** sit under Home: About, Academics, Athletics, Contact, Guidance, Newsroom, Parents, Students.
- **Pages** (document type `page`) are built from **widgets** (block-list elements) in the `blocks`, `headerBlocks` and `sidebarBlocks` properties. Each widget renders through `Views/Partials/Widgets/<elementAlias>.cshtml`.
- **Link pages** (`linkPage`) are menu entries that point somewhere else (an external URL or a PDF).
- **Members** store alumni directory and memoir data. Real personal data lives in the database, never in uSync or git.

**Navigation:** every page shows in the main menu and breadcrumbs unless **"Hide from Navigation" (`umbracoNaviHide`) is ticked**. This is the standard Umbraco convention. See `Views/Partials/Shared/header.cshtml` and `breadcrumb.cshtml`.

**Sidebar:** put the sidebar widget on a section landing page once. Child pages inherit it automatically.

### Project layout

| Path | What is there |
|------|---------------|
| `Core/` | C# code: controllers, composers, services, alumni and calendar features, AD login |
| `Views/` | Razor templates. `Views/Partials/Widgets/` holds one partial per widget. `Views/Partials/Shared/` holds header, footer and metadata. |
| `wwwroot/css/styles.css` | Brand colors and fonts (CSS variables in `:root`) |
| `wwwroot/css/Widgets_CSS/` | One stylesheet per widget |
| `wwwroot/assets/` | Theme assets: images, fonts, vendor JS/CSS |
| `umbraco/models/` | Generated ModelsBuilder classes. **Commit these** when document types change. |
| `uSync/v17/` | Schema and content as files (see below) |
| `tests/` | xUnit tests |

### Cloning checklist for a new school

1. **Copy the repo** into a new repository for the new school, and create a new, empty database (Section 1, step 2).

2. **Choose how much content to keep.**
   - *Option A (recommended): schema only.* Before the first import, delete everything in `uSync/v17/Content/` and `uSync/v17/Media/`. Keep the other folders. Then import. You get every document type, widget and template, with an empty content tree. Create a new Home node (type `site`, template `home`) and build pages from there.
   - *Option B: start from Santiago's pages.* Import everything, then edit or delete pages in the backoffice. This is faster for page layout, but you must replace every Santiago text, image and link.

3. **Set the site-wide settings** on the new Home node: page title, meta description, logo, social links, top bar links and footer text.

4. **Rebrand the CSS.** Change the values of the brand variables in `wwwroot/css/styles.css`:
   ```css
   --shs-purple: #4a1f7a;
   --shs-purple-light: #6b3fa0;
   --shs-purple-pale: #f3edfa;
   --shs-gold: #c9a227;
   --shs-gold-text: #8f6d00;   /* darker gold for text on white (contrast) */
   --shs-orange-text: #a85f00;
   ```
   Keep the variable *names*, because widgets reference them. Many widget files also contain hard-coded fallbacks such as `var(--shs-purple, #4a1f7a)`. Those fallbacks only apply if a variable is missing, so they are harmless, but a search and replace keeps things tidy. Check that new colors still pass WCAG AA contrast (4.5:1 for text).

5. **Replace the logos** in `wwwroot/assets/images/logo/`. If the new school uses different fonts, change the Google Fonts links in `Views/Master.cshtml`.

6. **Replace hard-coded Santiago text in code.** At the time of writing these are:

   | File | What to change |
   |------|----------------|
   | `Views/Partials/Shared/metadata.cshtml` | `siteName` constant |
   | `Views/Partials/Shared/footer.cshtml` | School name, copyright line, `santiago-footer` class |
   | `Views/home.cshtml` | "Follow Santiago HS:" label |
   | `Views/Partials/Widgets/staffDirectory.cshtml` | Letterhead logo path and school name (printed directory) |
   | `Core/Controllers/AlumniSurfaceController.cs` | `StaffNotificationEmail` and the `from:` addresses |
   | `Core/Notifications/AlumniApprovalEmailHandler.cs` | `from:` address, subject and body text |
   | `Core/Composers/CalendarFeedComposer.cs` | HTTP User-Agent string |

   To find anything new, search the code (not `uSync/`) for `santiago`, `SHS` and `santiagohs`.

7. **Update the configuration** for the new school: a new `Umbraco:CMS:Global:Id` GUID, database name, SMTP account and AD domain.

8. **Set the domain** on the server in the backoffice: right-click Home, then **Culture and Hostnames**.

9. **Export a clean uSync set.** Once the new content is in place, go to **Settings → uSync → Export** and commit the result. The new school's repo then holds only its own content.

### Adding a new widget (short version)

1. Create the element type in the backoffice (Settings → Document Types → Element Type). Compose with `baseContent` if it needs a heading and intro text.
2. Add it as a block in the **`[Block List] Widgets`** data type.
3. Create `Views/Partials/Widgets/<elementAlias>.cshtml`.
4. Create `wwwroot/css/Widgets_CSS/<elementAlias>.css` and add it to `Core/Bundling/StylesheetBundle.cs`.
5. Commit the uSync files and the regenerated `umbraco/models/` class.

Conventions: widget headings default to `<h2>`, because the Hero widget owns the page `<h1>`. Use the brand CSS variables, not raw hex values.

---

## 4. uSync

[uSync](https://docs.jumoo.co.uk/usync/) writes Umbraco's database items out as `.config` (XML) files, so they can live in git and move between machines and servers.

### What is in `uSync/v17/`

| Folder | Contents |
|--------|----------|
| `ContentTypes/`, `DataTypes/`, `MediaTypes/`, `MemberTypes/`, `Templates/`, `Languages/`, `RelationTypes/` | **Settings** (the schema) |
| `Content/` | Every page and its widget content |
| `Media/` | Media *records* only (name, folder, crop data). The actual files are in `wwwroot/media/`, which is not in git. |

### Current settings

This project uses the uSync defaults. There is no `uSync` section in `appsettings.json`.

| Setting | Value | Meaning |
|---------|-------|---------|
| `ExportOnSave` | `All` | Every save in the backoffice rewrites the matching `.config` file straight away. |
| `ImportAtStartup` | `None` | Restarting the site does **not** import. You must import by hand. |
| `ImportOnFirstBoot` | `false` | A blank database stays empty until you import. |

### Everyday workflow

**You changed something in the backoffice:**
1. Save or publish as normal. uSync updates the files in `uSync/v17/`.
2. Run `git status`, review the changed `.config` files, and commit them with your code changes.
3. If you changed a document type, also commit the regenerated file in `umbraco/models/`.

**You pulled someone else's changes:**
1. Open **Settings → uSync** in the backoffice.
2. Click **Report** to preview what will change.
3. Click **Import**. Use **Import (Force)** only if a normal import skips items you know have changed.

**Setting up a new database:** run a full **Import** once (Section 1, step 7). uSync imports settings before content, so one click is enough.

**Deploying to a server:** copy the `uSync/` folder with the code, then run **Import** in that server's backoffice. Copy new files in `wwwroot/media/` too.

### uSync gotchas

- **Reload the uSync dashboard before importing** if you edited a `.config` by hand. Otherwise it can replay an out-of-date report and change nothing.
- **Never delete a document type that has content.** Umbraco deletes all its pages too. ExportOnSave then overwrites every matching `.config` with a delete marker (`<Empty Change="Delete">`). To recover, restore the folder from git (`git checkout -- uSync/`), restart, then run **Import (Force)**.
- **Changing a page's document type through uSync does not work.** Umbraco ignores an edited `<ContentType>` on import. Delete the page in the backoffice, restore the new `.config`, then import so uSync creates it fresh.
- **One file per page.** uSync names files after the page name. A rename, or a page created twice, can leave two `.config` files that share one `Key`. After an import or save, check `git status` and delete the stale duplicate.
- **Avoid a bare `&` in plain-text fields** (headings, labels). The save process turns it into `&amp;`, and the page then shows the literal text `&amp;`. Write "and" instead.
- **Formatting churn is normal.** After a backoffice save, uSync rewrites the JSON inside a `.config` in its own format. A diff that is only whitespace is not a real change.
