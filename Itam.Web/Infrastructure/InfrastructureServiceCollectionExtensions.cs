using Blazored.LocalStorage;
using Itam.Web.Application.Abstractions;
using Itam.Web.Infrastructure.Email;
using Itam.Web.Infrastructure.Persistence;
using Itam.Web.Infrastructure.Preferences;
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
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();
        services.AddAuthorization();

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IApplicationPersistence, EfCoreApplicationPersistence>();
        services.AddScoped<IApplicationEmailSender, SmtpApplicationEmailSender>();

        // Feature components receive only the named preference port below. Authentication
        // state, tokens, passwords, and email contents never belong in browser storage.
        services.AddBlazoredLocalStorage();
        services.AddScoped<IBrowserPreferences, BrowserPreferences>();

        return services;
    }
}
