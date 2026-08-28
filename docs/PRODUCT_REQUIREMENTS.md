# Product Requirements

## 1. Product vision

The product is a self-service web platform that helps Sri Lankan students discover suitable English-taught Master's programmes in Germany and organise their applications. It reduces dependence on education agencies while keeping the student in control of every official submission.

The platform provides guidance and workflow support only. It must never guarantee admission or visa approval, make a final eligibility decision, submit an application for a student, or collect credentials for a university or Uni-assist account.

## 2. MVP users and outcomes

### Student

A Sri Lankan applicant who wants to:

- use the service in English or Tamil;
- create a reusable academic profile;
- find verified Master's programmes in IT, Computer Science, Data Science, AI, Software Engineering, Business Analytics, and Engineering;
- understand an explainable, rule-based estimate of alignment with published requirements;
- save programmes and track documents, deadlines, and application progress; and
- follow a verified link and personally complete the application on the official university or Uni-assist website.

### Administrator/content reviewer

An authorised operator who wants to:

- maintain universities, programmes, requirement rules, deadlines, and official sources;
- review and publish course records through a controlled workflow;
- identify stale or incomplete records; and
- investigate security-sensitive activity through audit records.

### Privacy/support administrator

An authorised operator who handles consent history, data access/export/deletion requests, and limited support operations without gaining unrestricted access to sensitive documents.

## 3. Product principles and non-negotiable constraints

1. **Official-source truth:** every published programme has at least one official university or Uni-assist source URL and a `last verified` date. Requirements are transcribed only from official sources; unknown data is shown as unknown, never inferred.
2. **No deterministic promise:** eligibility output is an estimate with an explanation, source references, unmet/unknown criteria, and a prominent disclaimer. Rules cannot return “admitted” or “visa approved.”
3. **Human-controlled submission:** the final action is an external link. The student approves and performs submission outside this platform.
4. **No third-party passwords:** the system neither requests nor stores university, Hochschulstart, embassy, or Uni-assist passwords.
5. **Privacy by design:** collect the minimum necessary data, separate sensitive documents from public assets, and enforce purpose-based access and retention.
6. **Bilingual parity:** critical journeys, notices, validation, and transactional notification templates support English and Tamil.
7. **Traceability:** important administrative, consent, authentication, document-access, and rule/publication actions are auditable.

## 4. Functional requirements

### Public website

- Explain the service, limits, pricing (if any), privacy practices, and self-service model.
- Browse/search published programme summaries without authentication.
- Display the official source, last-verified date, application channel, deadline status, and a stale-data warning where relevant.
- Provide English/Tamil language selection with persistent preference.

### Authentication and account management

- Register, verify email, sign in/out, reset password, and manage active sessions.
- Use ASP.NET Identity password and lockout controls; administrators require MFA.
- Record acceptance of current Terms and Privacy Notice versions.
- Support account export and deletion-request workflows.

### Student onboarding and profile

- Capture only fields required for filtering/checking: identity/contact basics, citizenship/residency, degree, institution, subject, grading system/result, credits where known, graduation status/date, English test status/scores, work experience, and study interests.
- Make optional and required fields explicit and permit “unknown/not yet available.”
- Show completion status and allow review/update.

### Programme catalogue and search

- Filter by discipline, intake, application status/deadline, tuition/semester fee (when verified), institution, location, application channel, language/test requirement, degree/result prerequisites, and other verified facets.
- Clearly distinguish missing/unknown values from “no requirement.”
- Offer pagination and stable sort options; save/unsave a programme.

### Estimated eligibility

- Evaluate versioned, deterministic rules built from verified published requirements against a snapshot of the student's supplied profile.
- Return one of: `potentially_aligned`, `potential_gaps`, or `insufficient_information`—never a final decision.
- Explain each evaluated rule, student value, result, unknown data, source, rule/version, and evaluation time.
- Require acknowledgement that the estimate is guidance and direct the student to recheck official information.

### Documents and checklist

- Generate a programme/application-specific checklist from verified requirements and common administrative items, clearly labelling the origin of each item.
- Track not-started, preparing, ready, submitted, and not-applicable states.
- If file upload is included, validate type/size, malware-scan, encrypt, keep private, and deliver only via short-lived authorised download. The MVP may launch with metadata-only checklist tracking (see unresolved decisions).

### Application tracker

- Let the student create an application from a programme, record external reference (not a password), application channel, intended intake, milestones, notes, and status.
- Suggested statuses: planning, preparing, ready-to-submit, submitted-by-student, additional-information-requested, decision-received, offer, rejected, withdrawn.
- Record status history and make explicit that “submitted” is the student's declaration, not platform confirmation.

### Deadlines and notifications

- Store deadline type, intake, applicant category/context, timezone/date semantics, source, and verification date.
- Allow opt-in reminders with user-configurable channels/timing.
- Recheck current published data when generating reminders; display that official sources prevail.
- Record delivery outcome and support unsubscribe/preferences.

### Administration

- Role-based access for content authors, reviewers, support/privacy operators, and security administrators.
- Draft/review/publish/archive programme content with four-eyes approval for requirements and deadlines.
- Prevent publication without official source and verification date.
- Flag records as stale after a configurable review interval and exclude/label expired deadlines.
- Provide audit search, without exposing document contents or secrets in audit events.

## 5. Non-functional requirements

- **Availability/recovery:** stateless API replicas, health checks, automated PostgreSQL backups, restore drills, and documented recovery objectives before production.
- **Performance:** target p95 API latency below 500 ms for ordinary catalogue/profile operations under an agreed MVP load; use indexed server-side filtering and pagination.
- **Accessibility:** target WCAG 2.2 AA, keyboard operation, visible focus, semantic markup, screen-reader labels, and language/direction-safe layouts.
- **Internationalisation:** all user-visible strings are translation keys; use locale-aware dates/numbers and retain canonical UTC timestamps. Deadline date/timezone meaning must be explicit.
- **Observability:** structured logs, metrics, traces/correlation IDs, health/readiness endpoints, alerting, and redaction of personal/sensitive data.
- **Maintainability:** modular monolith boundaries, API contracts, migrations, automated tests, linting, formatting, dependency scanning, and reproducible containers.
- **Data quality:** publish gates, change history, provenance, stale-data reporting, and an operational verification queue.

## 6. Success measures

- Percentage of published programmes with a reachable official source and in-policy verification date (target 100%).
- Search-to-save and save-to-tracked-application conversion.
- Profile/checklist completion and deadline reminder opt-in/delivery rates.
- Number and age of stale programme records.
- Eligibility estimates with full rule/source explanations (target 100%).
- Accessibility, translation completeness, security incident, and privacy-request SLA indicators.
- Student-reported usefulness; no metric should reward overstating eligibility or admission likelihood.

## 7. Explicitly out of scope for MVP

- Bachelor's/PhD programmes or destinations outside Germany.
- Programmes outside the named English-taught subject areas.
- Admission/visa guarantees, probabilistic admission scoring, or AI final decisions.
- Applying, paying, signing, or uploading to university/Uni-assist on the student's behalf.
- Storing third-party credentials or scraping authenticated portals.
- Agency/consultant marketplace, visa case management, accommodation, loans, and automated document authorship.

