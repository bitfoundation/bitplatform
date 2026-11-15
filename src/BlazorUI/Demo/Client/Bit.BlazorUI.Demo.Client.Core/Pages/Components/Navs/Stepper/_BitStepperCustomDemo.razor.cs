namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Stepper;

public partial class _BitStepperCustomDemo
{
    private int basicIndex;
    private int linearIndex;

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
        new() { Name = "Account" },
        new() { Name = "Address" },
        new() { Name = "Payment" },
        new() { Name = "Review" }
    ];

    private readonly List<Milestone> iconItems =
    [
        new() { Name = "Account", Note = "Your details", Symbol = BitIconName.Contact },
        new() { Name = "Address", Note = "Where to ship", Symbol = BitIconName.Home },
        new() { Name = "Payment", Note = "Card or transfer", Symbol = BitIconName.PaymentCard },
        new() { Name = "Review", Note = "Check and confirm", Symbol = BitIconName.CheckList }
    ];

    private readonly List<Milestone> errorItems =
    [
        new() { Name = "Account" },
        new() { Name = "Address", Note = "The postal code is not valid", HasError = true },
        new() { Name = "Payment" },
        new() { Name = "Gift wrap", Note = "Not available for this order", Blocked = true },
        new() { Name = "Review" }
    ];

    private readonly List<Milestone> externalIconItems1 =
    [
        new() { Name = "Account", Glyph = "fa-solid fa-user" },
        new() { Name = "Address", Glyph = BitIconInfo.Fa("solid house"), HasError = true },
        new() { Name = "Payment", Glyph = BitIconInfo.Fa("solid credit-card") },
        new() { Name = "Review", Glyph = BitIconInfo.Fa("solid list-check") }
    ];

    private readonly List<Milestone> externalIconItems2 =
    [
        new() { Name = "Account", Glyph = BitIconInfo.Bi("person") },
        new() { Name = "Address", Glyph = BitIconInfo.Bi("house") },
        new() { Name = "Payment", Glyph = BitIconInfo.Bi("credit-card") },
        new() { Name = "Review", Glyph = BitIconInfo.Bi("card-checklist") }
    ];

    private readonly List<Milestone> styleClassItems =
    [
        new() { Name = "Account", Inline = "--bit-Stepper-active-color: seagreen;" },
        new() { Name = "Address", Css = "custom-item" },
        new() { Name = "Payment" },
        new() { Name = "Review" }
    ];

    private readonly List<Milestone> rtlItems =
    [
        new() { Name = "حساب کاربری" },
        new() { Name = "آدرس" },
        new() { Name = "پرداخت" },
        new() { Name = "بازبینی" }
    ];
}
