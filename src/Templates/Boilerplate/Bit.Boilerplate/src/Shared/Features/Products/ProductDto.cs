//+:cnd:noEmit
using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Shared.Features.Products;

[DtoResourceType(typeof(AppStrings))]
public partial class ProductDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// The product's ShortId is used to create a more human-friendly URL.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int ShortId { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MaxLength(64, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    [Display(Name = nameof(AppStrings.Name))]
    public string? Name { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Range(0, double.MaxValue, ErrorMessage = nameof(AppStrings.RangeAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Price))]
    public decimal Price { get; set; }

    /// <summary>
    /// The ISO 4217 code of the currency <see cref="Price"/> is an amount of. Null falls back to USD.
    /// </summary>
    [MaxLength(3, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    public string? CurrencyIso { get; set; }

    /// <summary>
    /// The symbol <see cref="FormattedPrice"/> renders <see cref="Price"/> with - stored alongside
    /// <see cref="CurrencyIso"/> so no currency-to-symbol table has to live in code. Null falls back to
    /// <see cref="CurrencyIso"/>, then to $.
    /// </summary>
    [MaxLength(8, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    public string? CurrencySymbol { get; set; }

    [MaxLength(4096, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    [Display(Name = nameof(AppStrings.Description))]
    public string? DescriptionHTML { get; set; }

    [MaxLength(4096, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    public string? DescriptionText { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Category))]
    public Guid? CategoryId { get; set; }

    [Display(Name = nameof(AppStrings.Category))]
    public string? CategoryName { get; set; }

    public long Version { get; set; }

    public bool HasPrimaryImage { get; set; } = false;

    [Display(Name = nameof(AppStrings.AltText))]
    public string? PrimaryImageAltText { get; set; }

    public string? GetPrimaryMediumImageUrl(Uri absoluteServerAddress)
    {
        return HasPrimaryImage is false
            ? null
            : new Uri(absoluteServerAddress, $"/api/v1/Attachment/GetAttachment/{Id}/{AttachmentKind.ProductPrimaryImageMedium}?v={Version}").ToString();
    }

    [JsonIgnore]
    public string FormattedPrice => FormatPrice();

    /// <summary>
    /// The viewer's culture decides digits, separators and symbol placement - never the currency itself: formatting
    /// with the culture's own currency symbol would re-denominate the price, not translate it.
    /// </summary>
    private string FormatPrice()
    {
        var currencySymbol = CurrencySymbol ?? CurrencyIso ?? "$";

        if (CultureInfoManager.InvariantGlobalization)
            return $"{currencySymbol}{Price:N2}";

        var numberFormat = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        numberFormat.CurrencySymbol = currencySymbol;

        return CultureInfo.CurrentCulture.TextInfo.IsRightToLeft
                ? $"{Price.ToString($"N{numberFormat.CurrencyDecimalDigits}")} {currencySymbol}"
                : Price.ToString("C", numberFormat);
    }

    //#if (module == "Sales")
    public string PageUrl => $"{PageUrls.Product}/{ShortId}";
    //#endif
}
