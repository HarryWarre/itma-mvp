namespace Itam.Web.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string FromAddress { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string? Password { get; set; }
}
