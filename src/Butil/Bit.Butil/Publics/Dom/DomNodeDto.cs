using System;

namespace Bit.Butil;

/// <summary>
/// The JSON shape the <c>BitButil.dom</c> module answers with: the registry id it filed an element
/// under, plus the little about it that is worth carrying across without a second call.
/// </summary>
/// <remarks>
/// Internal because a caller sees <see cref="DomHandle"/> instead - this exists only because a live
/// element cannot cross the interop boundary.
/// </remarks>
internal class DomNodeDto
{
    public Guid Id { get; set; }
    public string? TagName { get; set; }
}
