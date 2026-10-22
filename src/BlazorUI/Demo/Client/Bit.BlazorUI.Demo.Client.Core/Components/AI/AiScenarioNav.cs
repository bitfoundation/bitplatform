using Bit.BlazorUI.Demo.Client.Core.Shared;

namespace Bit.BlazorUI.Demo.Client.Core.Components.AI;

/// <summary>
/// The AI scenario pages (Pages/AI/Scenarios), read off the nav - the "Scenarios" group inside the "AI" one of
/// <see cref="MainLayout.NavItems"/> - rather than listed a second time, so the pager at the foot of a scenario walks
/// them in exactly the order the panel shows.
/// </summary>
public static class AiScenarioNav
{
    /// <summary>Every scenario page, in nav order.</summary>
    public static IReadOnlyList<BitNavItem> Items { get; } =
        [.. MainLayout.NavItems.FirstOrDefault(item => item.Text == "AI")?
                               .ChildItems.FirstOrDefault(item => item.Text == "Scenarios")?
                               .ChildItems ?? []];
}
