namespace Bit.BevTools;

/// <summary>What rendered while one page was shown, over every visit to it.</summary>
/// <param name="Route">The page, as the navigation named it: "/products", or a route template on .NET 10 ("/products/{id}").</param>
/// <param name="Visits">Navigations to it the timeline still holds.</param>
/// <param name="Renders">Component renders while it was shown.</param>
/// <param name="RenderMs">Their BuildRenderTree time.</param>
/// <param name="Top">The components that cost the most there, with their renders and time.</param>
internal sealed record BevToolsRouteCost(string Route, int Visits, int Renders, double RenderMs, IReadOnlyList<(string Component, int Renders, double Ms)> Top);
