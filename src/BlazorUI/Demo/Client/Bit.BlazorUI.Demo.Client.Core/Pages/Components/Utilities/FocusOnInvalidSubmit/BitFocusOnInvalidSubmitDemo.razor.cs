using Bit.BlazorUI.Demo.Client.Core.Components;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.FocusOnInvalidSubmit;

public partial class BitFocusOnInvalidSubmitDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "EditContext",
            Type = "EditContext?",
            DefaultValue = "null",
            Description = "The EditContext of the form the component sits in, cascaded by the EditForm. The component requires one: it throws an InvalidOperationException when rendered outside a form.",
        },
    ];

    private BitFocusOnInvalidSubmitDemoModel basicModel = new();
    private bool basicSubmitted;

    private readonly List<BitChoiceGroupItem<string>> planItems =
    [
        new() { Text = "Free", Value = "free" },
        new() { Text = "Team", Value = "team" },
        new() { Text = "Enterprise", Value = "enterprise" },
    ];

    private void ResetBasic()
    {
        basicModel = new();
        basicSubmitted = false;
    }
}
