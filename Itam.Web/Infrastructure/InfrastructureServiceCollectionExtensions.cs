using Itam.Web.Application.Abstractions;
using Itam.Web.Infrastructure.Email;
using Itam.Web.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Itam.Web.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();
        services.Configure<CookieAuthenticationOptions>(
            IdentityConstants.ApplicationScheme,
            options => options.Events.OnValidatePrincipal = async context =>
            {
                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<ApplicationUser>>();
                var user = context.Principal is null
                    ? null
                    : await userManager.GetUserAsync(context.Principal);
                if (user is null || !user.IsActive)
                {
                    context.RejectPrincipal();
                }
            });
        services.AddAuthorization();

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IApplicationPersistence, EfCoreApplicationPersistence>();
        services.AddScoped<IApplicationEmailSender, SmtpApplicationEmailSender>();
        services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSender>();

        return services;
    }
}
