# Development Plan and Testing Strategy

## 1. Delivery approach

Use short vertical increments behind feature flags. Each increment includes bilingual UX, accessibility, API/domain/data work, security controls, telemetry, tests, documentation, and an operational owner. Do not populate requirements from unverified or generated sources.

Planning documents precede application code. Architecture decisions with material trade-offs are recorded in `docs/adr/`, and threat models/runbooks evolve with implementation.

## 2. Phases

### Phase 0 — Discovery, governance, and decisions

- Validate journeys with Sri Lankan students and content administrators.
- Confirm legal entities, target markets, hosting/data regions, privacy roles, lawful bases, transfer mechanism, retention, and accessibility obligations with counsel/specialists.
- Define the official-source policy, subject taxonomy, bilingual glossary, programme freshness SLA, initial catalogue size, and content ownership.
- Decide authentication/session model, document-upload MVP scope, notification provider, production platform, recovery objectives, and support model.
- Produce context/data-flow diagrams, threat model, DPIA screening, data inventory, ADRs, release quality gates, and measurable load assumptions.

**Exit:** unresolved launch blockers have owners/dates; source and privacy governance are approved.

### Phase 1 — Engineering foundation

- Scaffold the `backend/` .NET solution/module boundaries, `frontend/` React/Vite/Tailwind/i18n app, PostgreSQL/EF configuration, and `infrastructure/` Docker Compose developer dependencies.
- Scaffold repository, .NET solution/module boundaries, React/Vite/Tailwind/i18n app, PostgreSQL/EF migrations, and Docker Compose developer dependencies.
- Establish CI for formatting, linting, type checking, build, tests, migration checks, SAST/SCA/secret/container scans, and artefacts.
- Implement configuration validation, secrets integration contracts, structured redacted logging, correlation, metrics/traces, health endpoints, problem details, baseline headers/CSP and test fixtures.
- Create test builders, ephemeral PostgreSQL integration environment, accessibility tooling, translation checks, and production deployment skeleton.

**Exit:** reproducible local/CI build and deployable empty vertical slice; security baseline verified.

### Phase 2 — Identity, privacy, profile, and public shell

- Implement accessible bilingual navigation/public/legal pages and language persistence.
- Deliver registration, verification, sessions, recovery, admin MFA, roles/policies, consent versioning, and privacy preferences.
- Deliver structured student profile/onboarding with validation, unknown states, ownership enforcement, and audit coverage.
- Create export/deletion request state machine without prematurely automating legally exceptional cases.

**Exit:** a student can securely create and manage a profile; negative authorisation and privacy tests pass.

### Phase 3 — Verified catalogue and administration

- Implement university/programme/version/source/verification/requirement/deadline model and migrations.
- Build author/reviewer workflow with publication invariants, concurrency protection, audit, stale queue, archive, and official URL validation.
- Build public catalogue detail and initial curated data import/review tooling; complete human two-person verification.

**Exit:** a record cannot publish without provenance; published versions are stable and queryable; launch dataset is verified.

### Phase 4 — Search, saved courses, and eligibility

- Implement indexed server-side filtering, pagination/facets, Tamil/English presentation, save/unsave, and caching limited to public data.
- Define a small typed rule vocabulary and implement deterministic three-valued evaluation, immutable snapshots, explanation, source links, disclaimers, and regression fixtures.
- Conduct content/editor usability review to ensure ambiguous facts remain unknown rather than forced into rules.

**Exit:** discover-and-assess journey passes contract, accessibility, performance, security, and rule golden tests.

### Phase 5 — Applications, checklist, and notifications

- Implement owned application records, status history, external official handoff, bounded notes, checklist generation/provenance, and item history.
- Implement preferences, scheduler/outbox, idempotent worker, email/in-app templates in both languages, retries/dead-letter operations, deadline revalidation and minimal email content.
- Enable file upload only if separately approved security gate, scanning, encrypted storage, retention and deletion tests all pass.

