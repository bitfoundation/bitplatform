using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Tests.Components.Extras.ErrorBoundary;

/// <summary>
/// Stands in for the logger every Blazor host registers, so a test can observe that the boundary logs
/// what it caught exactly as the framework's own boundary does.
/// </summary>
public class FakeErrorBoundaryLogger : IErrorBoundaryLogger
{
    public List<Exception> Logged { get; } = [];

    public ValueTask LogErrorAsync(Exception exception)
    {
        Logged.Add(exception);

        return ValueTask.CompletedTask;
    }
}
