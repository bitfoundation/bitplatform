namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class DemoPage
{
    private const string REPO_URL = "https://github.com/bitfoundation/bitplatform";

    /// <summary>The element the visibility observer watches to know the reader has reached the API tables.</summary>
    private const string API_ELEMENT_ID = "api-tables";

    /// <summary>The id the browser's idle queue is registered under. One per page is all it takes.</summary>
    private const string BACKFILL_ELEMENT_ID = "demo-page-backfill";

    /// <summary>The anchor of the section that holds the examples.</summary>
    private const string USAGE_ELEMENT_ID = "usage-section";

    private bool _forceAnimation;

    /// <summary>
    /// Whether the API tables have been built. They are the last thing on the page and nothing is
    /// anchored below them, so they wait until the reader scrolls that far; see
    /// <see cref="ShouldDeferApi"/>, <see cref="DemoContentDeferral"/> and the razor.
    /// </summary>
    private bool _isApiMounted = true;

    /// <summary>
    /// The room the held-back API block keeps on the prerendered page: the height its own prerendered
    /// copy was measured at. Zero on a page built client-side, which has no such copy.
    /// </summary>
    private int _pendingApiHeight;

    private DotNetObjectReference<DemoPage>? _dotnetObj;

    // What this page is still holding back, in document order, and how far the backfill has walked
    // it. Held here rather than in the scoped deferral service so that its lifetime is the page's:
    // navigating away disposes the queue with the components that filled it.
    private readonly List<Func<Task<bool>>> _backfill = [];
    private int _backfillIndex;
    private bool _isBackfillScheduled;
    private DotNetObjectReference<DemoPage>? _idleObj;

    /// <summary>
    /// False while this page is still being built for the first time, true from the moment it stands
    /// complete on screen. What it distinguishes is a navigation - where the reader is at the top of a
    /// page that is not there yet, and holding back what they cannot see costs them nothing - from
    /// everything that happens afterwards, with the reader standing in the middle of a finished page.
    /// <see cref="DemoExample"/> reads it to decide whether it may hold its own preview back.
    /// </summary>
    public bool HasRendered { get; private set; }

    [AutoInject] private DemoContentDeferral _contentDeferral = default!;

    /// <summary>
    /// This page's entry in the catalog, which is what supplies the category shown above the title.
    /// Null for a component that has no nav entry, in which case the eyebrow is simply not rendered.
    /// </summary>
    private ComponentCatalogItem? _catalogItem;

    /// <summary>
    /// The NuGet package this component ships in. Which package a component belongs to is not
    /// something a reader can infer from the component itself, and reaching for a BitChart or a
    /// BitDataGrid without referencing Bit.BlazorUI.Extras is the most common false start with this
    /// library - so the page says so beside the title rather than only in the install guide.
    /// </summary>
    private string _packageName = "Bit.BlazorUI";

    /// <summary>The component's own source on GitHub, and the same URL in edit mode.</summary>
    private string? _sourceUrl;
    private string? _sourceEditUrl;

    [Parameter] public string Name { get; set; } = default!;
    [Parameter] public string[]? SecondaryNames { get; set; }
    [Parameter] public string? Description { get; set; }
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }
    [Parameter] public string? Notes { get; set; }
    [Parameter] public RenderFragment? NotesTemplate { get; set; }
    [Parameter] public string? IntroductionVideoUrl { get; set; }
    [Parameter] public string? Introduction { get; set; }
    [Parameter] public RenderFragment? IntroductionTemplate { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Examples { get; set; }
    [Parameter] public List<ComponentParameter> Parameters { get; set; } = [];
    [Parameter] public List<ComponentSubClass> SubClasses { get; set; } = [];
    [Parameter] public List<ComponentSubEnum> SubEnums { get; set; } = [];
    [Parameter] public List<ComponentParameter> PublicMembers { get; set; } = [];
    [Parameter] public string? GitHubUrl { get; set; }
    [Parameter] public string? GitHubExtrasUrl { get; set; }
    [Parameter] public string? GitHubLegacyUrl { get; set; }
    [Parameter] public string? GitHubDemoUrl { get; set; }
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }


    /// <summary>
    /// Whether the API tables may wait - and, when they may, how much room they have to keep meanwhile.
    /// The same two cases <see cref="DemoExample"/> distinguishes for its own preview: a page built on
    /// the client, where nothing on screen contradicts an empty block, and the prerendered first page,
    /// where the block is only allowed to empty out if it keeps the height its prerendered copy had.
    /// <para>
    /// And, as there, an address carrying a fragment holds nothing back at all: the anchors of the
    /// sub-enum tables (#component-visibility, #component-dir) are inside this very block, so a deep
    /// link to one of them has nowhere to land while the block is empty.
    /// </para>
    /// </summary>
    private bool ShouldDeferApi()
    {
        if (RenderForMcpClient) return false;
        if (InPrerenderSession) return false;
        if (NavigationManager.Uri.Contains('#', StringComparison.Ordinal)) return false;

        if (_contentDeferral.IsEnabled || AppRenderMode.IsBlazorHybrid) return true;

        _pendingApiHeight = (int)Math.Ceiling(JSRuntime.TryGetElementHeight(API_ELEMENT_ID));

        return _pendingApiHeight > 0;
    }

    protected override Task OnInitAsync()
    {
        _isApiMounted = ShouldDeferApi() is false;

        return base.OnInitAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (RenderForMcpClient) return;

        if (_isApiMounted is false)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
            await JSRuntime.ObserveVisibility(API_ELEMENT_ID, _dotnetObj, nameof(OnApiReached));

            // Last in the queue, as it is last on the page.
            QueueBackfill(MountApiAsync);
        }

        // From here on the page on screen is one this app built, not the prerendered one, so every
        // page after this may hold back what the reader has not reached without having to measure
        // anything first. Set last, after this page has rendered in full.
        _contentDeferral.Enable();

        // Set last, once the page stands complete: from here on an example that renders is one the
        // reader has asked for from a page they are already looking at, not one more piece of a page
        // still being built.
        HasRendered = true;

        await ScheduleBackfillAsync();
    }

    /// <summary>
    /// Puts a held-back block in line to be built during the browser's idle time. Called by the
    /// examples as they render, so the queue ends up in document order - which is the order the
    /// reader would have reached them in.
    /// <para>
    /// An example that registers after this page has rendered restarts the queue rather than joining
    /// one that has already been walked: nothing re-renders this page to notice such an example
    /// arriving, so the restart has to come from here.
    /// </para>
    /// </summary>
    public void QueueBackfill(Func<Task<bool>> mount)
    {
        // A drained queue holds nothing but delegates over previews that are already built, so it is
        // emptied rather than grown - the index would otherwise only ever walk forwards.
        if (_backfillIndex >= _backfill.Count)
        {
            _backfill.Clear();
            _backfillIndex = 0;
        }

        _backfill.Add(mount);

        // The page-load pass is scheduled once, by OnAfterFirstRenderAsync, after every example has
        // had its turn to register - so that the queue is walked in document order.
        if (HasRendered is false) return;
        if (_isBackfillScheduled) return;

        _ = ScheduleBackfillAsync();
    }

    /// <summary>
    /// Takes a block back out of the queue, because the component that put it there is going away -
    /// a BitPivot tab switch disposes the whole tab it was rendering. Left in, its delegate would
    /// keep that component and its render fragment alive for the life of the page, and still report
    /// that it built something, spending a whole idle slice on a component nobody can see.
    /// </summary>
    public void DequeueBackfill(Func<Task<bool>> mount)
    {
        var index = _backfill.IndexOf(mount);

        if (index < 0) return;

        _backfill.RemoveAt(index);

        // Everything the walk has already passed sits before the index, so dropping one of those
        // shifts the rest down under it.
        if (index < _backfillIndex) _backfillIndex--;
    }

    private async Task ScheduleBackfillAsync()
    {
        if (_backfillIndex >= _backfill.Count) return;

        _idleObj ??= DotNetObjectReference.Create(this);
        _isBackfillScheduled = true;

        try
        {
            await JSRuntime.RequestIdleWork(BACKFILL_ELEMENT_ID, _idleObj, nameof(OnIdleBackfill));
        }
        catch (JSDisconnectedException)
        {
            _isBackfillScheduled = false; // the circuit is already gone; there is nobody left to fill in for
        }
        catch (Exception ex)
        {
            // Anything else - a script that predates requestIdleWork, a torn-down circuit. The flag
            // has to come back down whatever it was: left latched, every later QueueBackfill returns
            // early and the page stays half-built for good. Reported rather than rethrown because
            // QueueBackfill's call is fire-and-forget, where a rethrow is an unobserved task and
            // nobody hears it.
            _isBackfillScheduled = false;

            ExceptionHandler.Handle(ex);
        }
    }

    /// <summary>
    /// One block per idle slice, then ask for the next one. Draining the whole queue in a single
    /// callback would put back exactly the stall the deferral was there to avoid.
    /// </summary>
    [JSInvokable]
    public async Task OnIdleBackfill()
    {
        _isBackfillScheduled = false;

        // A block the reader has already scrolled to is mounted and reports that it had nothing to
        // do, which is not worth an idle slice of its own - the queue walks on until something is
        // actually built.
        while (_backfillIndex < _backfill.Count)
        {
            var mount = _backfill[_backfillIndex++];

            try
            {
                if (await mount()) break;
            }
            catch (JSDisconnectedException) { return; } // the circuit is gone, and with it the idle queue
            catch (ObjectDisposedException) { } // that example is already gone; the next one may not be
            catch (Exception ex)
            {
                // One example whose own markup throws must not strand the rest: letting it out of
                // here would end the self-rescheduling chain, and every block after it - and the API
                // tables behind them - would stay unmounted for the life of the page.
                ExceptionHandler.Handle(ex);
            }
        }

        await ScheduleBackfillAsync();
    }

    /// <summary>The reader has scrolled within reach of the API section. Mounting is one way.</summary>
    [JSInvokable]
    public async Task OnApiReached()
    {
        // The observer stops watching before it reports, so there is nothing left to unregister.
        await MountApiAsync(stillObserved: false);
    }

    private Task<bool> MountApiAsync() => MountApiAsync(stillObserved: true);

    private async Task<bool> MountApiAsync(bool stillObserved)
    {
        if (_isApiMounted) return false;

        _isApiMounted = true;

        // Only the backfill leaves a live observer behind.
        if (stillObserved)
        {
            try
            {
                await JSRuntime.UnobserveVisibility(API_ELEMENT_ID);
            }
            catch (JSDisconnectedException) { } // the circuit is already gone, nothing left to unregister
        }

        _dotnetObj?.Dispose();
        _dotnetObj = null;

        await InvokeAsync(StateHasChanged);

        return true;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            if (_dotnetObj is not null)
            {
                try
                {
                    await JSRuntime.UnobserveVisibility(API_ELEMENT_ID);
                }
                catch (JSDisconnectedException) { } // the circuit is already gone, nothing left to unregister

                _dotnetObj.Dispose();
                _dotnetObj = null;
            }

            if (_idleObj is not null)
            {
                try
                {
                    await JSRuntime.CancelIdleWork(BACKFILL_ELEMENT_ID);
                }
                catch (JSDisconnectedException) { }

                _idleObj.Dispose();
                _idleObj = null;
            }
        }

        await base.DisposeAsync(disposing);
    }

    protected override Task OnParamsSetAsync()
    {
        _catalogItem = ComponentCatalog.Find(NavigationManager.Uri);

        // Exactly one of the three source parameters is set on any given page, and which one it is
        // says both which package the component comes from and where its source lives.
        (_packageName, _sourceUrl) =
            GitHubExtrasUrl.HasValue() ? ("Bit.BlazorUI.Extras", $"{REPO_URL}/blob/develop/src/BlazorUI/Bit.BlazorUI.Extras/Components/{GitHubExtrasUrl}")
            : GitHubLegacyUrl.HasValue() ? ("Bit.BlazorUI.Legacy", $"{REPO_URL}/blob/develop/src/BlazorUI/Bit.BlazorUI.Legacy/Components/{GitHubLegacyUrl}")
            : GitHubUrl.HasValue() ? ("Bit.BlazorUI", $"{REPO_URL}/blob/develop/src/BlazorUI/Bit.BlazorUI/Components/{GitHubUrl}")
            : ("Bit.BlazorUI", null);

        _sourceEditUrl = _sourceUrl?.Replace("/blob/", "/edit/", StringComparison.Ordinal);

        return base.OnParamsSetAsync();
    }

    private readonly List<ComponentParameter> _componentBaseParameters =
    [
        new()
        {
            Name = "AriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the accessible label for the component, used by assistive technologies.",
        },
        new()
        {
            Name = "Class",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the CSS class name(s) to apply to the rendered element.",
        },
        new()
        {
            Name = "Dir",
            Type = "BitDir?",
            DefaultValue = "null",
            Description = "Gets or sets the text directionality for the component's content.",
            LinkType = LinkType.Link,
            Href = "#component-dir",
        },
        new()
        {
            Name = "ForceAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gets or sets a value indicating whether the component's animations play at their full duration even when reduced motion is requested.",
        },
        new()
        {
            Name = "HtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Captures additional HTML attributes to be applied to the rendered element, in addition to the component's parameters.",
        },
        new()
        {
            Name = "Id",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the unique identifier for the component's root element.",
        },
        new()
        {
            Name = "IsEnabled",
            Type = "bool",
            DefaultValue = "true",
            Description = "Gets or sets a value indicating whether the component is enabled and can respond to user interaction.",
        },
        new()
        {
            Name = "Style",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the CSS style string to apply to the rendered element.",
        },
        new()
        {
            Name = "TabIndex",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the tab order index for the component when navigating with the keyboard.",
        },
        new()
        {
            Name = "Visibility",
            Type = "BitVisibility",
            DefaultValue = "BitVisibility.Visible",
            Description = "Gets or sets the visibility state (visible, hidden, or collapsed) of the component.",
            LinkType = LinkType.Link,
            Href = "#component-visibility",
        },
    ];

    private readonly List<ComponentParameter> _componentBasePublicMembers =
    [
        new()
        {
            Name = "UniqueId",
            Type = "Guid",
            DefaultValue = "Guid.NewGuid()",
            Description = "Gets the readonly unique identifier for the component's root element, assigned when the component instance is constructed.",
        },
        new()
        {
            Name = "RootElement",
            Type = "ElementReference",
            Description = "Gets the reference to the root HTML element associated with this component.",
        },
    ];

    private readonly List<ComponentSubEnum> _componentBaseSubEnums =
    [
        new()
        {
            Id = "component-visibility",
            Name = "BitVisibility",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Visible",
                    Value="0",
                    Description="The content of the component is visible.",
                },
                new()
                {
                    Name= "Hidden",
                    Value="1",
                    Description="The content of the component is hidden, but the space it takes on the page remains (visibility:hidden).",
                },
                new()
                {
                    Name= "Collapsed",
                    Value="2",
                    Description="The component is hidden (display:none).",
                }
            ]
        },
        new()
        {
            Id = "component-dir",
            Name = "BitDir",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Ltr",
                    Value="0",
                    Description="Ltr (left to right) is to be used for languages that are written from the left to the right (like English).",
                },
                new()
                {
                    Name= "Rtl",
                    Value="1",
                    Description="Rtl (right to left) is to be used for languages that are written from the right to the left (like Arabic).",
                },
                new()
                {
                    Name= "Auto",
                    Value="2",
                    Description="Auto lets the user agent decide. It uses a basic algorithm as it parses the characters inside the element until it finds a character with a strong directionality, then applies that directionality to the whole element.",
                }
            ]
        }
    ];



    private readonly List<string> _inputComponents = [
        "Calendar", "Checkbox", "ChoiceGroup", "DatePicker", "DateRangePicker", "Dropdown", "NumberField", "OtpInput", "Rating",
        "SearchBox", "TextField", "TimePicker", "CircularTimePicker", "Toggle", "TagsInput"
    ];

    private readonly List<ComponentParameter> _inputBaseParameters =
    [
        new()
        {
            Name = "DefaultValue",
            Type = "TValue?",
            DefaultValue = "null",
            Description = "The default value of the input to be used in uncontrolled mode (i.e. when the Value is not bound), typically used alongside the OnChange callback.",
        },
        new()
        {
            Name = "DisplayName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the display name for this field.",
        },
        new()
        {
            Name = "InputHtmlAttributes",
            Type = "IReadOnlyDictionary<string, object>?",
            DefaultValue = "null",
            Description = "Gets or sets a collection of additional attributes that will be applied to the created element.",
        },
        new()
        {
            Name = "Name",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the element. Allows access by name from the associated form.",
        },
        new()
        {
            Name = "NoValidate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables the validation of the input.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<TValue?>",
            DefaultValue = "",
            Description = "Callback for when the input value changes.",
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the input read-only.",
        },
        new()
        {
            Name = "Required",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the input required.",
        },
        new()
        {
            Name = "Value",
            Type = "TValue?",
            DefaultValue = "null",
            Description = "Gets or sets the value of the input. This should be used with two-way binding.",
        },
    ];

    private readonly List<ComponentParameter> _inputBasePublicMembers =
    [
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference of the input element.",
        },
        new()
        {
            Name = "FocusAsync()",
            Type = "() => ValueTask",
            Description = "Gives focus to the input element.",
        },
        new()
        {
            Name = "FocusAsync(bool preventScroll)",
            Type = "(bool preventScroll) => ValueTask",
            Description = "Gives focus to the input element.",
        },
    ];



    private readonly List<string> _textInputComponents = [
        "NumberField", "TextField", "SearchBox", "PhoneInput"
    ];

    private readonly List<ComponentParameter> _textInputBaseParameters =
    [
        new()
        {
            Name = "AutoComplete",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the value of the autocomplete attribute of the input component.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the text input is auto focused on first render.",
        },
        new()
        {
            Name = "DebounceTime",
            Type = "int",
            DefaultValue = "0",
            Description = "The debounce time in milliseconds.",
        },
        new()
        {
            Name = "Immediate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Change the content of the input field when the user write text (based on 'oninput' HTML event).",
        },
        new()
        {
            Name = "ThrottleTime",
            Type = "int",
            DefaultValue = "0",
            Description = "The throttle time in milliseconds.",
        },
    ];



    private readonly List<string> _notInheritedComponents = [
        "CascadingValueProvider", "Chart", "ChartLegacy", "DataGrid", "DataGridLegacy", "ModalService", "Params", "ProModalService"
    ];
}
