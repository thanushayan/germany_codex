# Codex Project Instructions

## Scope and precedence

These instructions apply to the entire repository. Read this file and the relevant files in `docs/` before changing anything. A more deeply nested `AGENTS.md` may add or override instructions for its subtree. Direct user and system instructions take precedence.

This repository is currently planning-first. Do not implement product features until the planning phase is explicitly approved. Keep the planning documents aligned with consequential architecture, scope, security, privacy, or delivery decisions.

## Product purpose and MVP boundaries

Build a self-service platform that helps Sri Lankan students find suitable German Master's programmes and organise applications without relying on expensive education agencies. The MVP is limited to:

- Sri Lankan students;
- Germany;
- Master's programmes;
- English-taught IT, Computer Science, Data Science, AI, Software Engineering, Business Analytics, and Engineering programmes; and
- English and Tamil user interfaces.

Students must review, approve, and perform final submissions themselves on official university or Uni-assist websites. The product provides discovery, workflow, and explainable guidance; it is not an admissions authority, university representative, visa adviser, or application-submission agent.

Do not add other countries, study levels, subject areas, automated submissions, authenticated third-party portal integrations, admission/visa prediction, agency services, or AI decision-making to the MVP without an approved scope change. Do not store university, Uni-assist, Hochschulstart, embassy, email, or other third-party passwords.

### Mandatory product-safety rules

- **Never invent university or course information.** Preserve unknown or ambiguous values as unknown and refer the student to the official source.
- **Never represent estimated eligibility as an official admission decision.** Eligibility must remain deterministic, rule-based, versioned, explainable, and advisory. AI must not make the final decision.
- **Never display “admission guaranteed” or “visa guaranteed.”** Do not use equivalent wording that implies certainty.
- **Every published course must include an official source URL and verification date.** Requirements, rules, deadlines, fees, and application routes must be supported by verified official sources. Aggregators, agencies, search snippets, and generated content are not authoritative sources.
- Preserve the student's control of final application submission. External handoff must not proxy credentials or submit on the student's behalf.

## Authoritative planning documents

- `docs/PRODUCT_REQUIREMENTS.md`: product users, capabilities, constraints, and non-functional requirements.
- `docs/ARCHITECTURE.md`: runtime architecture, module boundaries, repository layout, entities, and API areas.
- `docs/MVP_SCOPE.md`: release slices, critical journeys, definition of done, and deferred work.
- `docs/SECURITY_REQUIREMENTS.md`: security, privacy, document, infrastructure, and launch controls.
- `docs/DEVELOPMENT_PLAN.md`: phases, test strategy, assumptions, and unresolved decisions.

If documents conflict, choose the safer/narrower interpretation and report the conflict rather than silently deciding. Record material architecture decisions in `docs/adr/` when that directory is introduced. Legal and compliance statements require qualified review and must not be presented as legal advice.

## Architecture and technology choices

- Use a modular monolith for the MVP: React/TypeScript/Vite/Tailwind CSS web client, ASP.NET Core Web API, PostgreSQL with Entity Framework Core, ASP.NET Identity, and a separate ASP.NET background worker.
- Use Docker Compose for reproducible local infrastructure. Production should use managed secrets, database, object storage, monitoring, and backup capabilities where selected.
- Use xUnit for backend tests and Vitest with React Testing Library for frontend unit/component tests.
- Keep the browser untrusted. Business validation, authorisation, ownership enforcement, publication invariants, and eligibility evaluation belong on the server.
- Prefer secure HttpOnly cookies for sessions; never persist bearer tokens in browser local storage without an approved, documented security decision.
- Use PostgreSQL search first. Do not introduce microservices, a message broker, or a separate search cluster without measured need and an architecture decision.
- Use a transactional outbox/durable job model for notifications and background work. Jobs and externally visible state changes must be idempotent where retries can occur.
- Store uploaded files, if uploads are approved, in encrypted private object storage—not PostgreSQL or a public/web-root path. File uploads remain conditional on the security gate in `docs/SECURITY_REQUIREMENTS.md`.

## Repository folder conventions

Follow the planned structure as it is scaffolded:

