namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Stepper;

public partial class _BitStepperOptionDemo
{
    private readonly string example1RazorCode = @"
<style>
    .step-content {
        padding: 1rem;
        border-radius: 0.25rem;
        border: 1px dashed var(--bit-clr-brd-pri);
    }
</style>

<BitStepper TItem=""BitStepperOption"" @bind-CurrentIndex=""basicIndex"">
    <BitStepperOption Text=""Account"" />
    <BitStepperOption Text=""Address"" />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Review"" />
</BitStepper>

<div class=""step-content"">@(basicIndex < StepCount ? $""Content of step {basicIndex + 1}"" : ""All steps are done"")</div>

<BitButton Variant=""BitVariant.Outline"" Disabled=""basicIndex == 0"" OnClick=""() => basicIndex--"">Back</BitButton>
<BitButton Disabled=""basicIndex >= StepCount"" OnClick=""() => basicIndex++"">@(basicIndex < StepCount - 1 ? ""Next"" : ""Finish"")</BitButton>";
    private readonly string example1CsharpCode = @"
private const int StepCount = 4;
private int basicIndex;";

    private readonly string example2RazorCode = @"
<BitStepper TItem=""BitStepperOption"" Vertical CurrentIndex=""1"">
    <BitStepperOption Text=""Account"" />
    <BitStepperOption Text=""Address"" />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Review"" />
</BitStepper>";

    private readonly string example3RazorCode = @"
<BitStepper TItem=""BitStepperOption"" CurrentIndex=""1"">
    <BitStepperOption Text=""Account"" Description=""Your details"" IconName=""@BitIconName.Contact"" />
    <BitStepperOption Text=""Address"" Description=""Where to ship"" IconName=""@BitIconName.Home"" />
    <BitStepperOption Text=""Payment"" Description=""Card or transfer"" IconName=""@BitIconName.PaymentCard"" />
    <BitStepperOption Text=""Review"" Description=""Check and confirm"" IconName=""@BitIconName.CheckList"" />
</BitStepper>";

    private readonly string example4RazorCode = @"
<BitStepper TItem=""BitStepperOption"" Linear @bind-CurrentIndex=""linearIndex"">
    <BitStepperOption Text=""Account"" />
    <BitStepperOption Text=""Address"" />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Review"" />
</BitStepper>

<BitButton Variant=""BitVariant.Outline"" Disabled=""linearIndex == 0"" OnClick=""() => linearIndex--"">Back</BitButton>
<BitButton Disabled=""linearIndex >= StepCount"" OnClick=""() => linearIndex++"">Next</BitButton>";
    private readonly string example4CsharpCode = @"
private const int StepCount = 4;
private int linearIndex;";

    private readonly string example5RazorCode = @"
<BitStepper TItem=""BitStepperOption"" ReadOnly CurrentIndex=""2"">
    <BitStepperOption Text=""Account"" />
    <BitStepperOption Text=""Address"" />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Review"" />
</BitStepper>";

    private readonly string example6RazorCode = @"
<BitStepper TItem=""BitStepperOption"" CurrentIndex=""2"">
    <BitStepperOption Text=""Account"" />
    <BitStepperOption Text=""Address"" Description=""The postal code is not valid"" IsError />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Gift wrap"" Description=""Not available for this order"" IsDisabled />
    <BitStepperOption Text=""Review"" />
</BitStepper>

<BitStepper TItem=""BitStepperOption"" CurrentIndex=""1"" Disabled>
    <BitStepperOption Text=""Account"" />
    <BitStepperOption Text=""Address"" />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Review"" />
</BitStepper>";

    private readonly string example7RazorCode = @"
<BitStepper TItem=""BitStepperOption""
            CurrentIndex=""eventsIndex""
            OnChange=""(int index) => { if (Math.Abs(index - eventsIndex) == 1) eventsIndex = index; eventsChange = index; }""
            OnItemClick=""(BitStepperOption item) => eventsClicked = item.Text"">
    <BitStepperOption Text=""Account"" />
    <BitStepperOption Text=""Address"" />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Review"" />
</BitStepper>

<div>Clicked step: <b>@eventsClicked</b></div>
<div>Asked for step: <b>@(eventsChange + 1)</b>, current step: <b>@(eventsIndex + 1)</b></div>";
    private readonly string example7CsharpCode = @"
private int eventsIndex;
private int eventsChange;
private string? eventsClicked;";

