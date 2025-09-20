namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Message;

public partial class BitMessageDemo
{
    private readonly string example1RazorCode = @"
<BitMessage>This is a Message.</BitMessage>";

    private readonly string example2RazorCode = @"
<BitMessage Color=""BitColor.Primary"">Primary.</BitMessage>
<BitMessage Color=""BitColor.Secondary"">Secondary.</BitMessage>
<BitMessage Color=""BitColor.Tertiary"">Tertiary.</BitMessage>
<BitMessage Color=""BitColor.Info"">Info (default).</BitMessage>
<BitMessage Color=""BitColor.Success"">Success.</BitMessage>
<BitMessage Color=""BitColor.Warning"">Warning.</BitMessage>
<BitMessage Color=""BitColor.SevereWarning"">SevereWarning.</BitMessage>
<BitMessage Color=""BitColor.Error"">Error.</BitMessage>

<BitMessage Color=""BitColor.PrimaryBackground"">PrimaryBackground.</BitMessage>
<BitMessage Color=""BitColor.SecondaryBackground"">SecondaryBackground.</BitMessage>
<BitMessage Color=""BitColor.TertiaryBackground"">TertiaryBackground.</BitMessage>
                
<BitMessage Color=""BitColor.PrimaryForeground"">PrimaryForeground.</BitMessage>
<BitMessage Color=""BitColor.SecondaryForeground"">SecondaryForeground.</BitMessage>
<BitMessage Color=""BitColor.TertiaryForeground"">TertiaryForeground.</BitMessage>
<BitMessage Color=""BitColor.PrimaryBorder"">PrimaryBorder.</BitMessage>
<BitMessage Color=""BitColor.SecondaryBorder"">SecondaryBorder.</BitMessage>
<BitMessage Color=""BitColor.TertiaryBorder"">TertiaryBorder.</BitMessage>";

    private readonly string example3RazorCode = @"
<BitMessage Color=""BitColor.Primary"" Variant=""BitVariant.Fill"">Primary.</BitMessage>
<BitMessage Color=""BitColor.Secondary"" Variant=""BitVariant.Fill"">Secondary.</BitMessage>
<BitMessage Color=""BitColor.Tertiary"" Variant=""BitVariant.Fill"">Tertiary.</BitMessage>
<BitMessage Color=""BitColor.Info"" Variant=""BitVariant.Fill"">Info.</BitMessage>
<BitMessage Color=""BitColor.Success"" Variant=""BitVariant.Fill"">Success.</BitMessage>
<BitMessage Color=""BitColor.Warning"" Variant=""BitVariant.Fill"">Warning.</BitMessage>
<BitMessage Color=""BitColor.SevereWarning"" Variant=""BitVariant.Fill"">SevereWarning.</BitMessage>
<BitMessage Color=""BitColor.Error"" Variant=""BitVariant.Fill"">Error.</BitMessage>

<BitMessage Color=""BitColor.Primary"" Variant=""BitVariant.Outline"">Primary.</BitMessage>
<BitMessage Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"">Secondary.</BitMessage>
<BitMessage Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"">Tertiary.</BitMessage>
<BitMessage Color=""BitColor.Info"" Variant=""BitVariant.Outline"">Info.</BitMessage>
<BitMessage Color=""BitColor.Success"" Variant=""BitVariant.Outline"">Success.</BitMessage>
<BitMessage Color=""BitColor.Warning"" Variant=""BitVariant.Outline"">Warning.</BitMessage>
<BitMessage Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"">SevereWarning.</BitMessage>
<BitMessage Color=""BitColor.Error"" Variant=""BitVariant.Outline"">Error.</BitMessage>

<BitMessage Color=""BitColor.Primary"" Variant=""BitVariant.Text"">Primary.</BitMessage>
<BitMessage Color=""BitColor.Secondary"" Variant=""BitVariant.Text"">Secondary.</BitMessage>
<BitMessage Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"">Tertiary.</BitMessage>
<BitMessage Color=""BitColor.Info"" Variant=""BitVariant.Text"">Info.</BitMessage>
<BitMessage Color=""BitColor.Success"" Variant=""BitVariant.Text"">Success.</BitMessage>
<BitMessage Color=""BitColor.Warning"" Variant=""BitVariant.Text"">Warning.</BitMessage>
<BitMessage Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"">SevereWarning.</BitMessage>
<BitMessage Color=""BitColor.Error"" Variant=""BitVariant.Text"">Error.</BitMessage>";

