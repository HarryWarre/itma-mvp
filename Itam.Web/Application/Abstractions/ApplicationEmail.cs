namespace Itam.Web.Application.Abstractions;

public sealed record ApplicationEmail(string Recipient, string Subject, string Body);