    private readonly string example8RazorCode = @"
<!-- FontAwesome -->
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitStepper TItem=""BitStepperOption""
            CurrentIndex=""2""
            CompletedIcon=""@(""fa-solid fa-circle-check"")""
            ErrorIcon=""@BitIconInfo.Fa(""solid triangle-exclamation"")"">
    <BitStepperOption Text=""Account"" Icon=""@(""fa-solid fa-user"")"" />
    <BitStepperOption Text=""Address"" Icon=""@BitIconInfo.Fa(""solid house"")"" IsError />
    <BitStepperOption Text=""Payment"" Icon=""@BitIconInfo.Fa(""solid credit-card"")"" />
    <BitStepperOption Text=""Review"" Icon=""@BitIconInfo.Fa(""solid list-check"")"" />
</BitStepper>

<!-- Bootstrap Icons -->
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitStepper TItem=""BitStepperOption"" CurrentIndex=""1"" CompletedIcon=""@BitIconInfo.Bi(""check-lg"")"">
    <BitStepperOption Text=""Account"" Icon=""@BitIconInfo.Bi(""person"")"" />
    <BitStepperOption Text=""Address"" Icon=""@BitIconInfo.Bi(""house"")"" />
    <BitStepperOption Text=""Payment"" Icon=""@BitIconInfo.Bi(""credit-card"")"" />
    <BitStepperOption Text=""Review"" Icon=""@BitIconInfo.Bi(""card-checklist"")"" />
</BitStepper>";

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

<BitStepper TItem=""BitStepperOption"" CurrentIndex=""1"" Style=""--bit-Stepper-active-color: tomato; --bit-Stepper-indicator-size: 1.5rem;"">
    <BitStepperOption Text=""Account"" />
    <BitStepperOption Text=""Address"" />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Review"" />
</BitStepper>

<BitStepper TItem=""BitStepperOption"" CurrentIndex=""1"" Class=""custom-class"">
    <BitStepperOption Text=""Account"" />
    <BitStepperOption Text=""Address"" />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Review"" />
</BitStepper>

<BitStepper TItem=""BitStepperOption"" CurrentIndex=""1"">
    <BitStepperOption Text=""Account"" Style=""--bit-Stepper-active-color: seagreen;"" />
    <BitStepperOption Text=""Address"" Class=""custom-item"" />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Review"" />
</BitStepper>

<BitStepper TItem=""BitStepperOption""
            CurrentIndex=""2""
            Styles=""@(new() { Indicator = ""border-radius: 0.25rem;"", Text = ""text-transform: uppercase;"", Connector = ""height: 0.25rem;"" })"">
    <BitStepperOption Text=""Account"" />
    <BitStepperOption Text=""Address"" />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Review"" />
</BitStepper>

<BitStepper TItem=""BitStepperOption""
            CurrentIndex=""2""
            Classes=""@(new() { CurrentStep = ""custom-current"", Connector = ""custom-connector"" })"">
    <BitStepperOption Text=""Account"" />
    <BitStepperOption Text=""Address"" />
    <BitStepperOption Text=""Payment"" />
    <BitStepperOption Text=""Review"" />
</BitStepper>";

    private readonly string example10RazorCode = @"
<div dir=""rtl"">
    <BitStepper TItem=""BitStepperOption"" Dir=""BitDir.Rtl"" CurrentIndex=""1"">
        <BitStepperOption Text=""حساب کاربری"" />
        <BitStepperOption Text=""آدرس"" />
        <BitStepperOption Text=""پرداخت"" />
        <BitStepperOption Text=""بازبینی"" />
    </BitStepper>

    <BitStepper TItem=""BitStepperOption"" Dir=""BitDir.Rtl"" Vertical CurrentIndex=""1"">
        <BitStepperOption Text=""حساب کاربری"" />
        <BitStepperOption Text=""آدرس"" />
        <BitStepperOption Text=""پرداخت"" />
        <BitStepperOption Text=""بازبینی"" />
    </BitStepper>
</div>";
}
