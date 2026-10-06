namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Accordion;

public partial class BitAccordionDemo
{
    private readonly string example1RazorCode = @"
<BitAccordion Title=""Accordion"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""General settings"" Description=""The general settings of the application"" IconName=""@BitIconName.Settings"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""Expanded by default"" IconName=""@BitIconName.People"" DefaultIsExpanded>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";

    private readonly string example2RazorCode = @"
<BitAccordion Title=""ExpanderIconName"" ExpanderIconName=""@BitIconName.ChevronDown"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""ExpanderIconPlacement Start"" IconName=""@BitIconName.Settings"" ExpanderIconPlacement=""BitPlacement.Start"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""HideExpanderIcon"" HideExpanderIcon>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""NoExpanderRotation"" ExpanderIconName=""@BitIconName.ChevronDown"" NoExpanderRotation>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""ExpandedExpanderIconName"" ExpanderIconName=""@BitIconName.Add"" ExpandedExpanderIconName=""@BitIconName.Remove"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";

    private readonly string example3RazorCode = @"
<BitAccordion Title=""Project settings"" Description=""@($""Renamed {renameCount} times"")"">
    <Actions>
        <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Rename"" Title=""Rename"" OnClick=""() => renameCount++"" />
        <BitButton Variant=""BitVariant.Text"" Color=""BitColor.Error"" IconName=""@BitIconName.Delete"" Title=""Delete"" />
    </Actions>
    <Body>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </Body>
</BitAccordion>";
    private readonly string example3CsharpCode = @"
private int renameCount;";

    private readonly string example4RazorCode = @"
<BitToggle @bind-Value=""bindingIsEnabled"" OnText=""Enabled"" OffText=""Disabled"" />
<BitToggle @bind-Value=""bindingIsExpanded"" OnText=""Expanded"" OffText=""Collapsed"" />
<BitAccordion Title=""Bound"" IsEnabled=""bindingIsEnabled"" @bind-IsExpanded=""bindingIsExpanded"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>


<BitAccordion Title=""General settings""
              IsExpanded=""controlledExpandedItem == 1""
              OnClick=""() => controlledExpandedItem = controlledExpandedItem == 1 ? 0 : 1"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""Users""
              IsExpanded=""controlledExpandedItem == 2""
              OnClick=""() => controlledExpandedItem = controlledExpandedItem == 2 ? 0 : 2"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""Advanced settings""
              IsExpanded=""controlledExpandedItem == 3""
              OnClick=""() => controlledExpandedItem = controlledExpandedItem == 3 ? 0 : 3"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";
    private readonly string example4CsharpCode = @"
private bool bindingIsEnabled = true;
private bool bindingIsExpanded;
private int controlledExpandedItem = 1;";

    private readonly string example5RazorCode = @"
<BitToggle @bind-Value=""lockAccordion"" OnText=""Locked open"" OffText=""Unlocked"" />
<BitAccordion Title=""Unsaved changes""
              Description=""@(lockAccordion ? ""Unlock to close this panel"" : ""Free to close"")""
              DefaultIsExpanded
              OnClick=""() => clickCount++""
              OnChange=""(bool v) => lastChange = v""
              OnExpand=""() => expandCount++""
              OnCollapse=""() => collapseCount++""
              OnToggling=""HandleOnToggling"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>

<div>Clicks: <b>@clickCount</b>, last OnChange: <b>@lastChange</b></div>
<div>Expanded: <b>@expandCount</b>, collapsed: <b>@collapseCount</b>, refused: <b>@refusedCount</b></div>";
    private readonly string example5CsharpCode = @"
private int clickCount;
private bool lastChange;
private int expandCount;
private int collapseCount;
private int refusedCount;
private bool lockAccordion;
private void HandleOnToggling(BitAccordionToggleArgs args)
{
    if (args.IsExpanding || lockAccordion is false) return;

    args.Cancel = true;
    refusedCount++;
}";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""() => accordionRef.Expand()"">Expand</BitButton>
<BitButton OnClick=""() => accordionRef.Collapse()"">Collapse</BitButton>
<BitButton OnClick=""() => accordionRef.Toggle()"">Toggle</BitButton>
<BitButton OnClick=""async () => { await accordionRef.Expand(); await accordionRef.FocusAsync(); }"">Expand &amp; focus</BitButton>

<BitAccordion @ref=""accordionRef"" Title=""Accordion"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";
    private readonly string example6CsharpCode = @"
private BitAccordion accordionRef = default!;";

