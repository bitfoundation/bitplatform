namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Stepper;

public partial class _BitStepperItemDemo
{
    private int basicIndex;
    private int linearIndex;

    private int eventsIndex;
    private int eventsChange;
    private string? eventsClicked;

    private readonly List<BitStepperItem> basicItems =
    [
        new() { Text = "Account" },
        new() { Text = "Address" },
        new() { Text = "Payment" },
        new() { Text = "Review" }
    ];

    private readonly List<BitStepperItem> iconItems =
    [
        new() { Text = "Account", Description = "Your details", IconName = BitIconName.Contact },
        new() { Text = "Address", Description = "Where to ship", IconName = BitIconName.Home },
        new() { Text = "Payment", Description = "Card or transfer", IconName = BitIconName.PaymentCard },
        new() { Text = "Review", Description = "Check and confirm", IconName = BitIconName.CheckList }
    ];

    private readonly List<BitStepperItem> errorItems =
    [
        new() { Text = "Account" },
        new() { Text = "Address", Description = "The postal code is not valid", IsError = true },
        new() { Text = "Payment" },
        new() { Text = "Gift wrap", Description = "Not available for this order", IsDisabled = true },
        new() { Text = "Review" }
    ];

    private readonly List<BitStepperItem> externalIconItems1 =
    [
        new() { Text = "Account", Icon = "fa-solid fa-user" },
        new() { Text = "Address", Icon = BitIconInfo.Fa("solid house"), IsError = true },
        new() { Text = "Payment", Icon = BitIconInfo.Fa("solid credit-card") },
        new() { Text = "Review", Icon = BitIconInfo.Fa("solid list-check") }
    ];

    private readonly List<BitStepperItem> externalIconItems2 =
    [
        new() { Text = "Account", Icon = BitIconInfo.Bi("person") },
        new() { Text = "Address", Icon = BitIconInfo.Bi("house") },
        new() { Text = "Payment", Icon = BitIconInfo.Bi("credit-card") },
        new() { Text = "Review", Icon = BitIconInfo.Bi("card-checklist") }
    ];

    private readonly List<BitStepperItem> styleClassItems =
    [
        new() { Text = "Account", Style = "--bit-Stepper-active-color: seagreen;" },
        new() { Text = "Address", Class = "custom-item" },
        new() { Text = "Payment" },
        new() { Text = "Review" }
    ];

    private readonly List<BitStepperItem> rtlItems =
    [
        new() { Text = "حساب کاربری" },
        new() { Text = "آدرس" },
        new() { Text = "پرداخت" },
        new() { Text = "بازبینی" }
    ];
}
