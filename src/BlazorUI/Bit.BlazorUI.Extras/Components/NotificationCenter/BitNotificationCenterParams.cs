using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitNotificationCenter"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the notification centers of an app agree on: the look of their parts, their words, the
/// bell, the time zone and how much history to keep. Whether one is open (<see cref="BitNotificationCenter.IsOpen"/>
/// and its callback) is deliberately not here: it is the state of a single center.
/// </remarks>
public class BitNotificationCenterParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitNotificationCenter"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitNotificationCenter value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitNotificationCenter)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitNotificationCenterParams))]
    public BitNotificationCenterParams() { }



    public string Name => ParamName;



    /// <summary>
    /// The text of the button that forgets every notification of the history.
    /// </summary>
    public string? ClearAllText { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the notification center.
    /// </summary>
    public BitNotificationCenterClassStyles? Classes { get; set; }

    /// <summary>
    /// The text the panel shows when there are no notifications.
    /// </summary>
    public string? EmptyText { get; set; }

    /// <summary>
    /// The text at the top of the panel.
    /// </summary>
    public string? HeaderText { get; set; }

    /// <summary>
    /// How many notifications the center makes the snack bar service keep.
    /// </summary>
    public int? HistoryLimit { get; set; }

    /// <summary>
    /// The icon of the bell button.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon of the bell button.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The custom template of each notification of the list.
    /// </summary>
    public RenderFragment<BitSnackBarItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the notification center.
    /// </summary>
    public BitNotificationCenterClassStyles? Styles { get; set; }

    /// <summary>
    /// The time zone the time of each notification is shown in.
    /// </summary>
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// The composite format the unread count joins the name of the bell button with ({0} = the count).
    /// </summary>
    public string? UnreadAriaLabelFormat { get; set; }

    /// <summary>
    /// What a screen reader hears of a notification that had not been read when the panel opened.
    /// </summary>
    public string? UnreadItemText { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitNotificationCenter"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitNotificationCenter"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitNotificationCenter"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitNotificationCenter"/>.
    /// </remarks>
    /// <param name="bitNotificationCenter">
    /// The <see cref="BitNotificationCenter"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitNotificationCenter bitNotificationCenter)
    {
        if (bitNotificationCenter is null) return;

        UpdateBaseParameters(bitNotificationCenter);

        if (ClearAllText is not null)
        {
            bitNotificationCenter.TakeFromCascade(nameof(ClearAllText), ClearAllText, static c => c.ClearAllText, static (c, v) => c.ClearAllText = v);
        }

        if (Classes is not null)
        {
            bitNotificationCenter.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (EmptyText is not null)
        {
            bitNotificationCenter.TakeFromCascade(nameof(EmptyText), EmptyText, static c => c.EmptyText, static (c, v) => c.EmptyText = v);
        }

        if (HeaderText is not null)
        {
            bitNotificationCenter.TakeFromCascade(nameof(HeaderText), HeaderText, static c => c.HeaderText, static (c, v) => c.HeaderText = v);
        }

        if (HistoryLimit.HasValue)
        {
            bitNotificationCenter.TakeFromCascade(nameof(HistoryLimit), HistoryLimit.Value, static c => c.HistoryLimit, static (c, v) => c.HistoryLimit = v);
        }

        if (Icon is not null)
        {
            bitNotificationCenter.TakeFromCascade(nameof(Icon), Icon, static c => c.Icon, static (c, v) => c.Icon = v);
        }

        if (IconName is not null)
        {
            bitNotificationCenter.TakeFromCascade(nameof(IconName), IconName, static c => c.IconName, static (c, v) => c.IconName = v);
        }

        if (ItemTemplate is not null)
        {
            bitNotificationCenter.TakeFromCascade(nameof(ItemTemplate), ItemTemplate, static c => c.ItemTemplate, static (c, v) => c.ItemTemplate = v);
        }

        if (Styles is not null)
        {
            bitNotificationCenter.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (TimeZone is not null)
        {
            bitNotificationCenter.TakeFromCascade(nameof(TimeZone), TimeZone, static c => c.TimeZone, static (c, v) => c.TimeZone = v);
        }

        if (UnreadAriaLabelFormat is not null)
        {
            bitNotificationCenter.TakeFromCascade(nameof(UnreadAriaLabelFormat), UnreadAriaLabelFormat, static c => c.UnreadAriaLabelFormat, static (c, v) => c.UnreadAriaLabelFormat = v);
        }

        if (UnreadItemText is not null)
        {
            bitNotificationCenter.TakeFromCascade(nameof(UnreadItemText), UnreadItemText, static c => c.UnreadItemText, static (c, v) => c.UnreadItemText = v);
        }
    }
}
