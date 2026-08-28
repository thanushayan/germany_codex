# Proposed System Architecture

## 1. Architecture choice

Build the MVP as a **modular monolith** with a React single-page application, an ASP.NET Core Web API, PostgreSQL, and a separately hosted background worker. This keeps deployment and transactions simple while preserving module boundaries that can be extracted later if scale or ownership warrants it.

```text
Browser (React + TypeScript + Vite + Tailwind)
          |
       HTTPS
          v
Reverse proxy / WAF / rate limiting
          |
          v
ASP.NET Core Web API (modular monolith)
  |-- Identity & Access       |-- Catalogue & Sources
  |-- Student Profiles        |-- Search
  |-- Eligibility Rules       |-- Saved Courses
  |-- Applications/Checklist  |-- Consent & Privacy
  |-- Admin/Publishing        |-- Audit
          |                         |
          +-----------+-------------+
                      v
                 PostgreSQL
                      ^
                      |
Background worker ----+---- notification provider
          |
          +---- private object storage + malware scanner (if uploads enabled)
```

## 2. Runtime components

### Web client

- React, TypeScript, Vite, Tailwind CSS, a router, typed API client, and an i18n library.
- Treat the browser as untrusted: it provides UX validation but no authorisation or eligibility authority.
- Store session material in secure, HttpOnly, SameSite cookies where feasible; do not persist bearer tokens in local storage.
- Lazy-load routes, use an error boundary, and include English/Tamil translation resources.

### API

- ASP.NET Core Web API on a supported .NET LTS release, with endpoint groups/controllers mapped to application modules.
- ASP.NET Identity for local account lifecycle, roles, MFA, lockout, and secure token generation.
- Entity Framework Core with PostgreSQL migrations and optimistic concurrency tokens on mutable administrative records.
- Layer each module into API, application/use-case, domain, and infrastructure concerns. Modules may share platform primitives but must not bypass another module's application interface.
- Publish an OpenAPI contract and use RFC 9457-style problem details for errors.

### Worker

- Executes durable jobs for reminders, stale-source checks, exports/deletions, and (if enabled) document scanning.
- Uses a PostgreSQL-backed outbox/job mechanism for MVP, with idempotency keys, retries with backoff, a dead-letter state, and operational visibility.
- Never sends a reminder directly inside a user request transaction.

### Data stores

- PostgreSQL is the system of record. Use separate schemas or explicit naming by module and least-privilege database roles for migrations versus runtime.
- Use PostgreSQL full-text/trigram search initially; introduce a dedicated search engine only after measured need.
- If uploads are approved, store encrypted blobs in private S3-compatible object storage. PostgreSQL stores metadata and opaque object keys only. Public catalogue media uses a separate bucket/domain.
- Keep secrets in a managed secrets service in production, never source control or images.

## 3. Key architectural flows

### Catalogue publishing and provenance

1. An author creates a draft university/programme version and adds requirement/deadline facts.
2. Every fact links to an official `SourceReference` and records verification metadata.
3. Validation blocks incomplete provenance and invalid date/context combinations.
4. A different reviewer approves and publishes an immutable version.
5. Search reads only the current published version. Revisions create new versions; existing eligibility evaluations retain their version snapshot.

An official source is a university-controlled page/document or the relevant official application service. Search snippets, aggregators, agency sites, and generated text are not evidence.

### Eligibility estimation

1. The API loads the published programme version and its typed, versioned rules.
2. It creates a snapshot of only relevant student profile inputs.
3. A deterministic evaluator handles supported operators (for example threshold, set membership, required evidence) and three-valued outcomes: pass, potential gap, unknown.
4. Aggregation maps results to `potentially_aligned`, `potential_gaps`, or `insufficient_information`.
5. The result persists input/rule versions, per-rule explanations, official sources, timestamp, and disclaimer acknowledgement.

Free-form AI is not in this decision path. Unsupported or ambiguous requirements remain human-readable and produce unknown rather than an invented rule.

### External application handoff

The application record exposes the verified official destination as a normal external navigation. It does not proxy credentials or submit forms. The student confirms any tracker status change after acting on the official service.

### Notification delivery

A scheduler selects opted-in reminders, checks the current deadline/version and user timezone/preferences, writes an outbox item transactionally, and a worker sends it. Delivery and suppression are recorded; duplicate delivery is prevented by a unique idempotency key.

## 4. Proposed repository structure

