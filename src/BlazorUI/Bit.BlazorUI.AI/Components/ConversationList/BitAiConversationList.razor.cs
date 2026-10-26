namespace Bit.BlazorUI;

/// <summary>
/// BitAiConversationList is the sidebar list of conversations, grouped by recency the way people actually think about
/// them: today, yesterday, the previous seven days, the previous thirty days, then by month.
/// </summary>
/// <remarks>
/// The conversation that is open is marked with <c>aria-current</c>, each group is a list named by its heading, and the
/// delete button of a conversation is named after it, so a row of them names the conversation each one removes. The
/// words come from the string table of the options in force (<see cref="BitAiStrings"/>).
/// </remarks>
public partial class BitAiConversationList : BitAiComponentBase
{
    // The buttons that open the conversations, by conversation, and the new-conversation button: where the focus goes
    // once a delete button went away with its conversation (see FocusAfterDeleteAsync).
    private readonly Dictionary<string, ElementReference> _itemButtons = new(StringComparer.Ordinal);
    private BitActionButton? _newButton;
    private (string Id, int Position)? _focusAfterDelete;
    private TimeZoneInfo? _browserTimeZone;
    private bool _browserTimeZoneAsked;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the conversation list component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple conversation list
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiConversationListParams.ParamName)]
    public BitAiConversationListParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the conversation list.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiConversationListClassStyles? Classes { get; set; }

    /// <summary>
    /// The conversations to list.
    /// </summary>
    [Parameter] public IReadOnlyList<BitAiConversationSummary>? Conversations { get; set; }

    /// <summary>
    /// The callback for when a conversation is deleted. The delete buttons are shown only while it is set.
    /// </summary>
    [Parameter] public EventCallback<BitAiConversationSummary> OnDelete { get; set; }

    /// <summary>
    /// The callback for when the new-conversation button is pressed.
    /// </summary>
    [Parameter] public EventCallback OnNew { get; set; }

    /// <summary>
    /// The callback for when a conversation is chosen.
    /// </summary>
    [Parameter] public EventCallback<BitAiConversationSummary> OnSelect { get; set; }

    /// <summary>
    /// The identifier of the conversation currently open.
    /// </summary>
    [Parameter] public string? SelectedId { get; set; }

    /// <summary>
    /// Shows the new-conversation button at the top of the list.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool ShowNewButton { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for different parts of the conversation list.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiConversationListClassStyles? Styles { get; set; }

    /// <summary>
    /// The time zone the conversations are grouped by day in: what counts as today, yesterday, the previous seven and
    /// thirty days, and the month of an older one. While it is null, the <see cref="BitAiUiOptions.TimeZone"/> in force,
    /// else the browser's: under Blazor Server the list is drawn in the zone of the server until the browser has said
    /// which zone it runs in.
    /// </summary>
    [Parameter] public TimeZoneInfo? TimeZone { get; set; }



    protected override string RootElementClass => "bit-aicvl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // Read in the first render after the delete was handed to the app, by which time the app has drawn the list again
        // with what it did about it (see HandleOnDelete): without the conversation, the focus moves to the one in its place;
        // with it still there - the app asks first, or the delete failed - it stays on the delete button, and a list that
        // drops the conversation much later does not take the focus then.
        if (_focusAfterDelete is { } deleted)
        {
            _focusAfterDelete = null;

            if (Contains(deleted.Id) is false)
            {
                await FocusAfterDeleteAsync(deleted.Position);
            }
        }

        // Only a rendered list can ask the browser, and only one with no zone of its own or of the options needs to.
        if (_browserTimeZoneAsked is false && (TimeZone ?? Options.TimeZone) is null)
        {
            _browserTimeZoneAsked = true;

            _browserTimeZone = await BitAiBrowserTimeZone.GetAsync(_js);

            if (_browserTimeZone is not null && IsDisposed is false)
            {
                StateHasChanged();
            }
        }
    }



    // The order the conversations are drawn in, newest first; the groups follow it, so it is the order of the page too.
    private IOrderedEnumerable<BitAiConversationSummary> Ordered() => (Conversations ?? []).OrderByDescending(c => c.UpdatedAt);

    private IEnumerable<IGrouping<string, BitAiConversationSummary>> Grouped()
    {
        var today = Local(DateTimeOffset.UtcNow).Date;

        return Ordered().GroupBy(c => Bucket(today, c.UpdatedAt));
    }

    private string Bucket(DateTime today, DateTimeOffset updated)
    {
        var local = Local(updated);
        var age = today - local.Date;

        return age.TotalDays switch
        {
            < 1 => Strings.Today,
            < 2 => Strings.Yesterday,
            < 7 => Strings.PreviousSevenDays,
            < 30 => Strings.PreviousThirtyDays,
            _ => Strings.MonthGroup(local),
        };
    }

    // The wall clock of a moment in the zone the days are counted in (see TimeZone): the list's own, else the one of the
    // options in force, which the times under the messages are shown in, else the browser's.
    private DateTime Local(DateTimeOffset moment) => (TimeZone ?? Options.TimeZone ?? _browserTimeZone) is { } zone ? TimeZoneInfo.ConvertTime(moment, zone).DateTime : moment.LocalDateTime;

    private bool Contains(string id) => (Conversations ?? []).Any(c => c.ConversationId == id);

    private async Task HandleOnNew()
    {
        if (Disabled) return;

        await OnNew.InvokeAsync();
    }

    private async Task HandleOnSelect(BitAiConversationSummary summary)
    {
        if (Disabled) return;

        await OnSelect.InvokeAsync(summary);
    }

    private async Task HandleOnDelete(BitAiConversationSummary summary, int position)
    {
        if (Disabled) return;

        // What the app did is known only once it has drawn the list again, which happens after this handler returns:
        // the render that follows reads it (OnAfterRenderAsync).
        _focusAfterDelete = (summary.ConversationId, position);

        await OnDelete.InvokeAsync(summary);
    }

    // The delete button that was pressed went away with its conversation, which would drop the focus to the page; it is
    // put on the conversation that took its place in the list (or the one that is now last), or on the new-conversation
    // button once there is none left.
    private async Task FocusAfterDeleteAsync(int position)
    {
        foreach (var gone in _itemButtons.Keys.Where(k => Contains(k) is false).ToList()) _itemButtons.Remove(gone);

        if (IsDisposing) return;

        try
        {
            var next = Ordered().Skip(Math.Max(0, position)).FirstOrDefault() ?? Ordered().LastOrDefault();

            if (next is not null && _itemButtons.TryGetValue(next.ConversationId, out var button))
            {
                await button.FocusAsync();
            }
            else if (next is null && ShowNewButton && _newButton is not null)
            {
                await _newButton.FocusAsync();
            }
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
        catch (OperationCanceledException) { }
        catch (JSException) { } // a button taken off the page in the meantime has nothing to focus
        catch (InvalidOperationException) { } // a button not rendered yet has no element to focus
    }
}
