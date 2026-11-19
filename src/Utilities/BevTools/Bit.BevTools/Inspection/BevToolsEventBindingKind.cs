namespace Bit.BevTools;

public enum BevToolsEventBindingKind
{
    /// <summary>A DOM event handler on an element: <c>@onclick="Save"</c>.</summary>
    DomEvent,

    /// <summary>The change handler <c>@bind</c> generates, which writes the element's value back into the component.</summary>
    Bind,

    /// <summary>An <c>EventCallback</c> or delegate the component passes to a child component as a parameter.</summary>
    ChildCallback,
}
