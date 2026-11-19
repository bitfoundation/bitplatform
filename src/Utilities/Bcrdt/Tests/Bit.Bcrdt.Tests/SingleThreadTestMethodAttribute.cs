using System.Runtime.CompilerServices;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Marks a test that runs from start to finish on a <see cref="SingleThreadContext"/> of its own, the way code
/// in a Blazor component runs on the renderer's.
/// </summary>
/// <remarks>
/// <para>
/// For a test that keeps a replica connected and reads or edits its document: the providers it builds capture
/// the context and apply what they receive there, never while the test is between two <c>await</c>s. See
/// <see cref="SingleThreadContext"/> for the race it rules out. A test that never touches a connected
/// replica's document has no need of it, and keeps <see cref="TestMethodAttribute"/> so the provider's
/// context-free path stays covered.
/// </para>
/// <para>
/// The context outlives everything the test disposes, so nothing it awaits on the way out is posted to a
/// context that already shut down. Work posted to it that throws - which would otherwise end the test host -
/// fails the test instead.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SingleThreadTestMethodAttribute(
    [CallerFilePath] string callerFilePath = "",
    [CallerLineNumber] int callerLineNumber = -1) : TestMethodAttribute(callerFilePath, callerLineNumber)
{
    public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
    {
        var context = new SingleThreadContext();
        TestResult result;
        try
        {
            result = await context.InvokeAsync(() => testMethod.InvokeAsync(null)).ConfigureAwait(false);
        }
        finally
        {
            context.Dispose();
        }

        if (context.Faults.Count > 0)
        {
            var faults = new AggregateException("Work the test posted to its SingleThreadContext failed.", context.Faults);
            if (result.Outcome == UnitTestOutcome.Passed)
            {
                result.Outcome = UnitTestOutcome.Failed;
                result.TestFailureException = faults;
            }
            else
            {
                result.LogError += Environment.NewLine + faults;
            }
        }

        return [result];
    }
}
