using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// A row of the avatars of a group of people - the members of a team, the people on a document - drawn as overlapping
/// coins of <see cref="BitPersona"/>, with a "+N" coin standing in for the ones past <see cref="MaxDisplayed"/>.
/// </summary>
/// <remarks>
/// Each avatar is a persona that shows only its coin: its picture, or the initials of its name, with its presence dot.
/// It is named after its person to the screen readers, and it is a button where the group has an
/// <see cref="OnItemClick"/>, or a link where the item has an <see cref="BitAvatarGroupItem.Href"/>. The overflow coin
/// names the people it hides, and is a button where the group has an <see cref="OnOverflowClick"/> - to open the full
/// list, most of the time.
/// </remarks>
public partial class BitAvatarGroup : BitComponentBase
{
    private const string DefaultOverflowAriaLabelFormat = "{0} more: {1}";
    private const string OverflowKey = "bit-avg-overflow";



    /// <summary>
    /// The default values of the parameters of the avatar group, cascaded through a <see cref="BitParams"/>. A value set
    /// on the avatar group itself wins over it.
    /// </summary>
    [CascadingParameter(Name = BitAvatarGroupParams.ParamName)]
    public BitAvatarGroupParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for the different parts of the avatar group.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAvatarGroupClassStyles? Classes { get; set; }

    /// <summary>
    /// The people of the group, in the order their avatars are drawn.
    /// </summary>
    [Parameter] public IEnumerable<BitAvatarGroupItem> Items { get; set; } = [];

    /// <summary>
    /// The number of avatars drawn before the rest are counted by the overflow coin ("+N") instead. By default every
    /// item is drawn.
    /// </summary>
    [Parameter] public int? MaxDisplayed { get; set; }

    /// <summary>
    /// The callback for when an avatar is clicked, which makes every avatar a button.
    /// </summary>
    [Parameter] public EventCallback<BitAvatarGroupItem> OnItemClick { get; set; }

    /// <summary>
    /// The callback for when the overflow coin is clicked, given the items it stands in for. It makes the overflow coin
    /// a button.
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyList<BitAvatarGroupItem>> OnOverflowClick { get; set; }

    /// <summary>
    /// The name of the overflow coin to the screen readers, as a composite format string: {0} is the number of the
    /// people it hides and {1} their names. Default: "{0} more: {1}".
    /// </summary>
    [Parameter] public string? OverflowAriaLabelFormat { get; set; }

    /// <summary>
    /// The size of the avatars. Size8, which has no coin, is drawn as Size24. Default: Size32.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitPersonaSize Size { get; set; } = BitPersonaSize.Size32;

    /// <summary>
    /// Lays the avatars out side by side with a gap between them, instead of overlapping each other.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool Spread { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the avatar group.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAvatarGroupClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-avg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => CoinSize switch
        {
            BitPersonaSize.Size24 => "bit-avg-s24",
            BitPersonaSize.Size40 => "bit-avg-s40",
            BitPersonaSize.Size48 => "bit-avg-s48",
            BitPersonaSize.Size56 => "bit-avg-s56",
            BitPersonaSize.Size72 => "bit-avg-s72",
            BitPersonaSize.Size100 => "bit-avg-s100",
            BitPersonaSize.Size120 => "bit-avg-s120",
            _ => "bit-avg-s32"
        });

        ClassBuilder.Register(() => Spread ? "bit-avg-spr" : string.Empty);
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



    // The smallest persona is a presence dot without a coin, which a group of avatars has no use for.
    private BitPersonaSize CoinSize => Size is BitPersonaSize.Size8 ? BitPersonaSize.Size24 : Size;

    private (IReadOnlyList<BitAvatarGroupItem> Shown, IReadOnlyList<BitAvatarGroupItem> Hidden) SplitItems()
    {
        var items = Items as IReadOnlyList<BitAvatarGroupItem> ?? Items.ToList();

        if (MaxDisplayed is not { } max || items.Count <= max) return (items, []);

        var count = Math.Max(0, max);

        return (items.Take(count).ToList(), items.Skip(count).ToList());
    }

    private static object GetItemKey(BitAvatarGroupItem item) => item.Key ?? (object)item;

    // Only a group that does something with a click turns its avatars into buttons: a default callback would make
    // every coin a control with nothing behind it.
    private EventCallback<MouseEventArgs> GetItemClick(BitAvatarGroupItem item)
    {
        if (OnItemClick.HasDelegate is false) return default;

        return EventCallback.Factory.Create<MouseEventArgs>(this, () => OnItemClick.InvokeAsync(item));
    }

    private EventCallback<MouseEventArgs> GetOverflowClick(IReadOnlyList<BitAvatarGroupItem> hidden)
    {
        if (OnOverflowClick.HasDelegate is false) return default;

        return EventCallback.Factory.Create<MouseEventArgs>(this, () => OnOverflowClick.InvokeAsync(hidden));
    }

    private static string GetOverflowTitle(IReadOnlyList<BitAvatarGroupItem> hidden) => string.Join(", ", hidden.Select(i => i.Title ?? i.Text).Where(n => n.HasValue()));

    private string GetOverflowAriaLabel(IReadOnlyList<BitAvatarGroupItem> hidden)
    {
        var format = OverflowAriaLabelFormat.HasValue() ? OverflowAriaLabelFormat! : DefaultOverflowAriaLabelFormat;

        return string.Format(CultureInfo.CurrentCulture, format, hidden.Count, string.Join(", ", hidden.Select(i => i.Text).Where(n => n.HasValue())));
    }
}
