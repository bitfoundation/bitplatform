using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm.Demo.Client.Forms;

public enum AccountKind { Personal, Business }
public enum ContactMethod { Email, Phone, Mail }

/// <summary>Model for the "from C# type" + conditional visibility demo.</summary>
public class AccountSignup
{
    [Required, Display(Name = "Full name"), StringLength(60, MinimumLength = 2)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Display(Name = "Account type")]
    public AccountKind AccountKind { get; set; }

    [Display(Name = "Company name")]
    public string? CompanyName { get; set; }

    [Display(Name = "VAT number")]
    public string? VatNumber { get; set; }

    [Display(Name = "I accept the terms and conditions")]
    public bool AcceptTerms { get; set; }
}

/// <summary>
/// Model for the "settled answers" demo: a booking that locks itself once it has been confirmed, and
/// whose values are tidied up on the way out.
/// </summary>
public class BookingRequest
{
    [Display(Name = "Booking reference")]
    public string? Reference { get; set; }

    [Required, Display(Name = "Contact email")]
    public string ContactEmail { get; set; } = "";

    [Display(Name = "Arrival")]
    public DateOnly? Arrival { get; set; }

    [Display(Name = "Check-in time")]
    public TimeOnly? CheckIn { get; set; }

    [Display(Name = "This booking is confirmed")]
    public bool Confirmed { get; set; }
}

/// <summary>Model for the array/repeater demo: an invoice with line items.</summary>
public class Invoice
{
    [Required, Display(Name = "Customer")]
    public string Customer { get; set; } = "";

    public List<InvoiceLine> Lines { get; set; } = new();

    /// <summary>Derived by the schema from <see cref="Lines"/>, not typed by the user.</summary>
    [Display(Name = "Invoice total")]
    public decimal Total { get; set; }
}

public class InvoiceLine
{
    [Required]
    public string Description { get; set; } = "";

    [Range(1, 9999)]
    public int Quantity { get; set; } = 1;

    [Range(0, 1_000_000), Display(Name = "Unit price")]
    public decimal UnitPrice { get; set; }

    /// <summary>Derived from <see cref="Quantity"/> and <see cref="UnitPrice"/>.</summary>
    [Display(Name = "Line total")]
    public decimal LineTotal { get; set; }
}

/// <summary>Model for the FluentValidation demo.</summary>
public class JobApplication
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int YearsExperience { get; set; }
    [Display(Name = "Desired salary")]
    public int DesiredSalary { get; set; }
    [Display(Name = "Why do you want this role?")]
    public string Motivation { get; set; } = "";
}

/// <summary>Days a candidate is available. A [Flags] enum renders as a multi-select.</summary>
[Flags]
public enum Weekdays
{
    None = 0,
    [Display(Name = "Mon")] Monday = 1,
    [Display(Name = "Tue")] Tuesday = 2,
    [Display(Name = "Wed")] Wednesday = 4,
    [Display(Name = "Thu")] Thursday = 8,
    [Display(Name = "Fri")] Friday = 16
}

/// <summary>Model for the file upload demo.</summary>
public class DocumentUpload
{
    [Required, Display(Name = "Applicant")]
    public string Applicant { get; set; } = "";

    [Display(Name = "CV (PDF or Word, max 2 MB)")]
    public IBrowserFile? Resume { get; set; }

    [Display(Name = "Supporting documents")]
    public List<IBrowserFile> Attachments { get; set; } = new();
}

/// <summary>Model for the cascading-options demo.</summary>
public class DeliveryAddress
{
    [Required] public string Country { get; set; } = "";
    [Required] public string City { get; set; } = "";
    [Display(Name = "Delivery notes")] public string? Notes { get; set; }
    [Display(Name = "Business delivery")] public bool IsBusiness { get; set; }
    [Display(Name = "Company name")] public string? CompanyName { get; set; }
}

/// <summary>Model for the multi-column layout demo.</summary>
public class Profile
{
    [Required, Display(Name = "First name")] public string FirstName { get; set; } = "";
    [Required, Display(Name = "Last name")] public string LastName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Phone] public string? Phone { get; set; }
    [Display(Name = "Available on")] public Weekdays Availability { get; set; }

    /// <summary>
    /// A plain collection of enum members — no <c>[Flags]</c> — and still a set of tick boxes rather
    /// than a repeater of dropdowns, because the answers are a closed set either way.
    /// </summary>
    [Display(Name = "Preferred contact")] public List<ContactMethod> ContactMethods { get; set; } = [];

    public PostalAddress Address { get; set; } = new();
    public string? Bio { get; set; }
}

