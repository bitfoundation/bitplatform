namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Data;

public sealed record PosOrder(string Number, DateTimeOffset Time, string CashierKey, int Items, decimal Total, string PaymentKey, PosOrderStatus Status);
