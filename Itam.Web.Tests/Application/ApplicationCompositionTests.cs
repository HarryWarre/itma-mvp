using FluentValidation;
using Itam.Web.Application;
using Itam.Web.Application.Abstractions;
using Itam.Web.Application.Requests;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Itam.Web.Tests.Application;

public sealed class ApplicationCompositionTests
{
    [Fact]
    public async Task Registers_production_requests_with_mediatr()
    {
        using var provider = BuildProvider(new FakePersistence(), new FakeEmailSender());

        var status = await provider.GetRequiredService<ISender>().Send(new GetPlatformStatusQuery());

        Assert.Equal("ITMA", status.ApplicationName);
        Assert.Equal("Application foundation ready", status.FoundationStatus);
    }

    [Fact]
    public async Task Sends_a_valid_request_through_the_application_seam()
    {
        var persistence = new FakePersistence();
        var emailSender = new FakeEmailSender();
        using var provider = BuildProvider(persistence, emailSender);

        var response = await provider.GetRequiredService<ISender>().Send(
            new TestRequest("platform-ready"));

        Assert.Equal("platform-ready", response.Value);
        Assert.Equal(1, persistence.CommitCount);
        var message = Assert.Single(emailSender.Messages);
        Assert.Equal("platform-ready", message.Subject);
    }

    [Fact]
    public async Task Rejects_invalid_requests_before_adapters_are_called()
    {
        var persistence = new FakePersistence();
        var emailSender = new FakeEmailSender();
        using var provider = BuildProvider(persistence, emailSender);

        await Assert.ThrowsAsync<ValidationException>(() => provider.GetRequiredService<ISender>()
            .Send(new TestRequest("")));

        Assert.Equal(0, persistence.CommitCount);
        Assert.Empty(emailSender.Messages);
    }

    private static ServiceProvider BuildProvider(
        FakePersistence persistence,
        FakeEmailSender emailSender)
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IApplicationPersistence>(persistence);
        services.AddSingleton<IApplicationEmailSender>(emailSender);
        services.AddTransient<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddTransient<IValidator<TestRequest>, TestRequestValidator>();
        return services.BuildServiceProvider();
    }

    private sealed record TestRequest(string Value) : IRequest<TestResponse>;

    private sealed record TestResponse(string Value);

    private sealed class TestRequestHandler(
        IApplicationPersistence persistence,
        IApplicationEmailSender emailSender)
        : IRequestHandler<TestRequest, TestResponse>
    {
        public async Task<TestResponse> Handle(
            TestRequest request,
            CancellationToken cancellationToken)
        {
            await persistence.CommitAsync(cancellationToken);
            await emailSender.SendAsync(
                new ApplicationEmail("test@example.invalid", request.Value, "test body"),
                cancellationToken);
            return new TestResponse(request.Value);
        }
    }

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(request => request.Value).NotEmpty();
        }
    }

    private sealed class FakePersistence : IApplicationPersistence
    {
        public int CommitCount { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            CommitCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailSender : IApplicationEmailSender
    {
        public List<ApplicationEmail> Messages { get; } = [];

        public Task SendAsync(ApplicationEmail email, CancellationToken cancellationToken)
        {
            Messages.Add(email);
            return Task.CompletedTask;
        }
    }
}
