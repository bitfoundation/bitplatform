using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitContextMenu{TItem}"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the context menus of an app agree on: the look of their menus, their name and whether an
/// item closes them. The items, and whether one is open, are the state of a single context menu.
/// </remarks>
public partial class BitContextMenuParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitContextMenu{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitContextMenu value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitContextMenu";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitContextMenuParams))]
    public BitContextMenuParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Whether the menu closes once one of its items is clicked.
    /// </summary>
    public bool? CloseOnItemClick { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the menu.
    /// </summary>
    public BitMenuButtonClassStyles? MenuClasses { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the menu.
    /// </summary>
    public BitMenuButtonClassStyles? MenuStyles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitContextMenu{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitContextMenu{TItem}"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitContextMenu"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitContextMenu"/>.
    /// </remarks>
    /// <param name="bitContextMenu">
    /// The <see cref="BitContextMenu{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem>(BitContextMenu<TItem> bitContextMenu) where TItem : class
    {
        if (bitContextMenu is null) return;

        UpdateBaseParameters(bitContextMenu);

        if (CloseOnItemClick.HasValue)
        {
            bitContextMenu.TakeFromCascade(nameof(CloseOnItemClick), CloseOnItemClick.Value, static c => c.CloseOnItemClick, static (c, v) => c.CloseOnItemClick = v);
        }

        if (MenuClasses is not null)
        {
            bitContextMenu.TakeFromCascade(nameof(MenuClasses), MenuClasses, static c => c.MenuClasses, static (c, v) => c.MenuClasses = v);
        }

        if (MenuStyles is not null)
        {
            bitContextMenu.TakeFromCascade(nameof(MenuStyles), MenuStyles, static c => c.MenuStyles, static (c, v) => c.MenuStyles = v);
        }
    }
}
