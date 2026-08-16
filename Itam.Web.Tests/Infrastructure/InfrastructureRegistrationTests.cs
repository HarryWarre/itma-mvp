using Itam.Web.Infrastructure;
using Itam.Web.Infrastructure.Email;
using Itam.Web.Infrastructure.Persistence;
using Itam.Web.Infrastructure.Preferences;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Itam.Web.Tests.Infrastructure;

public sealed class InfrastructureRegistrationTests
{
    [Fact]
    public void Registers_confirmed_identity_postgres_and_safe_preference_ports()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=itma;Username=postgres",
                ["Smtp:Host"] = "smtp.ethereal.email",
                ["Smtp:FromAddress"] = "itma@example.invalid"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.True(provider.GetRequiredService<IOptions<IdentityOptions>>().Value.SignIn.RequireConfirmedEmail);
        Assert.IsType<EfCoreApplicationPersistence>(
            provider.GetRequiredService<Itam.Web.Application.Abstractions.IApplicationPersistence>());
        Assert.IsType<SmtpApplicationEmailSender>(
            provider.GetRequiredService<Itam.Web.Application.Abstractions.IApplicationEmailSender>());
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IBrowserPreferences));
        Assert.Equal("smtp.ethereal.email", provider.GetRequiredService<IOptions<SmtpOptions>>().Value.Host);
    }
}
