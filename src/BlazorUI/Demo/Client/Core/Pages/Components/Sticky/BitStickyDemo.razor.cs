namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Sticky;

public partial class BitStickyDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Bottom",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifying the vertical position of a positioned element from bottom."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Sticky, it can be any custom tag or text."
        },
        new()
        {
            Name = "Left",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifying the horizontal position of a positioned element from left."
        },
        new()
        {
            Name = "Right",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifying the horizontal position of a positioned element from right."
        },
        new()
        {
            Name = "StickyPosition",
            Type = "BitStickyPosition",
            DefaultValue= "BitStickyPosition.Top",
            Description = "Region to render sticky component in.",
            Href = "#sticky-position-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Top",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifying the vertical position of a positioned element from top."
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "sticky-position-enum",
            Name = "BitStickyPosition",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name = "Top",
                    Value = "0",
                },
                new()
                {
                    Name = "Bottom",
                    Value = "1",
                },
                new()
                {
                    Name = "TopAndBottom",
                    Value = "2",
                },
                new()
                {
                    Name = "Left",
                    Value = "3",
                },
                new()
                {
                    Name = "Right",
                    Value = "4",
                },
                new()
                {
                    Name = "LeftAndRight",
                    Value = "5",
                }
            }
        }
    };



    private readonly string example1RazorCode = @"
<style>
    .vertical-container {
        height: 16rem;
        overflow: auto;
        max-width: 32rem;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }
</style>


<div class=""custom-vertical-scroll"">
    <BitSticky Class=""custom-sticky"">Sticky Component</BitSticky>
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
</div>";

    private readonly string example2RazorCode = @"
<style>
    .vertical-container {
        height: 16rem;
        overflow: auto;
        max-width: 32rem;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }
</style>


<div class=""vertical-container"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </p>
    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.Top"">Stick to Top</BitSticky>
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
</div>


<div class=""vertical-container"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
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
    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.Bottom"">Stick to Bottom</BitSticky>
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
</div>


<div class=""vertical-container"">
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
    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.TopAndBottom"">Stick to Top and Bottom</BitSticky>
    <p>
        Sed condimentum ultricies turpis convallis pharetra. Sed sagittis quam pharetra luctus porttitor. Cras vel
        consequat lectus. Sed nec fringilla urna, a aliquet libero. Aenean sed nisl purus. Vivamus vulputate felis
        et odio efficitur suscipit. Ut volutpat dictum lectus, ac rutrum massa accumsan at. Sed pharetra auctor
        finibus. In augue libero, commodo vitae nisi non, sagittis convallis ante. Phasellus malesuada eleifend
        mollis. Curabitur ultricies leo ac metus venenatis elementum.
    </p>
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
</div>";

    private readonly string example3RazorCode = @"
<style>
    .vertical-container {
        height: 16rem;
        overflow: auto;
        max-width: 32rem;
    }

    .sticky {
        color: black;
        padding: 0.5rem;
        background-color: #AAA;
        border: 1px solid #777;
    }
</style>


<div class=""horizontal-container"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada.
    </p>
    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.Left"">Stick to Left</BitSticky>
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada.
    </p>
</div>


<div class=""horizontal-container"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada.
    </p>
    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.Right"">Stick to Right</BitSticky>
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada.
    </p>
</div>


<div class=""horizontal-container"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada.
    </p>
    <BitSticky Class=""sticky"" Position=""@BitStickyPosition.LeftAndRight"">Stick to Left and Right</BitSticky>
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada.
    </p>
</div>";
}

