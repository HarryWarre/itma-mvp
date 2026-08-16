# itma-mvp
ITMA MVP — A minimum viable product for ITMA, focused on validating core features, user flows, and the technical foundation before full-scale development.

## Local development

The application targets .NET 10 and PostgreSQL. Configure `ConnectionStrings:DefaultConnection` and, when email flows are added, the `Smtp` section through user secrets or environment variables. Ethereal is the intended local SMTP endpoint; credentials must not be committed.

Apply the checked-in EF Core migration with:

```sh
dotnet tool restore
dotnet ef database update --project Itam.Web --startup-project Itam.Web
```

For a local run that applies migrations on startup, set `Database__ApplyMigrations=true`. The default is false so the shell can start without a running database while the schema is being prepared.

Run the application and tests with:

```sh
dotnet run --project Itam.Web
dotnet test Itam.slnx
```
