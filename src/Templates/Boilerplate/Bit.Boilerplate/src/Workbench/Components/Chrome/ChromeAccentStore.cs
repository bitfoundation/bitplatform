namespace Boilerplate.Workbench.Components.Chrome;

public partial class ChromeAccentStore : IBitAccentColorStore
{
    [AutoInject] private ChromeStore chromeStore = default!;

    public Task<string?> GetAccentAsync() => Task.FromResult(chromeStore.Accent);

    public Task SetAccentAsync(string accent)
    {
        chromeStore.PickAccent(accent);

        return Task.CompletedTask;
    }

    public Task RemoveAccentAsync()
    {
        chromeStore.PickAccent(null);

        return Task.CompletedTask;
    }
}
