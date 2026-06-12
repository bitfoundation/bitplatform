namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Modal;

public partial class BitModalDemo
{
    [CascadingParameter(Name = BitAppShell.Container)]
    private ElementReference? appShellContainer { get; set; }



    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Blocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay).",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of Modal, It can be Any custom tag or a text.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitModalClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitModal component.",
            LinkType = LinkType.Link,
            Href = "#modal-class-styles",
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Modal height 100% of its parent container.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Modal width 100% of its parent container.",
        },
        new()
        {
            Name = "IsAlert",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Determines the ARIA role of the Modal (alertdialog/dialog).",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Modal is displayed.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when the Modal is dismissed.",
        },
        new()
        {
            Name = "OnOverlayClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when somewhere on the overlay element of the Modal is clicked.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitModalClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitModal component.",
            LinkType = LinkType.Link,
            Href = "#modal-class-styles",
        },
        new()
        {
            Name = "SubtitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the subtitle of the Modal, if any.",
        },
        new()
        {
            Name = "TitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the title of the Modal, if any.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "modal-class-styles",
            Title = "BitModalClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitModal."
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overlay of the BitModal."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the content of the BitModal."
               }
            ]
        }
    ];

    private bool isOpenBasic;

    private bool isOpenBlocking;

    private bool isOpenCustomContent;

    private bool isEventsOpen;
    private bool isDismissed;
    private bool isOverlayClicked;
    private async Task HandleOnDismiss()
    {
        isDismissed = true;
        await Task.Delay(3000);
        isDismissed = false;
    }
    private void HandleOnOverlayClick()
    {
        isOverlayClicked = true;
        _ = Task.Delay(2000).ContinueWith(_ =>
            {
                isOverlayClicked = false;
                InvokeAsync(StateHasChanged);
            });
    }


    private bool isOpenStyle;
    private bool isOpenClass;
    private bool isOpenStyles;
    private bool isOpenClasses;

    private bool isOpenRtl;


    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => isOpenBasic = true"">Open Modal</BitButton>

<BitModal @bind-IsOpen=""isOpenBasic"">
    <div style=""padding:1rem;max-width:40rem"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
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
    </div>
</BitModal>";
    private readonly string example1CsharpCode = @"
private bool isOpenBasic;";

    private readonly string example2RazorCode = @"
