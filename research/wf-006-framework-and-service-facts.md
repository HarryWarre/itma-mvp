# WF-006 — Framework and service facts for the identity foundation

Research completed: 2026-08-17  
Scope: current .NET 10 ASP.NET Core Identity guidance, PostgreSQL through EF Core/Npgsql, and Ethereal SMTP for local email testing.  
Method: primary sources only. No application code was changed.

## Executive findings

- Use ASP.NET Core Identity's built-in password hasher and token providers rather than storing or implementing password/token cryptography in application code. In .NET 10, `PasswordHasherOptions` defaults to Identity V3 compatibility and 100,000 PBKDF2 iterations; the work factor is configurable. ([Microsoft Learn: `PasswordHasherOptions`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasheroptions?view=aspnetcore-10.0), [ASP.NET Core Identity configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0), [ASP.NET Core v10.0.0 source](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Identity/Extensions.Core/src/PasswordHasherOptions.cs))
- Require confirmed email before sign-in and send confirmation/password-reset links through a registered `IEmailSender<TUser>`. Identity exposes the token generation and confirmation/reset operations through `UserManager<TUser>`. ([ASP.NET Core Blazor account confirmation and password recovery](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/account-confirmation-and-password-recovery?view=aspnetcore-10.0), [ASP.NET Core Identity API guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0))
- Target the EF Core 10-compatible Npgsql provider (`Npgsql.EntityFrameworkCore.PostgreSQL` 10.x) and register the context with `UseNpgsql`. Npgsql 10.0 is released and explicitly documents EF Core 10 support. ([Npgsql EF Core provider](https://www.npgsql.org/efcore/), [Npgsql EF Core 10.0 release notes](https://www.npgsql.org/efcore/release-notes/10.0.html))
- Ethereal is suitable for local flow testing: it accepts SMTP mail, stores it for web preview, and never delivers it to real recipients. It is not a production delivery provider. ([Ethereal](https://ethereal.email/), [Ethereal Help](https://ethereal.email/help), [Nodemailer: Testing with Ethereal](https://nodemailer.com/guides/testing-with-ethereal))

## ASP.NET Core Identity on .NET 10

### Password hashing

`PasswordHasherOptions.CompatibilityMode` defaults to `IdentityV3`. `IterationCount` controls PBKDF2 work for Identity V3 hashes and defaults to `100,000`; the value must be positive. Identity's verifier selects the algorithm from the hash format marker, so changing the mode affects new hashes while existing supported formats remain verifiable. ([ASP.NET Core Identity configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0), [Microsoft Learn: `PasswordHasherOptions`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasheroptions?view=aspnetcore-10.0), [ASP.NET Core v10.0.0 `PasswordHasherOptions` source](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Identity/Extensions.Core/src/PasswordHasherOptions.cs))

The documented default password policy requires a minimum length of six, uppercase and lowercase characters, a digit, a non-alphanumeric character, and one unique character. These are defaults, not an MVP product decision; the application should explicitly choose and test its policy through `IdentityOptions.Password`. ([ASP.NET Core Identity configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0))

### Email confirmation and password reset

The .NET 10 Blazor Web App guidance uses `AddIdentityCore<TUser>`, `.AddEntityFrameworkStores<TContext>()`, `.AddSignInManager()`, and `.AddDefaultTokenProviders()`. It sets `options.SignIn.RequireConfirmedAccount = true` and registers an `IEmailSender<TUser>` implementation. ([ASP.NET Core Blazor account confirmation and password recovery](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/account-confirmation-and-password-recovery?view=aspnetcore-10.0))

The intended flow is:

1. Registration calls `UserManager<TUser>.GenerateEmailConfirmationTokenAsync` and sends a confirmation link through `IEmailSender<TUser>.SendConfirmationLinkAsync`. ([Microsoft Learn: `GenerateEmailConfirmationTokenAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.generateemailconfirmationtokenasync?view=aspnetcore-10.0), [ASP.NET Core Blazor email-sender example](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/account-confirmation-and-password-recovery?view=aspnetcore-10.0))
2. The confirmation endpoint passes the token to `UserManager<TUser>.ConfirmEmailAsync`; the user cannot sign in while `RequireConfirmedEmail`/`RequireConfirmedAccount` blocks unconfirmed accounts. ([Microsoft Learn: `ConfirmEmailAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.confirmemailasync?view=aspnetcore-10.0), [ASP.NET Core Identity API guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0))
3. Password recovery calls `UserManager<TUser>.GeneratePasswordResetTokenAsync`, sends a reset link or code through the email sender, and applies the submitted token with Identity's password-reset operation. ([Microsoft Learn: `GeneratePasswordResetTokenAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.generatepasswordresettokenasync?view=aspnetcore-10.0), [ASP.NET Core Blazor password recovery guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/account-confirmation-and-password-recovery?view=aspnetcore-10.0))

The built-in Identity user tokens have a default one-day lifespan. Microsoft documents changing all data-protection token lifespans with `DataProtectionTokenProviderOptions`, or defining separate providers when email confirmation and password reset need different lifetimes. ([ASP.NET Core Blazor token-lifetime guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/account-confirmation-and-password-recovery?view=aspnetcore-10.0), [ASP.NET Core account confirmation and password recovery](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-10.0))

The Blazor guidance also recommends keeping provider secrets out of project code and configuration files; for local development it documents User Secrets as an option. ([ASP.NET Core Blazor email-provider configuration](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/account-confirmation-and-password-recovery?view=aspnetcore-10.0))

### Data-protection key implication

Identity's email and password-reset tokens are data-protection tokens. The data-protection system persists and rotates keys, and its documentation explains that protected payloads depend on the key ring for unprotection. Therefore, the implementation should use a stable key location for any environment where a token must survive an application restart or be validated by more than one instance. This is an implementation inference from the documented key-ring behavior, not a new Identity API requirement. ([ASP.NET Core key management](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-management?view=aspnetcore-10.0), [ASP.NET Core key storage providers](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0))

## PostgreSQL through EF Core and Npgsql

Npgsql publishes `Npgsql.EntityFrameworkCore.PostgreSQL` as the EF Core provider for PostgreSQL. Its current provider documentation shows the package, a `DbContext`, and dependency-injection registration with `AddDbContextPool` and `UseNpgsql(connectionString)`. For EF Core 9 and later, `UseNpgsql()` is also the documented configuration point for Npgsql-specific options. ([Npgsql EF Core provider](https://www.npgsql.org/efcore/))

Npgsql's 10.0 release notes state that `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0 is available and adds support for EF Core 10 features. The application should keep the Npgsql major version aligned with the EF Core major version because EF Core providers generally do not work across major versions. ([Npgsql EF Core 10.0 release notes](https://www.npgsql.org/efcore/release-notes/10.0.html), [EF Core database providers](https://learn.microsoft.com/en-us/ef/core/providers/))

For schema evolution, EF Core migrations compare the current model with a model snapshot, generate migration files, and track applied migrations in a history table. Microsoft documents `dotnet ef migrations add <Name>` for creating a migration and `dotnet ef database update` for local development application. Production deployment should review generated SQL or use an appropriate controlled migration strategy. ([EF Core migrations overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/), [Applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying))

## Ethereal SMTP for local testing

### Capabilities

- Ethereal is a fake SMTP service intended for development and testing. Messages are captured and displayed for inspection; they are never delivered to real recipients. ([Ethereal](https://ethereal.email/), [Nodemailer: Testing with Ethereal](https://nodemailer.com/guides/testing-with-ethereal))
- The official Ethereal help page documents SMTP host `smtp.ethereal.email`, port `587`, and STARTTLS. ([Ethereal Help](https://ethereal.email/help))
- Ethereal works with standard SMTP clients, not only Nodemailer. Nodemailer documents automatic test-account creation, returned SMTP credentials, reuse of saved credentials, and browser preview URLs. ([Ethereal FAQ](https://ethereal.email/faq), [Nodemailer: Testing with Ethereal](https://nodemailer.com/guides/testing-with-ethereal))

### Limitations relevant to this MVP

- A confirmation or reset email sent through Ethereal will not arrive in the user's real mailbox. The test must open the Ethereal web preview or use the returned preview URL to obtain and follow the link. ([Ethereal FAQ](https://ethereal.email/faq), [Nodemailer: Testing with Ethereal](https://nodemailer.com/guides/testing-with-ethereal))
- Ethereal message URLs are public and do not require authentication; the FAQ says authentication information is encoded in the URL. Do not expose preview URLs outside local development or put real secrets/personal data in test messages. ([Ethereal FAQ](https://ethereal.email/faq))
- Messages are retained only temporarily—Ethereal's FAQ says they are deleted after a few hours. The service is therefore unsuitable as an audit trail or durable test mailbox. ([Ethereal FAQ](https://ethereal.email/faq))
- Inbound email is disabled by default. The Ethereal help page says inbound access requires the `ETHEREAL_API_KEY` and a paid Postal System subscription for accounts that need inbound access. This MVP only needs outbound capture and preview, so it should not depend on inbound mail. ([Ethereal Help](https://ethereal.email/help))
- Ethereal is a test endpoint, not a deliverability, bounce, spam, or production SMTP validation service. This limitation follows directly from its documented no-delivery behavior. ([Ethereal](https://ethereal.email/), [Ethereal FAQ](https://ethereal.email/faq))

## Recommendation for the later implementation ticket

For local development, configure a server-side `IEmailSender<ApplicationUser>` backed by SMTP settings held in User Secrets. Point it at Ethereal's `smtp.ethereal.email:587` STARTTLS endpoint, persist one test account's credentials for repeatable previews, and log or expose the returned preview URL only in the Development environment. Keep the Identity default token lifetime unless the product decision ticket chooses separate confirmation and reset lifetimes. Use PostgreSQL with the EF Core 10-compatible Npgsql 10 provider and migrations; do not use `EnsureCreated` as the long-term schema-evolution mechanism. These are recommendations derived from the cited framework and service facts.

