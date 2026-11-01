namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>Thrown when so many captures wait for the headless browser that another one is turned away.</summary>
public sealed class WorkbenchCapturesBusyException() : Exception("Too many captures are waiting for the headless browser.")
{
}
