namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Stepper;

public partial class _BitStepperItemDemo
{
    private readonly string example1RazorCode = @"
<style>
    .step-content {
        padding: 1rem;
        border-radius: 0.25rem;
        border: 1px dashed var(--bit-clr-brd-pri);
    }
</style>

<BitStepper Items=""basicItems"" @bind-CurrentIndex=""basicIndex"" />

<div class=""step-content"">@(basicIndex < basicItems.Count ? $""Content of step {basicIndex + 1}"" : ""All steps are done"")</div>

<BitButton Variant=""BitVariant.Outline"" Disabled=""basicIndex == 0"" OnClick=""() => basicIndex--"">Back</BitButton>
<BitButton Disabled=""basicIndex >= basicItems.Count"" OnClick=""() => basicIndex++"">@(basicIndex < basicItems.Count - 1 ? ""Next"" : ""Finish"")</BitButton>";
    private readonly string example1CsharpCode = @"
private int basicIndex;

private readonly List<BitStepperItem> basicItems =
[
    new() { Text = ""Account"" },
    new() { Text = ""Address"" },
    new() { Text = ""Payment"" },
    new() { Text = ""Review"" }
];";

    private readonly string example2RazorCode = @"
<BitStepper Vertical Items=""basicItems"" CurrentIndex=""1"" />";
    private readonly string example2CsharpCode = @"
private readonly List<BitStepperItem> basicItems =
[
    new() { Text = ""Account"" },
    new() { Text = ""Address"" },
    new() { Text = ""Payment"" },
    new() { Text = ""Review"" }
];";

    private readonly string example3RazorCode = @"
<BitStepper Items=""iconItems"" CurrentIndex=""1"" />";
    private readonly string example3CsharpCode = @"
private readonly List<BitStepperItem> iconItems =
[
    new() { Text = ""Account"", Description = ""Your details"", IconName = BitIconName.Contact },
    new() { Text = ""Address"", Description = ""Where to ship"", IconName = BitIconName.Home },
    new() { Text = ""Payment"", Description = ""Card or transfer"", IconName = BitIconName.PaymentCard },
    new() { Text = ""Review"", Description = ""Check and confirm"", IconName = BitIconName.CheckList }
];";

    private readonly string example4RazorCode = @"
<BitStepper Linear Items=""basicItems"" @bind-CurrentIndex=""linearIndex"" />

<BitButton Variant=""BitVariant.Outline"" Disabled=""linearIndex == 0"" OnClick=""() => linearIndex--"">Back</BitButton>
<BitButton Disabled=""linearIndex >= basicItems.Count"" OnClick=""() => linearIndex++"">Next</BitButton>";
    private readonly string example4CsharpCode = @"
private int linearIndex;

private readonly List<BitStepperItem> basicItems =
[
    new() { Text = ""Account"" },
    new() { Text = ""Address"" },
    new() { Text = ""Payment"" },
    new() { Text = ""Review"" }
];";

    private readonly string example5RazorCode = @"
<BitStepper ReadOnly Items=""basicItems"" CurrentIndex=""2"" />";
    private readonly string example5CsharpCode = @"
private readonly List<BitStepperItem> basicItems =
[
    new() { Text = ""Account"" },
    new() { Text = ""Address"" },
    new() { Text = ""Payment"" },
    new() { Text = ""Review"" }
];";

    private readonly string example6RazorCode = @"
<BitStepper Items=""errorItems"" CurrentIndex=""2"" />

<BitStepper Items=""basicItems"" CurrentIndex=""1"" Disabled />";
    private readonly string example6CsharpCode = @"
private readonly List<BitStepperItem> errorItems =
[
    new() { Text = ""Account"" },
    new() { Text = ""Address"", Description = ""The postal code is not valid"", IsError = true },
    new() { Text = ""Payment"" },
    new() { Text = ""Gift wrap"", Description = ""Not available for this order"", IsDisabled = true },
    new() { Text = ""Review"" }
];

private readonly List<BitStepperItem> basicItems =
[
    new() { Text = ""Account"" },
    new() { Text = ""Address"" },
    new() { Text = ""Payment"" },
    new() { Text = ""Review"" }
];";

