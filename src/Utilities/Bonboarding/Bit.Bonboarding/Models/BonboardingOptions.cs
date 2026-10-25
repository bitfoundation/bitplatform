namespace Bit.Bonboarding;

/// <summary>
/// Application-wide defaults, configured through
/// <c>services.AddBitBonboardingServices(options =&gt; ...)</c>. Anything left <see langword="null"/>
/// falls back to <see cref="BonboardingDefaults"/>.
/// </summary>
public sealed class BonboardingOptions : BonboardingStepOptions
{
    /// <summary>Default user-visible strings. Overridable per tour and per step.</summary>
    public BonboardingLabels Labels { get; set; } = new();

    /// <summary>
    /// Whether tour progress and completion are written to the registered
    /// <see cref="IBonboardingStore"/>. Default <see langword="true"/>.
    /// </summary>
    public bool Persist { get; set; } = true;

    /// <summary>Key prefix used by storage implementations. Default <c>"bit-bonboarding:"</c>.</summary>
    public string StorageKeyPrefix { get; set; } = "bit-bonboarding:";

    /// <summary>
    /// Which browser storage the default store uses. Default <see cref="BonboardingStorageLocation.Local"/>.
    /// </summary>
    public BonboardingStorageLocation StorageLocation { get; set; } = BonboardingStorageLocation.Local;

    /// <summary>
    /// Keeps each user's progress apart on a shared device, or for an application where several accounts can be
    /// signed in: returns who the progress belongs to (a user id, say), and the default store keys every record
    /// with it. Asked on every read and write, so return a cached value. <see langword="null"/> (the default, and
    /// the result while nobody is signed in) stores progress for the device as a whole.
    /// </summary>
    /// <example>
    /// <code>
    /// options.StorageScope = async (services, ct) =&gt;
    ///     (await services.GetRequiredService&lt;AuthenticationStateProvider&gt;().GetAuthenticationStateAsync())
    ///         .User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    /// </code>
    /// </example>
    public Func<IServiceProvider, CancellationToken, ValueTask<string?>>? StorageScope { get; set; }

    /// <summary>
    /// When a tour spans routes and a step declares a <see cref="BonboardingStepDefinition.Route"/>, navigate
    /// there automatically. Default <see langword="true"/>.
    /// </summary>
    public bool AutoNavigateToStepRoute { get; set; } = true;

    /// <summary>
    /// Emitted diagnostics verbosity guard: when false the library never touches
    /// <c>console</c> from JavaScript. Default <see langword="false"/>.
    /// </summary>
    public bool EnableJsDiagnostics { get; set; }
}