<style>
    .modal-header {
        gap: 0.5rem;
        display: flex;
        font-size: 24px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        border-top: 4px solid #0054C6;
    }

    .modal-header-text {
        flex-grow: 1;
    }

    .modal-body {
        max-width: 960px;
        line-height: 20px;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>


<BitButton OnClick=""() => isOpenCustomContent = true"">Open Modal</BitButton>

<BitModal @bind-IsOpen=""isOpenCustomContent"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Story title</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenCustomContent = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
        for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
        <br />
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        <br />
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
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
    </div>
</BitModal>";
    private readonly string example2CsharpCode = @"
private bool isOpenCustomContent;";

    private readonly string example3RazorCode = @"
<style>
    .modal-header {
        gap: 0.5rem;
        display: flex;
        font-size: 24px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        border-top: 4px solid #0054C6;
    }

    .modal-header-text {
        flex-grow: 1;
    }

    .modal-body {
        max-width: 960px;
        line-height: 20px;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>


<BitButton OnClick=""() => isOpenBlocking = true"">Open blocking Modal</BitButton>

<BitModal @bind-IsOpen=""isOpenBlocking"" Blocking>
    <div class=""modal-header"">
        <span class=""modal-header-text"">Blocking modal</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenBlocking = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>";
    private readonly string example3CsharpCode = @"
private bool isOpenBlocking;";

    private readonly string example4RazorCode = @"
<style>
    .modal-header {
        gap: 0.5rem;
        display: flex;
        font-size: 24px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        border-top: 4px solid #0054C6;
    }

    .modal-header-text {
        flex-grow: 1;
    }

    .modal-body {
        max-width: 960px;
        line-height: 20px;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>


<BitButton OnClick=""() => isEventsOpen = true"">Open Modal</BitButton>

<div>Dismissed? [@isDismissed]</div>

<div>Overlay clicked? [@isOverlayClicked]</div>

<BitModal @bind-IsOpen=""isEventsOpen""
          OnDismiss=""HandleOnDismiss""
          OnOverlayClick=""HandleOnOverlayClick"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Events modal</span>
        <BitButton Title=""Close""
                   Variant=""BitVariant.Text""
                   OnClick=""() => isEventsOpen = false""
                   IconName=""@BitIconName.ChromeClose"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>";
    private readonly string example4CsharpCode = @"
private bool isEventsOpen;
private bool isDismissed;
private bool isOverlayClicked;

private async Task HandleOnDismiss()
{
    isDismissed = true;
    await Task.Delay(3000);
    isDismissed = false;
}

private void HandleOnOverlayClick()
{
    isOverlayClicked = true;
    _ = Task.Delay(2000).ContinueWith(_ =>
        {
            isOverlayClicked = false;
            InvokeAsync(StateHasChanged);
        });
}";

    private readonly string example5RazorCode = @"
<style>
    .modal-header {
        gap: 0.5rem;
        display: flex;
        font-size: 24px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        border-top: 4px solid #0054C6;
    }

    .modal-header-text {
        flex-grow: 1;
    }

    .modal-body {
        max-width: 960px;
        line-height: 20px;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }

    .custom-class {
        border: 0.5rem solid tomato;
        background-color: darkgoldenrod;
    }

    .custom-root {
        border: 0.25rem solid #0054C6;
    }

    .custom-overlay {
        background-color: #ffbd5a66;
    }

    .custom-content {
        margin: 1rem;
        box-shadow: 0 0 10rem purple;
        border-end-end-radius: 1rem;
        border-end-start-radius: 1rem;
    }
</style>


<BitButton OnClick=""() => isOpenStyle = true"">Open styled modal</BitButton>
<BitButton OnClick=""() => isOpenClass = true"">Open classed modal</BitButton>
<BitModal @bind-IsOpen=""isOpenStyle"" Style=""box-shadow: inset 0px 0px 1.5rem 1.5rem palevioletred;"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Styled modal</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenStyle = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenClass"" Class=""custom-class"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Classed modal</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenClass = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>


<BitButton OnClick=""() => isOpenStyles = true"">Open modal styles</BitButton>
<BitButton OnClick=""() => isOpenClasses = true"">Open modal classes</BitButton>
<BitModal @bind-IsOpen=""isOpenStyles"" Styles=""@(new() { Overlay = ""background-color: #4776f433;"", Content = ""box-shadow: 0 0 1rem tomato;"" })"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Modal styles</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenStyles = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>
<BitModal @bind-IsOpen=""isOpenClasses"" Classes=""@(new() { Root = ""custom-root"", Overlay = ""custom-overlay"", Content = ""custom-content"" })"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">Modal classes</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenClasses = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitModal>";
    private readonly string example5CsharpCode = @"
private bool isOpenStyle;
private bool isOpenClass;
private bool isOpenStyles;
private bool isOpenClasses;";

    private readonly string example6RazorCode = @"
<style>
    .modal-header {
        gap: 0.5rem;
        display: flex;
        font-size: 24px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        border-top: 4px solid #0054C6;
    }

    .modal-header-text {
        flex-grow: 1;
    }

    .modal-body {
        max-width: 960px;
        line-height: 20px;
        overflow-y: hidden;
        padding: 0 24px 24px;
    }
</style>


<BitButton Dir=""BitDir.Rtl"" OnClick=""() => isOpenRtl = true"">باز کردن مُدال</BitButton>
<BitModal Dir=""BitDir.Rtl"" @bind-IsOpen=""isOpenRtl"">
    <div class=""modal-header"">
        <span class=""modal-header-text"">لورم ایپسوم</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => isOpenRtl = false"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </p>
        <p>
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </p>
        <p>
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </p>
    </div>
</BitModal>";
    private readonly string example6CsharpCode = @"
private bool isOpenRtl;";
}
