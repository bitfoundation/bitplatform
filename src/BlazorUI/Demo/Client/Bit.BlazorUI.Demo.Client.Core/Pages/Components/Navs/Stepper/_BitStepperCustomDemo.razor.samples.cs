namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Stepper;

public partial class _BitStepperCustomDemo
{
    private readonly string example1RazorCode = @"
<style>
    .step-content {
        padding: 1rem;
        border-radius: 0.25rem;
        border: 1px dashed var(--bit-clr-brd-pri);
    }
</style>

<BitStepper Items=""basicItems"" NameSelectors=""nameSelectors"" @bind-CurrentIndex=""basicIndex"" />

<div class=""step-content"">@(basicIndex < basicItems.Count ? $""Content of step {basicIndex + 1}"" : ""All steps are done"")</div>

<BitButton Variant=""BitVariant.Outline"" Disabled=""basicIndex == 0"" OnClick=""() => basicIndex--"">Back</BitButton>
<BitButton Disabled=""basicIndex >= basicItems.Count"" OnClick=""() => basicIndex++"">@(basicIndex < basicItems.Count - 1 ? ""Next"" : ""Finish"")</BitButton>";
    private readonly string example1CsharpCode = @"
public class Milestone
{
    public string? Name { get; set; }
    public string? Note { get; set; }
    public string? Symbol { get; set; }
    public BitIconInfo? Glyph { get; set; }
    public bool HasError { get; set; }
    public bool Blocked { get; set; }
    public string? Css { get; set; }
    public string? Inline { get; set; }
}

private int basicIndex;

private static readonly BitStepperNameSelectors<Milestone> nameSelectors = new()
{
    Text = { Name = nameof(Milestone.Name) },
    Description = { Name = nameof(Milestone.Note) },
    IconName = { Name = nameof(Milestone.Symbol) },
    Icon = { Selector = m => m.Glyph },
    IsError = { Name = nameof(Milestone.HasError) },
    IsDisabled = { Name = nameof(Milestone.Blocked) },
    Class = { Name = nameof(Milestone.Css) },
    Style = { Name = nameof(Milestone.Inline) },
};

private readonly List<Milestone> basicItems =
[
    new() { Name = ""Account"" },
    new() { Name = ""Address"" },
    new() { Name = ""Payment"" },
    new() { Name = ""Review"" }
];";

    private readonly string example2RazorCode = @"
<BitStepper Vertical Items=""basicItems"" NameSelectors=""nameSelectors"" CurrentIndex=""1"" />";
    private readonly string example2CsharpCode = @"
private static readonly BitStepperNameSelectors<Milestone> nameSelectors = new()
{
    Text = { Name = nameof(Milestone.Name) },
    Description = { Name = nameof(Milestone.Note) },
    IconName = { Name = nameof(Milestone.Symbol) },
    Icon = { Selector = m => m.Glyph },
    IsError = { Name = nameof(Milestone.HasError) },
    IsDisabled = { Name = nameof(Milestone.Blocked) },
    Class = { Name = nameof(Milestone.Css) },
    Style = { Name = nameof(Milestone.Inline) },
};

private readonly List<Milestone> basicItems =
[
    new() { Name = ""Account"" },
    new() { Name = ""Address"" },
    new() { Name = ""Payment"" },
    new() { Name = ""Review"" }
];";

    private readonly string example3RazorCode = @"
<BitStepper Items=""iconItems"" NameSelectors=""nameSelectors"" CurrentIndex=""1"" />";
    private readonly string example3CsharpCode = @"
private static readonly BitStepperNameSelectors<Milestone> nameSelectors = new()
{
    Text = { Name = nameof(Milestone.Name) },
    Description = { Name = nameof(Milestone.Note) },
    IconName = { Name = nameof(Milestone.Symbol) },
    Icon = { Selector = m => m.Glyph },
    IsError = { Name = nameof(Milestone.HasError) },
    IsDisabled = { Name = nameof(Milestone.Blocked) },
    Class = { Name = nameof(Milestone.Css) },
    Style = { Name = nameof(Milestone.Inline) },
};

private readonly List<Milestone> iconItems =
[
    new() { Name = ""Account"", Note = ""Your details"", Symbol = BitIconName.Contact },
    new() { Name = ""Address"", Note = ""Where to ship"", Symbol = BitIconName.Home },
    new() { Name = ""Payment"", Note = ""Card or transfer"", Symbol = BitIconName.PaymentCard },
    new() { Name = ""Review"", Note = ""Check and confirm"", Symbol = BitIconName.CheckList }
];";

