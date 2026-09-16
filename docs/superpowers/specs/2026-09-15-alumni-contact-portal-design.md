# Alumni Contact Portal — design

Date: 2026-09-15
Status: approved for planning

## Goal

Let alumni browse a public directory of other alumni and send them a
message, without either side ever seeing the other's email address. Also
bring in the existing member list the user already copied from the live
site, as a one-time import.

## Background

The current "Alumni Directory" page (`uSync/v17/Content/alumni-directory.config`,
`/contact/alumni-directory/`) is a landing page of Link Cards pointing at
external tools (Register Yourself, Alumni Calendar, Post a Memoir, Read
Memoirs). Those links are `#` placeholders today — the user asked for no
redirects to the live site until real destinations exist. There is no
roster of individual alumni anywhere in this Umbraco site, and no
email-sending infrastructure (no SMTP config, no mail service; the only
`SurfaceController` in the repo is an unrelated network-switch script
generator).

This design adds that roster, a public browse/search view, a contact-relay
form, a public self-service signup form, and a one-time bulk import of the
spreadsheet the user already copied from the live site.

## Decisions

| Question | Decision |
|---|---|
| Where alumni records live | Umbraco **Members** (the built-in visitor-account entity), used purely as a structured data store — no public login is exposed. |
| Why not uSync content nodes | uSync exports content as XML committed to git. 200–1,000 personal records (email, phone, address) must never enter source control or git history. Members live in the database only; uSync does not touch them. |
| Why not a custom DB table | Members give a free backoffice list, search, and an `IsApproved` moderation flag out of the box — a custom table would need a hand-built admin UI for the same thing. |
| Public access | No login required to browse or to send a message. Spam is handled by a honeypot, rate limiting, and signup moderation — not by accounts. |
| New signups | Created with `IsApproved = false`. Staff approves in the existing backoffice Members section — no new UI to build. |
| Bulk import of the copied spreadsheet | A one-time local console script, run once by a developer, creating Members with `IsApproved = true` directly. Not part of the deployed site. |
| Email sending | District SMTP / Google Workspace relay (`@ggusd.us`). Needs SMTP credentials or an app password from IT before implementation. |
| Contact relay | Sender's message is emailed to the target's stored address server-side, `Reply-To` set to the sender's own email, `From` a fixed no-reply address. The target's email never reaches the browser, a log line, or an HTTP response. |
| Search scale | 200–1,000 rows — in-memory filtering is enough. No search index needed. |

## Data model — Member Type `alumniMember`

| Field | Alias | Visibility | Notes |
|---|---|---|---|
| First / Last Name | `firstName` / `lastName` | Public | |
| Former Last Name | `formerLastName` | Public | maiden/prior name |
| Grad Year | `gradYear` | Public | |
| Industry | `industry` | Public | |
| Profession | `profession` | Public | |
| Update (short bio) | `update` | Public | free text |
| Homepage URL | `homepageUrl` | Public | |
| Email | *(built-in Member email)* | Private | never rendered to the page. Import uses spreadsheet "Email 1"; "Email 2" is dropped. |
| Phone | `phone` | Private | Import uses "Phone 1"; "Phone 2" is dropped. |
| Address | `address` | Private | single free-text field, combining Street/City/State/Zip/Country from the import |
| Gender | `gender` | Private | |
| `emailingOk` | `emailingOk` | Private | from the spreadsheet's "Emailing is OK" column, default `true` for new signups. When false, the directory card still shows but the "Send a message" button is hidden/disabled — see Contact-relay flow. |
| `legacyRecId` | `legacyRecId` | Private | the spreadsheet's REC_ID, stored so re-running the import is idempotent (skip rows whose REC_ID already exists). Not set for new self-service signups. |
| `IsApproved` | *(built-in)* | System | false until staff approves; import script sets it true directly |

The public-facing widget and API only ever read the public fields plus a
Member Id (to route a contact request) and `emailingOk` (to decide whether
to show the contact button). Private fields are touched only by the
contact-relay controller and the signup/import code paths, server-side.

Spreadsheet columns not imported: Title, Middle Name, Record Date (not
part of the approved field set — dropped rather than added as new Member
properties, per YAGNI).

## Signup flow

1. Public form on a new page (or a section of the existing Alumni landing
   page): all fields above, submitted by the alumnus themself.
2. Posts to `AlumniSurfaceController.SignUp`.
3. Controller validates input, checks the honeypot and rate limit, creates
   a Member via `MemberService` with `IsApproved = false`.
4. Best-effort email to staff with a moderation notice. If that email
   fails, the Member is still created — staff pick it up on their next
   regular check of the Members section.
