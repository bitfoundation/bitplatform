namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

/// <summary>A team member's sales and orders in the period; the name key also finds their photo.</summary>
public sealed record PosStaffMember(string NameKey, decimal Sales, int Orders);
