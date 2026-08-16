# Rate-limit email verification resends

Requests to resend an email-verification link are limited to one request every two minutes per account and source IP. The verification page displays the remaining cooldown, uses a generic response for known and unknown email addresses, and makes only the newest verification link valid.

## Considered Options

- A two-minute account-and-IP cooldown — chosen to balance usability during local testing with protection against email abuse and account enumeration.
- No cooldown or a client-only cooldown — rejected because the limit must be enforced by the server.