5. Submitter sees a "thanks, pending review" confirmation.

## Bulk import (one-time)

A small local console script (or a throwaway top-level program), not part
of the deployed site:

Source file: `AlumniDirectory.xls`, 800 rows, headers confirmed as REC_ID,
Record Date, Graduation Year, Title, First Name, Middle Name, Last Name,
Former Last Name, Gender, Email 1, Email 2, Homepage, Phone 1, Phone 2,
Street, City, State, Zip, Country, Industry, Profession, Other
information, Emailing is OK.

1. Read the `.xls` file (legacy Excel binary format — needs a library that
   handles it, e.g. `ExcelDataReader`, not just `System.Text.Csv`).
2. Map each row to the `alumniMember` fields per the table above
   (Graduation Year → `gradYear`, Other information → `update`, Street/
   City/State/Zip/Country joined → `address`, Emailing is OK → `emailingOk`,
   REC_ID → `legacyRecId`; Title/Middle Name/Record Date dropped).
3. Skip rows whose `legacyRecId` already exists as a Member (idempotent
   re-runs).
4. Create a Member per new row via `MemberService`, with `IsApproved = true`
   (these are already-known real members, not public submissions).
5. Run once locally against a handful of sample rows first, verify the
   result in the backoffice Members list, then run against the full file.

## Browse / search widget

- New widget `Views/Partials/Widgets/AlumniDirectory.cshtml` — a
  feed-style widget like `eventsWidget`, reading from `MemberService`
  directly rather than iterating Block List content (same precedent as the
  Athletics Schedule / Events widgets that read an external feed).
- Query params `?q=` (name search) and `?year=` (grad year filter),
  filtered server-side with in-memory LINQ over approved Members.
- Paginated grid of cards: name, former last name, grad year, industry,
  profession, update, homepage link, and a "Send a message" button
  carrying only the Member Id.
- The existing Alumni landing page's placeholder "Alumni Directory" /
  "Register Yourself" Link Card entries get pointed at this new browse
  page and the signup form, closing out that open placeholder from the
  earlier build (see `[[alumni-directory-page]]` memory).

## Contact-relay flow

1. The public card shows "Send a message" only when the target's
   `emailingOk` is true; otherwise no contact button renders at all (the
   card still shows name/grad year/etc.). This is a display-time check in
   the widget, not just a controller-side guard.
2. "Send a message" button opens a small form: sender name, sender email,
   message, plus a hidden honeypot field.
3. Posts to `AlumniSurfaceController.SendMessage(memberId, senderName, senderEmail, message)`.
4. Controller re-checks the honeypot, rate limit, and `emailingOk` (never
   trust the client — a direct POST could otherwise bypass a hidden
   button), then validates input.
5. Loads the target Member server-side via `MemberService`, reads its
   private email.
6. Sends via the district SMTP relay: `To` = target's email, `Reply-To` =
   sender's email, `From` = a fixed no-reply address.
7. Returns a small partial (success or a generic failure message) swapped
   into the form via fetch — no page reload, no personal data in the
   response.

## Spam guards and error handling

- **Honeypot**: a hidden field on both forms, named to look real (e.g.
  `website`). A filled honeypot silently drops the submission — no error
  shown, so bots don't learn to avoid it.
- **Rate limiting**: per-IP caps (e.g. 5 contact messages / 3 signups per
  hour) via ASP.NET Core's built-in rate-limiting middleware — no new
  dependency.
- **Validation**: email format, message length caps, required fields.
  Rejected requests get a generic 400 — never confirming or denying
  whether a given name or Id exists.
- **SMTP failure**: logged server-side (Umbraco's own logger, no personal
  data beyond what the sender themself typed), sender sees a generic "try
  again later" — never raw SMTP errors.

## Testing

- Unit tests for the honeypot / rate-limit / validation logic in the
  controller, with a fake `IEmailSender` — no live SMTP calls in tests.
- Assert `To` / `Reply-To` / body are built correctly from a fake sender
  and Member.
- Manual test: run the import script against a handful of sample CSV rows
  locally before running it against the real file.

## Page placement

The Browse and Sign Up pages are new children of the existing Alumni
Directory page (`/contact/alumni-directory/…`), alongside its current
Link Cards. The placeholder "Alumni Directory" and "Register Yourself"
Link Card entries point at these two new pages instead of `#`.

## Open questions before implementation

- District SMTP host/credentials from IT — the site has no mail config
  today. Implementation can proceed with the relay code written against
  an `IEmailSender` abstraction; the real SMTP settings slot into
  configuration once IT provides them, without touching the relay logic.
