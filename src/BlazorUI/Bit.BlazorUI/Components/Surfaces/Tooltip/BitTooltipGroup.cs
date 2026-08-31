using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Groups the tooltips inside it so that they behave as the one piece of chrome a reader takes them for:
/// they share the delays they are shown and hidden with, the second of them is shown without its delay
/// while the first is still fresh in mind, and only one of them is on the screen at a time.
/// </summary>
/// <remarks>
/// It renders nothing of its own - it is a cascading value around whatever is put inside it - so it can
/// be wrapped around a toolbar, a row of icon buttons or a whole page without changing the layout. A
/// tooltip that sets a delay of its own keeps it; the group only fills in the ones that were left alone.
/// </remarks>
public class BitTooltipGroup : ComponentBase
{
    // The tooltips that have this group above them, so that showing one of them can take the others off
    // the screen. They add themselves as they are initialized and take themselves off as they go.
    private readonly HashSet<BitTooltip> _members = [];

    // When the last tooltip of the group left the screen, which is what the skip window is measured from.
    // Zero stands for "none has yet", so that the first tooltip of a page waits out its delay in full.
    private long _lastHiddenAt;



    /// <summary>
    /// The tooltips the group is around.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The delay in milliseconds before hiding, for every tooltip in the group that does not set one of
    /// its own.
    /// </summary>
    [Parameter] public int? HideDelay { get; set; }

    /// <summary>
    /// Lets more than one tooltip of the group be on the screen at a time.
    /// </summary>
    /// <remarks>
    /// A group shows one tooltip at a time by default, which is what a row of controls with a tooltip
    /// each needs: the pointer passing along it leaves one surface behind rather than a trail of them.
    /// Turn it on where the tooltips are meant to be read side by side.
    /// </remarks>
    [Parameter] public bool AllowMultiple { get; set; }

    /// <summary>
    /// The delay in milliseconds before showing, for every tooltip in the group that does not set one of
    /// its own.
    /// </summary>
    [Parameter] public int? ShowDelay { get; set; }

    /// <summary>
    /// How long in milliseconds after a tooltip of the group has been hidden another one of them is shown
    /// at once rather than waiting out the show delay. Zero makes every tooltip wait out its own delay.
    /// </summary>
    /// <remarks>
    /// The delay is there to keep a pointer merely crossing the page quiet. Once the reader has stopped
    /// on one control of a row and read its tooltip, they are reading the row rather than crossing it, so
    /// the next tooltip along is what they asked for and waiting for it again only reads as lag.
    /// </remarks>
    [Parameter] public int SkipDelay { get; set; } = 300;



    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BitTooltipGroup>>(0);
        builder.AddAttribute(1, "Value", this);
        builder.AddAttribute(2, "IsFixed", true);
        builder.AddAttribute(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    }



    internal void Register(BitTooltip tooltip) => _members.Add(tooltip);

    internal void Unregister(BitTooltip tooltip) => _members.Remove(tooltip);

    internal bool ShouldSkipShowDelay()
    {
        if (SkipDelay <= 0) return false;
        if (_lastHiddenAt == 0) return false;

        return Environment.TickCount64 - _lastHiddenAt <= SkipDelay;
    }

    internal async Task NotifyShown(BitTooltip tooltip)
    {
        if (AllowMultiple) return;

        // The list is copied because hiding a member renders it, and a render is where a member that is
        // going away takes itself off the group.
        foreach (var member in _members.ToArray())
        {
            if (ReferenceEquals(member, tooltip)) continue;

            await member.HideFromGroup();
        }
    }

    internal void NotifyHidden() => _lastHiddenAt = Environment.TickCount64;
}
