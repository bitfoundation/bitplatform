namespace Bit.Butil.Demo.Services;

/// <summary>
/// Holds the site-wide dark/light theme, dogfooding Bit.Butil itself:
/// the choice is persisted with <see cref="LocalStorage"/> and the OS preference
/// is detected with <see cref="Window.MatchMedia"/>.
/// </summary>
public class ThemeService(LocalStorage localStorage, Window window)
{
    private const string StorageKey = "bit-butil-demo-theme";

    public string Current { get; private set; } = "dark";

    public bool IsDark => Current == "dark";

    public event Action? Changed;

    public async Task Initialize()
    {
        var stored = await localStorage.GetItem(StorageKey);
        if (stored is "dark" or "light")
        {
            Current = stored;
        }
        else
        {
            var query = await window.MatchMedia("(prefers-color-scheme: light)");
            Current = query.Matches ? "light" : "dark";
        }

        Changed?.Invoke();
    }

    public async Task Toggle()
    {
        Current = IsDark ? "light" : "dark";
        await localStorage.SetItem(StorageKey, Current);
        Changed?.Invoke();
    }
}
