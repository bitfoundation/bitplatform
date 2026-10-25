using System.IO;
using System.Net.Http;

namespace Bit.BlazorUI;

public partial class BitPdfViewer
{
    private BitPdfHttpRangeByteSource? _rangeSource;
    private bool _rangePublished;
    private bool _rangeEssentialsPending;
    private bool _rangeAttachmentsLoaded;
    private bool _rangeDetailsLoaded;
    private Task? _rangeBackgroundTask;
    private readonly SemaphoreSlim _detailsGate = new(1, 1);
    private BitPdfMetadata? _rangeMetadata;
    private IReadOnlyList<string> _rangeLabels = [];
    private IReadOnlyList<BitPdfFormField> _rangeForms = [];
    private IReadOnlyList<BitPdfStructElement> _rangeStructure = [];
    private double _rangeEstimateWidth = 612, _rangeEstimateHeight = 792;
    private bool HasDeferredAttachments => _rangeSource is not null && !_rangeAttachmentsLoaded;
    private bool IsNonBuffered => _streamSource is not null || _rangeSource is not null;

    private sealed record RangeAttempt<T>(T? Value, List<(int Offset, int Length)>? Missing);

    private async Task<T> RunRangeFactoryAsync<T>(BitPdfHttpRangeByteSource source, Func<T> build, int version, CancellationToken token)
    {
        using var operation = source.BeginOperation();
        for (int attempt = 0; attempt < 512; attempt++)
        {
            BitPdfMissingDataException? missing = null;
            await _renderGate.WaitAsync(token);
            try
            {
                CheckRangeOperation(source, version, token);
                try
                {
                    return !OperatingSystem.IsBrowser() || BackgroundRendering
                        ? await Task.Run(build, token) : build();
                }
                catch (BitPdfMissingDataException ex) { missing = ex; }
            }
            finally { _renderGate.Release(); }
            source.RecordRestart();
            await source.EnsureAsync(missing!.Offset, missing.Length, token);
        }
        throw new BitPdfByteSourceIOException("The PDF load exceeded its missing-data retry limit.");
    }

    private void CheckRangeOperation(BitPdfHttpRangeByteSource source, int version, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (IsDisposed || version != _loadVersion || !ReferenceEquals(_rangeSource, source))
            throw new OperationCanceledException(token);
        source.CheckRead();
    }