    private readonly string example4RazorCode = @"
<BitMessage Alignment=""BitAlignment.Start"" Color=""BitColor.Primary"">Start</BitMessage>
<BitMessage Alignment=""BitAlignment.Center"" Color=""BitColor.Secondary"">Center</BitMessage>
<BitMessage Alignment=""BitAlignment.End"" Color=""BitColor.Tertiary"">End</BitMessage>";

    private readonly string example5RazorCode = @"
<BitMessage Elevation=""(int)elevation"">Elevated Message</BitMessage>

<BitSlider Label=""Elevation"" Min=""0"" Max=""24"" Step=""1"" @bind-Value=""elevation"" />";
    private readonly string example5CsharpCode = @"
private double elevation = 7;";

    private readonly string example6RazorCode = @"
<BitMessage Multiline Color=""BitColor.Success"">
    In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
    each word has the power to transform into something extraordinary. Here lies the start of something new—an 
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
    begins here, in this quiet moment where everything is possible.
</BitMessage>";

    private readonly string example7RazorCode = @"
<BitMessage Truncate Color=""BitColor.Warning"">
    In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
    each word has the power to transform into something extraordinary. Here lies the start of something new—an 
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
    begins here, in this quiet moment where everything is possible.
</BitMessage>";

    private readonly string example8RazorCode = @"
@if (isDismissed is false)
{
    <BitMessage OnDismiss=""() => isDismissed = true"" Color=""BitColor.SevereWarning"">
        Dismiss option enabled by adding <strong>OnDismiss</strong> parameter.
    </BitMessage>
}
else
{
    <BitButton OnClick=""() => isDismissed = false"">Dismissed, click to reset</BitButton>
}

@if (isAutoDismissed is false)
{
    <BitMessage AutoDismissTime=""TimeSpan.FromSeconds(5)"" OnDismiss=""() => isAutoDismissed = true"">
        Auto-Dismiss option enabled by adding <strong>AutoDismissTime</strong> parameter alongside of OnDismiss.
    </BitMessage>
}
else
{
    <BitButton OnClick=""() => isAutoDismissed = false"">Auto-Dismissed, click to reset</BitButton>
}";
    private readonly string example8CsharpCode = @"
private bool isDismissed;
private bool isAutoDismissed;";

    private readonly string example9RazorCode = @"
<BitMessage>
    <Actions>
        <BitButton Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" IconName=""@BitIconName.Up"" />
        &nbsp;
        <BitButton Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" IconName=""@BitIconName.Down"" />
    </Actions>
    <Content>
        Message with single line and action buttons.
    </Content>
</BitMessage>";

    private readonly string example10RazorCode = @"
<BitMessage Color=""BitColor.Info"" HideIcon>Info (default) Message.</BitMessage>
<BitMessage Color=""BitColor.Success"" HideIcon>Success Message.</BitMessage>
<BitMessage Color=""BitColor.Warning"" HideIcon>Warning Message.</BitMessage>
<BitMessage Color=""BitColor.SevereWarning"" HideIcon>SevereWarning Message.</BitMessage>
<BitMessage Color=""BitColor.Error"" HideIcon>Error Message.</BitMessage>";

    private readonly string example11RazorCode = @"
<BitMessage Color=""BitColor.Success"" IconName=""@BitIconName.CheckMark"">
    Message with a custom icon.
</BitMessage>

<BitMessage Color=""BitColor.Warning"" OnDismiss=""() => {}"" DismissIcon=""@BitIconName.Blocked2Solid"">
    Message with a custom dismiss icon.
</BitMessage>

<BitMessage Truncate Color=""BitColor.Warning""
            ExpandIcon=""@BitIconName.ChevronDownEnd""
            CollapseIcon=""@BitIconName.ChevronUpEnd"">
    In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
    each word has the power to transform into something extraordinary. Here lies the start of something new—an 
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
    begins here, in this quiet moment where everything is possible.
</BitMessage>";

