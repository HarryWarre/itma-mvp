using Blazored.LocalStorage;

namespace Itam.Web.Infrastructure.Preferences;

public sealed class BrowserPreferences(ILocalStorageService localStorage) : IBrowserPreferences
{
    private const string LanguageKey = "itma.preferences.language";
    private const string CompactNavigationKey = "itma.preferences.compact-navigation";

    public Task<string?> GetLanguageAsync(CancellationToken cancellationToken = default) =>
        localStorage.GetItemAsync<string>(LanguageKey, cancellationToken).AsTask();

    public Task SetLanguageAsync(
        string language,
        CancellationToken cancellationToken = default) =>
        localStorage.SetItemAsync(LanguageKey, language, cancellationToken).AsTask();

    public async Task<bool> GetCompactNavigationAsync(
        CancellationToken cancellationToken = default)
    {
        return await localStorage.GetItemAsync<bool?>(CompactNavigationKey, cancellationToken)
            .AsTask() ?? false;
    }

    public Task SetCompactNavigationAsync(
        bool compact,
        CancellationToken cancellationToken = default) =>
        localStorage.SetItemAsync(CompactNavigationKey, compact, cancellationToken).AsTask();
}
