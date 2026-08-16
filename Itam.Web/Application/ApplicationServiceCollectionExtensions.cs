using FluentValidation;
using Itam.Web.Application.Validation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Itam.Web.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceCollectionExtensions).Assembly;
        services.AddLogging();
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(applicationAssembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(applicationAssembly);

        return services;
    }
}
