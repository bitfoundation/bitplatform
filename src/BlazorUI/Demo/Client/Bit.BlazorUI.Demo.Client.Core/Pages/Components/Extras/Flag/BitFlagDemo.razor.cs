namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Flag;

public partial class BitFlagDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "Country",
            Type = "BitCountry?",
            DefaultValue = "null",
            Description = "The country to render the flag.",
            LinkType = LinkType.Link,
            Href = "#country",
         },
         new()
         {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip value of the flag element.",
         },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "country",
            Title = "BitCountry",
            Description = "Represents the basic information of a specific country.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string",
                    Description = "The full name of the country."
                },
                new()
                {
                    Name = "Code",
                    Type = "string",
                    Description = "The dialing code of the country."
                },
                new()
                {
                    Name = "Iso2",
                    Type = "string",
                    Description = "The ISO 3166-1 alpha-2 code of the country."
                },
                new()
                {
                    Name = "Iso3",
                    Type = "string",
                    Description = "The ISO 3166-1 alpha-3 code of the country."
                },
            ]
        }
    ];






    private readonly string example1RazorCode = @"
<div style=""display:flex;gap:2px;flex-wrap:wrap"">
    @foreach (var country in BitCountries.All)
    {
        <BitFlag Country=""country"" Title=""@($""{country.Name}-{country.Iso2}"")"" />
    }
</div>";
}