    private readonly string example12RazorCode = @"
<BitMessage Truncate OnDismiss=""() => isWarningDismissed = true"" Color=""BitColor.Warning"">
    <Content>
        In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
        shaped into meaning, and the emotions ready to resonate with every reader.
        <br />
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
        each word has the power to transform into something extraordinary. Here lies the start of something new—an 
        opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
        idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
        begins here, in this quiet moment where everything is possible.
    </Content>
    <Actions>
        <div style=""display:flex;align-items:center;gap:4px;min-height:32px"">
            <button>Yes</button>
            <button>No</button>
        </div>
    </Actions>
</BitMessage>

<BitMessage Multiline OnDismiss=""() => isErrorDismissed = true"" Color=""BitColor.Error"">
    <Content>
        In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
        shaped into meaning, and the emotions ready to resonate with every reader.
        <br />
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
        each word has the power to transform into something extraordinary. Here lies the start of something new—an 
        opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
        idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
        begins here, in this quiet moment where everything is possible.
    </Content>
    <Actions>
        <BitButton Variant=""BitVariant.Outline"">Yes</BitButton>
        &nbsp;
        <BitButton Variant=""BitVariant.Outline"">No</BitButton>
    </Actions>
</BitMessage>";
    private readonly string example12CsharpCode = @"
private bool isWarningDismissed;
private bool isErrorDismissed;";

    private readonly string example13RazorCode = @"
<BitMessage Size=""BitSize.Small"" Color=""BitColor.Primary"">Small</BitMessage>
<BitMessage Size=""BitSize.Medium"" Color=""BitColor.Secondary"">Medium</BitMessage>
<BitMessage Size=""BitSize.Large"" Color=""BitColor.Tertiary"">Large</BitMessage>";

    private readonly string example14RazorCode = @"
<style>
    .custom-class {
        padding: 1rem;
        color: deeppink;
        font-size: 1rem;
        font-style: italic;
    }

    .custom-icon {
        font-size: 2rem;
    }

    .custom-content {
        font-size: 1.5rem;
    }

    .custom-expander-icon {
        margin: 0.5rem;
        font-size: 2rem;
    }

    .custom-dismiss-icon {
        margin: 0.5rem;
        font-size: 2rem;
    }
</style>

<BitMessage Multiline 
            OnDismiss=""() => {}""
            Color=""BitColor.Info"" 
            Style=""padding:8px;color:red;"">
    <b>Styled Message.</b>
    In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
    each word has the power to transform into something extraordinary. Here lies the start of something new—an 
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
    begins here, in this quiet moment where everything is possible.
</BitMessage>

<BitMessage Truncate 
            Class=""custom-class""
            Color=""BitColor.Success"">
    <b>Classed Message.</b>
    In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
    each word has the power to transform into something extraordinary. Here lies the start of something new—an 
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
    begins here, in this quiet moment where everything is possible.
</BitMessage>


<BitMessage Multiline
        OnDismiss=""() => {}"" 
        Color=""BitColor.Warning"" 
        Styles=""@(new() { Root=""padding:1rem"",
                          IconContainer=""line-height:1.25"",
                          Content=""color:blueviolet"",
                          ContentContainer=""margin:0 10px"",
                          DismissIcon=""font-size:1rem"",
                          Actions=""justify-content:center;gap:1rem"" })"">
<Content>
    <b>Styles.</b>
    In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
    each word has the power to transform into something extraordinary. Here lies the start of something new—an 
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
    begins here, in this quiet moment where everything is possible.
</Content>
<Actions>
    <BitButton Variant=""BitVariant.Text"">Ok</BitButton>
    <BitButton Variant=""BitVariant.Text"">Cancel</BitButton>
</Actions>
</BitMessage>

<BitMessage Truncate
            OnDismiss=""() => {}"" 
            Color=""BitColor.SevereWarning"" 
            Classes=""@(new() { Icon=""custom-icon"",
                               Content=""custom-content"",
                               ExpanderIcon=""custom-expander-icon"",
                               DismissIcon=""custom-dismiss-icon"" })"">
    <b>Classes.</b>
    In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
    each word has the power to transform into something extraordinary. Here lies the start of something new—an 
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
    begins here, in this quiet moment where everything is possible.
</BitMessage>";

    private readonly string example15RazorCode = @"
<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Info"">
    پیام خبری (پیش فرض). <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Success"" Truncate OnDismiss=""() => {}"">
    پیام موفق. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است
    و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
    کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها
    شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Warning"" Multiline OnDismiss=""() => {}"">
    پیام هشدار. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
    <br />
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است
    و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
    کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها
    شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.SevereWarning"">
    پیام هشدار شدید. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Error"">
    پیام خطا. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>";
}
