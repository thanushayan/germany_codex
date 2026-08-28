# Authentication and Authorization

## Session architecture

The browser authenticates with an ASP.NET Identity application cookie. The cookie is HttpOnly, SameSite Strict, scoped to `/`, non-persistent at login, and Secure outside local HTTP development. The frontend always uses `credentials: include`; it never reads or stores the authentication ticket and never writes authentication tokens to `localStorage` or `sessionStorage`.

Production startup requires `DataProtection:KeysPath` to point to a protected persistent volume. This preserves cookie-signing keys across restarts/instances. The deployment owner must restrict filesystem access, encrypt the volume, back up/rotate it appropriately, and coordinate key retention with the maximum cookie lifetime.

Cookie-authenticated mutation endpoints require an ASP.NET antiforgery token in `X-CSRF-TOKEN`. `/api/auth/csrf` stores the antiforgery cookie and returns only the request token; the frontend holds that request token in module memory. CORS permits credentials only for explicitly configured origins.

## Account lifecycle

- Registration creates an ASP.NET Identity user, assigns only `Student`, creates the owned student profile, generates an email confirmation token, and delegates delivery to `IAccountEmailSender`.
- Email confirmation is required before login.
- Login uses a generic failure response, lockout-on-failure, a five-attempt lockout threshold, and a non-persistent cookie.
- Forgot-password always returns the same accepted response, whether the email is absent, unconfirmed, or registered.
- Reset and verification codes are encoded for transport and are never included in application logs or audit metadata. Frontend routes read these codes from the URL fragment, which is not sent in HTTP requests to the static host, and post them only in CSRF-protected API request bodies.
- Password reset refreshes the security stamp. Cookie security stamps are revalidated every 15 minutes.
- Logout invalidates the browser cookie and requires CSRF validation.
- The current-user endpoint returns only ID, email, locale, and role names—never password hashes, security stamps, lockout internals, or tokens.

The development email sender validates that a message was requested and logs only message type and opaque user ID. It deliberately does not log recipient email, confirmation/reset token, or generated link. A reviewed production provider must replace it before production deployment.

## Roles and policies

| Role | Intended scope |
|---|---|
| `Student` | Own profile, saved courses, assessments, applications, documents, notifications, consents, and support tickets |
| `ContentEditor` | Draft and edit catalogue content, but not final review approval |
| `Reviewer` | Review and approve verified content under the four-eyes workflow |
| `SupportAgent` | Purpose-limited support workflows; no unrestricted sensitive-document access |
| `Admin` | Administrative operations and explicitly authorised ownership override |
| `SuperAdmin` | Restricted emergency/platform administration; assignment requires separate operational control |

Named policies group content editing, review, support, and administration. `OwnsStudentResource` compares the authenticated `NameIdentifier` with a server-loaded owner ID. Only `Admin` and `SuperAdmin` bypass that comparison. Endpoint code must load the resource by opaque ID, authorise against its stored owner, and only then return or mutate it; it must never trust a user ID from the request.

## Rate limits

Authentication uses per-remote-address fixed windows:

- login: 5 requests per minute;
- registration: 3 requests per 10 minutes; and
- password recovery/email verification: 3 requests per 10 minutes.

Production reverse-proxy configuration must establish trusted forwarded headers before remote-address partitioning is relied upon. Account lockout remains independent of the network rate limit.

## Security audit events

Registration, login success/failure/lockout, logout, password-reset request for a confirmed account, password-reset result, and email-verification result create append-only `AuditLog` records. Events include an opaque actor/target user ID when known, action, outcome, UTC time, and correlation ID. They do not contain email addresses, passwords, cookies, reset/verification codes, IP addresses, or request bodies.

## API endpoints

| Method | Endpoint | Authentication | CSRF | Rate policy |
|---|---|---|---|---|
| GET | `/api/auth/csrf` | Anonymous | N/A | — |
| POST | `/api/auth/register` | Anonymous | Required | registration |
| POST | `/api/auth/login` | Anonymous | Required | authentication |
| POST | `/api/auth/logout` | Required | Required | — |
| GET | `/api/auth/me` | Required | N/A | — |
| POST | `/api/auth/forgot-password` | Anonymous | Required | password-recovery |
| POST | `/api/auth/reset-password` | Anonymous | Required | password-recovery |
| POST | `/api/auth/verify-email` | Anonymous | Required | password-recovery |

## Deployment decisions still required

Before production, select and threat-model the real email provider, public verification/reset URL construction, sender-domain protections, trusted proxy/network configuration, distributed rate limiting for multi-instance deployment, data-protection key storage/encryption, administrative MFA and role-assignment workflow, and session/device revocation UX. These are launch blockers rather than assumptions that the scaffold silently resolves.
