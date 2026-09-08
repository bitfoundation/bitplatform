using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Tests.Components.Extras.ErrorBoundary;

/// <summary>
/// A logger that fails, so a test can pin that a failing logger does not take the error UI with it.
/// </summary>
public class ThrowingErrorBoundaryLogger : IErrorBoundaryLogger
{
    public ValueTask LogErrorAsync(Exception exception)
    {
        throw new InvalidOperationException("the logger failed");
    }
}
