using Microsoft.JSInterop;

namespace Bit.Besql;

/// <summary>
/// <inheritdoc cref="IBitBesqlStorage"/>
/// </summary>
public sealed class BitBesqlBrowserCacheStorage(IJSRuntime jsRuntime) : IBitBesqlStorage
{
    HashSet<string>? pausedFilesList;

    public async Task Persist(string fileName)
    {
        if (pausedFilesList is not null)
        {
            pausedFilesList.Add(fileName);
            return;
        }

        await jsRuntime.InvokeVoidAsync("BitBesql.persist", fileName).ConfigureAwait(false);
    }

    public async Task Load(string fileName)
    {
        await jsRuntime.InvokeVoidAsync("BitBesql.load", fileName).ConfigureAwait(false);
    }

    public void PauseAutomaticPersistent()
    {
        pausedFilesList = [];
    }

    public async Task ResumeAutomaticPersistent()
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
