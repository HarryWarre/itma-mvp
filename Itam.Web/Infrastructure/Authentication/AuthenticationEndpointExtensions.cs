using System.Web;
using Itam.Web.Application.Authentication;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;

namespace Itam.Web.Infrastructure.Authentication;

public static class AuthenticationEndpointExtensions
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/register", async (
            HttpRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var result = await sender.Send(new RegisterCommand(
                FormValue(form, "Email"),
                FormValue(form, "Password"),
                FormValue(form, "TenantName")), cancellationToken);
            return result.Succeeded
                ? Results.Redirect("/account/check-email")
                : Results.Redirect($"/account/register?error={HttpUtility.UrlEncode(result.Error)}");
        }).WithMetadata(new RequireAntiforgeryTokenAttribute());

        endpoints.MapPost("/account/login", async (
            HttpRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var result = await sender.Send(new LoginCommand(
                FormValue(form, "Email"),
                FormValue(form, "Password")), cancellationToken);
            if (result.Succeeded)
            {
                return Results.Redirect("/");
            }

            var error = result.RequiresConfirmation
                ? "Please confirm your email before signing in."
                : "Sign-in failed. Check your email and password.";
            return Results.Redirect($"/account/login?error={HttpUtility.UrlEncode(error)}");
        }).WithMetadata(new RequireAntiforgeryTokenAttribute());

        endpoints.MapPost("/account/logout", async (
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            await sender.Send(new LogoutCommand(), cancellationToken);
            return Results.Redirect("/");
        }).RequireAuthorization().WithMetadata(new RequireAntiforgeryTokenAttribute());

        endpoints.MapGet("/account/confirm-email", async (
            string? userId,
            string? token,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var confirmed = !string.IsNullOrWhiteSpace(userId) &&
                !string.IsNullOrWhiteSpace(token) &&
                await sender.Send(new ConfirmEmailCommand(userId, token), cancellationToken);
            return Results.Redirect($"/account/confirmation-result?success={confirmed.ToString().ToLowerInvariant()}");
        });

        endpoints.MapPost("/account/forgot-password", async (
            HttpRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var form = await request.ReadFormAsync(cancellationToken);
            await sender.Send(new RequestPasswordResetCommand(FormValue(form, "Email")), cancellationToken);
            return Results.Redirect("/account/reset-email-sent");
        }).WithMetadata(new RequireAntiforgeryTokenAttribute());

        endpoints.MapPost("/account/reset-password", async (
            HttpRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var result = await sender.Send(new ResetPasswordCommand(
                FormValue(form, "UserId"),
                FormValue(form, "Token"),
                FormValue(form, "Password")), cancellationToken);
            return Results.Redirect(result ? "/account/login?reset=success" : "/account/reset-password?error=invalid");
        }).WithMetadata(new RequireAntiforgeryTokenAttribute());

        return endpoints;
    }

    private static string FormValue(IFormCollection form, string key) => form[key].ToString();
}
