using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Finbridge.Backoffice.Services;

public sealed class ThemeService
{
    private readonly IJSRuntime _js;
    private string _current = "lavender";
    private event Action? OnThemeChanged;

    public string Current => _current;

    public IReadOnlyList<ThemeInfo> Themes { get; } = new ThemeInfo[]
    {
        new("lavender",     "Лавандовая",   "#7c3aed", "light", "Нежная фиолетовая"),
        new("arctic-light", "Арктическая",   "#0284c7", "light", "Холодная голубая"),
        new("belaya",       "Белая",        "#dc2626", "light", "Чистая белая"),
    };

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitAsync()
    {
        var saved = await _js.InvokeAsync<string?>("themeStorage.get");
        if (!string.IsNullOrEmpty(saved) && Themes.Any(t => t.Id == saved))
        {
            _current = saved;
        }
        await ApplyAsync();
    }

    public async Task SetThemeAsync(string themeId)
    {
        if (!Themes.Any(t => t.Id == themeId))
            return;

        _current = themeId;
        await _js.InvokeVoidAsync("themeStorage.set", themeId);
        await ApplyAsync();
        OnThemeChanged?.Invoke();
    }

    public void Subscribe(Action callback) => OnThemeChanged += callback;
    public void Unsubscribe(Action callback) => OnThemeChanged -= callback;

    private async Task ApplyAsync()
    {
        await _js.InvokeVoidAsync("themeApply.apply", _current);
    }

    public record ThemeInfo(string Id, string Name, string Color, string Mode, string Description);
}
