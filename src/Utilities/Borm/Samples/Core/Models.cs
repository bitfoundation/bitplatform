using System.ComponentModel.DataAnnotations;

namespace Bit.Borm.Samples.Core;

// Plain models: nothing in them knows about Borm, or about the host the form runs in.

public enum Plan { Free, Team, Enterprise }

/// <summary>The "from a type" sample: every field, label and rule comes from this class and its DataAnnotations.</summary>
public class Signup
{
    [Required, Display(Name = "Full name"), StringLength(60, MinimumLength = 2)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    public Plan Plan { get; set; }

    [Range(1, 500)]
    public int Seats { get; set; } = 1;

    [Display(Name = "Start date")]
    public DateOnly? StartDate { get; set; }

    [Required, Display(Name = "I accept the terms")]
    public bool AcceptTerms { get; set; }
}

public enum AccountKind { Personal, Business }

/// <summary>The builder sample: conditions, an async rule and a custom renderer, configured field by field.</summary>
public class Profile
{
    public string Username { get; set; } = "";

    public AccountKind AccountKind { get; set; }

    [Display(Name = "Company name")]
    public string? CompanyName { get; set; }

    [Display(Name = "How do you like it so far?")]
    public int Rating { get; set; }

    [Display(Name = "What should we improve?")]
    public string? Improvement { get; set; }
}

public enum TripKind { Leisure, Business }

/// <summary>The wizard sample: four steps, one of them only for business trips, and a review at the end.</summary>
public class TripBooking
{
    [Required, Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    public string Destination { get; set; } = "";

    public DateOnly? Departure { get; set; }

    [Display(Name = "Return")]
    public DateOnly? ReturnDate { get; set; }

    [Display(Name = "Kind of trip")]
    public TripKind Kind { get; set; }

    [Display(Name = "Cost centre")]
    public string? CostCentre { get; set; }

    [Display(Name = "Purchase order")]
    public string? PurchaseOrder { get; set; }
}

/// <summary>The static form sample: bound from the post by <c>[SupplyParameterFromForm]</c>.</summary>
public class VolunteerApplication
{
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    [Display(Name = "Hours a week")]
    public int Hours { get; set; }

    [Display(Name = "Where you'd like to help")]
    public string Team { get; set; } = "";

    [Display(Name = "Send me the newsletter")]
    public bool Newsletter { get; set; }
}
