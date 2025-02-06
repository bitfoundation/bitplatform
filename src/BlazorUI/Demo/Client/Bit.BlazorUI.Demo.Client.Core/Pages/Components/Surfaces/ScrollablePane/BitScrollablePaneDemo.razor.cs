namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.ScrollablePane;

public partial class BitScrollablePaneDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "Overflow",
            Type = "BitOverflow?",
            DefaultValue= "null",
            Description = "Controls the visibility of scrollbars in the ScrollablePane.",
            Href = "#overflow-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "OverflowX",
            Type = "BitOverflow?",
            DefaultValue= "null",
            Description = "Controls the visibility of X-axis scrollbar in the ScrollablePane.",
            Href = "#overflow-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "OverflowY",
            Type = "BitOverflow?",
            DefaultValue= "null",
            Description = "Controls the visibility of Y-axis scrollbar in the ScrollablePane.",
            Href = "#overflow-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ScrollbarGutter",
            Type = "BitScrollbarGutter?",
            DefaultValue= "null",
            Description = "Allows to reserve space for the scrollbar, preventing unwanted layout changes as the content grows while also avoiding unnecessary visuals when scrolling isn't needed.",
            Href = "#scrollbarGutter-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Width",
            Type = "double?",
            DefaultValue= "null",
            Description = "The width of the ScrollablePane.",
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "overflow-enum",
            Name = "BitOverflow",
            Description = "",
            Items =
            [
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
                },
                new()
                {
                    Name = "Visible",
                    Value = "3",
                    Description = "Overflow content is not clipped and may be visible outside the element's padding box."
                }
            ]
        },
        new()
        {
            Id = "scrollbarGutter-enum",
            Name = "BitScrollbarGutter",
            Description = "",
            Items =
            [
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
            ]
        }
    ];



    private double overflowItemsCount = 6;
    private BitOverflow overflow;

    private double gutterItemsCount = 6;
    private BitScrollbarGutter gutter;



    private readonly string example1RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid #999;
    }
</style>

<BitScrollablePane Style=""height:350px;"" Class=""pane"">
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
    amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
    sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
    turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
    ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
    Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
    Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
    efficitur.
    <br />
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
    amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
    sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
    turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
    ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
    Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
    Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
    efficitur.
    <br />
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
    amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
    sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
    turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
    ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
    Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
    Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
    efficitur.
    <br />
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
    amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
    sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
    turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
    ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
    Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
    Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
    efficitur.
</BitScrollablePane>";

    private readonly string example2RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid #999;
    }
</style>

<BitScrollablePane Height=""15rem"" Class=""pane"">
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
    amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
    sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
    turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
    ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
    Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
    Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
    efficitur.
    <br />
    Mauris at nunc eget lectus lobortis facilisis et eget magna. Vestibulum venenatis augue sapien, rhoncus
    faucibus magna semper eget. Proin rutrum libero sagittis sapien aliquet auctor. Suspendisse tristique a
    magna at facilisis. Duis rhoncus feugiat magna in rutrum. Suspendisse semper, dolor et vestibulum lacinia,
    nunc felis malesuada ex, nec hendrerit justo ex et massa. Quisque quis mollis nulla. Nam commodo est ornare,
    rhoncus odio eu, pharetra tellus. Nunc sed velit mi.
    <br />
    Sed condimentum ultricies turpis convallis pharetra. Sed sagittis quam pharetra luctus porttitor. Cras vel
    consequat lectus. Sed nec fringilla urna, a aliquet libero. Aenean sed nisl purus. Vivamus vulputate felis
    et odio efficitur suscipit. Ut volutpat dictum lectus, ac rutrum massa accumsan at. Sed pharetra auctor
    finibus. In augue libero, commodo vitae nisi non, sagittis convallis ante. Phasellus malesuada eleifend
    mollis. Curabitur ultricies leo ac metus venenatis elementum.
</BitScrollablePane>

<BitScrollablePane Width=""300px"" Class=""pane"" Style=""white-space:nowrap"">
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
    amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
    sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
    turpis. In hac habitasse platea dictumst.
    <br />
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
    amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
    sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
    turpis. In hac habitasse platea dictumst.
    <br />
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
    amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
    sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
    turpis. In hac habitasse platea dictumst.
    <br />
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
    amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
    sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
    turpis. In hac habitasse platea dictumst.
    <br />
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
    amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
    sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
    turpis. In hac habitasse platea dictumst.
</BitScrollablePane>";

    private readonly string example3RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid #999;
    }

    .item {
        color: black;
        height: 2.75rem;
        margin: 0.5rem 0;
        background-color: #777;
        padding: 0.5rem 1.25rem;
    }
</style>
                    
<BitChoiceGroup @bind-Value=""overflow""
                Horizontal
                Label=""Overflow""
                TItem=""BitChoiceGroupOption<BitOverflow>"" TValue=""BitOverflow"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitOverflow.Auto"" />
    <BitChoiceGroupOption Text=""Hidden"" Value=""BitOverflow.Hidden"" />
    <BitChoiceGroupOption Text=""Scroll"" Value=""BitOverflow.Scroll"" />
    <BitChoiceGroupOption Text=""Visible"" Value=""BitOverflow.Visible"" />
</BitChoiceGroup>

<BitSpinButton Label=""Items count"" Min=""4"" @bind-Value=""@overflowItemsCount"" />

<BitScrollablePane Overflow=""@overflow"" Height=""16rem"" Class=""pane"">
    @for (int i = 0; i < overflowItemsCount; i++)
    {
        var index = i;
        <div class=""item"">@index</div>
    }
</BitScrollablePane>";
    private readonly string example3CsharpCode = @"
private double overflowItemsCount = 6;
private BitOverflow overflow;
";

    private readonly string example4RazorCode = @"
<style>
    .pane {
        padding: 0 0.25rem;
        border: 1px solid #999;
    }

    .item {
        color: black;
        height: 2.75rem;
        margin: 0.5rem 0;
        background-color: #777;
        padding: 0.5rem 1.25rem;
    }
</style>
                    
<BitChoiceGroup @bind-Value=""gutter""
                Horizontal
                Label=""Scrollbar gutter""
                TItem=""BitChoiceGroupOption<BitScrollbarGutter>"" TValue=""BitScrollbarGutter"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitScrollbarGutter.Auto"" />
    <BitChoiceGroupOption Text=""Stable"" Value=""BitScrollbarGutter.Stable"" />
    <BitChoiceGroupOption Text=""BothEdges"" Value=""BitScrollbarGutter.BothEdges"" />
</BitChoiceGroup>

<BitSpinButton Label=""Items count"" Min=""4"" @bind-Value=""@gutterItemsCount"" />

<BitScrollablePane Gutter=""@gutter"" Height=""16rem"" Class=""pane"">
    @for (int i = 0; i < gutterItemsCount; i++)
    {
        var index = i;
        <div class=""item"">@index</div>
    }
</BitScrollablePane>";
    private readonly string example4CsharpCode = @"
private double gutterItemsCount = 6;
private BitScrollbarGutter gutter;
";
}