public class PostalAddress
{
    [Required] public string Street { get; set; } = "";
    [Required] public string City { get; set; } = "";
    [Required, Display(Name = "Post code")] public string PostCode { get; set; } = "";
    [Required] public string Country { get; set; } = "";
}

/// <summary>Model for the validation-modes demo. Uses [Compare] for the confirm-password rule.</summary>
public class NewAccount
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password), MinLength(8)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    [Display(Name = "Username")]
    public string Username { get; set; } = "";
}

/// <summary>
/// Model for the dependent-fields demo. Every rule here reads a value from a <em>different</em> field,
/// which is the case a form engine gets wrong by default: the rule runs when its own field changes and
/// never again, so correcting the other one leaves the complaint on screen.
/// </summary>
public class TripRequest
{
    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required, EmailAddress, Display(Name = "Confirm email")]
    [Compare(nameof(Email), ErrorMessage = "The two addresses do not match.")]
    public string ConfirmEmail { get; set; } = "";

    [Display(Name = "Leaving on")]
    public DateOnly? Start { get; set; }

    [Display(Name = "Coming back on")]
    public DateOnly? End { get; set; }

    [Display(Name = "Travellers")]
    public int Travellers { get; set; } = 1;

    [Display(Name = "Rooms booked")]
    public int Rooms { get; set; } = 1;

    [Display(Name = "Taking a hire car")]
    public bool HireCar { get; set; }

    [Display(Name = "Hire car days")]
    public int? CarDays { get; set; }

    [Display(Name = "Estimated cost")]
    public decimal Estimate { get; set; }
}

/// <summary>Model for the field-polish demo: affixes, counters, live updates and static sections.</summary>
public class ListingDraft
{
    [Display(Name = "Listing title")]
    public string Title { get; set; } = "";

    [Display(Name = "Summary")]
    public string Summary { get; set; } = "";

    [Display(Name = "Monthly price")]
    public decimal Price { get; set; }

    [Display(Name = "Floor area")]
    public int FloorArea { get; set; }

    [Display(Name = "City")]
    public string City { get; set; } = "";

    [Display(Name = "Published")]
    public bool Published { get; set; }

    [Display(Name = "Edit passcode")]
    public string Passcode { get; set; } = "";
}

/// <summary>Model for the combobox / tags / change-handler demo.</summary>
public class TalentProfile
{
    [Required, Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Display(Name = "Country")]
    public string? Country { get; set; }

    [Display(Name = "City")]
    public string? City { get; set; }

    [Display(Name = "Plan")]
    public string Plan { get; set; } = "solo";

    [Display(Name = "Seats")]
    public int Seats { get; set; } = 1;

    [Display(Name = "Skills")]
    public List<string> Skills { get; set; } = new();
}

/// <summary>
/// Model for the DataAnnotations demo. The attributes become field rules through the generator; the
/// <see cref="IValidatableObject"/> implementation is the part no attribute can express, and the part
/// the integration exists for.
/// </summary>
public class RoomBooking : IValidatableObject
{
    [Required, Display(Name = "Guest name")]
    public string GuestName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Display(Name = "Check in")] public DateOnly CheckIn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Display(Name = "Check out")] public DateOnly CheckOut { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Range(1, 8)] public int Guests { get; set; } = 1;

    [Display(Name = "Room type")] public string RoomType { get; set; } = "single";

    /// <summary>The rules that need more than one property, which is what an attribute cannot see.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckOut <= CheckIn)
            yield return new ValidationResult("Check out must be after check in.", [nameof(CheckOut)]);

        if (CheckIn < DateOnly.FromDateTime(DateTime.Today))
            yield return new ValidationResult("Check in cannot be in the past.", [nameof(CheckIn)]);

        var capacity = RoomType switch { "single" => 1, "double" => 2, _ => 4 };
        if (Guests > capacity)
            yield return new ValidationResult(
                $"A {RoomType} room sleeps {capacity}.", [nameof(Guests), nameof(RoomType)]);

        if ((CheckOut.DayNumber - CheckIn.DayNumber) > 28)
            yield return new ValidationResult("Stays longer than 28 nights have to be booked by phone.");
    }
}

/// <summary>Model for the per-row conditions demo: each contact row asks for a different field.</summary>
public class ContactSheet
{
    [Required, Display(Name = "Sheet name")]
    public string Name { get; set; } = "";

    public List<ContactEntry> Contacts { get; set; } = new();
}

public class ContactEntry
{
    [Display(Name = "Reach them by")]
    public string Kind { get; set; } = "email";

    [Display(Name = "Email address")]
    public string? Email { get; set; }

    [Display(Name = "Phone number")]
    public string? Phone { get; set; }

    [Display(Name = "Postal address")]
    public string? Address { get; set; }
}

