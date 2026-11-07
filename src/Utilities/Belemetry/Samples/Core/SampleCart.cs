namespace Bit.Belemetry.Samples.Core;

/// <summary>The sample's application state: a cart, scoped to the user (a circuit on Blazor Server).</summary>
public sealed class SampleCart
{
    /// <summary>The products in the cart.</summary>
    public List<string> Items { get; } = [];

    /// <summary>The coupon code applied to the cart.</summary>
    public string Coupon { get; set; } = "WELCOME10";

    /// <summary>The cart owner's e-mail address.</summary>
    public string? Owner { get; set; } = "jane@example.com";
}
