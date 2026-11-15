using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Progress;

/// <summary>
/// A question an agent asks on its task, answered here: in words, by picking one or several of its options, or with a yes or
/// no. Every kind takes a few words too, typed or dictated, which the agent gets with the answer. A secret is pasted in a
/// hidden field and goes only where the question says, which the card tells; the agent learns only that it is there.
/// </summary>
public partial class ProgressAskCard
{
    private string? text;
    private string? secret;
    private bool isSending;
    private List<string> picked = [];
    private BitChoiceGroupItem<string>[] choiceItems = [];

    [Parameter, EditorRequired] public WorkbenchProgressAsk Ask { get; set; } = default!;

    [Parameter] public bool CanAnswer { get; set; }

    /// <summary>The agent that asks, named where the card tells where a secret goes.</summary>
    [Parameter] public string? Agent { get; set; }

    /// <summary>Whether a secret can be pasted here: only on the computer the Workbench runs on.</summary>
    [Parameter] public bool CanStoreSecret { get; set; }

    /// <summary>Sends the answer: the options picked and the text; true once it reached the task.</summary>
    [Parameter] public Func<WorkbenchProgressAsk, IReadOnlyList<string>, string?, Task<bool>>? OnAnswer { get; set; }

    /// <summary>Stores the secret pasted where the question says.</summary>
    [Parameter] public Func<WorkbenchProgressAsk, string, Task>? OnSecret { get; set; }

    private string HeadingId => $"ask-{Ask.Id}";

    private string KindIcon => Ask.Kind switch
    {
        WorkbenchProgressAskKind.Choice => BitIconName.CheckList,
        WorkbenchProgressAskKind.Choices => BitIconName.CheckListCheck,
        WorkbenchProgressAskKind.Confirm => BitIconName.StatusCircleQuestionMark,
        WorkbenchProgressAskKind.Secret => BitIconName.Lock,
        _ => BitIconName.Message
    };

    /// <summary>Where the secret goes, so the user knows before pasting it.</summary>
    private string Where => Ask.Secret switch
    {
        { To: WorkbenchProgressSecretTo.GitHub, Environment: { } environment } github => Localizer[nameof(WorkbenchStrings.ProgressSecretToGitHubEnvironment), github.Name ?? string.Empty, environment, Agent ?? string.Empty],
        { To: WorkbenchProgressSecretTo.GitHub } github => Localizer[nameof(WorkbenchStrings.ProgressSecretToGitHub), github.Name ?? string.Empty, Agent ?? string.Empty],
        _ => Localizer[nameof(WorkbenchStrings.ProgressSecretToFile), Agent ?? string.Empty]
    };

    private bool CanStore => isSending is false && string.IsNullOrWhiteSpace(secret) is false;

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

    /// <summary>Hands the secret on and forgets it, stored or not, so it stays in the field only while it is typed.</summary>
    private async Task Store()
    {
        if (OnSecret is null || CanStore is false)
            return;

        var pasted = secret!;

        isSending = true;
        secret = null;

        try
        {
            await OnSecret(Ask, pasted);
        }
        finally
        {
            isSending = false;
        }
    }
}
