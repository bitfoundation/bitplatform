using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// BitStepper shows the steps of a process in their order - the ones done, the one the user is on and the ones still
/// to come - horizontally or vertically, and lets the user go to a step by clicking it.
/// </summary>
/// <remarks>
/// The stepper draws the steps alone: what each step shows, and the buttons that move between them, are the page's,
/// which moves the stepper with <see cref="CurrentIndex"/>. A <see cref="Linear"/> stepper lets the user go back to a
/// step already done but not skip ahead, and a <see cref="ReadOnly"/> one only shows where the process is.
/// </remarks>
public partial class BitStepper<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem> : BitComponentBase where TItem : class
{
    private const string DefaultCompletedIconName = "CheckMark";
    private const string DefaultErrorIconName = "ErrorBadge";



    private List<TItem> _items = [];
    private IEnumerable<TItem>? _oldItems;



    /// <summary>
    /// The default values of the parameters of the stepper, cascaded through a <see cref="BitParams"/>. A value set on the
    /// stepper itself wins over it.
    /// </summary>
    [CascadingParameter(Name = BitStepperParams.ParamName)]
    public BitStepperParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for the different parts of the stepper.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitStepperClassStyles? Classes { get; set; }

    /// <summary>
    /// The icon shown in the indicator of a completed step, using custom CSS classes for external icon libraries. Takes
    /// precedence over <see cref="CompletedIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? CompletedIcon { get; set; }

    /// <summary>
    /// The name of the icon shown in the indicator of a completed step, from the built-in Fluent UI icons. Defaults to
    /// "CheckMark".
    /// </summary>
    [Parameter] public string? CompletedIconName { get; set; }

    /// <summary>
    /// The index of the current step. The steps before it are completed and the ones after it are upcoming; an index
    /// past the last step shows every step completed, and a negative one shows none started. Default: 0.
    /// </summary>
    [Parameter, TwoWayBound]
    public int CurrentIndex { get; set; }

    /// <summary>
    /// The icon shown in the indicator of a step in error, using custom CSS classes for external icon libraries. Takes
    /// precedence over <see cref="ErrorIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? ErrorIcon { get; set; }

    /// <summary>
    /// The name of the icon shown in the indicator of a step in error, from the built-in Fluent UI icons. Defaults to
    /// "ErrorBadge".
    /// </summary>
    [Parameter] public string? ErrorIconName { get; set; }

    /// <summary>
    /// The steps of the stepper.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// Keeps the user from skipping ahead: only the steps before the current one (and the current one) can be clicked,
    /// so a step is reached only by the page moving <see cref="CurrentIndex"/> forward.
    /// </summary>
    [Parameter] public bool Linear { get; set; }

    /// <summary>
    /// The callback for when the user goes to another step, with its index. It runs even when
    /// <see cref="CurrentIndex"/> is bound one way and could not be written back, so a page holding the index itself
    /// still hears about the step the user asked for.
    /// </summary>
    [Parameter] public EventCallback<int> OnChange { get; set; }

    /// <summary>
    /// The callback for when a step is clicked, the current one included, as long as it can be gone to.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Only shows where the process is: no step can be clicked.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the stepper.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitStepperClassStyles? Styles { get; set; }

    /// <summary>
    /// Lays the steps out from top to bottom instead of from start to end.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }



    protected override string RootElementClass => "bit-stp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Vertical ? "bit-stp-vrt" : "bit-stp-hrz");

        ClassBuilder.Register(() => ReadOnly ? "bit-stp-rdo" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        if (ReferenceEquals(_oldItems, Items) is false)
        {
            _oldItems = Items;
            _items = Items?.ToList() ?? [];
        }

        base.OnParametersSet();
    }



    private BitStepperStepState GetState(TItem item, int index)
    {
        if (GetIsError(item)) return BitStepperStepState.Error;

        if (index < CurrentIndex) return BitStepperStepState.Completed;

        return index == CurrentIndex ? BitStepperStepState.Current : BitStepperStepState.Upcoming;
    }

    // Whether a click on the step goes anywhere: never on a read-only stepper or a disabled step, and on a linear one
    // only back (or to the current step), since going forward is the page's to do.
    private bool CanGoTo(TItem item, int index)
    {
        if (ReadOnly || Disabled || GetIsDisabled(item)) return false;

        return Linear is false || index <= CurrentIndex;
    }