    private readonly string example7RazorCode = @"
<BitStepper Items=""basicItems""
            CurrentIndex=""eventsIndex""
            OnChange=""(int index) => { if (Math.Abs(index - eventsIndex) == 1) eventsIndex = index; eventsChange = index; }""
            OnItemClick=""(BitStepperItem item) => eventsClicked = item.Text"" />

<div>Clicked step: <b>@eventsClicked</b></div>
<div>Asked for step: <b>@(eventsChange + 1)</b>, current step: <b>@(eventsIndex + 1)</b></div>";
    private readonly string example7CsharpCode = @"
private int eventsIndex;
private int eventsChange;
private string? eventsClicked;

private readonly List<BitStepperItem> basicItems =
[
    new() { Text = ""Account"" },
    new() { Text = ""Address"" },
    new() { Text = ""Payment"" },
    new() { Text = ""Review"" }
];";

    private readonly string example8RazorCode = @"
<!-- FontAwesome -->
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitStepper Items=""externalIconItems1""
            CurrentIndex=""2""
            CompletedIcon=""@(""fa-solid fa-circle-check"")""
            ErrorIcon=""@BitIconInfo.Fa(""solid triangle-exclamation"")"" />

<!-- Bootstrap Icons -->
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitStepper Items=""externalIconItems2""
            CurrentIndex=""1""
            CompletedIcon=""@BitIconInfo.Bi(""check-lg"")"" />";
    private readonly string example8CsharpCode = @"
private readonly List<BitStepperItem> externalIconItems1 =
[
    new() { Text = ""Account"", Icon = ""fa-solid fa-user"" },
    new() { Text = ""Address"", Icon = BitIconInfo.Fa(""solid house""), IsError = true },
    new() { Text = ""Payment"", Icon = BitIconInfo.Fa(""solid credit-card"") },
    new() { Text = ""Review"", Icon = BitIconInfo.Fa(""solid list-check"") }
];

private readonly List<BitStepperItem> externalIconItems2 =
[
    new() { Text = ""Account"", Icon = BitIconInfo.Bi(""person"") },
    new() { Text = ""Address"", Icon = BitIconInfo.Bi(""house"") },
    new() { Text = ""Payment"", Icon = BitIconInfo.Bi(""credit-card"") },
    new() { Text = ""Review"", Icon = BitIconInfo.Bi(""card-checklist"") }
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

<BitStepper Items=""basicItems"" CurrentIndex=""1"" Style=""--bit-Stepper-active-color: tomato; --bit-Stepper-indicator-size: 1.5rem;"" />

<BitStepper Items=""basicItems"" CurrentIndex=""1"" Class=""custom-class"" />

<BitStepper Items=""styleClassItems"" CurrentIndex=""1"" />

<BitStepper Items=""basicItems""
            CurrentIndex=""2""
            Styles=""@(new() { Indicator = ""border-radius: 0.25rem;"", Text = ""text-transform: uppercase;"", Connector = ""height: 0.25rem;"" })"" />

<BitStepper Items=""basicItems""
            CurrentIndex=""2""
            Classes=""@(new() { CurrentStep = ""custom-current"", Connector = ""custom-connector"" })"" />";
    private readonly string example9CsharpCode = @"
private readonly List<BitStepperItem> basicItems =
[
    new() { Text = ""Account"" },
    new() { Text = ""Address"" },
    new() { Text = ""Payment"" },
    new() { Text = ""Review"" }
];

private readonly List<BitStepperItem> styleClassItems =
[
    new() { Text = ""Account"", Style = ""--bit-Stepper-active-color: seagreen;"" },
    new() { Text = ""Address"", Class = ""custom-item"" },
    new() { Text = ""Payment"" },
    new() { Text = ""Review"" }
];";

    private readonly string example10RazorCode = @"
<div dir=""rtl"">
    <BitStepper Dir=""BitDir.Rtl"" Items=""rtlItems"" CurrentIndex=""1"" />

    <BitStepper Dir=""BitDir.Rtl"" Vertical Items=""rtlItems"" CurrentIndex=""1"" />
</div>";
    private readonly string example10CsharpCode = @"
private readonly List<BitStepperItem> rtlItems =
[
    new() { Text = ""حساب کاربری"" },
    new() { Text = ""آدرس"" },
    new() { Text = ""پرداخت"" },
    new() { Text = ""بازبینی"" }
];";
}
