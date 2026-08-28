# MVP Scope and Feature Breakdown

## 1. Release objective

Deliver a trustworthy, bilingual self-service journey in which a Sri Lankan student can discover a verified in-scope German Master's programme, assess published requirements, organise their own preparation, receive reminders, and record the application they personally submit.

## 2. MVP feature slices

| Slice | MVP acceptance boundary |
|---|---|
| Public site | English/Tamil landing, service limitations, privacy/terms, programme browse/detail, official source and verification date visible. |
| Authentication | Email registration/verification, sign-in/out, reset, lockout, session handling; administrator MFA and role policies. |
| Onboarding/profile | Structured academic, language, experience, and preference data; unknown values; completion indicator; edit/review. |
| Catalogue admin | University/programme drafts, controlled disciplines, intakes, routes, verified sources, requirement/deadline authoring, review/publish/archive. |
| Search | Server-side keyword/facet search for published in-scope programmes, sorting, paging, empty/error states, stale/unknown labels. |
| Saved courses | Authenticated save/unsave/list with uniqueness and ownership enforcement. |
| Eligibility estimate | Deterministic rules, three outcome categories, profile gaps, per-rule explanations and citations, persisted versioned evaluation, disclaimer. |
| Checklist | Create checklist per tracked application; provenance labels; item status/history. File upload is conditional, not required for the first usable release. |
| Application tracker | Create from programme; channel/intake/deadline/reference; status timeline, bounded notes, official handoff link; no submission automation. |
| Reminders | Verified application-deadline reminders, email plus in-app inbox, opt-in/preferences, retries/deduplication/delivery state. SMS/push are deferred. |
| Privacy | Versioned notices/consents, preference withdrawal, account data export/delete request, retention workflow. |
| Audit | Append-only events for privileged/content/privacy/auth/document activity; restricted query and redaction. |
| Admin operations | Stale-source queue, review separation, basic notification/privacy operational views. |

## 3. Critical end-to-end journeys

1. **Discover:** visitor selects Tamil, filters the catalogue, opens a programme, and verifies its official source/date.
2. **Assess:** verified student completes profile, runs an estimate, sees potential matches/gaps/unknowns and the evidence for every rule.
3. **Prepare and apply:** student saves a programme, creates a tracker/checklist, follows the official link, submits personally, and records that status.
4. **Stay on schedule:** opted-in student receives a non-duplicated reminder that links back to the current programme/deadline record and official source.
5. **Publish safely:** author transcribes official information; a different reviewer validates provenance and publishes; stale content is flagged.
6. **Exercise privacy rights:** student views consent history and starts an export or deletion request with identity confirmation and trackable completion.

## 4. Definition of done for MVP

- All critical journeys work in supported desktop/mobile browsers in both languages.
- No programme can be published without valid official-source metadata and verification date.
- Eligibility outputs are reproducible, explainable, source-linked, and never phrased as admission/visa decisions.
- Authorisation, ownership, rate limiting, audit, backup/restore, monitoring, incident response, and privacy workflows meet the security requirements.
- Accessibility review finds no known critical WCAG 2.2 AA blockers.
- Automated unit/integration/component/contract/end-to-end tests protect critical paths; CI security checks pass.
- Seed/launch catalogue has a named owner and completed two-person verification.
- Production runbooks, recovery drill, data map, retention schedule, and launch threat model are approved.

## 5. Deferred after MVP

- AI-based recommendations or requirement extraction (any later extraction must be human-reviewed before publication).
- Native mobile applications, SMS/push/WhatsApp reminders, social/community features, consultant marketplace, payments, and premium agency services.
- Automated authenticated portal integration, application submission, or admission/visa prediction.
- Additional countries, study levels, teaching languages, and subject areas.
- Dedicated search cluster, microservices, advanced analytics/experimentation, and institution partner APIs until justified.
- File storage if metadata-only checklists meet initial needs; if enabled later it must pass the document security gate.

## 6. Scope controls and content policy

- A programme is in scope only when its award level, German institution, English teaching status, and subject classification are verified.
- Marketing copy and filters must not imply completeness of the German market.
- Ambiguous requirements are displayed verbatim only as a short, attributed fact or summarised conservatively within copyright limits; they are not converted to executable rules until reviewed.
- Stale data remains visibly labelled or is unpublished according to an agreed freshness policy. A deadline reminder must never silently rely on superseded data.
- Feature requests involving submission, credentials, sensitive document use, or automated decisions require architecture, security, privacy, and product review.

