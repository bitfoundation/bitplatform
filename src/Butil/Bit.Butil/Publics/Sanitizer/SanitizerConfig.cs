namespace Bit.Butil;

/// <summary>
/// A <see href="https://developer.mozilla.org/en-US/docs/Web/API/Sanitizer">Sanitizer</see>
/// configuration: which elements and attributes survive sanitizing.
/// </summary>
/// <remarks>
/// A configuration is either an allow-list or a deny-list, not both: setting
/// <see cref="Elements"/> means "only these", and setting <see cref="RemoveElements"/> means
/// "everything the browser's baseline allows, minus these". Mixing the two throws in the browser and
/// comes back here as a failed <see cref="Sanitizer.Create"/>.
/// <br/>
/// Every property left null is left unset, so the browser's own default applies to it - which is
/// what you want unless you have a reason to say otherwise. The baseline already removes scripts,
/// event-handler attributes and <c>javascript:</c> URLs; a configuration narrows what is left, it
/// does not switch safety on.
/// </remarks>
public class SanitizerConfig
{
    /// <summary>
    /// The only elements allowed, by lower-case name, e.g. <c>["p", "b", "a", "ul", "li"]</c>.
    /// Everything else is dropped along with its contents.
    /// </summary>
    public string[]? Elements { get; set; }

    /// <summary>
    /// Elements to drop, along with their contents, from what the baseline would otherwise allow.
    /// </summary>
    public string[]? RemoveElements { get; set; }

    /// <summary>
    /// Elements to unwrap: the element itself goes, its children stay. The way to drop a
    /// <c>&lt;div&gt;</c> wrapper without losing the paragraph inside it.
    /// </summary>
    public string[]? ReplaceWithChildrenElements { get; set; }

    /// <summary>
    /// The only attributes allowed, by lower-case name, e.g. <c>["href", "title"]</c>.
    /// </summary>
    public string[]? Attributes { get; set; }

    /// <summary>Attributes to drop from what the baseline would otherwise allow.</summary>
    public string[]? RemoveAttributes { get; set; }

    /// <summary>Whether HTML comments survive. Null leaves the browser's default (they do not).</summary>
    public bool? Comments { get; set; }

    /// <summary>
    /// Whether <c>data-*</c> attributes survive as a group, without naming each one. Null leaves the
    /// browser's default.
    /// </summary>
    public bool? DataAttributes { get; set; }
}