/// <summary>
/// Model for the composition demo: named groups, a page-owned layout, per-field undo and an options
/// provider that is allowed to fail.
/// </summary>
public class SupportTicket
{
    [Required, Display(Name = "Your name", GroupName = "Who you are")]
    public string ReporterName { get; set; } = "";

    [Required, EmailAddress, Display(Name = "Email", GroupName = "Who you are")]
    public string ReporterEmail { get; set; } = "";

    [Display(Name = "Team", GroupName = "Where it belongs")]
    public string? Team { get; set; }

    [Display(Name = "Component", GroupName = "Where it belongs")]
    public string? Component { get; set; }

    [Display(Name = "Related ticket")]
    public string? RelatedTicket { get; set; }

    [Required, Display(Name = "Subject")]
    public string Subject { get; set; } = "";

    [Display(Name = "What happened?")]
    public string Details { get; set; } = "";
}

/// <summary>Model for the expressions demo: a quote whose totals, conditions and rules are written as text.</summary>
public class Quote
{
    public string Customer { get; set; } = "";

    [Display(Name = "Customer type")]
    public string CustomerType { get; set; } = "person";

    [Display(Name = "VAT number")]
    public string? VatNumber { get; set; }

    public List<QuoteLine> Lines { get; set; } = [new()];

    public decimal? Discount { get; set; }

    /// <summary>Derived by the schema's expression, not typed by the user.</summary>
    public decimal Total { get; set; }

    [Display(Name = "Approved by")]
    public string? Approver { get; set; }

    [Display(Name = "Issued on")]
    public DateOnly? IssuedOn { get; set; }

    [Display(Name = "Valid until")]
    public DateOnly? ValidUntil { get; set; }
}

public class QuoteLine
{
    public string Product { get; set; } = "";

    public int Quantity { get; set; } = 1;

    [Display(Name = "Unit price")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Line total")]
    public decimal LineTotal { get; set; }
}

/// <summary>Model for the tabs and review demo: a customer record edited in any order rather than walked through.</summary>
public class CustomerRecord
{
    public string Name { get; set; } = "";

    public string Email { get; set; } = "";

    public string? Phone { get; set; }

    public string Plan { get; set; } = "team";

    public int Seats { get; set; } = 5;

    /// <summary>A month picker's value: the first day of the month chosen.</summary>
    [Display(Name = "Billing from")]
    public DateOnly? BillingFrom { get; set; }

    [Display(Name = "Paperless invoices")]
    public bool Paperless { get; set; } = true;

    /// <summary>A week picker's value, kept as the picker's own text (2026-W44).</summary>
    [Display(Name = "Kick-off week")]
    public string? KickoffWeek { get; set; }

    [Display(Name = "Notes for the team")]
    public string? Notes { get; set; }
}

/// <summary>Model for the drafts demo: a form long enough that losing it halfway through would hurt.</summary>
public class MembershipApplication
{
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    [Display(Name = "Membership")]
    public string Tier { get; set; } = "standard";

    [Display(Name = "Why do you want to join?")]
    public string Motivation { get; set; } = "";

    /// <summary>Marked sensitive by the schema, so no draft ever holds it.</summary>
    [Display(Name = "IBAN")]
    public string? Iban { get; set; }

    [Display(Name = "Choose a password")]
    public string Password { get; set; } = "";
}

/// <summary>Model for the search lookup demo: a deal that names one customer out of thousands.</summary>
public class DealAssignment
{
    [Display(Name = "Deal")]
    public string Title { get; set; } = "";

    [Display(Name = "Customer")]
    public string? CustomerId { get; set; }

    [Display(Name = "Account manager")]
    public string? ManagerId { get; set; }
}

/// <summary>Model for the localization demo.</summary>
public class TeamOrder
{
    public string Company { get; set; } = "";

    public string Plan { get; set; } = "team";

    public int Seats { get; set; } = 5;

    public string? Notes { get; set; }
}

/// <summary>Model for the Blazor inputs demo: a card some of whose fields are edited by Blazor's own InputText and InputNumber.</summary>
public class ContactCard
{
    public string Name { get; set; } = "";

    public string Email { get; set; } = "";

    public int Age { get; set; }

    public ContactCardAddress Address { get; set; } = new();
}

public class ContactCardAddress
{
    public string City { get; set; } = "";
}

/// <summary>One of the many customers the search lookup demo looks through.</summary>
public sealed record DemoCustomer(string Id, string Name, string City);

/// <summary>Stand-in for a repository the demo's async rules and option providers call.</summary>
public sealed class DemoDirectory
{
    private static readonly string[] TakenUsernames = ["admin", "root", "ada", "grace"];