**Exit:** prepare/apply/remind journey is reliable; student remains final submitter; failure/retry and timezone boundary tests pass.

### Phase 6 — Hardening, beta, and launch

- Complete WCAG audit, translation/content QA, browser/device testing, load/capacity testing, threat-model refresh, external penetration test, privacy review, backup restore, incident tabletop and disaster-recovery drill.
- Validate dashboards/alerts, stale catalogue operations, support/privacy workflows, runbooks, on-call, retention jobs, key rotation, and rollback.
- Run a limited beta with consented users, triage by severity, measure data quality/usefulness without admission-probability claims, and perform go/no-go review.

**Exit:** MVP definition of done and security launch acceptance are signed off; no unresolved critical/high issue.

## 3. Testing strategy

### Test pyramid and ownership

- **Backend unit (xUnit):** domain invariants, typed eligibility operators/aggregation, grading/unit handling, deadline semantics, permissions, state transitions, redaction, and retention decisions. Use table/property-style boundary cases.
- **Frontend unit/component (Vitest + React Testing Library):** forms, validation, language switching, source/stale/unknown labels, disclaimers, status controls, error/loading/empty states, keyboard/focus and accessible names. Test behaviour rather than implementation details.
- **Integration (xUnit + real PostgreSQL container):** EF mappings/migrations/constraints, Identity flows, ownership/policies, publication transaction, outbox/idempotency, concurrency, query/filter correctness, privacy deletion/export and audit production.
- **API contract:** OpenAPI validation, generated TypeScript client compatibility, error shape, pagination, localisation-independent enums, backward compatibility, and unauthorised/forbidden distinctions that do not leak resources.
- **End-to-end:** critical journeys in English and Tamil using a production-like stack. Include student-versus-admin separation, external handoff (without submission), reminder preference, consent withdrawal, and stale course behaviour.
- **Architecture tests:** module dependency rules, domain isolation from infrastructure, and no cross-module database shortcuts.

### Eligibility and data-quality testing

- Maintain reviewer-approved golden fixtures derived from official requirements, containing thresholds, equality boundaries, alternate conditions, missing inputs, unrecognised grading systems, contradictory/ambiguous facts, and version changes.
- Assert that unknown never becomes pass, no output uses guaranteed/final language, every evaluated criterion has an explanation/source, and old evaluations remain reproducible after publication changes.
- Validate every publishable course through automated constraints: official HTTPS source policy (with reviewed exceptions), verification date, reviewer separation, intake/deadline context, supported language/degree/scope, and no executable rule without evidence.
- Use scheduled link/freshness checks only to create review work; never automatically rewrite requirements from web content.

### Security and privacy testing

- SAST, SCA, secret scanning, SBOM, container and IaC scanning on CI; dynamic scanning against staging.
- Automated role/resource matrix with cross-user IDOR attempts, CSRF/CORS/cookie/header tests, rate-limit tests, injection/XSS/open-redirect/SSRF cases, session fixation/revocation and recovery abuse.
- Upload polyglots, forged MIME, oversize, malware test signatures, scan timeouts, unauthorised downloads, expired links and erasure/backup behaviour if documents are enabled.
- Verify logs/traces/analytics contain no tokens, passwords, document data, sensitive profile values, or unsafe notification content.
- Independent penetration test and threat-model abuse-case review before launch.

### Accessibility, localisation, and quality

- Automated accessibility checks plus manual keyboard, screen-reader, zoom/reflow, contrast, focus, errors and language-attribute testing; automation alone is insufficient.
- Translation-key completeness, placeholder/plural/date/number tests, Tamil font/rendering and long-text layout review by qualified speakers.
- Test authoritative deadline timezone/date boundaries, leap years, daylight-saving effects in Germany, user locale differences, and “date only” deadlines.
- Supported-browser matrix, responsive checks, resilient slow/offline/error states, and no sensitive data in URLs.

### Performance and resilience

