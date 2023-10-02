namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.ScrollablePane;

public partial class BitScrollablePaneDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of ScrollablePane, It can be Any custom tag or a text.",
        },
        new()
        {
            Name = "Height",
            Type = "double?",
            DefaultValue= "null",
            Description = "The height of the ScrollablePane.",
        },
        new()
        {
            Name = "OnScroll",
            Type = "EventCallback",
            Description = "Callback for when the ScrollablePane scrolled.",
        },
        new()
        {
            Name = "ScrollbarGutter",
            Type = "BitScrollbarGutter",
            DefaultValue= "BitScrollbarGutter.Auto",
            Description = "Allows to reserve space for the scrollbar, preventing unwanted layout changes as the content grows while also avoiding unnecessary visuals when scrolling isn't needed.",
            Href = "#scrollbar-gutter-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ScrollbarVisibility",
            Type = "BitScrollbarVisibility",
            DefaultValue= "BitScrollbarVisibility.Auto",
            Description = "Controls the visibility of scrollbars in the ScrollablePane.",
            Href = "#scrollbar-visibility-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ScrollContainerFocus",
            Type = "bool",
            DefaultValue= "false",
            Description = "Makes the scrollable container focusable, to aid with keyboard-only scrolling Should only be set to true if the scrollable region will not contain any other focusable items.",
        },
        new()
        {
            Name = "Width",
            Type = "double?",
            DefaultValue= "null",
            Description = "The width of the ScrollablePane.",
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "scrollbar-visibility-enum",
            Name = "BitScrollbarVisibility",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new() 
                {
                    Name = "Auto",
                    Value = "0",
                    Description = "Scrollbars are displayed automatically when needed based on the content size, and hidden when not needed."
                },
                new() 
                { 
                    Name = "Hidden",
                    Value = "1",
                    Description = "Scrollbars are always hidden, even if the content overflows the visible area."
                },
                new() 
                { 
                    Name = "Scroll",
                    Value = "2",
                    Description = "Scrollbars are always visible, allowing users to scroll through the content even if it doesn't overflow the visible area."
                }
            }
        },
        new()
        {
            Id = "scrollbar-gutter-enum",
            Name = "BitScrollbarGutter",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new() 
                {
                    Name = "Auto",
                    Value = "0",
                    Description = "The initial value. Classic scrollbars create a gutter when overflow is scroll, or when overflow is auto and the box is overflowing. Overlay scrollbars do not consume space."
                },
                new() 
                { 
                    Name = "Stable",
                    Value = "1",
                    Description = "When using classic scrollbars, the gutter will be present if overflow is auto, scroll, or hidden even if the box is not overflowing.When using overlay scrollbars, the gutter will not be present."
                },
                new() 
                { 
                    Name = "BothEdges",
                    Value = "2",
                    Description = "If a gutter would be present on one of the inline start/end edges of the box, another will be present on the opposite edge as well."
                }
            }
        }
    };



    private double visibilityItemsCount = 25;
    private BitScrollbarVisibility scrollbarVisibility;

    private double gutterItemsCount = 25;
    private BitScrollbarGutter scrollbarGutter;



    private readonly string example1RazorCode = @"
<BitScrollablePane Style=""height: 22rem; max-width: 32rem;"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
        Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
        Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
        efficitur.
    </p>
    <p>
        Mauris at nunc eget lectus lobortis facilisis et eget magna. Vestibulum venenatis augue sapien, rhoncus
        faucibus magna semper eget. Proin rutrum libero sagittis sapien aliquet auctor. Suspendisse tristique a
        magna at facilisis. Duis rhoncus feugiat magna in rutrum. Suspendisse semper, dolor et vestibulum lacinia,
        nunc felis malesuada ex, nec hendrerit justo ex et massa. Quisque quis mollis nulla. Nam commodo est ornare,
        rhoncus odio eu, pharetra tellus. Nunc sed velit mi.
    </p>
    <p>
        Sed condimentum ultricies turpis convallis pharetra. Sed sagittis quam pharetra luctus porttitor. Cras vel
        consequat lectus. Sed nec fringilla urna, a aliquet libero. Aenean sed nisl purus. Vivamus vulputate felis
        et odio efficitur suscipit. Ut volutpat dictum lectus, ac rutrum massa accumsan at. Sed pharetra auctor
        finibus. In augue libero, commodo vitae nisi non, sagittis convallis ante. Phasellus malesuada eleifend
        mollis. Curabitur ultricies leo ac metus venenatis elementum.
    </p>
</BitScrollablePane>";

    private readonly string example2RazorCode = @"
<BitScrollablePane Height=""16rem"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
        Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
        Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
        efficitur.
    </p>
    <p>
        Mauris at nunc eget lectus lobortis facilisis et eget magna. Vestibulum venenatis augue sapien, rhoncus
        faucibus magna semper eget. Proin rutrum libero sagittis sapien aliquet auctor. Suspendisse tristique a
        magna at facilisis. Duis rhoncus feugiat magna in rutrum. Suspendisse semper, dolor et vestibulum lacinia,
        nunc felis malesuada ex, nec hendrerit justo ex et massa. Quisque quis mollis nulla. Nam commodo est ornare,
        rhoncus odio eu, pharetra tellus. Nunc sed velit mi.
    </p>
    <p>
        Sed condimentum ultricies turpis convallis pharetra. Sed sagittis quam pharetra luctus porttitor. Cras vel
        consequat lectus. Sed nec fringilla urna, a aliquet libero. Aenean sed nisl purus. Vivamus vulputate felis
        et odio efficitur suscipit. Ut volutpat dictum lectus, ac rutrum massa accumsan at. Sed pharetra auctor
        finibus. In augue libero, commodo vitae nisi non, sagittis convallis ante. Phasellus malesuada eleifend
        mollis. Curabitur ultricies leo ac metus venenatis elementum.
    </p>