    private readonly string example4RazorCode = @"
<BitStepper Linear Items=""basicItems"" NameSelectors=""nameSelectors"" @bind-CurrentIndex=""linearIndex"" />

<BitButton Variant=""BitVariant.Outline"" Disabled=""linearIndex == 0"" OnClick=""() => linearIndex--"">Back</BitButton>
<BitButton Disabled=""linearIndex >= basicItems.Count"" OnClick=""() => linearIndex++"">Next</BitButton>";
    private readonly string example4CsharpCode = @"
private int linearIndex;

private static readonly BitStepperNameSelectors<Milestone> nameSelectors = new()
{
    Text = { Name = nameof(Milestone.Name) },
    Description = { Name = nameof(Milestone.Note) },
    IconName = { Name = nameof(Milestone.Symbol) },
    Icon = { Selector = m => m.Glyph },
    IsError = { Name = nameof(Milestone.HasError) },
    IsDisabled = { Name = nameof(Milestone.Blocked) },
    Class = { Name = nameof(Milestone.Css) },
    Style = { Name = nameof(Milestone.Inline) },
};

private readonly List<Milestone> basicItems =
[
    new() { Name = ""Account"" },
    new() { Name = ""Address"" },
    new() { Name = ""Payment"" },
    new() { Name = ""Review"" }
];";

    private readonly string example5RazorCode = @"
<BitStepper ReadOnly Items=""basicItems"" NameSelectors=""nameSelectors"" CurrentIndex=""2"" />";
    private readonly string example5CsharpCode = @"
private static readonly BitStepperNameSelectors<Milestone> nameSelectors = new()
{
    Text = { Name = nameof(Milestone.Name) },
    Description = { Name = nameof(Milestone.Note) },
    IconName = { Name = nameof(Milestone.Symbol) },
    Icon = { Selector = m => m.Glyph },
    IsError = { Name = nameof(Milestone.HasError) },
    IsDisabled = { Name = nameof(Milestone.Blocked) },
    Class = { Name = nameof(Milestone.Css) },
    Style = { Name = nameof(Milestone.Inline) },
};

private readonly List<Milestone> basicItems =
[
    new() { Name = ""Account"" },
    new() { Name = ""Address"" },
    new() { Name = ""Payment"" },
    new() { Name = ""Review"" }
];";

    private readonly string example6RazorCode = @"
<BitStepper Items=""errorItems"" NameSelectors=""nameSelectors"" CurrentIndex=""2"" />

<BitStepper Items=""basicItems"" NameSelectors=""nameSelectors"" CurrentIndex=""1"" Disabled />";
    private readonly string example6CsharpCode = @"
private static readonly BitStepperNameSelectors<Milestone> nameSelectors = new()
{
    Text = { Name = nameof(Milestone.Name) },
    Description = { Name = nameof(Milestone.Note) },
    IconName = { Name = nameof(Milestone.Symbol) },
    Icon = { Selector = m => m.Glyph },
    IsError = { Name = nameof(Milestone.HasError) },
    IsDisabled = { Name = nameof(Milestone.Blocked) },
    Class = { Name = nameof(Milestone.Css) },
    Style = { Name = nameof(Milestone.Inline) },
};

private readonly List<Milestone> errorItems =
[
    new() { Name = ""Account"" },
    new() { Name = ""Address"", Note = ""The postal code is not valid"", HasError = true },
    new() { Name = ""Payment"" },
    new() { Name = ""Gift wrap"", Note = ""Not available for this order"", Blocked = true },
    new() { Name = ""Review"" }
];

private readonly List<Milestone> basicItems =
[
    new() { Name = ""Account"" },
    new() { Name = ""Address"" },
    new() { Name = ""Payment"" },
    new() { Name = ""Review"" }
];";

