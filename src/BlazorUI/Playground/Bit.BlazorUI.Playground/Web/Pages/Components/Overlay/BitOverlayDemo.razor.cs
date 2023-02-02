using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Overlay;

public partial class BitOverlayDemo
{
    private bool BasicIsVisible;
    private bool AutoToggleIsVisible;
    private bool AbsoluteIsVisible;
    private bool ScrollerIsVisible;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "true",
            Description = "By default, it will be closed wherever the Overlay is clicked.",
        },
        new ComponentParameter()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "true",
            Description = "When the overlay is open, the element behind it cannot be scrolled, and when the overlay is closed, it returns to its previous state.",
        },
        new ComponentParameter()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            Description = "Set the Absolute Position style to Overlay when the Overlay is only for part of the page.",
        },
        new ComponentParameter()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "HTML content inside the Overlay.",
        },
        new ComponentParameter()
        {
            Name = "IsVisible",
            Type = "bool",
            Description = "Whether to display Overlay or not.",
        },
        new ComponentParameter()
        {
            Name = "ScrollerSelector",
            Type = "string",
            DefaultValue = "body",
            Description = "Set specific element to toggle scroll on behind of Overlay.",
        },
    };

    #region Sample Code 1

    private readonly string example1HTMLCode = @"
<style>
    .overlay {
        background-color: rgba(0,0,0,.4);
        z-index: 9999;
        align-items: center;
        justify-content: center;
    }

    .story-box {
        display: flex;
        flex-flow: column nowrap;
        width: 85%;
        height: 250px;
        overflow: auto;
        background-color: white;
        border-top: 4px solid blue;
        border-radius: 3px;
        padding: 15px;
    }
</style>

<BitButton OnClick=""() => BasicIsVisible = true"">Show Overlay</BitButton>
<BitOverlay @bind-IsVisible=""BasicIsVisible""
            Class=""overlay"">
    <div class=""story-box"">
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

    #endregion

    #region Sample Code 2

    private readonly string example2HTMLCode = @"
<style>
    .overlay {
        background-color: rgba(0,0,0,.4);
        z-index: 9999;
        align-items: center;
        justify-content: center;
    }

    .story-box {
        display: flex;
        flex-flow: column nowrap;
        width: 85%;
        height: 250px;
        overflow: auto;
        background-color: white;
        border-top: 4px solid blue;
        border-radius: 3px;
        padding: 15px;
    }

    .overlay-box {
        position: relative;
        height: 380px;
        border: 1px solid blue;
    }

    .close-overlay-btn {
        width: fit-content;
        position: absolute;
        right: 0;
        top: 0;
    }

    .show-overlay-btn {
        width: fit-content;
        position: absolute;
        left: 0;
        top: 0;
    }
</style>

<div class=""overlay-box"">
    <BitButton Class=""show-overlay-btn"" OnClick=""() => AbsoluteIsVisible = true"">Show Overlay</BitButton>
    <BitOverlay @bind-IsVisible=""AbsoluteIsVisible""
                Class=""overlay""
                AutoClose=""false""
                AbsolutePosition=""true"">
        <BitButton Class=""close-overlay-btn"" OnClick=""() => AbsoluteIsVisible = false"">Close Overlay</BitButton>
        <div class=""story-box"">
            <h3>Lorem Ipsum</h3>
            <p>
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
                amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
                sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
                turpis. In hac habitasse platea dictumst.
            </p>
        </div>
    </BitOverlay>
</div>
";

    private readonly string example2CSharpCode = @"
private bool AutoToggleIsVisible;
";

    #endregion

    #region Sample Code 3

    private readonly string example3HTMLCode = @"
<style>
    .overlay {
        background-color: rgba(0,0,0,.4);
        z-index: 9999;
        align-items: center;
        justify-content: center;
    }

    .story-box {
        display: flex;
        flex-flow: column nowrap;
        width: 85%;
        height: 250px;
        overflow: auto;
        background-color: white;
        border-top: 4px solid blue;
        border-radius: 3px;
        padding: 15px;
    }
</style>

<BitButton OnClick=""() => AutoToggleIsVisible = true"">Show Overlay</BitButton>
<BitOverlay @bind-IsVisible=""AutoToggleIsVisible""
            Class=""overlay""
            AutoToggleScroll=""false"">
    <div class=""story-box"">
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

    private readonly string example3CSharpCode = @"
private bool AbsoluteIsVisible;
";

    #endregion

    #region Sample Code 4

    private readonly string example4HTMLCode = @"
<style>
    .overlay {
        background-color: rgba(0,0,0,.4);
        z-index: 9999;
        align-items: center;
        justify-content: center;
    }

    .story-box {
        display: flex;
        flex-flow: column nowrap;
        width: 85%;
        height: 250px;
        overflow: auto;
        background-color: white;
        border-top: 4px solid blue;
        border-radius: 3px;
        padding: 15px;
    }

    .overlay-box {
        position: relative;
        height: 380px;
        border: 1px solid blue;
    }

    .show-overlay-btn {
        width: fit-content;
        position: absolute;
        left: 0;
        top: 0;
    }

    .scroller {
        display: flex;
        flex-flow: column nowrap;
        border-radius: 3px;
        padding: 40px 15px 15px 15px;
        overflow: auto;
        width: 100%;
        height: 100%;
    }
</style>

<div class=""overlay-box"">
    <BitButton Class=""show-overlay-btn"" OnClick=""() => ScrollerIsVisible = true"">Show Overlay</BitButton>
    <BitOverlay @bind-IsVisible=""ScrollerIsVisible""
                Class=""overlay""
                ScrollerSelector="".scroller""
                AbsolutePosition=""true"">
        <div class=""story-box"">
            <h3>Lorem Ipsum</h3>
            <p>
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
                amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
                sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
                turpis. In hac habitasse platea dictumst.
            </p>
        </div>
    </BitOverlay>

    <div class=""scroller"">
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
            Aenean egestas quam ut erat commodo blandit. Mauris ante nisl, pellentesque sed venenatis nec, aliquet sit
            amet enim. Praesent vitae diam non diam aliquet tristique non ut arcu. Pellentesque et ultrices eros. Fusce
            diam metus, mattis eu luctus nec, facilisis vel erat. Nam a lacus quis tellus gravida euismod. Nulla sed sem
            eget tortor cursus interdum. Sed vehicula tristique ultricies. Aenean libero purus, mollis quis massa quis,
            eleifend dictum massa. Fusce eu sapien sit amet odio lacinia placerat. Mauris varius risus sed aliquet
            cursus. Aenean lectus magna, tincidunt sit amet sodales a, volutpat ac leo. Morbi nisl sapien, tincidunt sit
            amet mauris quis, sollicitudin auctor est.
        </p>
        <p>
            Nam id mi justo. Nam vehicula vulputate augue, ac pretium enim rutrum ultricies. Sed aliquet accumsan
            varius. Quisque ac auctor ligula. Fusce fringilla, odio et dignissim iaculis, est lacus ultrices risus,
            vitae condimentum enim urna eu nunc. In risus est, mattis non suscipit at, mattis ut ante. Maecenas
            consectetur urna vel erat maximus, non molestie massa consequat. Duis a feugiat nibh. Sed a hendrerit diam,
            a mattis est. In augue dolor, faucibus vel metus at, convallis rhoncus dui.
        </p>
    </div>
</div>
";

    private readonly string example4CSharpCode = @"
private bool ScrollerIsVisible;
";

    #endregion
}
