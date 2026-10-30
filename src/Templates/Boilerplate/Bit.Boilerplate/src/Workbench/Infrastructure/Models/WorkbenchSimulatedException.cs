namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed class WorkbenchSimulatedException(string simulatedSource) : Exception($"The workbench simulated a failure of '{simulatedSource}'.")
{
    public string SimulatedSource { get; } = simulatedSource;
}