```text
apps/web/                  React/Vite application
apps/api/                  ASP.NET Core API host
apps/worker/               background worker host
src/<Module>/              backend business modules
src/BuildingBlocks/        narrowly shared backend primitives
tests/Unit/                backend unit tests
tests/Integration/         real-PostgreSQL integration tests
tests/Architecture/        module-boundary tests
tests/Contract/            API/OpenAPI compatibility tests
tests/EndToEnd/            critical journey tests
docs/adr/                  architecture decision records
docs/runbooks/             operational procedures
docs/threat-models/        threat models and data-flow notes
deploy/                    local/production deployment material
scripts/                   repeatable development/CI utilities
```

- Organise frontend code under `apps/web/src/app`, `features`, `components`, `api`, `i18n/{en,ta}`, and `test`.
- Align frontend feature folders with backend modules where practical.
- Within a backend module, separate Domain, Application, Infrastructure, and API concerns. Domain code must not depend on infrastructure.
- A module must use another module's public application contract, not reach into its tables or internal implementation.
- Put shared code in `BuildingBlocks` or shared frontend components only when it is genuinely cross-cutting; do not create dumping grounds.
- Add new projects/packages only when they enforce a meaningful boundary or provide a justified capability.

## Backend coding standards

- Enable nullable reference types and treat compiler warnings as actionable. Prefer explicit types at public boundaries and small, cohesive classes/functions.
- Use async APIs for I/O and propagate `CancellationToken` through request, database, and provider calls.
- Keep controllers/endpoints thin. Put use-case orchestration in the application layer and invariants in the domain.
- Use dependency injection and options validation. Never read secrets from committed configuration.
- Validate input on the server with bounded lengths, allowlists, safe URL handling, and consistent RFC 9457-style problem details.
- Use EF Core parameterisation; justify and review raw SQL. Avoid lazy-loading and unbounded queries. Use server-side pagination and appropriate indexes.
- Represent timestamps in UTC while retaining an authoritative deadline's local date/time, timezone, precision, and applicant context.
- Use UUID identifiers and enforce ownership independently of identifier opacity. Apply optimistic concurrency to mutable admin and workflow records.
- Use policy-based authorisation and deny by default. Derive the current user from the authenticated principal, never from a client-supplied owner ID.
- Eligibility operators must use three-valued outcomes for pass, potential gap, and unknown. Unsupported requirements must not be guessed.
- Emit structured, redacted logs with correlation IDs. Do not interpolate entire request, profile, Identity, or entity objects into logs.
- Do not wrap imports/usings in try/catch blocks.

## Frontend coding standards

- Use strict TypeScript. Avoid `any`; narrow `unknown` safely and model API states explicitly.
- Use functional React components and hooks. Keep route/page orchestration separate from reusable presentational components and API access.
- Use the generated or centrally typed API client; do not duplicate server contracts ad hoc across features.
- Treat all frontend checks as usability only. Never rely on hidden controls or client-side validation for security.
- Use semantic HTML before ARIA, accessible form labels and errors, predictable keyboard/focus behaviour, and responsive Tailwind utilities.
- Do not render untrusted HTML. If a future requirement makes it unavoidable, use an approved sanitizer and add focused XSS tests.
- Handle loading, empty, validation, unauthorised, forbidden, stale-data, and failure states deliberately.
- **Never expose secrets or API keys in frontend code.** Vite-exposed environment variables are public. Only put non-secret public configuration in `VITE_*` variables.
- Do not put tokens, sensitive profile values, application status details, or document identifiers in URLs, analytics, or client logs.
- Add React Testing Library tests based on user-observable behaviour rather than implementation details.

## Build and local development commands

The application has not yet been scaffolded. Once the planned files exist, use the repository scripts/configuration as the source of truth. Expected baseline commands are:

```bash
# Local dependencies / complete stack
docker compose up -d
docker compose down

# Backend (from repository root)
dotnet restore GermanyApplications.sln
dotnet build GermanyApplications.sln --no-restore

# Frontend (from apps/web)
npm ci
npm run build
npm run dev
```

Use the package manager and lockfile committed by the project. Do not switch package managers or regenerate lockfiles incidentally. If actual project scripts differ, update this file in the same change that establishes the new canonical command.

## Test and quality commands

**Run relevant tests after every implementation.** Start with the narrowest affected tests, then run the appropriate broader suites before completion. Expected baseline commands after scaffolding are:

