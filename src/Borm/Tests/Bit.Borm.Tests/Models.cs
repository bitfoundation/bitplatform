using System.ComponentModel.DataAnnotations;

namespace Bit.Borm.Tests;

public enum AccountType { Personal, Business }

/// <summary>Exercises [Display] labelling and ordering on enum members.</summary>
public enum Priority
{
    [Display(Name = "Low priority", Order = 2)] Low,
    [Display(Name = "High priority", Order = 1)] High,
    [Display(AutoGenerateField = false)] Internal
}

/// <summary>Exercises the [Flags] => multi-select path.</summary>
[Flags]
public enum Days
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4
}

public class Address
{
    [Required] public string Street { get; set; } = "";
    [Required] public string City { get; set; } = "";
    [RegularExpression(@"^\d{5}$")] public string Zip { get; set; } = "";
}

public class LineItem
{
    [Required] public string Product { get; set; } = "";
    [Range(1, 100)] public int Quantity { get; set; }
}

public class RegistrationModel
{
    [Required, Display(Name = "First name")]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Range(18, 120)]
    public int Age { get; set; }

    public AccountType AccountType { get; set; }

    public string? CompanyName { get; set; }

    public Address Address { get; set; } = new();

    public List<LineItem> Items { get; set; } = new();
}

/// <summary>Covers the value shapes the converter and the typed accessor have to survive.</summary>
public class TypedModel
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public int? OptionalCount { get; set; }
    public decimal Price { get; set; }
    public TimeSpan Duration { get; set; }
    public DateOnly Day { get; set; }
    public TimeOnly Moment { get; set; }
    public Guid Id { get; set; }
    public Uri? Website { get; set; }
    public Priority Priority { get; set; }
    public Days Availability { get; set; }
    public List<AccountType> Accounts { get; set; } = [];
    public string[] Tags { get; set; } = [];

    /// <summary>A computed property: shown, but never editable.</summary>
    public string Summary => $"{Name} x{Count}";
}

public class SignupModel
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
    [Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
    public bool IsBusiness { get; set; }
    public string? CompanyName { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
}

public class BookingModel
{
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
}
