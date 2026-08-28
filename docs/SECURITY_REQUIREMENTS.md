# Security, Privacy, and Compliance Requirements

## 1. Security objectives

Protect student identity, academic data, application activity, and any uploaded documents; preserve the integrity and provenance of course requirements; prevent unauthorised administrative publication; and keep the service available without weakening student control.

Security requirements must be validated against applicable law and counsel before launch, including GDPR territorial applicability and roles, German/EU requirements, Sri Lankan Personal Data Protection Act obligations, international transfers, electronic communications rules, and retention duties. This document is engineering guidance, not legal advice.

## 2. Data classification

| Class | Examples | Minimum handling |
|---|---|---|
| Public | Published programme facts and official source links | Integrity controls, versioning, safe caching. |
| Internal | Operational dashboards, non-sensitive configuration | Authenticated workforce access, logging. |
| Confidential personal | Contact, profile, grades, application history, consent records | Encryption, strict ownership/RBAC, minimised logging, retention limits. |
| Highly sensitive | Passport/identity/academic document files, reset/MFA secrets | Strong encryption, isolated private storage, short-lived access, malware scanning, access audit, tightly restricted support access. |
| Secret | Signing/encryption keys, provider/database credentials | Managed secrets/KMS, rotation, no application logs/source control. |

## 3. Authentication and session requirements

- Use ASP.NET Identity with current recommended password hashing parameters, breached-password controls where lawful/available, email verification, generic recovery responses, token expiry and one-time use.
- Require MFA for all administrative roles; offer MFA to students. Require step-up authentication for role changes, privacy exports/deletions, sensitive document access, and other high-risk operations.
- Enforce progressive throttling, lockout protections that resist denial of service, credential-stuffing detection, and alerts for suspicious login/recovery changes.
- Prefer Secure, HttpOnly, appropriately scoped SameSite cookies; protect cookie-authenticated mutations against CSRF. Rotate session identifiers after authentication/privilege changes and revoke sessions on password reset or compromise.
- Do not store university, Uni-assist, Hochschulstart, embassy, email, or other third-party passwords. User notes and forms must warn against entering them.

## 4. Authorisation and tenant/ownership isolation

- Default deny. Enforce policy-based permissions on the server for student, content-author, content-reviewer, privacy-support, security-admin, and system-worker roles.
- Authorisation checks apply at both endpoint and resource level. A student can access only their own profile, saved items, evaluations, applications, checklist, documents, notifications, and privacy requests.
- Use non-sequential identifiers but never treat them as an access control.
- Separate content author and approver for requirements/deadlines. Restrict role assignment and audit it.
- Support personnel get time-bound, purpose-limited access; sensitive document access requires explicit entitlement, reason, step-up authentication, and alert/audit. No general “super-admin reads everything” path.
- Add automated broken-object-level-authorisation and role-matrix tests.

## 5. Application and API security

- Validate all inputs server-side; bound pagination and text; allowlist schemes/hosts as appropriate for official links; normalise carefully; encode output by context.
- Use parameterised EF Core queries. Review any raw SQL. Protect React rendering from XSS and prohibit unsafe HTML unless sanitised with an approved library.
- Apply CSP, HSTS, frame protections, restrictive referrer/permissions policies, secure cookies, and a narrowly configured CORS policy.
- Apply per-IP/account/action rate limits, request size limits, timeouts, cancellation, and abuse monitoring. More strongly protect auth, exports, eligibility runs, searches, and admin mutations.
- Validate redirects and external links to prevent open redirects and deceptive destinations. Admin-entered source URLs require review; fetching remote sources must mitigate SSRF, DNS rebinding, and dangerous content.
- Return minimal problem details externally; log correlated diagnostic detail without secrets or personal document content.
- Maintain software bill of materials, locked dependencies, automated SCA, secret scanning, SAST, container/IaC scanning, signed build provenance where feasible, and patch SLAs.

## 6. Document security gate (if uploads are in scope)

Uploads must not launch until all controls below are tested:

1. Private, non-web-root, non-public object storage with public access blocked and separate from catalogue assets.
2. Server-generated opaque object keys; original filenames retained only as safely encoded metadata.
3. File extension, MIME, and magic-byte allowlists; strict size/page/count quotas; archive/macro/executable rejection.
4. Quarantine on upload and asynchronous malware scanning. Files are inaccessible until a clean result; failures/timeouts fail closed.
5. TLS in transit and envelope encryption at rest using managed keys with rotation and separated access.
6. Downloads authorised on every request and delivered with a very short-lived single-purpose URL or streamed response, safe content disposition, and `nosniff`.
7. Document access/deletion audit events, anomaly alerts, lifecycle deletion, backup deletion considerations, and tested erasure.
8. No document bytes/text in logs, analytics, eligibility rules, support tools, or AI systems without a separately approved purpose and explicit lawful basis/consent.

Metadata-only checklist tracking is the safer MVP default until this gate and operational incident handling are ready.

