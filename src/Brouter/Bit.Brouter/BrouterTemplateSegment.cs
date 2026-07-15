namespace Bit.Brouter;

internal class BrouterTemplateSegment
{
    /// <summary>
    /// The literal text for a literal segment, or the parameter name for a parameter segment.
    /// </summary>
    public string Value { get; }

    public bool IsParameter { get; }

    /// <summary>True for the literal-wildcard form <c>**</c> or the parameter-catch-all form <c>{**name}</c>.</summary>
    public bool IsCatchAll { get; }

    /// <summary>True for the literal single-segment wildcard <c>*</c>.</summary>
    public bool IsSingleWildcard { get; }

    /// <summary>True for parameters declared with a trailing <c>?</c>, e.g. <c>{id?}</c>.</summary>
    public bool IsOptional { get; }

    public BrouterRouteConstraintBinding[] Constraints { get; }

    public BrouterTemplateSegment(string template, string segment, bool isParameter, BrouterConstraintRegistry? constraints = null)
    {
        IsParameter = isParameter;

        if (isParameter is false)
        {
            Value = segment;
            Constraints = [];
            IsSingleWildcard = segment == "*";
            IsCatchAll = segment == "**";
            IsOptional = false;
            return;
        }

        // Parameter segment: parse {name}, {name?}, {**name}, {name:int}, {name:int:long}, etc.
        var name = segment;

        // Catch-all parameter: leading "**".
        if (name.StartsWith("**", StringComparison.Ordinal))
        {
            IsCatchAll = true;
            name = name[2..];
        }

        // Optional parameter: trailing "?".
        if (name.EndsWith('?'))
        {
            IsOptional = true;
            name = name[..^1];
        }

        // Constraints: split on ':'.
        var colonIdx = name.IndexOf(':');
        if (colonIdx < 0)
        {
            Value = name;
            Constraints = [];
        }
        else
        {
            var paramName = name[..colonIdx];
            var rest = name[(colonIdx + 1)..];

            if (paramName.Length == 0)
                throw new ArgumentException($"Malformed parameter '{segment}' in route '{template}' has no name before the constraints list.");

            if (IsCatchAll)
                throw new ArgumentException($"Catch-all parameter '{segment}' in route '{template}' cannot have constraints.");

            Value = paramName;
            Constraints = rest.Split(':')
                              .Select(c => new BrouterRouteConstraintBinding(c, BrouterRouteConstraint.Resolve(template, segment, c, constraints)))
                              .ToArray();
        }

        if (string.IsNullOrEmpty(Value))
            throw new ArgumentException($"Parameter segment '{segment}' in route '{template}' has no name.");
    }

    public bool TryMatch(string segment, StringComparison literalComparison, out object? matchedParameterValue)
    {
        if (IsParameter)
        {
            matchedParameterValue = segment;

            foreach (var binding in Constraints)
            {
                if (binding.Constraint.TryMatch(segment, out matchedParameterValue) is false) return false;
            }

            return true;
        }

        matchedParameterValue = null;
        return IsCatchAll || IsSingleWildcard || string.Equals(Value, segment, literalComparison);
    }

    /// <summary>
    /// A score used to rank competing matches. Higher = more specific.
    /// Heuristic borrowed from React Router v6 and ASP.NET Core's route matcher.
    /// </summary>
    public int Specificity
    {
        get
        {
            if (IsCatchAll) return 1;
            if (IsSingleWildcard) return 2;
            if (IsParameter)
            {
                // Cap parameter scores below the literal score (11) so a literal segment always
                // wins a tie at the same depth, even when a parameter declares many constraints.
                var score = (IsOptional ? 4 : 6) + Constraints.Length * 2;
                return Math.Min(score, 10);
            }
            return 11; // literal
        }
    }
}
