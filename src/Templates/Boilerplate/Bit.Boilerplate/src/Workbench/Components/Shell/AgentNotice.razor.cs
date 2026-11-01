namespace Boilerplate.Workbench.Components.Shell;

/// <summary>Says on every Workbench page that no agent is connected, so what a visitor sees is a limited experience.</summary>
public partial class AgentNotice
{
    [AutoInject] private WorkbenchReviewSession reviewSession = default!;
}
