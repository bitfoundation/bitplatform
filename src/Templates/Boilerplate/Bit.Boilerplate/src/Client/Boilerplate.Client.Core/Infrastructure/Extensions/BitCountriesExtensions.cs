namespace Bit.BlazorUI;

public static class BitCountiresExtensions
{
    extension(BitCountries source)
    {
        public static BitCountry Current
        {
            get
            {
                if (CultureInfoManager.InvariantGlobalization)
                    return BitCountries.UnitedStates;

                var regionInfo = new RegionInfo(CultureInfo.CurrentUICulture.Name);

                return BitCountries.All
                    .Single(c => string.Equals(c.Iso2, regionInfo.TwoLetterISORegionName, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
