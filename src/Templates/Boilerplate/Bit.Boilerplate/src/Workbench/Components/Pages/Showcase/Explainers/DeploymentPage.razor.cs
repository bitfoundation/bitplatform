using Bit.Blow;

namespace Boilerplate.Workbench.Components.Pages.Showcase.Explainers;

public partial class DeploymentPage
{
    private const string AppsHandle = "apps";

    private static readonly string[] appSteps = ["windows", "android", "ios"];

    private static readonly Dictionary<string, (double X, double Y, BlowPosition Target, BlowPosition Source)> wideLayout = new()
    {
        ["merge"] = (0, 0, BlowPosition.Left, BlowPosition.Right),
        ["images"] = (290, 0, BlowPosition.Left, BlowPosition.Right),
        ["compose"] = (580, 0, BlowPosition.Left, BlowPosition.Right),
        ["pull"] = (870, 0, BlowPosition.Left, BlowPosition.Right),
        ["migrate"] = (1160, 0, BlowPosition.Left, BlowPosition.Bottom),
        ["start"] = (1160, 116, BlowPosition.Top, BlowPosition.Left),
        ["check"] = (870, 116, BlowPosition.Right, BlowPosition.Left),
        ["purge"] = (580, 116, BlowPosition.Right, BlowPosition.Left),
        ["live"] = (290, 116, BlowPosition.Right, BlowPosition.Left),
        ["windows"] = (40, 232, BlowPosition.Left, BlowPosition.Right),
        ["android"] = (40, 324, BlowPosition.Left, BlowPosition.Right),
        ["ios"] = (40, 416, BlowPosition.Left, BlowPosition.Right)
    };

    private static readonly Dictionary<string, (double X, double Y, BlowPosition Target, BlowPosition Source)> narrowLayout = new()
    {
        ["merge"] = (0, 0, BlowPosition.Top, BlowPosition.Bottom),
        ["images"] = (0, 104, BlowPosition.Top, BlowPosition.Bottom),
        ["compose"] = (0, 208, BlowPosition.Top, BlowPosition.Bottom),
        ["pull"] = (0, 312, BlowPosition.Top, BlowPosition.Bottom),
        ["migrate"] = (0, 416, BlowPosition.Top, BlowPosition.Bottom),
        ["start"] = (0, 520, BlowPosition.Top, BlowPosition.Bottom),
        ["check"] = (0, 624, BlowPosition.Top, BlowPosition.Bottom),
        ["purge"] = (0, 728, BlowPosition.Top, BlowPosition.Bottom),
        ["live"] = (0, 832, BlowPosition.Top, BlowPosition.Bottom),
        ["windows"] = (290, 104, BlowPosition.Left, BlowPosition.Right),
        ["android"] = (290, 208, BlowPosition.Left, BlowPosition.Right),
        ["ios"] = (290, 312, BlowPosition.Left, BlowPosition.Right)
    };

    private BlowCanvas? canvas;
    private List<BlowNode> nodes = [];
    private List<BlowEdge> edges = [];
    private Dictionary<string, BlowRunState> runStates = [];
    private DeploymentStep? selected;
    private bool? lastRunSucceeded;
    private bool isLoading = true;
    private bool isFailed;
    private bool isRunning;
    private bool isNarrow;
    private bool simulateFailure;

    [AutoInject] private DeploymentService deploymentService = default!;

    private bool CanRun => isRunning is false && isLoading is false && isFailed is false;

    private string OutcomeKey => isRunning ? nameof(WorkbenchStrings.DeployRunning) : lastRunSucceeded switch
    {
        true => nameof(WorkbenchStrings.DeploySucceeded),
        false => nameof(WorkbenchStrings.DeployFailed),
        null => nameof(WorkbenchStrings.DeployNeverRun)
    };

    private string OutcomeIconName => isRunning ? BitIconName.Sync : lastRunSucceeded switch
    {
        true => BitIconName.CheckMark,
        false => BitIconName.Error,
        null => BitIconName.Clock
    };

    private BitColor OutcomeColor => isRunning ? BitColor.Info : lastRunSucceeded switch
    {
        true => BitColor.Success,
        false => BitColor.Error,
        null => BitColor.SecondaryForeground
    };

    protected override async Task OnInitializedAsync()
    {
        BuildGraph();

        await Load();
    }

    private async Task Load()
    {
        isLoading = true;
        isFailed = false;

        try
        {
            var hasLastRun = await deploymentService.HasLastRun(CurrentCancellationToken);

            runStates = hasLastRun ? DeploymentService.Steps.ToDictionary(step => step.Key, _ => new BlowRunState(BlowRunStatus.Succeeded)) : [];
            lastRunSucceeded = hasLastRun ? true : null;
            isLoading = false;
        }
        catch (WorkbenchSimulatedException)
        {
            isFailed = true;
            isLoading = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void BuildGraph()
    {
        var layout = isNarrow ? narrowLayout : wideLayout;

        nodes = [.. DeploymentService.Steps.Select(step =>
        {
            var (x, y, target, source) = layout[step.Key];

            return new BlowNode
            {
                Id = step.Key,
                Type = "step",
                Label = Localizer[step.TitleKey],
                Data = step,
                Width = 236,
                Height = 68,
                Position = new(x, y),
                TargetPosition = target,
                SourcePosition = source
            };
        })];

        edges = [.. DeploymentService.Steps.SelectMany(step => step.Next.Select(next => new BlowEdge
        {
            Id = $"{step.Key}-{next}",
            Source = step.Key,
            Target = next,
            SourceHandle = appSteps.Contains(next) ? AppsHandle : null,
            Type = BlowEdgeType.SmoothStep
        }))];
    }

    private BlowPosition AppsHandlePosition => isNarrow ? BlowPosition.Right : BlowPosition.Bottom;

    private void OnNarrowChanged(bool narrow)
    {
        if (narrow == isNarrow)
            return;

        isNarrow = narrow;
        BuildGraph();

        canvas?.UpdateNodeInternals();
        canvas?.FitView();
    }

    private void Select(BlowNode node) => selected = node.Data as DeploymentStep;

    private async Task Run()
    {
        isRunning = true;
        selected = null;

        var states = DeploymentService.Steps.ToDictionary(step => step.Key, _ => new BlowRunState(BlowRunStatus.Queued));
        DeploymentStep? failedStep = null;

        runStates = new(states);
        StateHasChanged();

        try
        {
            foreach (var stage in DeploymentService.Steps.GroupBy(step => step.Stage).OrderBy(group => group.Key))
            {
                var stageSteps = stage.ToArray();

                if (failedStep is not null)
                {
                    foreach (var step in stageSteps)
                    {
                        states[step.Key] = new(BlowRunStatus.Skipped);
                    }

                    continue;
                }

                foreach (var step in stageSteps)
                {
                    states[step.Key] = new(BlowRunStatus.Running);
                }

                runStates = new(states);
                StateHasChanged();

                await Task.WhenAll(stageSteps.Select(step => deploymentService.RunStep(step, CurrentCancellationToken)));

                foreach (var step in stageSteps)
                {
                    if (simulateFailure && step.Key == DeploymentService.FailingStepKey)
                    {
                        failedStep = step;
                        states[step.Key] = new(BlowRunStatus.Failed, Localizer[nameof(WorkbenchStrings.DeployCheckFailedMessage)]);
                    }
                    else
                    {
                        states[step.Key] = new(BlowRunStatus.Succeeded);
                    }
                }

                runStates = new(states);
                StateHasChanged();
            }

            lastRunSucceeded = failedStep is null;
            selected = failedStep;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            isRunning = false;
            runStates = new(states);
        }
    }
}
