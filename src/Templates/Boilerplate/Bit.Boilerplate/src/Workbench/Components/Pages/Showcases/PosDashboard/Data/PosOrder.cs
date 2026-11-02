namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

/// <summary>A recent order as the orders table shows it; the cashier and the payment method are resource keys.</summary>
public sealed record PosOrder(string Number, DateTimeOffset Time, string CashierKey, int Items, decimal Total, string PaymentKey, PosOrderStatus Status);
