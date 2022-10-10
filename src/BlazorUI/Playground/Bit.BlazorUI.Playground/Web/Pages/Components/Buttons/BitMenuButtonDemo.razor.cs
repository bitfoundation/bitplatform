using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitMenuButtonDemo
{
    private string basicSelectedItemKey;
    private string styledSelectedItemKey;
    private string itemTemplateSelectedItemKey;

    private List<BitMenuButtonItem> basicMenuButton = new()
    {
        new BitMenuButtonItem()
        {
            Text = "Item A",
            key = "A",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item B",
            key = "B",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item C",
            key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitMenuButtonItem> disabledItemMenuButton = new()
    {
        new BitMenuButtonItem()
        {
            Text = "Item A",
            key = "A",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item B",
            key = "B",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new BitMenuButtonItem()
        {
            Text = "Item C",
            key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitMenuButtonItem> itemTemplateMenuButton = new()
    {
        new BitMenuButtonItem()
        {
            Text = "Add",
            key = "add-key",
            IconName = BitIconName.Add
        },
        new BitMenuButtonItem()
        {
            Text = "Edit",
            key = "edit-key",
            IconName = BitIconName.Edit
        },
        new BitMenuButtonItem()
        {
            Text = "Delete",
            key = "delete-key",
            IconName = BitIconName.Delete
        }
    };
}