    // A step is a button whenever a click on it could ever go somewhere. A disabled one stays a (disabled) button, so it
    // is announced as unavailable rather than dropped; a step a linear stepper does not let the user reach yet is no
    // control at all, since there is nothing to press there.
    private bool IsButton(TItem item, int index)
    {
        if (ReadOnly) return false;

        return Linear is false || index <= CurrentIndex || Disabled || GetIsDisabled(item);
    }

    private async Task HandleOnStepClick(TItem item, int index)
    {
        if (CanGoTo(item, index) is false) return;

        if (item is BitStepperItem stepperItem)
        {
            stepperItem.OnClick?.Invoke(stepperItem);
        }

        await OnItemClick.InvokeAsync(item);

        if (index == CurrentIndex) return;

        await AssignCurrentIndex(index);

        await OnChange.InvokeAsync(index);
    }

    private BitIconInfo? GetIndicatorIcon(TItem item, BitStepperStepState state)
    {
        return state switch
        {
            BitStepperStepState.Error => BitIconInfo.From(ErrorIcon, ErrorIconName ?? DefaultErrorIconName),
            BitStepperStepState.Completed => BitIconInfo.From(CompletedIcon, CompletedIconName ?? DefaultCompletedIconName),
            _ => BitIconInfo.From(GetIcon(item), GetIconName(item))
        };
    }

    // What a screen reader is told about a step on top of its text: the current one says so through aria-current.
    private static string? GetStateText(BitStepperStepState state)
    {
        return state switch
        {
            BitStepperStepState.Completed => BitStrings.Current.StepCompleted,
            BitStepperStepState.Error => BitStrings.Current.StepError,
            _ => null
        };
    }

    private static string GetNumber(int index) => (index + 1).ToString(CultureInfo.CurrentCulture);

    private string GetStepClass(TItem item, BitStepperStepState state, bool disabled)
    {
        var classes = new List<string> { "bit-stp-stp" };

        switch (state)
        {
            case BitStepperStepState.Completed:
                classes.Add("bit-stp-cmp");
                classes.AddIfHasValue(Classes?.CompletedStep);
                break;
            case BitStepperStepState.Current:
                classes.Add("bit-stp-cur");
                classes.AddIfHasValue(Classes?.CurrentStep);
                break;
            case BitStepperStepState.Upcoming:
                classes.Add("bit-stp-upc");
                classes.AddIfHasValue(Classes?.UpcomingStep);
                break;
            case BitStepperStepState.Error:
                classes.Add("bit-stp-err");
                classes.AddIfHasValue(Classes?.ErrorStep);
                break;
        }

        if (disabled) classes.Add("bit-stp-sdi");

        classes.AddIfHasValue(Classes?.Step);
        classes.AddIfHasValue(GetClass(item));

        return string.Join(" ", classes);
    }

    private string? GetStepStyle(TItem item, BitStepperStepState state)
    {
        var stateStyle = state switch
        {
            BitStepperStepState.Completed => Styles?.CompletedStep,
            BitStepperStepState.Current => Styles?.CurrentStep,
            BitStepperStepState.Upcoming => Styles?.UpcomingStep,
            BitStepperStepState.Error => Styles?.ErrorStep,
            _ => null
        };

        var style = string.Join(";", new[] { Styles?.Step, stateStyle, GetStyle(item) }.Where(s => s.HasValue()));

        return style.HasValue() ? style : null;
    }

    private string GetItemKey(TItem item, int index) => GetKey(item) ?? index.ToString(CultureInfo.InvariantCulture);



    // The item readers. Only BitStepperItem is read for now; the readers are where the other item types will be read.

    private string? GetAriaLabel(TItem item) => (item as BitStepperItem)?.AriaLabel;

    private string? GetClass(TItem item) => (item as BitStepperItem)?.Class;

    private string? GetDescription(TItem item) => (item as BitStepperItem)?.Description;

    private BitIconInfo? GetIcon(TItem item) => (item as BitStepperItem)?.Icon;

    private string? GetIconName(TItem item) => (item as BitStepperItem)?.IconName;

    private bool GetIsDisabled(TItem item) => (item as BitStepperItem)?.IsDisabled ?? false;

    private bool GetIsError(TItem item) => (item as BitStepperItem)?.IsError ?? false;

    private string? GetKey(TItem item) => (item as BitStepperItem)?.Key;

    private string? GetStyle(TItem item) => (item as BitStepperItem)?.Style;

    private string? GetText(TItem item) => (item as BitStepperItem)?.Text;

    private string? GetTitle(TItem item) => (item as BitStepperItem)?.Title;
}
