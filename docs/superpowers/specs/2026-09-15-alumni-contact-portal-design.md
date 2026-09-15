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
| Email | *(built-in Member email)* | Private | never rendered to the page |
| Phone | `phone` | Private | |
| Address | `address` | Private | |
| Gender | `gender` | Private | |
| `IsApproved` | *(built-in)* | System | false until staff approves; import script sets it true directly |

The public-facing widget and API only ever read the public fields plus a
Member Id (to route a contact request). Private fields are touched only by
the contact-relay controller and the signup/import code paths, server-side.

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

1. Read the CSV/Excel export the user already copied from the live site.
2. Map each row's columns to the `alumniMember` fields above.
3. Create a Member per row via `MemberService`, with `IsApproved = true`
   (these are already-known real members, not public submissions).
4. Run once locally against a handful of sample rows first, verify the
   result in the backoffice Members list, then run against the full file.

Exact column names need confirming against the real spreadsheet before
this script is written — the user has described the fields but the script
should map real header names, not assume order.

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

1. "Send a message" button opens a small form: sender name, sender email,
   message, plus a hidden honeypot field.
2. Posts to `AlumniSurfaceController.SendMessage(memberId, senderName, senderEmail, message)`.
3. Controller re-checks the honeypot and rate limit, validates input.
4. Loads the target Member server-side via `MemberService`, reads its
   private email.
5. Sends via the district SMTP relay: `To` = target's email, `Reply-To` =
   sender's email, `From` = a fixed no-reply address.
6. Returns a small partial (success or a generic failure message) swapped
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

## Open questions before implementation

- Exact spreadsheet column headers (to write the import mapping).
- District SMTP host/credentials from IT — the site has no mail config
  today.
- Where exactly the new browse/signup pages sit in the content tree /
  navigation (a sensible default: children of the existing Alumni
  Directory page).
