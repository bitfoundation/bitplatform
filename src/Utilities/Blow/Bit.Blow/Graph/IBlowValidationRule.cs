namespace Bit.Blow;

/// <summary>A check over a graph that reports what it finds as issues.</summary>
public interface IBlowValidationRule
{
    /// <summary>The issues this rule finds in the graph; none when it is satisfied.</summary>
    IEnumerable<BlowValidationIssue> Validate(BlowValidationContext context);
}