    private readonly string example7RazorCode = @"
<BitAccordion Title=""LazyContent"" LazyContent>
    <BitTextField Placeholder=""Kept after a collapse..."" />
</BitAccordion>
<BitAccordion Title=""UnmountOnCollapse"" UnmountOnCollapse>
    <BitTextField Placeholder=""Thrown away on a collapse..."" />
</BitAccordion>
<BitAccordion Title=""Loaded on expand"" LazyContent OnToggling=""LoadOrders"">
    @foreach (var order in orders)
    {
        <div>@order</div>
    }
</BitAccordion>";
    private readonly string example7CsharpCode = @"
private string[] orders = [];
private async Task LoadOrders(BitAccordionToggleArgs args)
{
    if (args.IsExpanding is false || orders.Length > 0) return;

    await Task.Delay(1500); // e.g. await Http.GetFromJsonAsync<string[]>(""api/orders"")
    orders = [""#1001 - 2 items"", ""#1002 - 5 items"", ""#1003 - 1 item""];
}";

    private readonly string example8RazorCode = @"
<BitAccordion Title=""MaxHeight 6rem"" MaxHeight=""6rem"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take shape.
    Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
    inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
    spark emotion, and resonate with those who encounter them. This space is yours to craft, yours to shape.
</BitAccordion>
<BitAccordion Title=""Slow (1000ms)"" TransitionDuration=""1000"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""Instant (0)"" TransitionDuration=""0"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";

    private readonly string example9RazorCode = @"
<BitAccordion Title=""Which fruits ship overnight?"" HiddenUntilFound ExpandOnPrint>
    Citrus like the tangerine ships overnight in insulated boxes; berries ship on the next business day.
</BitAccordion>
<BitAccordion Title=""Can I change my order?"" HiddenUntilFound ExpandOnPrint>
    Orders can be changed until they are packed, usually within two hours of checkout.
</BitAccordion>
<BitAccordion Title=""Printed as a bare header"">
    Returns are free within thirty days of delivery.
</BitAccordion>";

    private readonly string example10RazorCode = @"
<BitAccordion IconName=""@BitIconName.Settings"" Description=""TitleTemplate"">
    <TitleTemplate>
        <BitStack Horizontal FitWidth AutoHeight Gap=""0.5rem"" VerticalAlign=""BitAlignment.Center"">
            <span>Advanced settings</span>
            <BitTag Text=""New"" Color=""BitColor.Info"" Size=""BitSize.Small"" />
        </BitStack>
    </TitleTemplate>
    <Body>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </Body>
</BitAccordion>
<BitAccordion Title=""ExpanderTemplate"" NoExpanderRotation>
    <ExpanderTemplate Context=""isExpanded"">
        <BitText Typography=""BitTypography.Caption1"" Color=""BitColor.Primary"">@(isExpanded ? ""Less"" : ""More"")</BitText>
    </ExpanderTemplate>
    <Body>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </Body>
</BitAccordion>
<BitAccordion>
    <HeaderTemplate Context=""isExpanded"">
        <BitIcon IconName=""@(isExpanded ? BitIconName.ChevronDown : BitIconName.ChevronRight)"" />
        <div class=""custom-header"">
            <span class=""custom-title"">HeaderTemplate</span>
            <span class=""custom-desc"">@(isExpanded ? ""Open"" : ""Closed"")</span>
        </div>
    </HeaderTemplate>
    <Body>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </Body>
</BitAccordion>";
    private const string example10ScssCode = @"
.custom-header {
    gap: 1rem;
    flex-grow: 1;
    display: flex;
    line-height: 1.5;
    align-items: center;
}

.custom-title {
    font-weight: 600;
    color: var(--bit-clr-pri);
}

.custom-desc {
    color: var(--bit-clr-fg-sec);
}";
    private readonly DemoCodeFile[] example10CodeFiles =
    [
        new("BitAccordionDemo.razor.scss", example10ScssCode),
    ];

    private readonly string example11RazorCode = @"
<BitAccordion Title=""Read-only""
              Description=""@($""Clicked {readOnlyClickCount} times, still open"")""
              OnClick=""() => readOnlyClickCount++""
              ReadOnly
              DefaultIsExpanded>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""Disabled"" IsEnabled=""false"" DefaultIsExpanded>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";
    private readonly string example11CsharpCode = @"
private int readOnlyClickCount;";

    private readonly string example12RazorCode = @"
<BitAccordion Title=""Top-level section"" Description=""aria-level 2"" HeadingLevel=""2"">
    <BitLink Href=""/components/accordion"">A link Tab only reaches while this panel is open.</BitLink>
</BitAccordion>
<BitAccordion HeaderAriaLabel=""Notifications"">
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Ringer"" />
    </HeaderTemplate>
    <Body>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </Body>
</BitAccordion>
<BitAccordion Title=""Nested (aria-level 3)"" NoContentRegion DefaultIsExpanded>
    <BitAccordion Title=""Nested (aria-level 4)"" NoContentRegion>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
</BitAccordion>";

    private readonly string example13RazorCode = @"
<BitParams Parameters=""@accordionParams"">
    <BitAccordion Title=""Takes the cascade"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
    <BitAccordion Title=""Also takes the cascade"" Description=""Searchable while closed"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
    <BitAccordion Title=""Sets its own ExpanderIconPlacement"" ExpanderIconPlacement=""BitPlacement.End"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
</BitParams>";
    private readonly string example13CsharpCode = @"
private readonly BitAccordionParams[] accordionParams =
[
    new()
    {
        HiddenUntilFound = true,
        ExpanderIconName = BitIconName.Add,
        ExpandedExpanderIconName = BitIconName.Remove,
        ExpanderIconPlacement = BitPlacement.Start,
    }
];";

    private readonly string example14RazorCode = @"
<BitChoiceGroup @bind-Value=""backgroundColorKind"" Horizontal TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>
<div style=""padding: 2rem; background: gray;"">
    <BitAccordion Title=""Background"" Background=""backgroundColorKind"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
</div>

<BitChoiceGroup @bind-Value=""borderColorKind"" Horizontal TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>
<BitAccordion Title=""Border"" Border=""borderColorKind"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>

<BitAccordion Title=""NoBorder"" NoBorder>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";
    private readonly string example14CsharpCode = @"
private BitColorKind backgroundColorKind = BitColorKind.Primary;
private BitColorKind borderColorKind = BitColorKind.Primary;";

    private readonly string example15RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitAccordion Title=""Bare class string"" ExpanderIcon=""@(""fa-solid fa-chevron-down"")"">
    ExpanderIcon=@(""fa-solid fa-chevron-down"")
</BitAccordion>
<BitAccordion Title=""Gear"" Icon=""@BitIconInfo.Fa(""solid gear"")"" ExpanderIcon=""@BitIconInfo.Fa(""solid plus"")"" ExpandedExpanderIcon=""@BitIconInfo.Fa(""solid minus"")"">
    Icon=""@BitIconInfo.Fa(""solid gear"")""
</BitAccordion>

<BitAccordion Title=""BitIconInfo.Css"" ExpanderIcon=""@BitIconInfo.Css(""bi bi-chevron-down"")"">
    ExpanderIcon=""@BitIconInfo.Css(""bi bi-chevron-down"")""
</BitAccordion>
<BitAccordion Title=""Gear"" Icon=""@BitIconInfo.Bi(""gear"")"" ExpanderIcon=""@BitIconInfo.Bi(""caret-down-fill"")"">
    Icon=""@BitIconInfo.Bi(""gear"")""
</BitAccordion>";

    private readonly string example16RazorCode = @"
<BitAccordion Title=""Small"" Description=""Description"" Size=""BitSize.Small"" IconName=""@BitIconName.Settings"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""Medium"" Description=""Description"" Size=""BitSize.Medium"" IconName=""@BitIconName.Settings"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""Large"" Description=""Description"" Size=""BitSize.Large"" IconName=""@BitIconName.Settings"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";

    private readonly string example17RazorCode = @"
<BitAccordion Title=""On the instance""
              Style=""--bit-Accordion-radius: 0; --bit-Accordion-border-width: 2px; --bit-Accordion-border-color: seagreen; --bit-Accordion-header-expanded-background: seagreen; --bit-Accordion-header-expanded-color: white; --bit-Accordion-content-background: color-mix(in srgb, seagreen 10%, transparent);"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<div class=""faq-accordions"">
    <BitAccordion Title=""On an ancestor"" Description=""Every accordion below it"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
    <BitAccordion Title=""Takes the same look"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
</div>


<div>
    <BitAccordion Title=""Style"" Style=""box-shadow: var(--bit-shd-card);"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
    <BitAccordion Title=""Class"" Class=""custom-class"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>

    <BitAccordion Title=""Styles""
                  Description=""I am an accordion""
                  Styles=""@(new() { Title = ""color: tomato;"",
                                    ExpanderIcon = ""color: tomato;"",
                                    ExpandedIcon = ""color: seagreen;"",
                                    Expanded = ""border-color: seagreen;"",
                                    Content = ""font-style: italic;"" })"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
    <BitAccordion Title=""Classes""
                  Description=""I am an accordion""
                  Classes=""@(new() { Title = ""custom-acd-title"", Content = ""custom-acd-content"" })"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
</div>";
    private const string example17ScssCode = @"
.faq-accordions {
    gap: 0.5rem;
    display: flex;
    flex-flow: column;
    // The variables inherit, so one block on an ancestor re-skins every accordion underneath it.
    --bit-Accordion-radius: 0.75rem;
    --bit-Accordion-header-padding: 1rem 1.25rem;
    --bit-Accordion-content-padding: 1rem 1.25rem;
    --bit-Accordion-border-color: color-mix(in srgb, var(--bit-clr-pri) 35%, transparent);
    --bit-Accordion-header-hover-background: color-mix(in srgb, var(--bit-clr-pri) 8%, transparent);
    --bit-Accordion-header-active-background: color-mix(in srgb, var(--bit-clr-pri) 16%, transparent);
    --bit-Accordion-header-expanded-background: color-mix(in srgb, var(--bit-clr-pri) 12%, transparent);
    --bit-Accordion-divider-color: color-mix(in srgb, var(--bit-clr-pri) 35%, transparent);
    --bit-Accordion-title-color: var(--bit-clr-pri);
    --bit-Accordion-expander-color: var(--bit-clr-pri);
}

::deep {
    .custom-class {
        border-color: blueviolet;
        background-color: color-mix(in srgb, blueviolet 12%, transparent);
    }

    .custom-acd-title {
        color: tomato;
        font-style: italic;
    }

    .custom-acd-content {
        font-family: monospace;
    }
}";
    private readonly DemoCodeFile[] example17CodeFiles =
    [
        new("BitAccordionDemo.razor.scss", example17ScssCode),
    ];

    private readonly string example18RazorCode = @"
<BitAccordion Dir=""BitDir.Rtl""
              Title=""تنظیمات""
              IconName=""@BitIconName.Settings""
              Description=""من یک آکاردئون هستم!"">
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
</BitAccordion>";
}