    private readonly string example7RazorCode = @"
<BitStepper Items=""basicItems""
            NameSelectors=""nameSelectors""
            CurrentIndex=""eventsIndex""
            OnChange=""(int index) => { if (Math.Abs(index - eventsIndex) == 1) eventsIndex = index; eventsChange = index; }""
            OnItemClick=""(Milestone item) => eventsClicked = item.Name"" />

<div>Clicked step: <b>@eventsClicked</b></div>
<div>Asked for step: <b>@(eventsChange + 1)</b>, current step: <b>@(eventsIndex + 1)</b></div>";
    private readonly string example7CsharpCode = @"
private int eventsIndex;
private int eventsChange;
private string? eventsClicked;

private static readonly BitStepperNameSelectors<Milestone> nameSelectors = new()
{
    Text = { Name = nameof(Milestone.Name) },
    Description = { Name = nameof(Milestone.Note) },
    IconName = { Name = nameof(Milestone.Symbol) },
    Icon = { Selector = m => m.Glyph },
    IsError = { Name = nameof(Milestone.HasError) },
    IsDisabled = { Name = nameof(Milestone.Blocked) },
    Class = { Name = nameof(Milestone.Css) },
    Style = { Name = nameof(Milestone.Inline) },
};

private readonly List<Milestone> basicItems =
[
    new() { Name = ""Account"" },
    new() { Name = ""Address"" },
    new() { Name = ""Payment"" },
    new() { Name = ""Review"" }
];";

    private readonly string example8RazorCode = @"
<!-- FontAwesome -->
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitStepper Items=""externalIconItems1""
            NameSelectors=""nameSelectors""
            CurrentIndex=""2""
            CompletedIcon=""@(""fa-solid fa-circle-check"")""
            ErrorIcon=""@BitIconInfo.Fa(""solid triangle-exclamation"")"" />

<!-- Bootstrap Icons -->
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitStepper Items=""externalIconItems2""
            NameSelectors=""nameSelectors""
            CurrentIndex=""1""
            CompletedIcon=""@BitIconInfo.Bi(""check-lg"")"" />";
    private readonly string example8CsharpCode = @"
private static readonly BitStepperNameSelectors<Milestone> nameSelectors = new()
{
    Text = { Name = nameof(Milestone.Name) },
    Description = { Name = nameof(Milestone.Note) },
    IconName = { Name = nameof(Milestone.Symbol) },
    Icon = { Selector = m => m.Glyph },
    IsError = { Name = nameof(Milestone.HasError) },
    IsDisabled = { Name = nameof(Milestone.Blocked) },
    Class = { Name = nameof(Milestone.Css) },
    Style = { Name = nameof(Milestone.Inline) },
};

private readonly List<Milestone> externalIconItems1 =
[
    new() { Name = ""Account"", Glyph = ""fa-solid fa-user"" },
    new() { Name = ""Address"", Glyph = BitIconInfo.Fa(""solid house""), HasError = true },
    new() { Name = ""Payment"", Glyph = BitIconInfo.Fa(""solid credit-card"") },
    new() { Name = ""Review"", Glyph = BitIconInfo.Fa(""solid list-check"") }
];

private readonly List<Milestone> externalIconItems2 =
[
    new() { Name = ""Account"", Glyph = BitIconInfo.Bi(""person"") },
    new() { Name = ""Address"", Glyph = BitIconInfo.Bi(""house"") },
    new() { Name = ""Payment"", Glyph = BitIconInfo.Bi(""credit-card"") },
    new() { Name = ""Review"", Glyph = BitIconInfo.Bi(""card-checklist"") }
];";

