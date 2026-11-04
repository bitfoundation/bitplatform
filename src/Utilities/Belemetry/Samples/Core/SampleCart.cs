namespace Bit.Belemetry.Samples.Core;

/// <summary>The sample's application state: a cart, scoped to the user (a circuit on Blazor Server).</summary>
public sealed class SampleCart
{
    public List<string> Items { get; } = [];

    public string Coupon { get; set; } = "WELCOME10";

    public string? Owner { get; set; } = "jane@example.com";
}
