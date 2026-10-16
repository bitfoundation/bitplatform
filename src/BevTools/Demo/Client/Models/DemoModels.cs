using System.ComponentModel.DataAnnotations;

namespace Bit.BevTools.Demo.Client.Models;

public sealed record Product(int Id, string Name, string Category, decimal Price, int Stock, double Rating);

public sealed record Order(int Id, string Customer, decimal Total, string Status, DateTimeOffset PlacedAt);

public sealed record Profile(string UserName, string Email, string AccessToken, string Plan);

public sealed class CartLine
{
    public required Product Product { get; init; }

    public int Quantity { get; set; }

    public decimal LineTotal => Product.Price * Quantity;
}

public sealed class CheckoutModel
{
    [Required, StringLength(60, MinimumLength = 2)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(120)]
    public string Address { get; set; } = "";

    [Required, RegularExpression(@"^\d{4,10}$", ErrorMessage = "Postal code must be 4-10 digits.")]
    public string PostalCode { get; set; } = "";

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms.")]
    public bool AcceptTerms { get; set; }

    public string? Notes { get; set; }
}

public sealed record ThemeSettings(string Accent, bool Dense, int Version);

/// <summary>Cascaded to the whole dashboard; changing it re-renders every subscriber (intentional cascade demo).</summary>
public sealed class DashboardSettings
{
    public string Accent { get; set; } = "#0969da";

    public bool Dense { get; set; }

    public int Version { get; set; }
}
