using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Itam.Web.Application.Validation;

public sealed class ValidationBehavior<TRequest, TResponse>(IServiceProvider services)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validator = services.GetService<IValidator<TRequest>>();
        if (validator is not null)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        return await next(cancellationToken);
    }
}
