namespace Boilerplate.Tests.Features.ErrorHandling;

/// <summary>
/// <c>AppProblemDetails</c>' implicit conversion to <see cref="Exception"/> is called from
/// <c>ExceptionDelegatingHandler</c>, i.e. from inside the client's own error handling. Anything it throws replaces
/// the real failure - the localized message the user was supposed to read - with a reflection exception in an
/// interrupting message box, and records that manufactured exception in telemetry instead of the real one.
/// <para>
/// The conversion runs on a body the server produced today, but the template's API is meant to sit behind whatever
/// a project puts in front of it, and it reconstructs types by name through <c>Activator.CreateInstance</c>. So the
/// contract asserted here is: any body at all, degrading to <c>UnknownException</c> rather than throwing.
/// </para>
/// <para>
/// Pure unit tests - the conversion touches nothing but reflection over <c>Boilerplate.Shared</c>.
/// </para>
/// </summary>
[TestClass]
public class ProblemDetailsConversionTests
{
    /// <summary>
    /// Each of these threw before: a missing <c>type</c> hit <c>Assembly.GetType(null)</c>'s
    /// <c>ArgumentNullException</c>, a missing <c>key</c> hit a <c>NullReferenceException</c>, and a <c>type</c>
    /// naming something without the expected constructor - an abstract exception, a non-exception type the fuzzy
    /// <c>EndsWith</c> fallback matched, or <c>TransientException</c>, which only offers the <c>(string)</c>
    /// overload - hit a <c>MissingMethodException</c>.
    /// </summary>
    [TestMethod]
    [DataRow(null, "SomeKey", "a body with no type member")]
    [DataRow("https://tools.ietf.org/html/rfc7231#section-6.5.4", "SomeKey", "an RFC url, which is what ASP.NET Core's own problem details carry")]
    [DataRow("Boilerplate.Shared.Infrastructure.Exceptions.BadRequestException", null, "a body with no key member")]
    [DataRow("Boilerplate.Shared.Infrastructure.Exceptions.KnownException", "SomeKey", "the abstract base type")]
    [DataRow("Boilerplate.Shared.Infrastructure.Exceptions.ErrorResourcePayload", "SomeKey", "an exported type that is not an exception")]
    [DataRow("Boilerplate.Shared.Infrastructure.Exceptions.TransientException", "SomeKey", "a type with no (LocalizedString) constructor")]
    [DataRow("", "", "empty strings")]
    public void AnyProblemDetails_Should_ConvertWithoutThrowing(string? type, string? key, string because)
    {
        var problemDetails = new AppProblemDetails { Type = type, Key = key, Title = "Something went wrong", Status = 400 };

        Exception exception = problemDetails; // The conversion under test.

        Assert.IsNotNull(exception, $"The conversion returned null for {because}.");
        Assert.AreEqual("Something went wrong", exception.Message,
            $"Whatever type it degrades to, the server's message must survive - it is the only thing the user reads. Input: {because}.");
    }

    /// <summary>
    /// The degradation must stay a degradation: a body the server actually produces has to come back as its own
    /// type, otherwise every <c>catch (ResourceNotFoundException)</c> and every <c>e.Key == nameof(...)</c> on the
    /// client stops matching and the guard above would be passing by turning everything into UnknownException.
    /// </summary>
    [TestMethod]
    public void AServerProducedBody_Should_ConvertToItsOwnExceptionType()
    {
        var problemDetails = new AppProblemDetails
        {
            Type = typeof(ResourceNotFoundException).FullName,
            Key = nameof(ResourceNotFoundException),
            Title = "User not found",
            Status = 404,
            Extensions = { { "traceId", "0HN7ABCDEF:00000001" } }
        };

        Exception exception = problemDetails;

        Assert.IsInstanceOfType<ResourceNotFoundException>(exception);
        Assert.AreEqual(nameof(ResourceNotFoundException), ((ResourceNotFoundException)exception).Key);
        Assert.AreEqual("User not found", exception.Message);
        Assert.AreEqual("0HN7ABCDEF:00000001", exception.Data["traceId"], "The trace id is what correlates the client report with the server log.");
    }

    /// <summary>
    /// <c>Key</c> is the stable resx key, and it is what the sign-in, sign-up and forgot-password panels compare
    /// against a <c>nameof(AppStrings.X)</c> to decide which panel to show. <c>ResourceValidationException</c> is
    /// built through its own <c>(string message, ErrorResourcePayload?)</c> constructor, which sets <c>Key</c> to
    /// the message - so without an explicit assignment its Key is the LOCALIZED sentence and those comparisons
    /// silently stop matching for anyone not running in English.
    /// </summary>
    [TestMethod]
    public void AReconstructedValidationException_Should_KeepTheStableKeyRatherThanTheLocalizedTitle()
    {
        var problemDetails = new AppProblemDetails
        {
            Type = typeof(ResourceValidationException).FullName,
            Key = nameof(ResourceValidationException),
            Title = "One or more validation errors occurred.", // The LOCALIZED sentence, which is the whole point.
            Status = 400
        };

        Exception exception = problemDetails;

        Assert.IsInstanceOfType<ResourceValidationException>(exception);
        Assert.AreEqual(nameof(ResourceValidationException), ((ResourceValidationException)exception).Key,
            "The reconstructed Key is culture dependent, so a client-side `e.Key == nameof(...)` works in one language and not another.");
    }
}