## 7. Data protection and consent

- Complete a data inventory, processing record, purpose/lawful-basis assessment, controller/processor allocation, DPIA screening, and international transfer assessment with counsel.
- Minimise collection; mark optional fields and explain purpose at collection. Do not use consent where it is not the appropriate lawful basis.
- Version privacy/terms/consent text and store who, what, when, locale, method, and withdrawal. Marketing and non-essential reminders require separable preferences.
- Provide authenticated access/correction/export/deletion/request workflows, identity verification proportionate to risk, SLA tracking, and exception handling.
- Define per-entity retention periods before launch. Automate expiry/anonymisation/deletion and preserve only legally justified minimal audit evidence.
- Analytics defaults to privacy-preserving, consent-aware configuration; prohibit sensitive field capture, session replay on authenticated/sensitive screens, and advertising profiles in MVP.
- Use privacy-filtered non-production data; never clone production personal data into development environments.

## 8. Logging, audit, and monitoring

- Security audit events include actor/service, action, target opaque ID/type, outcome, timestamp, correlation ID, and limited network/device context. Include login/MFA/recovery, role/policy changes, content publication, source/rule changes, eligibility version, consent/privacy actions, exports, and document access.
- Audit storage is append-only/tamper-evident, access-controlled, time-synchronised, retained under an approved schedule, and itself monitored/audited.
- Never log passwords, tokens, reset links, connection strings, document content, full identity numbers, or unnecessarily precise academic/profile data. Central redaction is mandatory.
- Alert on credential attacks, privilege changes, abnormal exports/document access, publication anomalies, scanning failures, repeated authorisation failures, and notification abuse.

## 9. Infrastructure, resilience, and operations

- Terminate modern TLS, encrypt internal sensitive traffic where applicable, segment database/object storage, block unnecessary egress, and expose only the proxy/load balancer publicly.
- Run containers as non-root with read-only filesystems/capability reduction where feasible; use minimal pinned images and routine scanning/rebuilds.
- Separate production from non-production accounts/networks/secrets. Apply least-privilege workload identity and database roles; rotate secrets and keys.
- Encrypt backups, restrict access, define RPO/RTO with stakeholders, test point-in-time restore and regional/provider failure procedures, and include deletion/retention implications.
- Establish incident severity, on-call ownership, evidence preservation, containment, recovery, notification assessment, student communications, and post-incident review runbooks.
- Commission independent penetration testing before launch and after material auth/upload/admin changes; remediate critical/high findings before release.

## 10. Principal risks and mitigations

| Risk | Impact | Required mitigation |
|---|---|---|
| Account takeover/credential stuffing | Exposure or alteration of student data | MFA, throttling, breached-password defence, secure recovery, session alerts/revocation. |
| IDOR/broken access control | Cross-student data/document exposure | Central ownership policies, deny-by-default, resource tests, opaque IDs, audit/anomaly detection. |
| Malicious document upload | Malware, stored XSS, parser exploitation | Prefer metadata-only MVP; otherwise quarantine, allowlists, scanning, private delivery and quotas. |
| Admin compromise/insider misuse | False requirements/deadlines or bulk access | Mandatory MFA, least privilege, four-eyes publishing, step-up access, immutable audit and alerts. |
| Stale/fabricated source data | Harmful application choices | Official-only evidence, publication validation, versioning, freshness SLA, stale labels/unpublish process. |
| Eligibility overclaim or rule defect | Student harm and regulatory/reputation risk | Deterministic limited vocabulary, unknown-safe evaluation, explanations/sources, review/versioning, disclaimers and regression tests. |
| Privacy overcollection/retention | Legal and personal harm | Data minimisation, retention automation, DPIA/legal review, rights workflows, no production data in test. |
| XSS/CSRF/injection/SSRF | Account or infrastructure compromise | Framework protections, encoding, CSRF, CSP, parameterisation, URL/fetch controls, security tests. |
| Notification leakage | Sensitive information disclosed by email | Minimal message content, preference controls, verified address, safe deep links, no grades/status detail in subject. |
| Supply-chain/container compromise | Broad system compromise | Lockfiles, SCA/SBOM/signing, secret scanning, pinned minimal images and patch policy. |
| Audit/log leakage | Secondary personal-data breach | Structured allowlisted fields, redaction tests, restricted access and retention. |
| Availability attack/job duplication | Missed deadlines or repeated messages | WAF/rate limits, queues/outbox, idempotency, backpressure, monitoring and recovery drills. |

## 11. Launch security acceptance

- Approved threat model and privacy/data-flow review for every critical journey.
- No open critical/high findings from dependency, code, infrastructure, or penetration testing.
- Role/ownership matrix and negative authorisation tests pass.
- Backup restore, incident tabletop, admin recovery, document quarantine (if applicable), and deletion exercises pass.
- Security headers/cookies/TLS, rate limits, log redaction, alerts, key rotation, and audit integrity are verified in a production-like environment.

