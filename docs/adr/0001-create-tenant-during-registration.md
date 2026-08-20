# Create a tenant during registration

Each new registration creates exactly one tenant named by the registrant, makes that user the tenant owner, and grants the initial administrative membership. The MVP does not support joining an existing tenant or creating additional tenants, while retaining tenant memberships in the domain model so those capabilities can be added later without changing the initial onboarding rule.

## Considered Options

- Create a tenant during registration — chosen because every authenticated user needs an immediate workspace and an unambiguous authorization boundary.
- Register users without a tenant — rejected because it leaves the first authenticated destination without an ownership context.
- Let users join or choose an existing tenant during registration — deferred because invitations and tenant discovery introduce a separate onboarding flow.
