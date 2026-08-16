using System.Net;
using System.Net.Mail;
using Itam.Web.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Itam.Web.Infrastructure.Email;

public sealed class SmtpApplicationEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpApplicationEmailSender> logger,
    IHostEnvironment? environment = null)
    : IApplicationEmailSender
{
    public async Task SendAsync(
        ApplicationEmail email,
        CancellationToken cancellationToken = default)
    {
        var smtpOptions = options.Value;
        if (string.IsNullOrWhiteSpace(smtpOptions.Host) ||
            string.IsNullOrWhiteSpace(smtpOptions.FromAddress))
        {
            throw new InvalidOperationException(
                "SMTP is not configured. Set Smtp:Host and Smtp:FromAddress before sending email.");
        }

        if (smtpOptions.Host.Equals("smtp.ethereal.email", StringComparison.OrdinalIgnoreCase) &&
            environment is not null &&
            !environment.IsDevelopment())
        {
            throw new InvalidOperationException("Ethereal SMTP is available only in the Development environment.");
        }

        if (string.IsNullOrWhiteSpace(smtpOptions.UserName) !=
            string.IsNullOrWhiteSpace(smtpOptions.Password))
        {
            throw new InvalidOperationException("SMTP username and password must be configured together.");
        }

        using var message = new MailMessage(smtpOptions.FromAddress, email.Recipient)
        {
            Subject = email.Subject,
            Body = email.Body,
            IsBodyHtml = false
        };
        using var client = new SmtpClient(smtpOptions.Host, smtpOptions.Port)
        {
            EnableSsl = smtpOptions.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(smtpOptions.UserName))
        {
            client.Credentials = new NetworkCredential(smtpOptions.UserName, smtpOptions.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation(
            "Application email sent to recipient domain {RecipientDomain}",
            GetRecipientDomain(email.Recipient));
    }

    private static string GetRecipientDomain(string recipient)
    {
        var separator = recipient.LastIndexOf('@');
        return separator >= 0 && separator < recipient.Length - 1
            ? recipient[(separator + 1)..]
            : "unknown";
    }
}
