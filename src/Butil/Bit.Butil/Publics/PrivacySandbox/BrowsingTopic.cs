namespace Bit.Butil;

/// <summary>
/// One coarse interest the browser is willing to share about this user, from the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Topics_API">Topics API</see>.
/// </summary>
/// <remarks>
/// A topic is an id in a public taxonomy ("/Arts &amp; Entertainment/Music"), derived by the browser
/// from the sites the user visited where <em>your</em> code was present - never a profile, an
/// identifier or a history. At most three come back per call, and only ones you had already observed
/// the user on.
/// </remarks>
public class BrowsingTopic
{
    /// <summary>The topic's id within the taxonomy named by <see cref="TaxonomyVersion"/>.</summary>
    public int Topic { get; set; }

    /// <summary>The combined version string - taxonomy, model and configuration together.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>The version of the browser's Topics configuration.</summary>
    public string ConfigVersion { get; set; } = string.Empty;

    /// <summary>The version of the classifier model that produced the topic.</summary>
    public string ModelVersion { get; set; } = string.Empty;

    /// <summary>The version of the taxonomy <see cref="Topic"/> indexes into.</summary>
    public string TaxonomyVersion { get; set; } = string.Empty;
}
