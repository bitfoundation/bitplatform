namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

public sealed record PosOrder(string Number, DateTimeOffset Time, string CashierKey, int Items, decimal Total, string PaymentKey, PosOrderStatus Status);
