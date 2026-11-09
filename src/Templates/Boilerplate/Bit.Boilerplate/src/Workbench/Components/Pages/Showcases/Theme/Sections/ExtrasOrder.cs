namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>An order as the Extras section's data grid lists it.</summary>
public sealed record ExtrasOrder(string Number, string Customer, DateTimeOffset Date, decimal Total, ExtrasOrderStatus Status);
