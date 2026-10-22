using System.Diagnostics.CodeAnalysis;

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



    protected override string RootElementClass => "bit-aicvl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiConversationListParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    private IEnumerable<IGrouping<string, BitAiConversationSummary>> Grouped()
    {
        var now = DateTimeOffset.Now;

        return (Conversations ?? [])
            .OrderByDescending(c => c.UpdatedAt)
            .GroupBy(c => Bucket(now, c.UpdatedAt));
    }

    private string Bucket(DateTimeOffset now, DateTimeOffset updated)
    {
        var age = now.Date - updated.LocalDateTime.Date;

        return age.TotalDays switch
        {
            < 1 => Strings.Today,
            < 2 => Strings.Yesterday,
            < 7 => Strings.PreviousSevenDays,
            < 30 => Strings.PreviousThirtyDays,
            _ => Strings.MonthGroup(updated.LocalDateTime),
        };
    }

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

    private async Task HandleOnDelete(BitAiConversationSummary summary)
    {
        if (Disabled) return;

        await OnDelete.InvokeAsync(summary);
    }
}
