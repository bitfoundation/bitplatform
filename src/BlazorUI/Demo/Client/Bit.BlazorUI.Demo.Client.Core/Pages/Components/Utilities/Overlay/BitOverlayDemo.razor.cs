namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Overlay;

public partial class BitOverlayDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the scroll behavior of the Scroller element behind the overlay will be disabled.",
        },
        new()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Overlay will be positioned absolute instead of fixed.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Overlay.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Overlay and its content will be shown.",
        },
        new()
        {
            Name = "NoAutoClose",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Overlay will be closed by clicking on it.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the toggle button is clicked.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string",
            DefaultValue = "body",
            Description = "Set the selector of the Selector element for the Overlay to disable its scroll if applicable.",
        }
    ];



    private bool BasicIsOpen;
    private bool AutoCloseIsOpen;
    private bool AbsoluteIsOpen;
    private bool AutoToggleIsOpen;
    private bool EventOnCloseIsOpen;
    private bool EnabledScrollerIsOpen;
    private bool DisabledScrollerIsOpen;



    private readonly string example1RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }
</style>


<BitButton OnClick=""() => BasicIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""BasicIsOpen"" Class=""overlay"">
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>";
    private readonly string example1CsharpCode = @"
private bool BasicIsOpen;";

    private readonly string example2RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

    .content {
        width: 85%;
        height: 250px;
        display: flex;
        padding: 15px;
        overflow: auto;
        border-radius: 3px;
        background-color: white;
        flex-flow: column nowrap;
    }

    .close-button {
        right: 10px;
        position: absolute;
    }
</style>


<BitButton OnClick=""() => AutoCloseIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""AutoCloseIsOpen"" Class=""overlay"" NoAutoClose>
    <div class=""content"">
        <BitButton Class=""close-button"" Variant=""BitVariant.Text"" OnClick=@(() => AutoCloseIsOpen = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
        <h3>Lorem Ipsum</h3>
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst.
        </p>
    </div>
</BitOverlay>";
    private readonly string example2CsharpCode = @"
private bool AutoCloseIsOpen;";

    private readonly string example3RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

    .show-button {
        gap: 5px;
        top: 15px;
        left: 10px;
        display: flex;
        width: fit-content;
        position: absolute;
        flex-flow: row wrap;
    }
</style>


<BitButton Class=""show-button"" OnClick=""() => AbsoluteIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""AbsoluteIsOpen""
            Class=""overlay""
            AbsolutePosition>
    <BitProgress Circular Indeterminate Thickness=""10"" />
</BitOverlay>

<h3>This is Container</h3>";
    private readonly string example3CsharpCode = @"
private bool AbsoluteIsOpen;";

    private readonly string example4RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }
</style>


<BitButton OnClick=""() => AutoToggleIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""AutoToggleIsOpen"" Class=""overlay"" AutoToggleScroll>
    <BitStack Alignment=""BitAlignment.Center"">
        <BitText Style=""color: dodgerblue;"" Typography=""BitTypography.H3"">Please wait...</BitText>
        <BitProgress Indeterminate Thickness=""10"" Style=""width: 19rem;""/>
    </BitStack>
</BitOverlay>";
    private readonly string example4CsharpCode = @"
private bool AutoToggleIsOpen;";

    private readonly string example5RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

    .content {
        width: 87%;
        display: flex;
        padding: 15px;
        overflow: auto;
        max-height: 288px;
        border-radius: 3px;
        position: relative;
        background-color: white;
        flex-flow: column nowrap;
        border: dodgerblue solid 1.6px;
    }

    .scroller {
        height: 360px;
        padding: 15px;
        overflow: auto;
        margin-top: 15px;
        position: relative;
        border-radius: 3px;
        align-items: center;
        border: 2px solid green;
    }
</style>


<BitButton OnClick=""() => EnabledScrollerIsOpen = true"">Show with Enabled scrolling</BitButton>
<BitButton OnClick=""() => DisabledScrollerIsOpen = true"">Show with Disabled scrolling</BitButton>

<div class=""scroller"">
    <BitOverlay @bind-IsOpen=""EnabledScrollerIsOpen""
                Class=""overlay""
                Style=""background-color:unset""
                ScrollerSelector="".scroller""
                AbsolutePosition>
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

    <BitOverlay @bind-IsOpen=""DisabledScrollerIsOpen""
                Class=""overlay""
                ScrollerSelector="".scroller""
                AbsolutePosition
                AutoToggleScroll>
        <BitProgress Circular Indeterminate Thickness=""10"" />
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
</div>";
    private readonly string example5CsharpCode = @"
private bool EnabledScrollerIsOpen;
private bool DisabledScrollerIsOpen;";

    private readonly string example6RazorCode = @"
<style>
    .overlay {
        z-index: 9999;
        align-items: center;
        justify-content: center;
        background-color: rgba(0,0,0,.4);
    }

    .content {
        width: 85%;
        height: 250px;
        display: flex;
        padding: 15px;
        overflow: auto;
        border-radius: 3px;
        background-color: white;
        flex-flow: column nowrap;
    }
</style>


<BitButton OnClick=""() => EventOnCloseIsOpen = true"">Show Overlay</BitButton>
<BitOverlay @bind-IsOpen=""EventOnCloseIsOpen"" Class=""overlay"" OnClick=@(() => EventOnCloseIsOpen = false) NoAutoClose>
    <div class=""content"">
        <h3>Lorem Ipsum</h3>
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst.
        </p>
    </div>
</BitOverlay>";
    private readonly string example6CsharpCode = @"
private bool EventOnCloseIsOpen;";
}