- Establish representative catalogue/user sizes and SLOs in Phase 0; load search, programme detail, profile and tracker paths and measure p50/p95/p99, error rate and database plans.
- Exercise connection exhaustion, provider timeout, worker restart, duplicate message, poison job, partial outage, retry/backoff, and graceful degradation.
- Test PostgreSQL point-in-time restore, encrypted backup access, migrations/rollback strategy, notification replay safety, and recovery objectives.

### CI/CD quality gates

Pull requests require formatting/lint/type/build/unit/component/integration/architecture checks, migration review, security scans, translation validation, and review ownership for security/catalogue/rule changes. Deployment promotes immutable artefacts through staging, smoke/E2E/security checks, manual production approval, post-deploy verification, and tested rollback. Flaky tests are quarantined only with an owner and expiry, never silently retried into acceptance.

## 4. Assumptions

- The MVP serves Sri Lankan applicants, but users may physically access it from other jurisdictions.
- Programme content is curated by trained humans from official public sources; the initial catalogue is intentionally bounded rather than exhaustive.
- English and Tamil have equal functional coverage; official German/English source text may remain linked rather than fully translated when translation could change legal/academic meaning.
- The platform is guidance/workflow software, not a university representative, regulated immigration adviser, admissions authority, or application submission agent.
- Email and in-app notifications are sufficient for MVP, and deadlines are reminders rather than a guaranteed delivery service.
- A modular monolith and PostgreSQL search meet initial traffic/content needs; production uses managed equivalents even though Docker Compose supports local development.
- Students provide accurate profile and tracker information and independently verify current official requirements before submitting.

## 5. Unresolved decisions and required owners

| Decision | Why it matters | Proposed owner / deadline |
|---|---|---|
| Exact legal entity, controller/processor roles, applicable laws, transfer and hosting region | Determines notices, contracts, residency, rights and incident duties | Legal/privacy, Phase 0 |
| File upload in first release versus metadata-only checklist | Materially changes breach impact, operations and security scope | Product + security/privacy, before Phase 1 architecture freeze |
| Authentication UX: server cookie/BFF details, external identity providers, student MFA | Affects CSRF/token/session design and user friction | Architecture + security, Phase 0 |
| Initial catalogue size, institutions, freshness interval, verification staffing and escalation | Determines trustworthiness and operational capacity | Product/content lead, Phase 0 |
| Formal definition of “English-taught” and subject taxonomy, including interdisciplinary programmes | Controls scope and search consistency | Academic/content lead, Phase 0 |
| Supported grading systems and whether any conversions are acceptable | Incorrect conversion can mislead eligibility | Academic/content + legal, before eligibility design |
| Eligibility rule vocabulary, aggregation precedence, disclaimer wording and review accountability | Central safety/product behaviour | Product, academic, legal, engineering, Phase 3 |
| Deadline precision/timezone/category model and stale-record handling | Prevents harmful reminders | Content + engineering, Phase 3 |
| Notification provider, sender domain, locale templates, consent basis and delivery SLA | Privacy, deliverability and operations | Product/platform/privacy, before Phase 5 |
| Retention schedule, deletion exceptions, audit retention and backup erasure | Needed to implement privacy rights correctly | Legal/privacy/security, Phase 0–2 |
| Admin organisation, role assignment, four-eyes coverage and emergency access | Defines insider-risk controls and staffing | Operations/security, before admin release |
| Production cloud, availability SLO, traffic model, RPO/RTO, budget and on-call | Drives infrastructure and resilience design | Product/platform, Phase 0–1 |
| Accessibility target verification body and supported browsers/devices | Establishes release acceptance evidence | Product/QA, Phase 0 |
| Monetisation and analytics | Affects consent, trust, payments and data use; default is no payments/ads in MVP | Product/privacy, before scope change |

No unresolved item authorises a developer to invent a course requirement or weaken the product restrictions. When evidence is unavailable, the product must represent it as unknown and direct the student to the official source.
