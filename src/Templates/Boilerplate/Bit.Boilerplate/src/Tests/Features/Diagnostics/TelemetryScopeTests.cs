using Boilerplate.Client.Core.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.Contracts;

namespace Boilerplate.Tests.Features.Diagnostics;

/// <summary>
/// Everything <c>ClientExceptionHandlerBase.Handle</c> puts into a logging scope ends up in the process-wide
/// <c>DiagnosticLogger.Store</c>, in the support engineer's diagnostic modal, in the chatbot's <c>CheckLastError</c>
/// tool output and - when an exporter is configured - off the device entirely. Two properties of that dictionary are
/// worth pinning: building it must never throw, and it must not carry credentials.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class TelemetryScopeTests
{
    /// <summary>
    /// <c>ToDictionary</c> copies the caller's dictionary in first. Adding the telemetry keys with collection-initializer
    /// (<c>Add</c>) semantics therefore threw <c>ArgumentException</c> whenever a caller supplied one of the reserved
    /// names - from inside the exception handler, replacing the error being reported with an unrelated one.
    /// </summary>
    [TestMethod]
    public void ToDictionary_Should_NotThrow_WhenTheCallerSuppliesAReservedKey()
    {
        ITelemetryContext telemetryContext = new AppTelemetryContext { PageUrl = "https://localhost/products", UserId = Guid.CreateVersion7() };

        var data = telemetryContext.ToDictionary(new() { ["PageUrl"] = "supplied by the caller", ["Reason"] = "upload failed" });

        Assert.AreEqual("https://localhost/products", data["PageUrl"], "A caller supplied value won over the telemetry context's own.");
        Assert.AreEqual("upload failed", data["Reason"]);
        Assert.AreEqual(telemetryContext.UserId, data["UserId"]);
    }

    /// <summary>
    /// <c>PageUrl</c> is the current address verbatim, and this app's own identity routes carry single-use credentials
    /// in the query string - <c>?token=</c>, <c>?emailToken=</c>, <c>?phoneToken=</c>, <c>?otp=</c>.
    /// </summary>
    [TestMethod]
    public void AMaskedUrl_Should_KeepTheParameterNamesAndDropEveryValue()
    {
        var masked = new Uri("https://localhost/settings/account?email=someone@example.com&emailToken=123456").GetUrlWithMaskedQueryValues();

        Assert.AreEqual("https://localhost/settings/account?email=***&emailToken=***", masked);
    }

    [TestMethod]
    public void AMaskedUrl_Should_LeaveAQueryLessUrlAlone()
    {
        Assert.AreEqual("https://localhost/products", new Uri("https://localhost/products").GetUrlWithMaskedQueryValues());
    }
}
