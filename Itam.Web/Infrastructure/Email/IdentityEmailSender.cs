using Itam.Web.Application.Abstractions;
using Itam.Web.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Itam.Web.Infrastructure.Email;

public sealed class IdentityEmailSender(IApplicationEmailSender sender)
    : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink) =>
        sender.SendAsync(new ApplicationEmail(
            email,
            "Confirm your ITMA email address",
            $"Confirm your ITMA email address by opening this link: {confirmationLink}"));

    public Task SendPasswordResetLinkAsync(
        ApplicationUser user,
        string email,
        string resetLink) =>
        sender.SendAsync(new ApplicationEmail(
            email,
            "Reset your ITMA password",
            $"Reset your ITMA password by opening this link: {resetLink}"));

    public Task SendPasswordResetCodeAsync(
        ApplicationUser user,
        string email,
        string resetCode) =>
        sender.SendAsync(new ApplicationEmail(
            email,
            "Reset your ITMA password",
            $"Use this ITMA password reset code: {resetCode}"));
}