</BitScrollablePane>

<BitScrollablePane Width=""50%"" Style=""white-space: nowrap;"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
        Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
        Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
        efficitur.
    </p>
    <p>
        Mauris at nunc eget lectus lobortis facilisis et eget magna. Vestibulum venenatis augue sapien, rhoncus
        faucibus magna semper eget. Proin rutrum libero sagittis sapien aliquet auctor. Suspendisse tristique a
        magna at facilisis. Duis rhoncus feugiat magna in rutrum. Suspendisse semper, dolor et vestibulum lacinia,
        nunc felis malesuada ex, nec hendrerit justo ex et massa. Quisque quis mollis nulla. Nam commodo est ornare,
        rhoncus odio eu, pharetra tellus. Nunc sed velit mi.
    </p>
    <p>
        Sed condimentum ultricies turpis convallis pharetra. Sed sagittis quam pharetra luctus porttitor. Cras vel
        consequat lectus. Sed nec fringilla urna, a aliquet libero. Aenean sed nisl purus. Vivamus vulputate felis
        et odio efficitur suscipit. Ut volutpat dictum lectus, ac rutrum massa accumsan at. Sed pharetra auctor
        finibus. In augue libero, commodo vitae nisi non, sagittis convallis ante. Phasellus malesuada eleifend
        mollis. Curabitur ultricies leo ac metus venenatis elementum.
    </p>
</BitScrollablePane>

<BitScrollablePane Width=""256px"" Height=""352px"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
        Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
        Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
        efficitur.
    </p>
    <p>
        Mauris at nunc eget lectus lobortis facilisis et eget magna. Vestibulum venenatis augue sapien, rhoncus
        faucibus magna semper eget. Proin rutrum libero sagittis sapien aliquet auctor. Suspendisse tristique a
        magna at facilisis. Duis rhoncus feugiat magna in rutrum. Suspendisse semper, dolor et vestibulum lacinia,
        nunc felis malesuada ex, nec hendrerit justo ex et massa. Quisque quis mollis nulla. Nam commodo est ornare,
        rhoncus odio eu, pharetra tellus. Nunc sed velit mi.
    </p>
    <p>
        Sed condimentum ultricies turpis convallis pharetra. Sed sagittis quam pharetra luctus porttitor. Cras vel
        consequat lectus. Sed nec fringilla urna, a aliquet libero. Aenean sed nisl purus. Vivamus vulputate felis
        et odio efficitur suscipit. Ut volutpat dictum lectus, ac rutrum massa accumsan at. Sed pharetra auctor
        finibus. In augue libero, commodo vitae nisi non, sagittis convallis ante. Phasellus malesuada eleifend
        mollis. Curabitur ultricies leo ac metus venenatis elementum.
    </p>
</BitScrollablePane>";

    private readonly string example3RazorCode = @"
<style>
    .vertical-scroll-item {
        height: 2.75rem;
        margin: 0.5rem 0.5rem;
        padding: 0.5rem 1.25rem;
        background-color: #f2f2f2;
    }
</style>
                    
<BitScrollablePane ScrollbarVisibility=""@scrollbarVisibility"" Height=""16rem;"">
    @for (int i = 0; i < itemsCount; i++)
    {
        var index = i;
        <div class=""vertical-scroll-item"">@index</div>
    }
</BitScrollablePane>

<BitSpinButton Min=""0"" @bind-Value=""@itemsCount"" Mode=""@BitSpinButtonMode.Inline"" Label=""Items count"" />
                    
<BitChoiceGroup @bind-Value=""scrollbarVisibility"" Label=""Scrollbar visibility"" TItem=""BitChoiceGroupOption<BitScrollbarVisibility>"" TValue=""BitScrollbarVisibility"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitScrollbarVisibility.Auto"" />
    <BitChoiceGroupOption Text=""Hidden"" Value=""BitScrollbarVisibility.Hidden"" />
    <BitChoiceGroupOption Text=""Scroll"" Value=""BitScrollbarVisibility.Scroll"" />
</BitChoiceGroup>";
    private readonly string example3CsharpCode = @"
private double itemsCount = 25;
private BitScrollbarVisibility scrollbarVisibility;
";

    private readonly string example4RazorCode = @"
<style>
    .vertical-scroll-item {
        height: 2.75rem;
        margin: 0.5rem 0.5rem;
        padding: 0.5rem 1.25rem;
        background-color: #f2f2f2;
    }
</style>
                    
<BitScrollablePane ScrollbarGutter=""@scrollbarGutter"" Height=""16rem"">
    @for (int i = 0; i < gutterItemsCount; i++)
    {
        var index = i;
        <div class=""vertical-scroll-item"">@index</div>
    }
</BitScrollablePane>

<BitSpinButton Min=""0"" @bind-Value=""@gutterItemsCount"" Mode=""@BitSpinButtonMode.Inline"" Label=""Items count"" />

<BitChoiceGroup @bind-Value=""scrollbarGutter"" Label=""Scrollbar gutter"" TItem=""BitChoiceGroupOption<BitScrollbarGutter>"" TValue=""BitScrollbarGutter"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitScrollbarGutter.Auto"" />
    <BitChoiceGroupOption Text=""Stable"" Value=""BitScrollbarGutter.Stable"" />
    <BitChoiceGroupOption Text=""BothEdges"" Value=""BitScrollbarGutter.BothEdges"" />
</BitChoiceGroup>";
    private readonly string example4CsharpCode = @"
private double gutterItemsCount = 25;
private BitScrollbarGutter scrollbarGutter;
";
}
