using System.Net;
using System.Security.Claims;
using Itam.Web.Application.Authorization;
using Itam.Web.Application.Abstractions;
using Itam.Web.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Itam.Web.Application.Authentication;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string TenantName) : IRequest<RegisterResult>;

public sealed record RegisterResult(bool Succeeded, string? Error = null);

public sealed class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext dbContext,
    IApplicationEmailSender emailSender,
    IConfiguration configuration,
    ILogger<RegisterCommandHandler> logger)
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var tenantName = request.TenantName.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(tenantName))
        {
            return new(false, "Email, password, and workspace name are required.");
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return new(false, "Registration could not be completed with those details.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            IsActive = true
        };
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return new(false, "Registration could not be completed. Check the password requirements.");
        }

        if (!await roleManager.RoleExistsAsync("Owner"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole("Owner"));
            if (!roleResult.Succeeded)
            {
                return new(false, "Registration could not be completed.");
            }
        }

        var roleAssignment = await userManager.AddToRoleAsync(user, "Owner");
        if (!roleAssignment.Succeeded)
        {
            return new(false, "Registration could not be completed.");
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = tenantName,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Tenants.Add(tenant);
        dbContext.TenantMemberships.Add(new TenantMembership
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            RoleName = "Owner"
        });

        foreach (var permissionName in PermissionCatalog.All)
        {
            var permission = await dbContext.PermissionDefinitions
                .SingleOrDefaultAsync(item => item.Name == permissionName, cancellationToken);
            if (permission is null)
            {
                permission = new PermissionDefinition { Id = Guid.NewGuid(), Name = permissionName };
                dbContext.PermissionDefinitions.Add(permission);
            }

            if (!await dbContext.RolePermissions.AnyAsync(
                    item => item.RoleName == "Owner" && item.PermissionName == permissionName,
                    cancellationToken))
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleName = "Owner",
                    PermissionName = permissionName
                });
            }
        }

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            TenantId = tenant.Id,
            ActorUserId = user.Id,
            Action = "account.registered",
            Target = user.Id,
            TimestampUtc = DateTimeOffset.UtcNow,
            Outcome = "succeeded"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var baseUrl = configuration["Application:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
        var link = $"{baseUrl}/account/confirm-email?userId={WebUtility.UrlEncode(user.Id)}&token={WebUtility.UrlEncode(token)}";
        try
        {
            await emailSender.SendAsync(
                new ApplicationEmail(
                    email,
                    "Confirm your ITMA email address",
                    $"Confirm your ITMA email address by opening this link: {link}"),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Confirmation email delivery failed for a new account");
            return new(false, "Your account was created, but the confirmation email could not be sent.");
        }

        return new(true);
    }
}

public sealed record ConfirmEmailCommand(string UserId, string Token) : IRequest<bool>;

public sealed class ConfirmEmailCommandHandler(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext)
    : IRequestHandler<ConfirmEmailCommand, bool>
{
    public async Task<bool> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null || !user.IsActive)
        {
            return false;
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return false;
        }

        var membership = await dbContext.TenantMemberships
            .SingleOrDefaultAsync(item => item.UserId == user.Id && item.IsActive, cancellationToken);
        if (membership is not null)
        {
            dbContext.AuditLogEntries.Add(new AuditLogEntry
            {
                TenantId = membership.TenantId,
                ActorUserId = user.Id,
                Action = "account.email-confirmed",
                Target = user.Id,
                TimestampUtc = DateTimeOffset.UtcNow,
                Outcome = "succeeded"
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public sealed record LoginResult(bool Succeeded, bool RequiresConfirmation = false);

public sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext dbContext)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
        {
            return new(false);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return new(false, result.IsNotAllowed);
        }

        var membership = await dbContext.TenantMemberships
            .SingleOrDefaultAsync(item => item.UserId == user.Id && item.IsActive, cancellationToken);
        if (membership is not null)
        {
            dbContext.AuditLogEntries.Add(new AuditLogEntry
            {
                TenantId = membership.TenantId,
                ActorUserId = user.Id,
                Action = "account.signed-in",
                Target = user.Id,
                TimestampUtc = DateTimeOffset.UtcNow,
                Outcome = "succeeded"
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new(true);
    }
}

public sealed record LogoutCommand : IRequest<Unit>;

public sealed class LogoutCommandHandler(SignInManager<ApplicationUser> signInManager)
    : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await signInManager.SignOutAsync();
        return Unit.Value;
    }
}

public sealed record RequestPasswordResetCommand(string Email) : IRequest<Unit>;

public sealed class RequestPasswordResetCommandHandler(
    UserManager<ApplicationUser> userManager,
    IApplicationEmailSender emailSender,
    IConfiguration configuration,
    ILogger<RequestPasswordResetCommandHandler> logger)
    : IRequestHandler<RequestPasswordResetCommand, Unit>
{
    public async Task<Unit> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || !await userManager.IsEmailConfirmedAsync(user))
        {
            return Unit.Value;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var baseUrl = configuration["Application:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
        var link = $"{baseUrl}/account/reset-password?userId={WebUtility.UrlEncode(user.Id)}&token={WebUtility.UrlEncode(token)}";
        try
        {
            await emailSender.SendAsync(
                new ApplicationEmail(
                    user.Email!,
                    "Reset your ITMA password",
                    $"Reset your ITMA password by opening this link: {link}"),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Password reset email delivery failed");
        }

        return Unit.Value;
    }
}

public sealed record ResetPasswordCommand(
    string UserId,
    string Token,
    string Password) : IRequest<bool>;

public sealed class ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ResetPasswordCommand, bool>
{
    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null || !user.IsActive)
        {
            return false;
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.Password);
        return result.Succeeded;
    }
}

public sealed record DeactivateAccountCommand(string UserId) : IRequest<bool>;

public sealed class DeactivateAccountCommandHandler(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext)
    : IRequestHandler<DeactivateAccountCommand, bool>
{
    public async Task<bool> Handle(
        DeactivateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null || !user.IsActive)
        {
            return false;
        }

        user.IsActive = false;
        await userManager.UpdateSecurityStampAsync(user);
        var membership = await dbContext.TenantMemberships
            .SingleOrDefaultAsync(item => item.UserId == user.Id && item.IsActive, cancellationToken);
        if (membership is not null)
        {
            dbContext.AuditLogEntries.Add(new AuditLogEntry
            {
                TenantId = membership.TenantId,
                ActorUserId = user.Id,
                Action = "account.deactivated",
                Target = user.Id,
                TimestampUtc = DateTimeOffset.UtcNow,
                Outcome = "succeeded"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
