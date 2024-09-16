namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonItemDemo
{
    private readonly string example1RazorCode = @"
<BitMenuButton Text=""MenuButton"" Items=""basicItems"" />";
    private readonly string example1CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example2RazorCode = @"
<BitMenuButton Text=""Split"" Items=""basicItems"" Split />";
    private readonly string example2CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example3RazorCode = @"
<BitMenuButton Text=""Fill"" Items=""basicItems"" Variant=""BitVariant.Fill"" />
<BitMenuButton Text=""Outline"" Items=""basicItems"" Variant=""BitVariant.Outline"" />
<BitMenuButton Text=""Text"" Items=""basicItems"" Variant=""BitVariant.Text"" />

<BitMenuButton Text=""Fill"" Items=""basicItems"" Variant=""BitVariant.Fill"" IsEnabled=""false"" />
<BitMenuButton Text=""Outline"" Items=""basicItems"" Variant=""BitVariant.Outline"" IsEnabled=""false"" />
<BitMenuButton Text=""Text"" Items=""basicItems"" Variant=""BitVariant.Text"" IsEnabled=""false"" />

<BitMenuButton Text=""Fill"" Items=""basicItems"" Variant=""BitVariant.Fill"" Split />
<BitMenuButton Text=""Outline"" Items=""basicItems"" Variant=""BitVariant.Outline"" Split />
<BitMenuButton Text=""Text"" Items=""basicItems"" Variant=""BitVariant.Text"" Split />

<BitMenuButton Text=""Fill"" Items=""basicItems"" Variant=""BitVariant.Fill"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Outline"" Items=""basicItems"" Variant=""BitVariant.Outline"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Text"" Items=""basicItems"" Variant=""BitVariant.Text"" IsEnabled=""false"" Split />";
    private readonly string example3CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];;";

    private readonly string example4RazorCode = @"
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" />

<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" Split />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" Split />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" Split />


<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" />

<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" Split />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" Split />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" Split />


<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" />

<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" Split />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" Split />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" Split />


<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" />

<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" Split />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" Split />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" Split />


<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" />

<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" Split />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" Split />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" Split />


<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" />

<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" Split />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" Split />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" Split />


<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" />

<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" Split />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" Split />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" Split />


<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" />

<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" Split />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" Split />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" Split />


<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"" />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"" />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"" />

<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"" Split />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"" Split />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"" Split />


<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"" />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"" />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"" />

<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"" Split />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"" Split />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"" Split />


<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"" />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" />

<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"" Split />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" Split />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" Split />


<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"" />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"" />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"" />

<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"" Split />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"" Split />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"" Split />


<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"" />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"" />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"" />

<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"" Split />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"" Split />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"" Split />


<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"" />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"" />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"" />

<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"" Split />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"" Split />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"" Split />


<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"" />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"" />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"" />

<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"" Split />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"" Split />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"" Split />


<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"" />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"" />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"" />

<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"" Split />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"" Split />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"" Split />


<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"" />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"" />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"" />

<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"" Split />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"" Split />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"" Split />";
    private readonly string example4CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example5RazorCode = @"
<BitMenuButton Text=""Small"" Items=""basicItems"" Variant=""BitVariant.Fill"" Size=""BitSize.Small"" />
<BitMenuButton Text=""Small"" Items=""basicItems"" Variant=""BitVariant.Outline"" Size=""BitSize.Small"" />
<BitMenuButton Text=""Small"" Items=""basicItems"" Variant=""BitVariant.Text"" Size=""BitSize.Small"" />

<BitMenuButton Text=""Medium"" Items=""basicItems"" Variant=""BitVariant.Fill"" Size=""BitSize.Medium"" />
<BitMenuButton Text=""Medium"" Items=""basicItems"" Variant=""BitVariant.Outline"" Size=""BitSize.Medium"" />
<BitMenuButton Text=""Medium"" Items=""basicItems"" Variant=""BitVariant.Text"" Size=""BitSize.Medium"" />

<BitMenuButton Text=""Large"" Items=""basicItems"" Variant=""BitVariant.Fill"" Size=""BitSize.Large"" />
<BitMenuButton Text=""Large"" Items=""basicItems"" Variant=""BitVariant.Outline"" Size=""BitSize.Large"" />
<BitMenuButton Text=""Large"" Items=""basicItems"" Variant=""BitVariant.Text"" Size=""BitSize.Large"" />";
    private readonly string example5CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example6RazorCode = @"
<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Fill"" Sticky />
<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Fill"" Split Sticky />

<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Outline"" Sticky />
<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Outline"" Split Sticky />

<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Text"" Sticky />
<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Text"" Split Sticky />";
    private readonly string example6CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example7RazorCode = @"
