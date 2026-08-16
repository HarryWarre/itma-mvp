---
id: wf-000
title: Multi-tenant authentication and authorization foundation
label: wayfinder:map
status: open
assignee: null
---

## Destination

Produce an implementation-ready specification for the ITMA MVP authentication foundation: one account creates one tenant, authenticates with email/password, and owns a fixed Owner role. The specification also defines the future-ready permission model, settings, and simple read-only audit-log view.

## Notes

Domain: tenant-aware identity and access management for the existing .NET 10 Blazor Web App. UI framework: MudBlazor.

Consult `domain-modeling` and `grilling` for every HITL decision; consult `research` for external framework and service facts. PostgreSQL is the persistence target. Ethereal is the initial local SMTP test service. Wayfinding plans the work; implementation begins only after the map is clear.

## Decisions so far

<!-- Open tickets are discovered by querying child issue metadata; only closed decisions are indexed here. -->

- [Framework and service facts for the identity foundation](006-framework-and-service-facts.md) — .NET 10 Identity, EF Core/Npgsql 10, and Ethereal local SMTP facts are captured in the linked research note.

## Not yet specified

- The exact ASP.NET Core Identity and EF Core/Npgsql composition, password policy, token/session behavior, and local SMTP configuration.
- The complete account, tenant, role, permission, settings, and audit-entry fields and invariants.
- The first permission catalog and which domain actions must emit audit events.
- The settings schema, editable values, and precedence behavior at system, tenant, and user scopes.
- The page-level UI, navigation, validation, error states, and acceptance criteria for each feature.
- The MudBlazor application shell, navigation structure, responsive behavior, theme, and reusable form/table conventions.
- Production email delivery, deployment configuration, secret management, observability, and operational retention policies.

## Out of scope

- Social login, SSO, and other external identity providers for the MVP.
- User invitations, adding additional users to a tenant, and multi-tenant memberships in the MVP.
- Custom role administration in the MVP; the initial tenant account receives a fixed Owner role.
- Audit-log editing, deletion, export, analytics, and workflow actions.
