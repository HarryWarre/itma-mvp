namespace Itam.Web.Infrastructure.Preferences;

public interface IBrowserPreferences
{
    Task<string?> GetLanguageAsync(CancellationToken cancellationToken = default);

    Task SetLanguageAsync(string language, CancellationToken cancellationToken = default);

    Task<bool> GetCompactNavigationAsync(CancellationToken cancellationToken = default);

    Task SetCompactNavigationAsync(bool compact, CancellationToken cancellationToken = default);
}
