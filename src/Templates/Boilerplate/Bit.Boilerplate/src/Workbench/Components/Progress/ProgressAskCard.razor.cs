using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Progress;

/// <summary>
/// A question an agent asks on its task, answered here: in words, by picking one or several of its options, or with a yes or
/// no. Every kind takes a few words too, typed or dictated, which the agent gets with the answer.
/// </summary>
public partial class ProgressAskCard
{
    private string? text;
    private bool isSending;
    private List<string> picked = [];
    private BitChoiceGroupItem<string>[] choiceItems = [];

    [Parameter, EditorRequired] public WorkbenchProgressAsk Ask { get; set; } = default!;

    [Parameter] public bool CanAnswer { get; set; }

    /// <summary>Sends the answer: the options picked and the text; true once it reached the task.</summary>
    [Parameter] public Func<WorkbenchProgressAsk, IReadOnlyList<string>, string?, Task<bool>>? OnAnswer { get; set; }

    private string HeadingId => $"ask-{Ask.Id}";

    private string KindIcon => Ask.Kind switch
    {
        WorkbenchProgressAskKind.Choice => BitIconName.CheckList,
        WorkbenchProgressAskKind.Choices => BitIconName.CheckListCheck,
        WorkbenchProgressAskKind.Confirm => BitIconName.StatusCircleQuestionMark,
        _ => BitIconName.Message
    };

    private string Placeholder => Ask.Kind is WorkbenchProgressAskKind.Text
        ? Ask.Placeholder ?? Localizer[nameof(WorkbenchStrings.ProgressAnswerPlaceholder)]
        : Localizer[nameof(WorkbenchStrings.ProgressNotePlaceholder)];

    private bool CanSend => isSending is false && Ask.Kind switch
    {
        WorkbenchProgressAskKind.Text => string.IsNullOrWhiteSpace(text) is false,
        _ => picked.Count > 0
    };

    protected override void OnParametersSet()
    {
        choiceItems = [.. Ask.Options.Select(option => new BitChoiceGroupItem<string> { Text = option, Value = option })];
        picked.RemoveAll(option => Ask.Options.Contains(option) is false);
    }

    private void Pick(string? option) => picked = option is null ? [] : [option];

    private void Toggle(string option, bool on)
    {
        picked.Remove(option);

        if (on)
        {
            // Kept in the options' order, which is how the agent reads them back.
            picked = [.. Ask.Options.Where(o => o == option || picked.Contains(o))];
        }
    }

    private async Task SendOnCtrlEnter(KeyboardEventArgs e)
    {
        if (e.Key is "Enter" && (e.CtrlKey || e.MetaKey) && Ask.Kind is not WorkbenchProgressAskKind.Confirm && CanSend)
        {
            await Send(picked);
        }
    }

    private async Task Send(IReadOnlyList<string> choices)
    {
        if (OnAnswer is null || isSending)
            return;

        isSending = true;

        try
        {
            if (await OnAnswer(Ask, Ask.Kind is WorkbenchProgressAskKind.Text ? [] : choices, string.IsNullOrWhiteSpace(text) ? null : text.Trim()))
            {
                text = null;
                picked = [];
            }
        }
        finally
        {
            isSending = false;
        }
    }
}
