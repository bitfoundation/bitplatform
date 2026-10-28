using AngleSharp.Dom;

namespace Bit.BlazorHeadlessUI.Tests;

/// <summary>Base class for component tests: a bUnit context prepared for BlazorHeadless.</summary>
public abstract class TestBase : BitBhTestContext
{
    /// <summary>The element referenced by <paramref name="element"/>'s aria-activedescendant.</summary>
    protected static IElement? ActiveOf(IElement element)
    {
        var id = element.GetAttribute("aria-activedescendant");
        if (string.IsNullOrEmpty(id)) return null;
        var root = element;
        while (root.ParentElement is not null) root = root.ParentElement;
        return root.QuerySelector($"[id='{id}']");
    }

    /// <summary>Normalized text content.</summary>
    protected static string Text(IElement? element)
        => element is null ? string.Empty : string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