<BitMenuButton Text=""IconName"" Items=""basicItemsIcon"" IconName=""@BitIconName.Edit"" />
<BitMenuButton Text=""ChevronDownIcon"" Items=""basicItemsIcon"" ChevronDownIcon=""@BitIconName.DoubleChevronDown"" Split />";
    private readonly string example7CsharpCode = @"
private List<BitMenuButtonItem> basicItemsIcon =
[
    new() { Text = ""Item A"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""Item B"", Key = ""B"", IconName = BitIconName.Emoji, IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"", IconName = BitIconName.Emoji2 }
];";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        margin-inline: 1rem;
        border-radius: 1rem;
        border-color: tomato;
        border-width: 0.25rem;
    }

    .custom-class > button {
        color: tomato;
        border-color: tomato;
        background: transparent;
    }

    .custom-class > button:hover {
        background-color: #ff63473b;
    }


    .custom-item {
        color: peachpuff;
        background-color: tomato;
    }


    .custom-button {
        color: deepskyblue;
        background: transparent;
    }

    .custom-opened .custom-button {
        color: cornflowerblue;
    }

    .custom-callout {
        border-radius: 1rem;
        border-color: lightgray;
        backdrop-filter: blur(20px);
        background-color: transparent;
        box-shadow: darkgray 0 0 0.5rem;
    }

    .custom-item-button {
        border-bottom: 1px solid gray;
    }

    .custom-item-button:hover {
        background-color: rgba(255, 255, 255, 0.2);
    }

    .custom-callout li:last-child .custom-item-button {
        border-bottom: none;
    }
</style>


<BitMenuButton Text=""Styled Button"" Items=""basicItems"" Style=""border-radius: 1rem; margin: 1rem; box-shadow: aqua 0 0 1rem; overflow: hidden;"" />
<BitMenuButton Text=""Classed Button"" Items=""basicItems"" Class=""custom-class"" Variant=""BitVariant.Outline"" />


<BitMenuButton Text=""Item Styled & Classed Button"" Items=""itemStyleClassItems"" Variant=""BitVariant.Text"" />


<BitMenuButton Text=""Classes"" Items=""basicItems"" IconName=""@BitIconName.FormatPainter"" Variant=""BitVariant.Text""
               Classes=""@(new() { OperatorButton = ""custom-button"",
                                  Opened = ""custom-opened"",
                                  Callout = ""custom-callout"",
                                  ItemButton = ""custom-item-button"" })"" />

<BitMenuButton Text=""Styles"" Items=""basicItems"" IconName=""@BitIconName.Brush""
               Styles=""@(new() { { Root = ""--button-background: tomato; background: var(--button-background); border-color: var(--button-background); border-radius: 0.25rem;"",
                                   Opened = ""--button-background: orangered;"",
                                   OperatorButton = ""background: var(--button-background);"",
                                   ItemButton = ""background: lightcoral;"",
                                   Callout = ""border-radius: 0.25rem; box-shadow: lightgray 0 0 0.5rem;"" })"" />";
    private readonly string example8CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
};

private static List<BitMenuButtonItem> itemStyleClassItems =
[
    new() { Text = ""Item A (Default)"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""Item C (Styled)"", Key = ""B"", IconName = BitIconName.Emoji, Style = ""color: tomato; border-color: brown; background-color: peachpuff;"" },
    new() { Text = ""Item B (Classed)"", Key = ""C"", IconName = BitIconName.Emoji2, Class = ""custom-item"" }
];";

    private readonly string example9RazorCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>


<BitMenuButton Items=""basicItems"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Text=""Items"" Items=""itemTemplateItems"" Split>
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color:brown"">@item.Text (@item.Key)</span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<BitMenuButton Text=""Items"" Items=""itemTemplateItems2"" />";
    private readonly string example9CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];

private List<BitMenuButtonItem> itemTemplateItems =
[
    new() { Text = ""Add"", Key = ""add-key"", IconName = BitIconName.Add },
    new() { Text = ""Edit"", Key = ""edit-key"", IconName = BitIconName.Edit },
    new() { Text = ""Delete"", Key = ""delete-key"", IconName = BitIconName.Delete }
];

private List<BitMenuButtonItem> itemTemplateItems2 = 
[
    new()
    {
        Text = ""Add"", Key = ""add-key"", IconName = BitIconName.Add,
        Template = (item => @<div class=""item-template-box"" style=""color:green"">@item.Text (@item.Key)</div>)
    },
    new ()
    {
        Text = ""Edit"", Key = ""edit-key"", IconName = BitIconName.Edit,
        Template = (item => @<div class=""item-template-box"" style=""color:yellow"">@item.Text (@item.Key)</div>)
    },
    new()
    {
        Text = ""Delete"", Key = ""delete-key"", IconName = BitIconName.Delete,
        Template = (item => @<div class=""item-template-box"" style=""color:red"">@item.Text (@item.Key)</div>)
    }
];";

    private readonly string example10RazorCode = @"
