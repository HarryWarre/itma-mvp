# Require email verification before sign-in

Users must verify control of their registered email address before they can sign in. This keeps the authenticated application and its tenant workspace unavailable to unverified accounts; unverified users are sent to an email-verification page where they can request another confirmation email subject to a resend cooldown.

## Considered Options

- Require verification before sign-in — chosen as the identity foundation’s trust boundary.
- Allow sign-in before verification — rejected because it would make an unverified account an authenticated tenant member.
