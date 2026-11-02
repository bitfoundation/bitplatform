namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>The failure a data source's first load throws in the Error data state.</summary>
public sealed class WorkbenchSimulatedException(string simulatedSource) : Exception($"The workbench simulated a failure of '{simulatedSource}'.")
{
}
