namespace Bit.Butil;

/// <summary>
/// Shapes a <see cref="Proofreader"/> session.
/// </summary>
/// <remarks>
/// The explainer's <c>includeCorrectionTypes</c> and <c>includeCorrectionExplanations</c> are not
/// exposed: no shipping implementation supports them, so sending them would only promise labels and
/// explanations that never arrive.
/// </remarks>
public class ProofreaderOptions
{
    /// <summary>The languages the input will be in, as BCP 47 tags.</summary>
    public string[]? ExpectedInputLanguages { get; set; }
}
