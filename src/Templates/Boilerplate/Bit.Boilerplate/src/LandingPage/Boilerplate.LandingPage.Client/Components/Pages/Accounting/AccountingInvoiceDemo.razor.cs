namespace Boilerplate.LandingPage.Client.Components.Pages.Accounting;

public partial class AccountingInvoiceDemo
{
    private const decimal VatRate = .2m;

    [AutoInject] private IStringLocalizer<AccountingStrings> A = default!;

    private decimal hours = 12;
    private decimal rate = 65;
    private bool chargeVat = true;
    private bool isSent;
    private DateTime issuedOn;

    private decimal Subtotal => Math.Round(hours * rate, 2);

    private decimal Vat => chargeVat ? Math.Round(Subtotal * VatRate, 2) : 0;

    private decimal Total => Subtotal + Vat;

    private DateTime DueOn => issuedOn.AddDays(14);

    protected override void OnInitialized()
    {
        issuedOn = DateTime.UtcNow.Date;

        base.OnInitialized();
    }

    private string Money(decimal value) => A[nameof(AccountingStrings.MoneyEUR), value.ToString("N2", CurrentCulture)];

    private string Date(DateTime value) => value.ToString("d MMMM yyyy", CurrentCulture);

    private void Send() => isSent = true;

    private void Reset()
    {
        isSent = false;
        hours = 12;
        rate = 65;
        chargeVat = true;
    }
}
