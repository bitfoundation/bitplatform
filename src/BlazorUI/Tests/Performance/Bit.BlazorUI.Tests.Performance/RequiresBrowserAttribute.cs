using System;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Keeps a browser test class out of a normal test run while leaving it discoverable, so that asking for it by
/// name still runs it: <c>RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~SomeBrowserTests</c>.
/// <see cref="IgnoreAttribute"/> cannot do that - it skips the class whatever the filter selected.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequiresBrowserAttribute : ConditionBaseAttribute
{
    public RequiresBrowserAttribute() : base(ConditionMode.Include)
    {
        IgnoreMessage = "Browser tests need a test host and the Playwright browsers, so they are opt-in: " +
                        "set RUN_BROWSER_TESTS=1 to run them.";
    }

    public override string GroupName => nameof(RequiresBrowserAttribute);

    public override bool IsConditionMet => Environment.GetEnvironmentVariable("RUN_BROWSER_TESTS") == "1";
}