```text
/
|-- frontend/                        # React/Vite application
|   `-- src/
|       |-- app/                     # composition, routes, providers
|       |-- features/                # auth, catalogue, profile, etc.
|       |-- components/              # reusable accessible UI
|       |-- api/                     # generated/typed client
|       |-- i18n/{en,ta}/
|       `-- test/
|-- backend/
|   |-- src/                         # API/worker hosts and business modules
|   |-- tests/                       # unit, integration, architecture, contract, E2E
|   `-- GermanyApplications.slnx
|-- docs/
|   |-- adr/                         # architecture decision records
|   |-- runbooks/
|   `-- threat-models/
|-- infrastructure/                 # Docker Compose and deployment configuration
|-- scripts/
|-- .github/workflows/
`-- README.md
```

Feature folders in the client align with API modules. Backend projects should be introduced only where they enforce a boundary; avoid a project per trivial type.

## 5. Database entity list

All mutable entities use UUID identifiers, UTC audit timestamps, and concurrency/version fields where relevant. Personal data is not copied into audit text.

| Module | Entities | Notes |
|---|---|---|
| Identity | `User`, `Role`, `UserRole`, `IdentitySession`, `MfaMethod` | Identity tables are based on ASP.NET Identity; session/MFA representation depends on chosen auth design. |
| Profile | `StudentProfile`, `AcademicQualification`, `LanguageQualification`, `WorkExperience` | Structured values include grading system/unit and unknown state; every student-owned row retains `UserId`. |
| Catalogue | `University`, `Course`, `CourseVersion`, `CourseIntake`, `ApplicationRoute` | Public reads target an approved numbered course version. |
| Provenance | `SourceReference`, `CourseRequirement`, `Deadline`, `DocumentRequirement` | Requirements/deadlines link to official sources; requirement values are typed and normalised. |
| Saved | `SavedCourse` | Unique per user/course. |
| Eligibility | `EligibilityEvaluation`, `EligibilityInputSnapshot`, `EligibilityRuleResult`, `DisclaimerAcknowledgement` | Retains explainability and exact programme/rule version. |
| Applications | `StudentApplication`, `ApplicationStatusHistory` | Status history is append-only; external references must never contain passwords. |
| Checklist/Documents | `DocumentRequirement`, `StudentDocument` | `StudentDocument` stores metadata only at this stage; no file bytes are stored. |
| Notifications | `Notification` | Template keys, scheduling, ownership, status, and idempotency are first-class. |
| Privacy | `ConsentRecord` | Consent type, policy version, grant/withdraw event, locale, and timestamp are immutable history. |
| Operations | `SupportTicket`, `AuditLog` | Tickets are student-owned; audit metadata is append-only and redacted. |

Final columns, relationships, deletion behaviour, and indexes require domain workshops and data-protection review. Likely indexes include programme publication/filter fields, unique source/version constraints, application owner/status, deadline scheduling, outbox status, and audit time/actor/target.

## 6. API module list and representative endpoints

Version the public contract under `/api/v1`; exact routes are confirmed during contract design.

| Module | Representative operations |
|---|---|
| Identity | register, verify email, sign in/out, reset password, MFA/session management |
| Profile | get/update profile, manage degrees/language/work experience, completion summary |
| Catalogue | list/get published universities and programmes, facets, official sources |
| Saved courses | list/save/remove programmes |
| Eligibility | create evaluation, retrieve explanation/history, acknowledge disclaimer |
| Applications | create/list/get/update owned applications, append status, manage notes |
| Checklists | generate/read checklist, update owned item status |
| Documents | initiate upload, complete scan, authorised short-lived download, delete (conditional scope) |
| Notifications | get/update preferences, list reminders, test/disable reminders |
| Privacy | current notices, record/withdraw consent, request/export/delete account data |
| Admin catalogue | draft/version/edit, attach sources, submit for review, approve/reject/publish/archive |
| Admin operations | stale-verification queue, notification failures, privacy-request workflow |
| Audit | restricted event search/export with reason and access auditing |

Every owned-resource endpoint derives the owner from authenticated identity, never a trusted client-supplied user ID. Admin endpoints use policy-based authorisation and step-up authentication for high-risk operations.

## 7. Cross-cutting design decisions

- **Validation:** server-side allowlists, bounded text, canonical enums, safe URLs, file signature checks, and consistent problem responses.
- **Concurrency:** ETags or row-version tokens for administrative editing and application updates.
- **Idempotency:** required for job production, notification delivery, and retryable state-changing operations where duplicates are harmful.
- **Caching:** cache only public, published catalogue projections. Never place authenticated/private responses in shared caches.
- **Deletion:** programme provenance is archived rather than hard-deleted; user deletion is an orchestrated, auditable process subject to legal retention decisions.
- **Time:** timestamps use UTC; deadlines preserve the authoritative local date/time, timezone, precision, and applicant context.
