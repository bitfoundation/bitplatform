namespace Bit.BlazorUI;

public static class BitCountriesExtensions
{
    extension(BitCountries source)
    {
        /// <summary>
        /// The country of the current UI culture, falling back to the United States whenever that culture cannot name
        /// one. Every step below is a real way this can fail rather than defensive padding: <c>InvariantGlobalization</c>
        /// is a build switch, so a globalization-enabled build can still be RUNNING on the invariant culture - which is
        /// what a Linux container with no LANG gives you - and <c>new RegionInfo("")</c> throws there. A neutral culture
        /// ("en", "fa") throws too, and a region that is not a country ("en-001" -> "001") matches nothing in
        /// <see cref="BitCountries.All"/>, which is why this looks the country up instead of demanding exactly one.
        /// </summary>
        public static BitCountry Current
        {
            get
            {
                if (CultureInfoManager.InvariantGlobalization)
                    return BitCountries.UnitedStates;

                var culture = CultureInfo.CurrentUICulture;

                if (string.IsNullOrEmpty(culture.Name) || culture.IsNeutralCulture)
                    return BitCountries.UnitedStates;

                try
                {
                    var regionInfo = new RegionInfo(culture.Name);

                    return BitCountries.All
                        .FirstOrDefault(c => string.Equals(c.Iso2, regionInfo.TwoLetterISORegionName, StringComparison.OrdinalIgnoreCase))
                        ?? BitCountries.UnitedStates;
                }
                catch (ArgumentException) // The culture has no region associated with it.
                {
                    return BitCountries.UnitedStates;
                }
            }
        }
    }
}