    private readonly string example9RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border-radius: 0.5rem;
        background-color: var(--bit-clr-bg-sec);
    }

    .custom-item .bit-stp-txt {
        color: dodgerblue;
    }

    .custom-current .bit-stp-ind {
        box-shadow: 0 0 0 0.25rem var(--bit-clr-pri-light);
    }

    .custom-connector {
        background-image: repeating-linear-gradient(90deg, var(--bit-clr-bg-pri) 0 0.25rem, transparent 0.25rem 0.5rem);
    }
</style>

<BitStepper Items=""basicItems"" NameSelectors=""nameSelectors"" CurrentIndex=""1"" Style=""--bit-Stepper-active-color: tomato; --bit-Stepper-indicator-size: 1.5rem;"" />

<BitStepper Items=""basicItems"" NameSelectors=""nameSelectors"" CurrentIndex=""1"" Class=""custom-class"" />

<BitStepper Items=""styleClassItems"" NameSelectors=""nameSelectors"" CurrentIndex=""1"" />

<BitStepper Items=""basicItems""
            NameSelectors=""nameSelectors""
            CurrentIndex=""2""
            Styles=""@(new() { Indicator = ""border-radius: 0.25rem;"", Text = ""text-transform: uppercase;"", Connector = ""height: 0.25rem;"" })"" />

<BitStepper Items=""basicItems""
            NameSelectors=""nameSelectors""
            CurrentIndex=""2""
            Classes=""@(new() { CurrentStep = ""custom-current"", Connector = ""custom-connector"" })"" />";
    private readonly string example9CsharpCode = @"
private static readonly BitStepperNameSelectors<Milestone> nameSelectors = new()
{
    Text = { Name = nameof(Milestone.Name) },
    Description = { Name = nameof(Milestone.Note) },
    IconName = { Name = nameof(Milestone.Symbol) },
    Icon = { Selector = m => m.Glyph },
    IsError = { Name = nameof(Milestone.HasError) },
    IsDisabled = { Name = nameof(Milestone.Blocked) },
    Class = { Name = nameof(Milestone.Css) },
    Style = { Name = nameof(Milestone.Inline) },
};

private readonly List<Milestone> basicItems =
[
    new() { Name = ""Account"" },
    new() { Name = ""Address"" },
    new() { Name = ""Payment"" },
    new() { Name = ""Review"" }
];

private readonly List<Milestone> styleClassItems =
[
    new() { Name = ""Account"", Inline = ""--bit-Stepper-active-color: seagreen;"" },
    new() { Name = ""Address"", Css = ""custom-item"" },
    new() { Name = ""Payment"" },
    new() { Name = ""Review"" }
];";

    private readonly string example10RazorCode = @"
<div dir=""rtl"">
    <BitStepper Dir=""BitDir.Rtl"" Items=""rtlItems"" NameSelectors=""nameSelectors"" CurrentIndex=""1"" />

    <BitStepper Dir=""BitDir.Rtl"" Vertical Items=""rtlItems"" NameSelectors=""nameSelectors"" CurrentIndex=""1"" />
</div>";
    private readonly string example10CsharpCode = @"
private static readonly BitStepperNameSelectors<Milestone> nameSelectors = new()
{
    Text = { Name = nameof(Milestone.Name) },
    Description = { Name = nameof(Milestone.Note) },
    IconName = { Name = nameof(Milestone.Symbol) },
    Icon = { Selector = m => m.Glyph },
    IsError = { Name = nameof(Milestone.HasError) },
    IsDisabled = { Name = nameof(Milestone.Blocked) },
    Class = { Name = nameof(Milestone.Css) },
    Style = { Name = nameof(Milestone.Inline) },
};

private readonly List<Milestone> rtlItems =
[
    new() { Name = ""حساب کاربری"" },
    new() { Name = ""آدرس"" },
    new() { Name = ""پرداخت"" },
    new() { Name = ""بازبینی"" }
];";
}