    private static readonly Dictionary<string, string[]> CitiesByCountry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fr"] = ["Paris", "Lyon", "Marseille"],
        ["gb"] = ["London", "Manchester", "Bristol"],
        ["de"] = ["Berlin", "Hamburg", "Munich"],
        ["jp"] = ["Tokyo", "Osaka", "Kyoto"]
    };

    public static IReadOnlyList<BormSelectOption> Countries =>
    [
        new("fr", "France"), new("gb", "United Kingdom"), new("de", "Germany"), new("jp", "Japan")
    ];

    /// <summary>Simulates the latency of a real lookup so the loading state is visible.</summary>
    public async ValueTask<IEnumerable<string>> GetCitiesAsync(string? countryCode, CancellationToken ct = default)
    {
        await Task.Delay(400, ct);
        return countryCode is not null && CitiesByCountry.TryGetValue(countryCode, out var cities)
            ? cities
            : [];
    }

    public async ValueTask<bool> IsUsernameAvailableAsync(string username, CancellationToken ct = default)
    {
        await Task.Delay(400, ct);
        return !TakenUsernames.Contains(username, StringComparer.OrdinalIgnoreCase);
    }

    private static readonly string[] CompanyNames =
        ["Northwind", "Contoso", "Fabrikam", "Tailspin", "Wingtip", "Litware", "Adventure Works", "Proseware",
         "Woodgrove", "Lucerne", "Alpine Ski", "Coho", "Datum", "Humongous", "Trey", "Blue Yonder"];

    private static readonly string[] CompanyKinds =
        ["Traders", "Logistics", "Bank", "Travel", "Toys", "Foods", "Studios", "Labs", "Publishing", "Energy"];

    private static readonly string[] CustomerCities =
        ["Paris", "Lyon", "London", "Bristol", "Berlin", "Hamburg", "Munich", "Tokyo", "Osaka", "Madrid",
         "Lisbon", "Oslo", "Vienna", "Zurich", "Dublin"];

    /// <summary>Far too many to put in a dropdown: 2,400 customers.</summary>
    private static readonly DemoCustomer[] Customers =
    [
        .. (from name in CompanyNames
            from kind in CompanyKinds
            from city in CustomerCities
            select (Name: $"{name} {kind}", City: city))
           .Select((c, i) => new DemoCustomer($"C-{i + 1:0000}", c.Name, c.City))
    ];

    private static readonly string[] Managers =
        ["Ada Lovelace", "Alan Turing", "Barbara Liskov", "Donald Knuth", "Edsger Dijkstra", "Frances Allen",
         "Grace Hopper", "John Backus", "Ken Thompson", "Margaret Hamilton", "Niklaus Wirth", "Radia Perlman"];

    /// <summary>
    /// A customer search as a server would run it: the first matches for the text, or - with no text and
    /// a current value, as an edit form opens - the one customer the stored id names.
    /// </summary>
    public async ValueTask<IEnumerable<DemoCustomer>> SearchCustomersAsync(string? text, string? currentId, CancellationToken ct = default)
    {
        await Task.Delay(250, ct);
        if (string.IsNullOrWhiteSpace(text))
            return currentId is null ? [] : Customers.Where(c => c.Id == currentId);

        // Matched against what the box shows, too: text committed before its results arrived is searched for
        // exactly, and a server that only knew names and cities apart would never find "Contoso Foods · Lyon".
        return Customers
            .Where(c => LabelOf(c).Contains(text.Trim(), StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToList();
    }

    /// <summary>How the search lookup demo shows a customer.</summary>
    public static string LabelOf(DemoCustomer customer) => $"{customer.Name} · {customer.City}";

    /// <summary>Account managers: few enough that an empty search may as well list the first of them.</summary>
    public async ValueTask<IEnumerable<string>> SearchManagersAsync(string? text, CancellationToken ct = default)
    {
        await Task.Delay(150, ct);
        return Managers
            .Where(m => string.IsNullOrWhiteSpace(text) || m.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .ToList();
    }

    public static IReadOnlyList<BormSelectOption> Teams =>
    [
        new("platform", "Platform"), new("billing", "Billing"), new("mobile", "Mobile")
    ];

    /// <summary>
    /// A lookup that fails on demand, so the composition demo can show what a form does when the
    /// service behind a dropdown is down — which is the interesting case, and the one nobody demos.
    /// </summary>
    public async ValueTask<IEnumerable<string>> GetComponentsAsync(string? team, bool fail, CancellationToken ct = default)
    {
        await Task.Delay(300, ct);
        if (fail) throw new InvalidOperationException("The component service is unavailable.");

        return team switch
        {
            "platform" => ["API", "Scheduler", "Auth"],
            "billing" => ["Invoicing", "Payments"],
            "mobile" => ["iOS app", "Android app"],
            _ => []
        };
    }
}
