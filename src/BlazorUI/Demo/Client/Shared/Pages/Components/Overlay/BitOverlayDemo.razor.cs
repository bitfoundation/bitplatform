﻿using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.Overlay;

public partial class BitOverlayDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "true",
            Description = "When true, the Overlay will be closed by clicking on it.",
        },
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "true",
            Description = "When true, the scroll behavior of the Scroller element behind the overlay will be disabled.",
        },
        new()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            Description = "When true, the Overlay will be positioned absolute instead of fixed.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of the Overlay.",
        },
        new()
        {
            Name = "IsVisible",
            Type = "bool",
            Description = "When true, the Overlay and its content will be shown.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string",
            DefaultValue = "body",
            Description = "Set the selector of the Selector element for the Overlay to disable its scroll if applicable.",
        },
    };



    private bool BasicIsVisible;
    private bool AutoCloseIsVisible;
    private bool AbsoluteIsVisible;
    private bool AutoToggleIsVisible;
    private bool EnabledScrollerIsVisible;
    private bool DisabledScrollerIsVisible;


    private readonly string example1HTMLCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

    .content {
        display: flex;
        flex-flow: column nowrap;
        width: 85%;
        height: 250px;
        overflow: auto;
        background-color: white;
        border-radius: 3px;
        padding: 15px;
    }
</style>

<BitButton OnClick=""() => BasicIsVisible = true"">Show Overlay</BitButton>
<BitOverlay @bind-IsVisible=""BasicIsVisible""
            Class=""overlay"">
    <div class=""content"">
        <h3>Lorem Ipsum</h3>
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst.
        </p>
    </div>
</BitOverlay>
";
    private readonly string example1CSharpCode = @"
private bool BasicIsVisible;
";

    private readonly string example2HTMLCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

    .content {
        display: flex;
        flex-flow: column nowrap;
        width: 85%;
        height: 250px;
        overflow: auto;
        background-color: white;
        border-radius: 3px;
        padding: 15px;
    }

    .close-button {
        position: absolute;
        right: 10px;
    }
</style>

<BitButton OnClick=""() => AutoCloseIsVisible = true"">Show Overlay</BitButton>
<BitOverlay @bind-IsVisible=""AutoCloseIsVisible""
            Class=""overlay""
            AutoClose=""false"">
    <div class=""content"">
        <BitButton Class=""close-button"" OnClick=""() => AutoCloseIsVisible = false"">Close Overlay</BitButton>
        <h3>Lorem Ipsum</h3>
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst.
        </p>
    </div>
</BitOverlay>
";
    private readonly string example2CSharpCode = @"
private bool AutoCloseIsVisible;
";

    private readonly string example3HTMLCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

    .content {
        display: flex;
        flex-flow: column nowrap;
        width: 85%;
        height: 250px;
        overflow: auto;
        background-color: white;
        border-radius: 3px;
        padding: 15px;
    }

    .container {
        display: flex;
        justify-content: center;
        align-items: center;
        position: relative;
        height: 480px;
        border: 2px solid blue;
        background-color: #eee;
    }
</style>

<div class=""overlay-box"">
    <BitButton Class=""show-overlay-btn"" OnClick=""() => AbsoluteIsVisible = true"">Show Overlay</BitButton>
    <BitOverlay @bind-IsVisible=""AbsoluteIsVisible""
                Class=""overlay""
                AbsolutePosition=""true"">
        <div class=""content"">
            <h3>Lorem Ipsum</h3>
            <p>
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
                amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
                sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
                turpis. In hac habitasse platea dictumst.
            </p>
        </div>
    </BitOverlay>
    <h3>This is Container</h3>
</div>
";
    private readonly string example3CSharpCode = @"
private bool AbsoluteIsVisible;
";

    private readonly string example4HTMLCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

    .content {
        display: flex;
        flex-flow: column nowrap;
        width: 85%;
        height: 250px;
        overflow: auto;
        background-color: white;
        border-radius: 3px;
        padding: 15px;
    }
</style>

<BitButton OnClick=""() => AutoToggleIsVisible = true"">Show Overlay</BitButton>
<BitOverlay @bind-IsVisible=""AutoToggleIsVisible""
            Class=""overlay""
            AutoToggleScroll=""false"">
    <div class=""content"">
        <h3>Lorem Ipsum</h3>
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst.
        </p>
    </div>
</BitOverlay>
";
    private readonly string example4CSharpCode = @"
private bool AutoToggleIsVisible;
";

    private readonly string example5HTMLCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

    .content {
        display: flex;
        flex-flow: column nowrap;
        width: 85%;
        height: 250px;
        overflow: auto;
        background-color: white;
        border-radius: 3px;
        padding: 15px;
    }

    .scroller {
        margin-top: 15px;
        align-items: center;
        position: relative;
        height: 480px;
        border: 2px solid green;
        background-color: #eee;
        border-radius: 3px;
        padding: 15px;
        overflow: auto;
    }

    .btn-container {
        display: flex;
        flex-flow: row wrap;
        gap: 1rem;
    }
</style>

<div class=""btn-container"">
    <BitButton OnClick=""() => EnabledScrollerIsVisible = true"">Show with Enabled scrolling</BitButton>
    <BitButton OnClick=""() => DisabledScrollerIsVisible = true"">Show with Disabled scrolling</BitButton>
</div>

<div class=""scroller"">
    <BitOverlay @bind-IsVisible=""EnabledScrollerIsVisible""
                Class=""overlay""
                Style=""background-color:unset""
                ScrollerSelector="".scroller""
                AbsolutePosition=""true""
                AutoToggleScroll=""false"">
        <div class=""content"">
            <h3>Lorem Ipsum</h3>
            <p>
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
                amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
                sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
                turpis. In hac habitasse platea dictumst.
            </p>
        </div>
    </BitOverlay>

    <BitOverlay @bind-IsVisible=""DisabledScrollerIsVisible""
                Class=""overlay""
                ScrollerSelector="".scroller""
                AbsolutePosition=""true"">
        <div class=""content"">
            <h3>Lorem Ipsum</h3>
            <p>
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
                amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
                sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
                turpis. In hac habitasse platea dictumst.
            </p>
        </div>
    </BitOverlay>

    <h3>Lorem Ipsum</h3>
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
    <p>
        Nam id mi justo. Nam vehicula vulputate augue, ac pretium enim rutrum ultricies. Sed aliquet accumsan
        varius. Quisque ac auctor ligula. Fusce fringilla, odio et dignissim iaculis, est lacus ultrices risus,
        vitae condimentum enim urna eu nunc. In risus est, mattis non suscipit at, mattis ut ante. Maecenas
        consectetur urna vel erat maximus, non molestie massa consequat. Duis a feugiat nibh. Sed a hendrerit diam,
        a mattis est. In augue dolor, faucibus vel metus at, convallis rhoncus dui.
    </p>
</div>
";
    private readonly string example5CSharpCode = @"
private bool EnabledScrollerIsVisible;
private bool DisabledScrollerIsVisible;
";
}
