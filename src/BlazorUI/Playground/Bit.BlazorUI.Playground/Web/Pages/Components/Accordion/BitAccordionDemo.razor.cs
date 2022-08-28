using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Accordion;

public partial class BitAccordionDemo
{
    public List<BitAccordionItem> BitAccordionItems { get; set; } = new List<BitAccordionItem>
    {
        new BitAccordionItem
        {
            Title = "title-1",
            Description = "a simple description",
            Text = "BitDataGrid is a robust way to display an information-rich collection of items, and allow people to sort, and filter the content. Use a data-grid when information density is critical."
        },
        new BitAccordionItem
        {
            Title = "title-2",
            Text = "BitDataGrid is a robust way to display an information-rich collection of items, and allow people to sort, and filter the content. Use a data-grid when information density is critical.",
            IsEnabled = false,
        },
        new BitAccordionItem
        {
            Title = "title-3",
            Text = "BitDataGrid is a robust way to display an information-rich collection of items, and allow people to sort, and filter the content. Use a data-grid when information density is critical.",
            IsExpanded = true
        },
        new BitAccordionItem
        {
            Title = "title-4",
            Text = "BitDataGrid is a robust way to display an information-rich collection of items, and allow people to sort, and filter the content. Use a data-grid when information density is critical."
        },
    };
}
