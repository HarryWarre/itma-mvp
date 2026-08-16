using Serilog.Events;

namespace Itam.Web.Infrastructure.Logging;

public static class SensitiveDataLogFilter
{
    private static readonly string[] SensitivePropertyFragments =
    [
        "password",
        "token",
        "smtpcredential",
        "credential",
        "emailbody",
        "emailcontent",
        "emailsubject",
        "body",
        "content",
        "subject",
        "email",
        "secret",
        "confirmationlink",
        "resetlink",
        "link",
        "code",
        "value",
        "payload",
        "data"
    ];

    private static readonly string[] SensitiveMessageFragments =
    [
        "password",
        "token",
        "smtp credential"
    ];

    public static bool IsSensitive(LogEvent logEvent)
    {
        if (logEvent.Properties.Any(property =>
                IsSensitivePropertyName(property.Key) ||
                ContainsSensitiveNestedProperty(property.Value)))
        {
            return true;
        }

        return SensitiveMessageFragments.Any(fragment =>
            logEvent.MessageTemplate.Text.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSensitivePropertyName(string propertyName) =>
        SensitivePropertyFragments.Any(fragment =>
            propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsSensitiveNestedProperty(LogEventPropertyValue value) => value switch
    {
        StructureValue structure => structure.Properties.Any(property =>
            IsSensitivePropertyName(property.Name) || ContainsSensitiveNestedProperty(property.Value)),
        SequenceValue sequence => sequence.Elements.Any(ContainsSensitiveNestedProperty),
        _ => false
    };
}
