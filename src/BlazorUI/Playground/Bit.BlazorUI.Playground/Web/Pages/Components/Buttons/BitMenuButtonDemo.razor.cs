using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitMenuButtonDemo
{
    private List<BitMenuButtonItem> example1Items = new()
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
}