```bash
# Backend
dotnet test GermanyApplications.sln --no-build

# Frontend (from apps/web)
npm run test -- --run
npm run lint
npm run typecheck

# Full local stack / integration and end-to-end suites
docker compose up -d
# Run the repository's documented integration/contract/E2E commands.
```

- Add or update tests for every behavioural change and regression fix.
- Test success, boundary, unknown/missing-data, unauthorised, forbidden, validation, concurrency, and failure paths as applicable.
- Eligibility changes require reviewer-approved official-source fixtures and regression tests proving that unknown does not become pass and no guaranteed/final language is emitted.
- Security-sensitive changes require negative authorisation and abuse-case tests. Localisation changes require both English and Tamil coverage. UI changes require automated accessibility checks plus relevant manual keyboard/visual review.
- Do not hide failures by weakening assertions, deleting tests, broad retries, or silently updating snapshots. Explain environmental limitations and any test not run.

## Database migration rules

- Create and apply EF Core migrations through the designated migration project/startup host after the solution is scaffolded. Use descriptive migration names.
- Review generated SQL and the model snapshot. Test migrations against a real PostgreSQL instance, including a database upgraded from the previous schema when practical.
- Migrations already shared or applied are immutable history. **Never modify or delete an existing migration without explaining why.** Prefer a new corrective migration; any exceptional rewrite requires explicit approval, a documented deployment/data-safety plan, and coordination with every affected environment.
- Do not use `EnsureCreated` for deployed environments or edit the production database manually.
- Make destructive or long-locking changes explicit. Plan backfills, compatibility windows, rollbacks/roll-forwards, indexes, defaults, nullability transitions, and deployment order.
- Preserve published programme versions, provenance, consent, eligibility snapshots, and required audit history. Configure deletion behaviours deliberately; do not accept cascade defaults without review.
- Never use fabricated university/course data as a production seed. Test fixtures must be clearly synthetic and must not be publishable as real programme data.

## Security requirements

- Follow least privilege, deny-by-default authorisation, resource ownership checks, administrative MFA, four-eyes approval for requirements/deadlines, and step-up authentication for high-risk operations.
- **Never log passwords, tokens, passport numbers or document contents.** Also redact reset links, connection strings, API keys, identity numbers, sensitive academic/profile fields, and private object keys unless a specifically reviewed operational need exists.
- Keep secrets in environment-specific managed secret storage. Never commit secrets, place them in images, return them to browsers, or include them in screenshots, fixtures, logs, traces, analytics, exception messages, or PR text.
- Use TLS, Secure/HttpOnly/SameSite cookies, CSRF protection for cookie-authenticated writes, restrictive CORS, CSP, HSTS, safe redirects/links, rate limits, request limits, and output encoding.
- Prevent IDOR with server-side ownership policies and cross-user tests. Opaque IDs are not authorisation.
- For remote URL retrieval, mitigate SSRF, DNS rebinding, redirects, unsafe schemes, and dangerous content. Do not automatically scrape requirements into published data.
- Do not enable document uploads until private storage, quarantine, signature/type/size checks, malware scanning, encryption, authorised short-lived download, access audit, retention, and erasure have passed the documented security gate.
- Run dependency, secret, static-analysis, container, and infrastructure scans when available. Do not add an unmaintained dependency when a platform capability is sufficient.
- **Report any unresolved security issue before marking work complete.** Never describe work as production-ready while a known critical/high issue or required launch control remains unresolved.

## Privacy rules

- Minimise personal data and collect it only for a stated purpose. Make optional fields clear and preserve “unknown/not provided” without coercion.
- Treat contact details, academic history, saved courses, evaluations, applications, checklists, consent, and notification activity as confidential personal data. Treat identity/academic documents and authentication secrets as highly sensitive.
- Maintain versioned privacy notices, terms, consent/preferences, and withdrawal history. Do not assume consent is the correct lawful basis for every processing activity.
- Build access, correction, export, deletion, retention, and exception workflows only from an approved data map and legal/privacy decisions.
- Keep production personal data out of development, tests, demos, analytics, session replay, screenshots, and support notes. Use clearly synthetic data.
- Never send grades, passport/document details, or sensitive application status in email subjects or other exposed notification surfaces.
- Preserve auditability without copying personal/document content into audit events. Apply approved retention and access controls to logs and audit data.
- Flag legal/privacy assumptions and unresolved cross-border, hosting, retention, or controller/processor decisions to the appropriate owner.

