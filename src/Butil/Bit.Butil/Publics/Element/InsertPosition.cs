namespace Bit.Butil;

/// <summary>Where <c>insertAdjacentHTML</c> and <c>insertAdjacentText</c> put what they are given, relative to the element.</summary>
public enum InsertPosition
{
    /// <summary>Immediately before the element itself.</summary>
    BeforeBegin,

    /// <summary>Inside the element, before its first child.</summary>
    AfterBegin,

    /// <summary>Inside the element, after its last child.</summary>
    BeforeEnd,

    /// <summary>Immediately after the element itself.</summary>
    AfterEnd
}
