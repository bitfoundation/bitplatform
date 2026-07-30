namespace Boilerplate.Shared.Features.Identity.Dtos;

[DtoResourceType(typeof(AppStrings))]
public partial class ClaimDto
{
    public string? ClaimType { get; set; }

    public string? ClaimValue { get; set; }

    public static bool operator ==(ClaimDto? @this, ClaimDto? that)
    {
        if (@this is null && that is null) return true;
        if (@this is null || that is null) return false;
        return @this.ClaimType == that.ClaimType
             && @this.ClaimValue == that.ClaimValue;
    }

    public static bool operator !=(ClaimDto? @this, ClaimDto? that)
    {
        return !(@this == that);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ClaimDto);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClaimType, ClaimValue);
    }

    public override string ToString()
    {
        return $"{ClaimType}={ClaimValue}";
    }
}
