namespace Bit.BlazorUI;

public partial class BitPdfViewer
{
    private BitPdfSeekableStreamByteSource? _streamSource;
    private CancellationTokenSource? _streamLoadCancellation;
    private StreamPreparation? _streamPreparation;

    private bool UseStreamWorker => BackgroundRendering || !OperatingSystem.IsBrowser();

    private bool UseDocumentWorker => BackgroundRendering
        || (IsNonBuffered && !OperatingSystem.IsBrowser());

    private sealed record StreamPreparation(BitPdfDocument Document, string? Title,
        IReadOnlyList<BitPdfOutlineItem> Outline, IReadOnlyList<BitPdfAttachment> Attachments,
        IReadOnlyList<BitPdfLayer> Layers, BitPdfMetadata? Metadata, IReadOnlyList<string> PageLabels,
        IReadOnlyList<BitPdfFormField> FormFields, IReadOnlyList<BitPdfStructElement> StructureTree);

    private static StreamPreparation PrepareStreamDocument(BitPdfSeekableStreamByteSource source, string? password)
    {
        var document = BitPdfDocument.Load(source, password);
        try
        {
            _ = document.Pages;
            BitPdfMetadata? metadata = null;
            IReadOnlyList<string> labels = [];
            IReadOnlyList<BitPdfFormField> forms = [];
            IReadOnlyList<BitPdfStructElement> structure = [];
            IReadOnlyList<BitPdfOutlineItem> outline = [];
            IReadOnlyList<BitPdfAttachment> attachments = [];
            IReadOnlyList<BitPdfLayer> layers = [];
            try { metadata = document.Metadata; }
            catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex)) { }
            try { labels = document.PageLabels; }
            catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex)) { }
            try { outline = document.Outline; }
            catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex)) { }
            try { attachments = document.Attachments; }
            catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex)) { }
            try { layers = document.Layers; }
            catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex)) { }
            // Public information getters must not initiate server I/O on a render.
            // Forms and tagged structure are populated explicitly by LoadDocumentDetailsAsync.
            string? title = string.IsNullOrWhiteSpace(metadata?.Title) ? null : metadata.Title;
            return new(document, title, outline, attachments, layers, metadata, labels, forms, structure);
        }
        catch
        {
            document.ReleaseByteWindows();
            throw;
        }
    }

    private async Task LoadStreamDocumentAsync(BitPdfSource source, int version)
    {
        var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _streamLoadCancellation = cancellation;
        Stream? opened = null;
        BitPdfSeekableStreamByteSource? owned = null;
        StreamPreparation? prepared = null;
        bool published = false;
        try
        {
            opened = UseStreamWorker
                ? await Task.Run(() => source.StreamFactory!(token), token)
                : await source.StreamFactory!(token);
            if (opened is null) throw new InvalidOperationException("The PDF stream factory returned no stream.");
            token.ThrowIfCancellationRequested();
            if (IsDisposed || version != _loadVersion) return;
            // Length/Position can themselves involve a provider: validate off circuit.
            owned = UseStreamWorker
                ? await Task.Run(() => new BitPdfSeekableStreamByteSource(opened, token), token)
                : new BitPdfSeekableStreamByteSource(opened, token);
            opened = null; // ownership transferred, including cleanup after validation
            token.ThrowIfCancellationRequested();
            if (IsDisposed || version != _loadVersion) return;
            _streamSource = owned;

            string? password = source.Password;
            while (true)
            {
                try
                {
                    await _renderGate.WaitAsync(token);
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        if (IsDisposed || version != _loadVersion) return;
                        prepared = UseStreamWorker
                            ? await Task.Run(() => PrepareStreamDocument(owned, password), token)
                            : PrepareStreamDocument(owned, password);
                    }
                    finally { _renderGate.Release(); }
                    break;
                }
                catch (BitPdfPasswordException ex)
                {
                    if (IsDisposed || version != _loadVersion) return;
                    // Match buffered loading: a prerender cannot answer the built-in password dialog.
                    if (OnPasswordRequested is null && ShowPasswordPrompt && IsRendered is false)
                    {
                        _loadDeferred = true;
                        return;
                    }
                    string? entered = OnPasswordRequested is not null
                        ? await OnPasswordRequested()
                        : ShowPasswordPrompt ? await PromptForPasswordAsync(ex.WasProvided) : null;
                    token.ThrowIfCancellationRequested();
                    if (IsDisposed || version != _loadVersion) return;
                    if (string.IsNullOrEmpty(entered)) throw;
                    password = entered;
                    _loading = true;
                    StateHasChanged();
                }
            }
            token.ThrowIfCancellationRequested();
            if (IsDisposed || version != _loadVersion) return;
            _document = prepared.Document;
            _streamPreparation = prepared;
            _documentTitle = prepared.Title;
            _outline = prepared.Outline;
            _attachments = prepared.Attachments;
            _layers = prepared.Layers;
            _hiddenLayers = _layers.Count > 0
                ? [.. _layers.Where(layer => !layer.VisibleByDefault).Select(layer => layer.Id)] : null;
            _hiddenLayersView = _hiddenLayers is null ? null : new HashSet<string>(_hiddenLayers);
            _bytes = null;
            published = true;
            PreparePages(); // geometry and page tree were resolved by the worker above
            await RenderCurrentPageEagerlyAsync();
            if (IsDisposed || version != _loadVersion) return;
            _status = string.Format(ActiveTexts.PageCountFormat, _document.PageCount);
            _spyPending = true;
            if (_document.Warnings.Count > 0 && OnWarnings.HasDelegate)
            {
                await OnWarnings.InvokeAsync(_document.Warnings);
                if (IsDisposed || version != _loadVersion) return;
            }
            _loadedVersion = version;
        }
        catch (Exception) when (token.IsCancellationRequested || IsDisposed || version != _loadVersion)
        {
            // Supersede/disposal is cancellation, not a failed document or page.
        }
        catch (Exception ex)
        {
            if (!published)
            {
                prepared?.Document.ReleaseByteWindows();
                if (owned is not null) await owned.DisposeAsync();
                if (ReferenceEquals(_streamSource, owned)) _streamSource = null;
            }
            _status = string.Format(ActiveTexts.ErrorFormat, ex.Message);
            _errorMessage = ex.Message;
            await OnError.InvokeAsync(ex.Message);
        }
        finally
        {
            if (opened is not null) await opened.DisposeAsync();
            if (!published)
            {
                prepared?.Document.ReleaseByteWindows();
                if (owned is not null) await owned.DisposeAsync();
                if (ReferenceEquals(_streamSource, owned)) _streamSource = null;
                if (ReferenceEquals(_streamLoadCancellation, cancellation)) _streamLoadCancellation = null;
                cancellation.Dispose();
            }
            if (version == _loadVersion && _loadDeferred is false)
            {
                _loading = false;
                _progress = null;
            }
        }
    }

    /// <summary>
    /// Returns complete original PDF bytes, or null when no document is loaded.
    /// A non-buffered source is copied asynchronously (from its captured stream
    /// origin, or through validated HTTP ranges). That array belongs to the caller: the viewer does not retain it, and GetBytes
    /// continues returning null. Buffered sources return their existing buffer.
    /// </summary>
    public async Task<byte[]?> GetBytesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_rangeSource is not null) return await GetRangeBytesAsync(cancellationToken);
        var source = _document is null ? null : _streamSource;
        if (source is null) return _bytes;
        int version = _loadVersion;
        var loadToken = _streamLoadCancellation?.Token ?? default;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(loadToken, cancellationToken);
        // Use the same scheduler as synchronous page/thumbnail builds: on WASM
        // they cannot wait for a yielding ReadAsync while holding the browser thread.
        await _renderGate.WaitAsync(linked.Token);
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            if (IsDisposed || version != _loadVersion) throw new OperationCanceledException(linked.Token);
            var bytes = UseStreamWorker
                ? await Task.Run(() => source.ReadAllAsync(linked.Token), linked.Token)
                : await source.ReadAllAsync(linked.Token);
            linked.Token.ThrowIfCancellationRequested();
            if (IsDisposed || version != _loadVersion) throw new OperationCanceledException(linked.Token);
            return bytes;
        }
        catch (ObjectDisposedException) when (IsDisposed || version != _loadVersion || linked.IsCancellationRequested)
        {
            throw new OperationCanceledException(linked.Token);
        }
        finally { _renderGate.Release(); }
    }

    private void RequireBufferedHelper(string asyncMethod)
    {
        if (_document is not null && IsNonBuffered)
            throw new InvalidOperationException($"A stream-backed or Range-backed PDF requires {asyncMethod} so reading does not block the circuit.");
    }

    private async Task<T> RunDocumentWorkAsync<T>(Func<BitPdfDocument, T> build, CancellationToken cancellationToken, int? pageIndex = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int version = _loadVersion;
        var document = _document ?? throw new InvalidOperationException("No PDF is loaded.");
        if (_rangeSource is { } rangeSource)
        {
            try { return await RunRangeWorkAsync(document, build, cancellationToken, pageIndex); }
            catch (BitPdfRangeVersionException ex)
            {
                await FailRangeVersionAsync(rangeSource, version, ex.Message);
                throw;
            }
        }
        var loadToken = _streamLoadCancellation?.Token ?? default;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(loadToken, cancellationToken);
        await _renderGate.WaitAsync(linked.Token);
        try
        {
            if (IsDisposed || version != _loadVersion) throw new OperationCanceledException(linked.Token);
            T result = UseDocumentWorker
                ? await Task.Run(() => build(document), linked.Token)
                : build(document);
            linked.Token.ThrowIfCancellationRequested();
            if (IsDisposed || version != _loadVersion) throw new OperationCanceledException(linked.Token);
            return result;
        }
        catch (ObjectDisposedException) when (IsDisposed || version != _loadVersion || linked.IsCancellationRequested)
        {
            throw new OperationCanceledException(linked.Token);
        }
        finally { _renderGate.Release(); }
    }

    /// <summary>Renders one page to HTML without blocking server stream reads on the circuit.</summary>
    public Task<string> RenderPageHtmlAsync(int pageNumber, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_document is null || pageNumber < 1 || pageNumber > _pages.Count) return Task.FromResult(string.Empty);
        int rotation = Rotation;
        var coalescing = TextCoalescing;
        var layers = _hiddenLayersView;
        return RunDocumentWorkAsync(document => new BitPdfHtmlRenderer(document.Pages[pageNumber - 1], document.XRef, rotation)
        {
            TextCoalescing = coalescing,
            HiddenLayers = layers,
        }.Render(), cancellationToken, pageNumber - 1);
    }

    /// <summary>Extracts one page's text asynchronously; empty when unavailable.</summary>
    public Task<string> ExtractPageTextAsync(int pageNumber, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_document is null || pageNumber < 1 || pageNumber > _pages.Count) return Task.FromResult(string.Empty);
        return RunDocumentWorkAsync(document => document.Pages[pageNumber - 1].ExtractText(), cancellationToken, pageNumber - 1);
    }

    /// <summary>Extracts all page text asynchronously, preserving the viewer's text index.</summary>
    public async Task<string> ExtractTextAsync(string pageSeparator = "\n\n", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_document is null) return string.Empty;
        var cached = _pageText?.ToArray();
        if (_rangeSource is not null)
        {
            int version = _loadVersion;
            var result = new string[_pages.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = cached?[i] ?? await ExtractPageTextAsync(i + 1, cancellationToken);
                if (IsDisposed || version != _loadVersion) throw new OperationCanceledException(cancellationToken);
            }
            _pageText = result;
            return string.Join(pageSeparator, result);
        }
        var text = await RunDocumentWorkAsync(document =>
        {
            var result = new string[document.PageCount];
            for (int i = 0; i < result.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result[i] = cached?[i] ?? document.Pages[i].ExtractText();
            }
            return result;
        }, cancellationToken);
        _pageText = text;
        return string.Join(pageSeparator, text);
    }
}
