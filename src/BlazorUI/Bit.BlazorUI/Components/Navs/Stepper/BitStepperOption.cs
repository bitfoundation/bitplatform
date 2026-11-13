namespace Bit.BlazorUI;

/// <summary>
/// A step of a <see cref="BitStepper{TItem}"/> declared in markup, as a child of a <c>BitStepper TItem="BitStepperOption"</c>.
/// </summary>
public partial class BitStepperOption : ComponentBase, IDisposable
{
    internal const string _OPTION_ID_ATTRIBUTE = "data-bit-stp-opt";

    internal string _OptionId { get; } = BitShortId.NewId();

    private bool _disposed;
    private string? _lastParametersSignature;



    [CascadingParameter] protected BitStepper<BitStepperOption> Parent { get; set; } = default!;



    /// <summary>
    /// The aria-label of a step that can be clicked, which replaces its text, description and state as what a screen
    /// reader names it by.
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// The custom CSS classes of the step.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// A second line of text under the text of the step.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// The icon shown in the indicator of the step in place of its number, using custom CSS classes for external icon
    /// libraries. Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon shown in the indicator of the step in place of its number, from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether the step is disabled: it cannot be gone to, and is drawn dimmed.
    /// </summary>
    [Parameter] public bool IsDisabled { get; set; }

    /// <summary>
    /// Whether the step is in error, which the stepper shows with its error icon and color whatever the position of the
    /// step.
    /// </summary>
    [Parameter] public bool IsError { get; set; }

    /// <summary>
    /// A unique value to use as the key of the step.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// The click handler of the step, run when it is clicked and it can be gone to.
    /// </summary>
    [Parameter] public EventCallback<BitStepperOption> OnClick { get; set; }

    /// <summary>
    /// The custom CSS styles of the step.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The text of the step.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The title (tooltip) of the step.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    protected override void OnInitialized()
    {
        Parent?.RegisterOption(this);

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // The stepper renders the steps from the options and reads their values during its own render, so a change to
        // an option's parameters only shows once the stepper renders again. It is told only when a value it renders has
        // changed, since this runs on every render of the stepper; the icon is compared by its CSS classes, so an equal
        // but new instance changes nothing.
        var signature = string.Join('\u001F', AriaLabel, Class, Description, Icon?.GetCssClasses(), IconName, IsDisabled,
                                              IsError, Key, Style, Text, Title);

        if (_lastParametersSignature is not null && _lastParametersSignature != signature)
        {
            Parent?.NotifyOptionParametersChanged();
        }

        _lastParametersSignature = signature;

        base.OnParametersSet();
    }

    // The stepper draws the steps itself, so the option renders nothing but a hidden marker in place, whose position in
    // the document is what keeps the order of the steps that of the markup, even for an option rendered conditionally
    // after the others (see BitStepper.ReorderOptions).
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, _OPTION_ID_ATTRIBUTE, _OptionId);
        builder.CloseElement();
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        Parent?.UnregisterOption(this);

        _disposed = true;
    }
}
