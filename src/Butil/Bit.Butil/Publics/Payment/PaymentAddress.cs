namespace Bit.Butil;

/// <summary>
/// A physical address as the payment sheet returns it. Which fields are filled in depends on the
/// country and on what the user's wallet holds, so treat every one of them as optional.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentAddress">PaymentAddress</see>
/// </summary>
public class PaymentAddress
{
    /// <summary>The street lines, in the order they should be printed.</summary>
    public string[] AddressLine { get; set; } = [];

    /// <summary>The country as an ISO 3166-1 alpha-2 code, e.g. <c>"US"</c>.</summary>
    public string? Country { get; set; }

    /// <summary>The city or town.</summary>
    public string? City { get; set; }

    /// <summary>The top-level administrative subdivision - a state, province or county.</summary>
    public string? Region { get; set; }

    /// <summary>The postal or ZIP code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>A neighbourhood or district within the city, where the country uses one.</summary>
    public string? DependentLocality { get; set; }

    /// <summary>The sorting code - France's CEDEX, for example.</summary>
    public string? SortingCode { get; set; }

    /// <summary>The organization or company at the address.</summary>
    public string? Organization { get; set; }

    /// <summary>The person the delivery is addressed to.</summary>
    public string? Recipient { get; set; }

    /// <summary>The phone number attached to the address.</summary>
    public string? Phone { get; set; }
}
