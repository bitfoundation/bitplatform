using Microsoft.JSInterop;

namespace Bit.Besql;

public sealed class BrowserCacheBesqlStorage(IJSRuntime jsRuntime) : IBesqlStorage
{
    HashSet<string>? pausedFilesList;

    public async Task Init(string filename)
    {
        await jsRuntime.InvokeVoidAsync("BitBesql.init", filename).ConfigureAwait(false);
    }

    public async Task Persist(string filename)
    {
        if (pausedFilesList is not null)
        {
            pausedFilesList.Add(filename);
            return;
        }

        await jsRuntime.InvokeVoidAsync("BitBesql.persist", filename).ConfigureAwait(false);
    }

    public void PauseSync()
    {
        pausedFilesList = [];
    }

    public async Task ResumeSync()
    {
        if (pausedFilesList is null)
            throw new InvalidOperationException("bit Besql storage is not paused.");

        var files = pausedFilesList;
        pausedFilesList = null;

        foreach (var file in files)
        {
            await Persist(file).ConfigureAwait(false);
        }
    }
}