    private async Task<T> RunRangeWorkAsync<T>(BitPdfDocument document, Func<BitPdfDocument, T> build,
        CancellationToken cancellationToken = default, int? pageIndex = null)
    {
        var source = _rangeSource ?? throw new InvalidOperationException("No Range PDF is loaded.");
        int version = _loadVersion;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_streamLoadCancellation?.Token ?? default, cancellationToken);
        var token = linked.Token;
        try
        {
            if (pageIndex is int requested) await PrepareRangePageAsync(document, source, version, requested, token);
            using var operation = source.BeginOperation();
            for (int attempt = 0; attempt < 512; attempt++)
            {
                await _renderGate.WaitAsync(token);
                RangeAttempt<T> outcome;
                try
                {
                    CheckRangeOperation(source, version, token);
                    RangeAttempt<T> Execute()
                    {
                        using var transaction = document.BeginPartialAttempt();
                        try
                        {
                            if (pageIndex is int page)
                            {
                                var dependencies = document.CollectPageDependencies(page, source);
                                if (dependencies.Count > 0)
                                {
                                    // Discovery completed: only complete immutable objects enter the xref cache.
                                    transaction.Commit();
                                    return new(default, dependencies);
                                }
                            }
                            T value = build(document);
                            transaction.Commit();
                            return new(value, null);
                        }
                        catch (BitPdfMissingDataException missing)
                        { return new(default, [(missing.Offset, missing.Length)]); }
                        finally { document.ReleaseByteWindows(); }
                    }
                    outcome = !OperatingSystem.IsBrowser() || BackgroundRendering
                        ? await Task.Run(Execute, token) : Execute();
                }
                finally { _renderGate.Release(); }
                CheckRangeOperation(source, version, token);
                if (outcome.Missing is null) return outcome.Value!;
                source.RecordRestart();
                await source.EnsureRangesAsync(outcome.Missing, token);
            }
            throw new BitPdfByteSourceIOException("The PDF operation exceeded its missing-data retry limit.");
        }
        catch (ObjectDisposedException) when (IsDisposed || version != _loadVersion || token.IsCancellationRequested)
        { throw new OperationCanceledException(token); }
    }

    private async Task PrepareRangePageAsync(BitPdfDocument document, BitPdfHttpRangeByteSource source,
        int version, int index, CancellationToken token)
    {
        CheckRangeOperation(source, version, token);
        // Children share this request budget, but own separate raw-byte pins.
        using var budget = source.BeginOperation();
        var walk = await RunRangeWorkAsync(document, d => d.BeginPartialPageWalk(index), token);
        if (walk is null) return;
        for (int step = 0; step < 2_000_128; step++)
        {
            CheckRangeOperation(source, version, token);
            if (await RunRangeWorkAsync(document, _ => walk.Advance(), token)) return;
            if (step % 64 == 63) await Task.Delay(1, token);
        }
        throw new BitPdfByteSourceIOException("The PDF page lookup exceeded its page-tree step limit.");
    }

    private async Task<(bool Handled, byte[]? Bytes, string? Password)> TryLoadRangeDocumentAsync(HttpClient http, BitPdfSource source, int version)
    {
        var cancellation = new CancellationTokenSource();
        _streamLoadCancellation = cancellation;
        var token = cancellation.Token;
        BitPdfHttpRangeByteSource? owned = null;
        BitPdfDocument? document = null;
        HttpResponseMessage? fallbackResponse = null;
        bool published = false, fallback = false;
        string? password = source.Password;
        try
        {
            owned = await BitPdfHttpRangeByteSource.OpenAsync(http, source, token);
            if (IsDisposed || version != _loadVersion) return (true, null, password);
            _rangeSource = owned;
            while (true)
            {
                try
                {
                    document = await RunRangeFactoryAsync(owned, () => BitPdfDocument.Load(owned, password), version, token);
                    break;
                }
                catch (BitPdfPasswordException ex)
                {
                    if (IsDisposed || version != _loadVersion) return (true, null, password);
                    if (OnPasswordRequested is null && ShowPasswordPrompt && !IsRendered)
                    { _loadDeferred = true; return (true, null, password); }
                    string? entered = OnPasswordRequested is not null ? await OnPasswordRequested()
                        : ShowPasswordPrompt ? await PromptForPasswordAsync(ex.WasProvided) : null;
                    if (string.IsNullOrEmpty(entered)) throw;
                    if (IsDisposed || version != _loadVersion) return (true, null, password);
                    password = entered; _loading = true; StateHasChanged();
                }
            }
            int pageCount = await RunRangeWorkAsync(document, d => d.PageCount, token);
            if (pageCount > 0)
                await PrepareRangePageAsync(document, owned, version, Math.Clamp(CurrentPage - 1, 0, pageCount - 1), token);
            var prepared = await RunRangeWorkAsync(document, d =>
            {
                int count = d.PageCount;
                int index = count == 0 ? 0 : Math.Clamp(CurrentPage - 1, 0, count - 1);
                var first = count == 0 ? null : d.Pages[index];
                string? title = null;
                try { title = d.ReadPartialTitle(); }
                catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex)) { }
                return (First: first, Title: title, Hidden: BitPdfOptionalContent.DefaultHiddenIds(d.XRef, d.Catalog));
            }, token);
            CheckRangeOperation(owned, version, token);
            _rangeEstimateWidth = prepared.First?.Width ?? 612;
            _rangeEstimateHeight = prepared.First?.Height ?? 792;
            _documentTitle = string.IsNullOrWhiteSpace(prepared.Title) ? null : prepared.Title;
            _document = document;
            _hiddenLayers = prepared.Hidden;
            _hiddenLayersView = new HashSet<string>(_hiddenLayers);
            _bytes = null;
            PreparePages();
            await RenderCurrentPageEagerlyAsync(throwRangeFailures: true);
            CheckRangeOperation(owned, version, token);
            _status = string.Format(ActiveTexts.PageCountFormat, document.PageCount);
            _loading = false; _progress = null;
            _spyPending = true;
            _rangePublished = true; published = true;
            _rangeEssentialsPending = true;
            if (document.Warnings.Count > 0 && OnWarnings.HasDelegate)
            {
                await OnWarnings.InvokeAsync(document.Warnings);
                if (IsDisposed || version != _loadVersion) return (true, null, password);
            }
            _loadedVersion = version;
            return (true, null, password);
        }
        catch (BitPdfRangeVersionException ex)
        {
            if (owned is not null) await FailRangeVersionAsync(owned, version, ex.Message);
            throw;
        }
        catch (BitPdfRangeFallbackException ex) { fallback = true; fallbackResponse = ex.Response; }
        catch (BitPdfByteSourceIOException) when (!published) { fallback = true; }
        finally
        {
            if (!published)
            {
                document?.ReleaseByteWindows();
                if (version == _loadVersion && ReferenceEquals(_document, document))
                {
                    _document = null;
                    _pages.Clear(); _thumbs.Clear(); _pageWidths.Clear(); _pageHeights.Clear();
                    _canvasOps.Clear(); _canvasDirty.Clear();
                }
                if (ReferenceEquals(_rangeSource, owned)) _rangeSource = null;
                if (owned is not null) await owned.DisposeAsync();
                if (!fallback)
                {
                    if (ReferenceEquals(_streamLoadCancellation, cancellation)) _streamLoadCancellation = null;
                    cancellation.Dispose();
                }
            }
        }
        try
        {
            if (IsDisposed || version != _loadVersion) { fallbackResponse?.Dispose(); return (true, null, password); }
            var bytes = await FetchAsync(http, source, version, fallbackResponse, source.RangeLoading!.MaxFallbackSize, token);
            return (false, bytes, password);
        }
        finally
        {
            if (ReferenceEquals(_streamLoadCancellation, cancellation)) _streamLoadCancellation = null;
            cancellation.Dispose();
        }
    }

    private async Task FailRangeVersionAsync(BitPdfHttpRangeByteSource source, int version, string message)
    {
        if (IsDisposed || version != _loadVersion || !ReferenceEquals(source, _rangeSource)) return;
        int failedVersion = ++_loadVersion;
        _streamLoadCancellation?.Cancel();
        await _renderGate.WaitAsync();
        try
        {
            if (IsDisposed || failedVersion != _loadVersion || !ReferenceEquals(source, _rangeSource)) return;
            _document?.ReleaseByteWindows(); _document = null;
            _loadedVersion = 0; _bytes = null;
            _pages.Clear(); _thumbs.Clear(); _pageWidths.Clear(); _pageHeights.Clear();
            _failedPages.Clear(); _failedThumbs.Clear();
            _renderEpoch++;
            _renderQueue.Clear(); _renderQueued.Clear(); _thumbQueue.Clear(); _thumbQueued.Clear();
            _canvasOps.Clear(); _canvasDirty.Clear(); _fontStore = null; _fontFaceStyle = string.Empty;
            _pageText = null; _documentTitle = null;
            _outline = []; _attachments = []; _layers = [];
            _rangeSource = null; ResetRangeDetails();
            _errorMessage = message; _status = string.Format(ActiveTexts.ErrorFormat, message);
            _loading = false; _progress = null;
        }
        finally { _renderGate.Release(); }
        await source.DisposeAsync();
        if (!IsDisposed && failedVersion == _loadVersion)
        { StateHasChanged(); await OnError.InvokeAsync(message); }
    }

    private void ResetRangeDetails()
    {
        _rangePublished = false; _rangeEssentialsPending = false;
        _rangeAttachmentsLoaded = false; _rangeDetailsLoaded = false;
        _rangeMetadata = null; _rangeLabels = []; _rangeForms = []; _rangeStructure = [];
        _rangeEssentialsLoaded = false; _rangeAttachmentsLoading = false;
        _rangeBackgroundTask = null;
    }

    private async Task RetireRangeSourceAsync()
    {
        var source = _rangeSource; _rangeSource = null; ResetRangeDetails();
        if (source is not null) await source.DisposeAsync();
    }

    private void PrepareRangeSlots()
    {
        int count = _document!.PageCount;
        bool swap = Rotation % 180 == 90;
        for (int i = 0; i < count; i++)
        {
            _pages.Add(null); _thumbs.Add(null);
            _pageWidths.Add(swap ? _rangeEstimateHeight : _rangeEstimateWidth);
            _pageHeights.Add(swap ? _rangeEstimateWidth : _rangeEstimateHeight);
        }
        if (_showThumbnails) _thumbSpyPending = true;
    }

    private async Task<bool> RenderRangePageAsync(int index, bool thumbnail, bool throwOnFailure = false)
    {
        var source = _rangeSource;
        var document = _document;
        var slots = thumbnail ? _thumbs : _pages;
        if (source is null || document is null || IsDisposed || (uint)index >= (uint)slots.Count || slots[index] is not null) return false;
        int version = _loadVersion, epoch = _renderEpoch, rotation = Rotation;
        var coalescing = TextCoalescing; var mode = RenderMode;
        bool rendered = false;
        try
        {
            var result = await RunRangeWorkAsync(document, d =>
            {
                var store = (_fontStore ?? new BitPdfFontStore()).Fork();
                var page = d.Pages[index];
                var built = thumbnail ? BuildThumb(index, rotation, coalescing, mode, store)
                    : BuildPage(index, rotation, coalescing, mode, false, store);
                // Only a completed render publishes its document-wide font registrations.
                _fontStore = store;
                return (Build: built, Width: page.Width, Height: page.Height);
            }, pageIndex: index);
            if (IsDisposed || version != _loadVersion || epoch != _renderEpoch || index >= slots.Count || slots[index] is not null) return false;
            double width = rotation % 180 == 90 ? result.Height : result.Width;
            double height = rotation % 180 == 90 ? result.Width : result.Height;
            if (_pageWidths[index] != width || _pageHeights[index] != height)
            {
                if (IsRendered && _navigationsInFlight == 0) await KeepReadingPositionAsync();
                if (IsDisposed || version != _loadVersion || epoch != _renderEpoch) return false;
                _pageWidths[index] = width; _pageHeights[index] = height;
                _spyPending = true; _fitPageIndex = -1;
            }
            slots[index] = CommitPage(index, result.Build);
            rendered = true;
            return true;
        }
        catch (BitPdfRangeVersionException ex)
        {
            await FailRangeVersionAsync(source, version, ex.Message);
            return false;
        }
        catch (Exception) when (IsDisposed || version != _loadVersion || epoch != _renderEpoch) { return false; }
        catch (OperationCanceledException) { return false; }
        catch (BitPdfByteSourceIOException) when (throwOnFailure && !_rangePublished) { throw; }
        catch (Exception ex)
        {
            (thumbnail ? _failedThumbs : _failedPages).Add(index);
            await OnError.InvokeAsync(ex.Message);
            return false;
        }
        finally
        {
            // Match the buffered/stream callback boundary: completed main pages
            // only, after the engine gate is free. Host exceptions propagate.
            if (rendered && !thumbnail && !IsDisposed && version == _loadVersion && epoch == _renderEpoch)
                await OnPageRendered.InvokeAsync(index + 1);
        }
    }

    private Task<T> ReadRangeDetailAsync<T>(Func<BitPdfDocument, T> read, T empty, CancellationToken token)
        => RunRangeWorkAsync(_document!, d =>
        {
            try { return read(d); }
            catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex)) { return empty; }
        }, token);

    private async Task LoadRangeEssentialsCoreAsync(BitPdfHttpRangeByteSource source, int version, CancellationToken token)
    {
        var outline = await ReadRangeDetailAsync(d => d.Outline, (IReadOnlyList<BitPdfOutlineItem>)[], token);
        CheckRangeOperation(source, version, token); _outline = outline;
        var layers = await ReadRangeDetailAsync(d => d.Layers, (IReadOnlyList<BitPdfLayer>)[], token);
        CheckRangeOperation(source, version, token); _layers = layers;
        _hiddenLayers ??= new HashSet<string>(layers.Where(l => !l.VisibleByDefault).Select(l => l.Id));
        _hiddenLayersView = new HashSet<string>(_hiddenLayers);
        var labels = await ReadRangeDetailAsync(d => d.PageLabels, (IReadOnlyList<string>)[], token);
        CheckRangeOperation(source, version, token); _rangeLabels = labels; _hasPageLabels = null;
        _rangeEssentialsLoaded = true;
        Repaint();
    }

    private bool _rangeEssentialsLoaded;
    private bool _rangeAttachmentsLoading;
    private async Task LoadRangeBackgroundAsync()
    {
        var source = _rangeSource;
        int version = _loadVersion;
        var token = _streamLoadCancellation?.Token ?? default;
        if (source is null) return;
        try
        {
            await _detailsGate.WaitAsync(token);
            try
            {
                CheckRangeOperation(source, version, token);
                if (!_rangeEssentialsLoaded) await LoadRangeEssentialsCoreAsync(source, version, token);
                if (_showAttachments) await LoadRangeAttachmentsCoreAsync(source, version, token);
            }
            finally { _detailsGate.Release(); }
            await CloseEmptyRangeAttachmentsAsync(version);
        }
        catch (Exception) when (IsDisposed || version != _loadVersion || token.IsCancellationRequested) { }
        catch (BitPdfRangeVersionException ex) { await FailRangeVersionAsync(source, version, ex.Message); }
        catch (Exception ex) { await OnError.InvokeAsync(ex.Message); }
    }

    private async Task LoadRangeAttachmentsCoreAsync(BitPdfHttpRangeByteSource source, int version, CancellationToken token)
    {
        if (_rangeAttachmentsLoaded) return;
        _rangeAttachmentsLoading = true; Repaint();
        try
        {
            var attachments = new List<BitPdfAttachment>();
            var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            async Task AddFileAsync(object? spec, int? page, string? name)
            {
                var file = await ReadRangeDetailAsync(d => BitPdfEmbeddedFiles.ReadFile(d.XRef, spec, page, name), null, token);
                CheckRangeOperation(source, version, token);
                if (file is not null && seen.Add(file.Identity)) attachments.Add(file.Attachment);
            }
            var root = await ReadRangeDetailAsync(d => BitPdfEmbeddedFiles.NameRoot(d.XRef, d.Catalog), null, token);
            var pending = new Stack<(object? Node, int Depth)>();
            if (root is not null) pending.Push((root, 0));
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            while (pending.TryPop(out var next))
            {
                if (next.Depth > 32) continue;
                var node = await ReadRangeDetailAsync(d => BitPdfEmbeddedFiles.ReadNameNode(d.XRef, next.Node), null, token);
                if (node is null || !visited.Add(node.Identity)) continue;
                foreach (var file in node.Files) await AddFileAsync(file.Spec, null, file.Name);
                for (int i = node.Children.Length - 1; i >= 0; i--) pending.Push((node.Children[i], next.Depth + 1));
            }
            for (int i = 0; i < _pages.Count; i++)
            {
                int index = i;
                await PrepareRangePageAsync(_document!, source, version, index, token);
                var files = await ReadRangeDetailAsync(d => BitPdfEmbeddedFiles.PageFiles(d.XRef, d.Pages[index]), [], token);
                foreach (var file in files) await AddFileAsync(file, index + 1, null);
                if (i % 32 == 31) await Task.Delay(1, token);
            }
            CheckRangeOperation(source, version, token);
            _attachments = attachments; _rangeAttachmentsLoaded = true;
        }
        finally
        {
            if (version == _loadVersion) { _rangeAttachmentsLoading = false; Repaint(); }
        }
    }

    private async Task LoadRangeAttachmentsForPanelAsync()
    {
        var source = _rangeSource;
        int version = _loadVersion;
        var token = _streamLoadCancellation?.Token ?? default;
        if (source is null || _rangeAttachmentsLoaded) return;
        try
        {
            await _detailsGate.WaitAsync(token);
            try { CheckRangeOperation(source, version, token); await LoadRangeAttachmentsCoreAsync(source, version, token); }
            finally { _detailsGate.Release(); }
            await CloseEmptyRangeAttachmentsAsync(version);
        }
        catch (Exception) when (IsDisposed || version != _loadVersion || token.IsCancellationRequested) { }
        catch (BitPdfRangeVersionException ex) { await FailRangeVersionAsync(source, version, ex.Message); }
        catch (Exception ex) { await OnError.InvokeAsync(ex.Message); }
    }

    private Task CloseEmptyRangeAttachmentsAsync(int version)
        => !IsDisposed && version == _loadVersion && _rangeAttachmentsLoaded && _attachments.Count == 0 && _showAttachments
            ? ShowSidebar(BitPdfSidebar.None) : Task.CompletedTask;

    /// <summary>Loads all still-deferred document information: metadata/XMP,
    /// bookmarks, layers, page labels, attachments, forms and tagged structure.
    /// Completes immediately when nothing is deferred. Range acquisition is
    /// asynchronous; stream work runs off the Server circuit. Cancellation and
    /// source failures propagate to an awaiting caller. UI-triggered loads report
    /// active failures through OnError.</summary>
    public async Task LoadDocumentDetailsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_document is null || _rangeDetailsLoaded || !IsNonBuffered) return;
        int version = _loadVersion;
        var source = _rangeSource;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_streamLoadCancellation?.Token ?? default, cancellationToken);
        var token = linked.Token;
        try
        {
            await _detailsGate.WaitAsync(token);
            try
            {
                if (IsDisposed || version != _loadVersion) throw new OperationCanceledException(token);
                if (_rangeDetailsLoaded) return;
                if (source is null)
                {
                    var prepared = _streamPreparation;
                    var details = await RunDocumentWorkAsync(d => (Forms: d.FormFields, Structure: d.StructureTree), token);
                    if (IsDisposed || version != _loadVersion) throw new OperationCanceledException(token);
                    if (prepared is not null) _streamPreparation = prepared with { FormFields = details.Forms, StructureTree = details.Structure };
                }
                else
                {
                    if (!_rangeEssentialsLoaded) await LoadRangeEssentialsCoreAsync(source, version, token);
                    var metadata = await ReadRangeDetailAsync(d => d.Metadata, (BitPdfMetadata?)null, token);
                    CheckRangeOperation(source, version, token); _rangeMetadata = metadata;
                    var forms = await ReadRangeDetailAsync(d => d.FormFields, (IReadOnlyList<BitPdfFormField>)[], token);
                    CheckRangeOperation(source, version, token); _rangeForms = forms;
                    var structure = await ReadRangeDetailAsync(d => d.StructureTree, (IReadOnlyList<BitPdfStructElement>)[], token);
                    CheckRangeOperation(source, version, token); _rangeStructure = structure;
                    await LoadRangeAttachmentsCoreAsync(source, version, token);
                }
                _rangeDetailsLoaded = true; Repaint();
            }
            finally { _detailsGate.Release(); }
        }
        catch (BitPdfRangeVersionException ex) when (source is not null)
        {
            // Retire and notify after releasing the details gate: OnError may
            // replace the source and await details for the new document.
            await FailRangeVersionAsync(source, version, ex.Message);
            throw;
        }
        await CloseEmptyRangeAttachmentsAsync(version);
    }

    private async Task LoadDetailsForPropertiesAsync()
    {
        int version = _loadVersion;
        try { await LoadDocumentDetailsAsync(); }
        catch (Exception) when (IsDisposed || version != _loadVersion) { }
        catch (OperationCanceledException) { }
        catch (Exception ex) { await OnError.InvokeAsync(ex.Message); }
    }

    private async Task DownloadRangeAsync()
    {
        var source = _rangeSource;
        if (!CanDownload || _downloading || _document is null || source is null) return;
        int version = _loadVersion;
        var token = _streamLoadCancellation?.Token ?? default;
        _downloading = true;
        string? failure = null;
        BitPdfRangeVersionException? changed = null;
        try
        {
            string fileName = DownloadFileName;
            if (OnDownloading.HasDelegate)
            {
                var args = new BitPdfDownloadArgs(fileName);
                await OnDownloading.InvokeAsync(args);
                if (args.Cancel || IsDisposed || version != _loadVersion) return;
                if (!string.IsNullOrWhiteSpace(args.FileName)) fileName = args.FileName;
            }
            try
            {
                // Copies acquire ranges asynchronously; page rendering can continue
                // without holding the synchronous engine gate over HTTP.
                using var stream = source.OpenReadStream();
                using var reference = new DotNetStreamReference(stream, leaveOpen: true);
                await _js.BitPdfViewerDownload(fileName, reference).AsTask().WaitAsync(token);
                CheckRangeOperation(source, version, token);
            }
            catch (BitPdfRangeVersionException ex) { changed = ex; }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // DotNetStreamReference reports provider exceptions through a JS
                // rejection. Recover the source's validator failure before reporting.
                try { source.CheckRead(); }
                catch (BitPdfRangeVersionException versionError) { changed = versionError; }
                if (changed is null) failure = ex.Message;
            }
        }
        catch (Exception) when (IsDisposed || version != _loadVersion) { }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally { _downloading = false; }
        if (changed is not null) await FailRangeVersionAsync(source, version, changed.Message);
        else if (failure is not null && !IsDisposed && version == _loadVersion)
            await OnError.InvokeAsync(failure);
    }

    private async Task<byte[]?> GetRangeBytesAsync(CancellationToken token)
    {
        var source = _rangeSource;
        if (_document is null || source is null) return null;
        int version = _loadVersion;
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_streamLoadCancellation?.Token ?? default, token);
            var bytes = await source.ReadAllAsync(linked.Token);
            CheckRangeOperation(source, version, linked.Token);
            return bytes;
        }
        catch (BitPdfRangeVersionException ex) { await FailRangeVersionAsync(source, version, ex.Message); throw; }
    }
}
