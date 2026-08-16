using Itam.Web.Infrastructure.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Itam.Web.Tests.Infrastructure;

public sealed class SensitiveDataLogFilterTests
{
    [Fact]
    public void Excludes_sensitive_properties_but_keeps_safe_structured_events()
    {
        var sink = new CollectingSink();
        using var logger = new LoggerConfiguration()
            .Filter.ByExcluding(SensitiveDataLogFilter.IsSensitive)
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Registration attempted with {Password}", "never-write-this");
        logger.Information("Email payload {@Payload}", new { Body = "never-write-this" });
        logger.Information("Generic input {Value}", "never-write-this");
        logger.Information(
            "Application email sent to recipient domain {RecipientDomain}",
            "example.invalid");

        var eventRecord = Assert.Single(sink.Events);
        Assert.Equal("example.invalid", eventRecord.Properties["RecipientDomain"].ToString().Trim('"'));
    }

    [Fact]
    public void Excludes_sensitive_values_when_the_message_template_names_them()
    {
        var sink = new CollectingSink();
        using var logger = new LoggerConfiguration()
            .Filter.ByExcluding(SensitiveDataLogFilter.IsSensitive)
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Sending a confirmation token");

        Assert.Empty(sink.Events);
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