## Accessibility requirements

- Target WCAG 2.2 AA for all MVP journeys in supported desktop and mobile browsers.
- Use semantic structure, keyboard operation, visible focus, sufficient contrast, text resizing/reflow, accessible names/descriptions, clear errors, and screen-reader announcements where needed.
- Do not communicate status, eligibility, deadline urgency, or validation by colour alone.
- Maintain logical focus after navigation, dialogs, errors, and asynchronous updates. Avoid unexpected focus movement and motion; respect reduced-motion preferences.
- Test with automated tools and manually with keyboard, zoom/reflow, and representative screen readers. Automated checks alone are not sufficient.
- Preserve accessibility in both English and Tamil, including language attributes, font rendering, long text, and form/error associations.

## English and Tamil localisation rules

- English and Tamil must have functional parity for critical MVP journeys, notices, validation, disclaimers, notifications, and accessibility labels.
- Put all user-visible text in translation resources under `apps/web/src/i18n/{en,ta}` once scaffolded. Do not hard-code user-facing strings in components or concatenate translated fragments.
- Use stable semantic translation keys, interpolation placeholders, and locale-aware plural/date/number formatting. Keep internal/API enums locale-independent.
- Set the document/element `lang` correctly. Do not assume Tamil is a right-to-left language; Tamil is left-to-right.
- Allow layouts to expand for translation. Do not encode meaning in text length, truncate critical requirements/disclaimers, or place text in images.
- Have qualified Tamil reviewers validate meaning and terminology, especially eligibility, deadlines, consent, security, and legal notices. Machine translation alone is not approval.
- Official-source facts may be conservatively translated for presentation, but retain a visible link to the official source and do not alter academic/legal meaning. When uncertain, label the translation and preserve unknowns rather than guessing.
- Add translation-completeness tests so missing Tamil keys fail CI or use a clearly visible development fallback; never silently ship critical English-only content.

## Git and commit expectations

- Inspect `git status`, applicable `AGENTS.md` files, relevant docs, and nearby code/tests before editing.
- Keep changes focused. Do not mix product features, refactors, dependency upgrades, generated-file churn, or formatting unrelated to the task.
- **Preserve existing working functionality.** Maintain backward compatibility unless an approved breaking change includes migration, communication, and rollback plans.
- Use short-lived branches and clear imperative Conventional Commit-style messages such as `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, or `chore:`.
- Do not commit secrets, personal data, build artefacts, editor files, local configuration, or production-derived fixtures.
- Review the complete diff and run `git diff --check` plus relevant builds/tests before committing. Document migrations, security/privacy impact, operational impact, tests, and unresolved decisions in the PR.
- Do not rewrite shared history, amend others' commits, force-push, or bypass required reviews unless explicitly authorised.
- Course content, requirement rules, security-sensitive code, migrations, and privacy changes require appropriate domain reviewers.

## Definition of done

A change is complete only when all applicable items below are true:

1. Scope and acceptance criteria are satisfied without weakening the product's safety boundaries.
2. Existing working functionality is preserved and the change is appropriately backward-compatible.
3. Code follows module, backend, frontend, security, privacy, accessibility, and localisation standards.
4. English and Tamil experiences have been implemented and reviewed where user-visible behaviour changed.
5. Official-source provenance and verification dates exist for every affected published course fact; unknown information remains unknown.
6. Relevant unit, component, integration, contract, end-to-end, accessibility, and security tests are added/updated and pass.
7. Builds, linting, type checking, migration validation, and `git diff --check` pass as applicable; any environment-limited check is explicitly reported.
8. Database changes use reviewed forward migrations and include deployment, backfill, compatibility, and recovery considerations where applicable.
9. Logs, telemetry, screenshots, fixtures, and errors have been checked for secrets and personal/sensitive data.
10. Documentation, translation resources, OpenAPI/contracts, ADRs, threat models, and runbooks are updated when affected.
11. The complete diff is reviewed, the commit/PR is focused and descriptive, and no unrelated generated or formatting churn remains.
12. **Any unresolved security issue is reported before work is marked complete.** Known critical/high issues block completion unless the responsible security owner explicitly accepts and documents the risk.

When a requirement cannot be met, stop short of claiming completion and clearly report the blocker, impact, evidence, and required owner/decision.
