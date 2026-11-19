namespace Bit.Bortable;

/// <summary>One <c>AddBitBortableServices(options =&gt; ...)</c> call's configuration, kept until a scope builds its options.</summary>
internal sealed class BortableConfiguration(Action<BortableOptions> configure)
{
    public void Apply(BortableOptions options) => configure(options);
}
