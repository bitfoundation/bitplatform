namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>How a view's data loads behave; Mixed gives each data source a state of its own.</summary>
public enum WorkbenchDataState
{
    Live,
    Loading,
    Empty,
    Error,
    Mixed,
    Slow
}