<BitMenuButton Text=""Items"" Items=""basicItems""
               OnChange=""(BitMenuButtonItem item) => eventsChangedItem = item?.Key""
               OnClick=""(BitMenuButtonItem item) => eventsClickedItem = item?.Key"" />

<BitMenuButton Split Text=""Items"" Items=""basicItemsOnClick""
               OnChange=""(BitMenuButtonItem item) => eventsChangedItem = item?.Key""
               OnClick=""@((BitMenuButtonItem item) => eventsClickedItem = ""Main button clicked"")"" />


<BitMenuButton Sticky Items=""basicItems""
               OnChange=""(BitMenuButtonItem item) => eventsChangedItem = item?.Key""
               OnClick=""(BitMenuButtonItem item) => eventsClickedItem = item?.Key"" />

<BitMenuButton Sticky Split Items=""basicItemsOnClick""
               OnChange=""(BitMenuButtonItem item) => eventsChangedItem = item?.Key""
               OnClick=""(BitMenuButtonItem item) => eventsClickedItem = item?.Key"" />


<div>Clicked item: @eventsClickedItem</div>
<div>Changed item: @eventsChangedItem</div>";
    private readonly string example10CsharpCode = @"
private string? eventsClickedItem;
private string? eventsChangedItem;

private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];

private List<BitMenuButtonItem> basicItemsOnClick =
[
    new() { Text = ""Item A"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""Item B"", Key = ""B"", IconName = BitIconName.Emoji, IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"", IconName = BitIconName.Emoji2 }
];

protected override void OnInitialized()
{
    Action<BitMenuButtonItem> onClick = item =>
    {
        eventsClickedItem = $""{item.Text}"";
        StateHasChanged();
    };

    basicItemsOnClick.ForEach(i => i.OnClick = onClick);
}";

    private readonly string example11RazorCode = @"
<BitMenuButton Split Sticky Items=""basicItems"" DefaultSelectedItem=""basicItems[1]"" />

<BitMenuButton Sticky Items=""basicItems"" @bind-SelectedItem=""twoWaySelectedItem"" />
<BitChoiceGroup Horizontal Items=""@choiceGroupItems"" @bind-Value=""@twoWaySelectedItem"" />

<BitMenuButton Sticky Items=""isSelectedItems"" />

<BitMenuButton Sticky Items=""basicItems"" IsOpen=""oneWayIsOpen"" />
<BitCheckbox Label=""One-way IsOpen"" @bind-Value=""oneWayIsOpen"" OnChange=""async _ => { await Task.Delay(2000); oneWayIsOpen = false; }"" />

<BitMenuButton Sticky Items=""basicItems"" @bind-IsOpen=""twoWayIsOpen"" />
<BitCheckbox Label=""Two-way IsOpen"" @bind-Value=""twoWayIsOpen"" />";
    private readonly string example11CsharpCode = @"
private BitMenuButtonItem twoWaySelectedItem = default!;
private bool oneWayIsOpen;
private bool twoWayIsOpen;

private static List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];

private static IEnumerable<BitChoiceGroupItem<BitMenuButtonItem>> choiceGroupItems =
    basicItems.Select(i => new BitChoiceGroupItem<BitMenuButtonItem>() { Id = i.Key, Text = i.Text, IsEnabled = i.IsEnabled, Value = i });

private List<BitMenuButtonItem> isSelectedItems =
[
    new() { Text = ""Item A"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""Item B"", Key = ""B"", IconName = BitIconName.Emoji },
    new() { Text = ""Item C"", Key = ""C"", IconName = BitIconName.Emoji2, IsSelected = true }
];

protected override void OnInitialized()
{
    twoWaySelectedItem = basicItems[2];
}";

    private readonly string example12RazorCode = @"
<BitMenuButton Text=""گزینه ها"" Dir=""BitDir.Rtl"" Items=""rtlItemsIcon"" IconName=""@BitIconName.Edit"" />
<BitMenuButton Text=""گزینه ها"" Dir=""BitDir.Rtl"" Items=""rtlItemsIcon"" ChevronDownIcon=""@BitIconName.DoubleChevronDown"" Split />";
    private readonly string example12CsharpCode = @"
 private static List<BitMenuButtonItem> rtlItemsIcon =
[
    new() { Text = ""گزینه الف"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""گزینه ب"", Key = ""B"", IconName = BitIconName.Emoji },
    new() { Text = ""گزینه ج"", Key = ""C"", IconName = BitIconName.Emoji2 }
];";
}
