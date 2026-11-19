namespace Bit.Blow;

/// <summary>
/// Checks a graph against a set of rules and reports every problem it finds, each tied to the node, edge or port
/// it concerns. Runs with no UI: validate before saving, before running a workflow, on a server receiving a graph,
/// or after every edit to mark problems on the canvas.
/// </summary>
/// <example>
/// <code>
/// var validator = BlowGraphValidator.CreateDefault()
///     .Add(BlowValidationRules.NoCycles())
///     .Add(BlowValidationRules.ForNodes("missing-owner", n =&gt; n.Type == "task" &amp;&amp; n.Data is null ? "Every task needs an owner." : null));
///
/// var result = validator.Validate(nodes, edges);
/// if (!result.IsValid) ShowProblems(result.Issues);
/// </code>
/// </example>
public sealed class BlowGraphValidator
{
    private readonly List<IBlowValidationRule> _rules;

    /// <summary>A validator with the given rules, and no others.</summary>
    public BlowGraphValidator(params IBlowValidationRule[] rules) => _rules = [.. rules];

    /// <summary>
    /// A validator with the rules every graph should pass: <see cref="BlowValidationRules.Structure"/> (unique
    /// ids, edges and parents that exist, no parent cycles, edges to declared ports) and
    /// <see cref="BlowValidationRules.Ports"/> (connection counts and data types of declared ports).
    /// </summary>
    /// <param name="mode">
    /// The canvas's <c>ConnectionMode</c>. In <see cref="BlowConnectionMode.Loose"/> mode any handle can be either end
    /// of an edge, so an edge may name a port of the other type at its end, and a port's connections are counted at
    /// both ends.
    /// </param>
    public static BlowGraphValidator CreateDefault(BlowConnectionMode mode = BlowConnectionMode.Strict)
        => new(BlowValidationRules.Structure(mode), BlowValidationRules.Ports(mode: mode));

    /// <summary>The rules, in the order they run.</summary>
    public IReadOnlyList<IBlowValidationRule> Rules => _rules;

    /// <summary>
    /// The most issues collected before the validation stops, so a hopelessly broken (or hostile) graph produces a
    /// bounded report in bounded time. 1,000 by default.
    /// </summary>
    public int MaxIssues { get; set; } = 1000;

    /// <summary>Adds a rule, and returns the validator for chaining.</summary>
    public BlowGraphValidator Add(IBlowValidationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
        return this;
    }

    /// <summary>Adds a rule written as a function, and returns the validator for chaining.</summary>
    public BlowGraphValidator Add(Func<BlowValidationContext, IEnumerable<BlowValidationIssue>> rule)
        => Add(new BlowDelegateRule(rule ?? throw new ArgumentNullException(nameof(rule))));

    /// <summary>Validates a graph.</summary>
    /// <exception cref="OperationCanceledException">The token was signalled.</exception>
    public BlowValidationResult Validate(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        var context = new BlowValidationContext(nodes.ToList(), edges.ToList(), cancellationToken);
        var issues = new List<BlowValidationIssue>();
        foreach (var rule in _rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var issue in rule.Validate(context))
            {
                if (issues.Count >= MaxIssues) return new BlowValidationResult(issues, truncated: true);
                issues.Add(issue);
            }
        }
        return new BlowValidationResult(issues);
    }
}
