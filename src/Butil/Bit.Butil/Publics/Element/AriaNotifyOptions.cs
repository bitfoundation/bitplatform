namespace Bit.Butil;

/// <summary>
/// How an <see cref="ElementReferenceAriaExtensions.AriaNotify(Microsoft.AspNetCore.Components.ElementReference, string, AriaNotifyOptions?)"/>
/// announcement should be queued.
/// </summary>
public class AriaNotifyOptions
{
    /// <summary>Where the announcement goes in the queue. Defaults to <see cref="AriaNotifyPriority.Normal"/>.</summary>
    public AriaNotifyPriority? Priority { get; set; }

    internal AriaNotifyJsOptions ToJsObject() => new()
    {
        Priority = Priority switch
        {
            AriaNotifyPriority.High => "high",
            _ => "normal"
        }
    };
}
