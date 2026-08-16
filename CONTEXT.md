# Domain Context

## Glossary

- **User** — A person who authenticates to the application.
- **Registration** — The process by which a person creates a User account before accessing the application.
- **Sign-in** — The process by which a registered User authenticates and enters the protected application.
- **Email verification** — Confirmation that a registered User controls the email address associated with the account. A User must complete email verification before signing in.
- **Access denial** — The result when an authenticated User lacks a permission required by a protected capability. It is distinct from email-verification failure and unauthenticated access.
- **Tenant** — An isolated workspace boundary. Tenant-owned users, roles, permissions, settings, and audit records must not be exposed across tenants.
- **Tenant membership** — A user's relationship to a tenant, including the roles that grant that user access within that tenant.
- **Tenant owner** — The user who creates a tenant and receives its initial administrative membership.
- **Role** — A named bundle of permissions assigned within a tenant.
- **Permission** — A named authorization capability that can be granted through a role.
- **Setting** — A configurable value at system, tenant, or user scope. More specific scopes override broader defaults.
- **Application language** — The language used for the application interface. Vietnamese is the initial default language and English is also supported.
- **Audit log entry** — A historical record of an important security or administrative event. The first release needs a simple read-only way to view these records.

## Relationships

- A user may belong to multiple tenants through tenant memberships.
- Registration creates a tenant for the registering user, who becomes its tenant owner.
- A tenant membership may have one or more roles.
- A role grants one or more permissions.
- Audit log entries are associated with the tenant in which the event occurred and identify the acting user when applicable.
