namespace Bit.Bonboarding;

/// <summary>
/// Finds the mistakes in a tour definition that would otherwise surface as a tour quietly misbehaving for a user:
/// a jump that lands nowhere, two steps sharing an id (so a jump, Back and a resume all find the first), a step with
/// no way forward. Only what can be known from the definition is checked; selectors are the browser's to judge.
/// </summary>
internal static class BonboardingTourValidator
{
    public static IReadOnlyList<BonboardingValidationIssue> Validate(BonboardingTourDefinition tour)
    {
        var issues = new List<BonboardingValidationIssue>();
        var tourName = string.IsNullOrWhiteSpace(tour.Id) ? "(no id)" : tour.Id;

        if (string.IsNullOrWhiteSpace(tour.Id))
        {
            issues.Add(Error("missing-tour-id", "The tour has no Id. The id names the tour for starting it by id and is its persistence key."));
        }

        if (tour.Steps.Count == 0)
        {
            issues.Add(Error("no-steps", $"Tour '{tourName}' has no steps."));
            return issues;
        }

        CheckOptions(tour, issues, tourName, stepId: null);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ids = new HashSet<string>(tour.Steps.Select(step => step.Id), StringComparer.Ordinal);

        foreach (var step in tour.Steps)
        {
            var id = step.Id;
            var where = $"Step '{id}' of tour '{tourName}'";

            if (string.IsNullOrWhiteSpace(id))
            {
                issues.Add(Error("blank-step-id", $"A step of tour '{tourName}' has a blank Id. Leave it unset to have one generated, or give it a real one."));
            }
            else if (id == BonboardingTourDefinition.EndStepId)
            {
                issues.Add(Error("reserved-step-id", $"{where} uses the reserved id '{BonboardingTourDefinition.EndStepId}', which ResolveNext returns to end the tour.", id));
            }
            else if (!seen.Add(id))
            {
                issues.Add(Error("duplicate-step-id", $"Tour '{tourName}' has more than one step with the id '{id}'. Jumps, Back and resuming all find the first of them.", id));
            }

            foreach (var action in step.Actions ?? [])
            {
                var name = action.Id ?? action.Label;

                if (string.IsNullOrWhiteSpace(action.Label))
                {
                    issues.Add(Error("action-without-label", $"{where} has an action with no Label; its button would have no text and no accessible name.", id));
                }

                if (action.Effect == BonboardingStepActionEffect.GoTo)
                {
                    if (string.IsNullOrWhiteSpace(action.TargetStepId))
                    {
                        issues.Add(Error("goto-without-target", $"{where} has an action '{name}' whose Effect is GoTo but which has no TargetStepId.", id));
                    }
                    else if (!ids.Contains(action.TargetStepId))
                    {
                        issues.Add(Error("goto-unknown-step", $"{where} has an action '{name}' that jumps to '{action.TargetStepId}', which is not a step of the tour.", id));
                    }
                }
            }

            if (step.Route is { } route && (route.Contains("://", StringComparison.Ordinal) || route.StartsWith("//", StringComparison.Ordinal)))
            {
                issues.Add(Warning("absolute-route", $"{where} has the route '{route}'. Routes are base-relative paths such as \"settings/profile\"; an absolute URL is never matched.", id));
            }

            var interaction = step.Interaction ?? tour.Interaction;
            var listensOnTarget = step.AdvanceOn is { Selector: null };

            if (listensOnTarget && step.Target.IsNone)
            {
                issues.Add(Warning("advance-on-without-target", $"{where} advances on an event of its target but has no target. Give the trigger a Selector, or the step a Target.", id));
            }
            else if (listensOnTarget && interaction == BonboardingInteractionMode.Blocked)
            {
                issues.Add(Warning("advance-on-blocked", $"{where} waits for the user to use its target, but its Interaction is Blocked, so the target cannot be reached. Leave Interaction unset (AdvanceOn implies TargetOnly) or set it to TargetOnly.", id));
            }

            var showsNext = step.ShowNext ?? tour.ShowNext ?? true;

            // Custom markup may carry its own way forward (a button calling NextAsync); a host-wide template is not
            // visible from here, which the message says.
            var customUi = step.Body is not null || step.Footer is not null || step.Template is not null || tour.Template is not null;
            var hasWayForward = showsNext || step.AdvanceOn is not null || step.AdvanceOnSignal is not null
                || step.Actions is { Count: > 0 } || customUi;

            if (!hasWayForward)
            {
                issues.Add(Warning("no-way-forward", $"{where} hides the Next button and has no AdvanceOn, AdvanceOnSignal, action or custom markup, so nothing on it moves the tour on (unless a BonboardingRoot template provides a way).", id));
            }

            CheckOptions(step, issues, tourName, id);
        }

        return issues;
    }

    private static void CheckOptions(BonboardingStepOptions options, List<BonboardingValidationIssue> issues, string tourName, string? stepId)
    {
        var where = stepId is null ? $"Tour '{tourName}'" : $"Step '{stepId}' of tour '{tourName}'";

        if (options.WaitTimeout is { } timeout && timeout < TimeSpan.Zero)
        {
            issues.Add(Error("negative-wait-timeout", $"{where} has a negative WaitTimeout. Use TimeSpan.Zero to wait for as long as it takes.", stepId));
        }

        if (options.OverlayOpacity is { } opacity && (opacity < 0 || opacity > 1))
        {
            issues.Add(Warning("overlay-opacity-out-of-range", $"{where} has an OverlayOpacity of {opacity}; it is a fraction between 0 and 1.", stepId));
        }
    }

    private static BonboardingValidationIssue Error(string code, string message, string? stepId = null)
        => new(BonboardingValidationSeverity.Error, code, message, stepId);

    private static BonboardingValidationIssue Warning(string code, string message, string? stepId = null)
        => new(BonboardingValidationSeverity.Warning, code, message, stepId);
}
