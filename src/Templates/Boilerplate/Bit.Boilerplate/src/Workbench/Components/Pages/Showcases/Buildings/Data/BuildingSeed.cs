namespace Boilerplate.Workbench.Components.Pages.Showcases.Buildings.Data;

public static class BuildingSeed
{
    public static List<Building> Create()
    {
        List<Building> buildings =
        [
            New("Rijksmuseum", "Amsterdam", DutchProvince.NorthHolland, BuildingType.Museum, 1885, null, null, 52.36000, 4.88528),
            New("Amsterdam Centraal", "Amsterdam", DutchProvince.NorthHolland, BuildingType.Station, 1889, null, null, 52.37895, 4.90016),
            New("Royal Palace of Amsterdam", "Amsterdam", DutchProvince.NorthHolland, BuildingType.Palace, 1665, null, null, 52.37317, 4.89136),
            New("Anne Frank House", "Amsterdam", DutchProvince.NorthHolland, BuildingType.Museum, 1635, null, null, 52.37515, 4.88404),
            New("Westerkerk", "Amsterdam", DutchProvince.NorthHolland, BuildingType.Church, 1631, 85, null, 52.37450, 4.88390),
            New("Rembrandt Tower", "Amsterdam", DutchProvince.NorthHolland, BuildingType.Office, 1994, 150, 35, 52.34508, 4.91701),
            New("A'DAM Tower", "Amsterdam", DutchProvince.NorthHolland, BuildingType.Tower, 1971, 100, 22, 52.38392, 4.90207),
            New("Johan Cruyff Arena", "Amsterdam", DutchProvince.NorthHolland, BuildingType.Stadium, 1996, null, null, 52.31417, 4.94194),
            New("Teylers Museum", "Haarlem", DutchProvince.NorthHolland, BuildingType.Museum, 1784, null, null, 52.38056, 4.64028),
            New("Waag", "Alkmaar", DutchProvince.NorthHolland, BuildingType.Landmark, 1603, null, null, 52.63140, 4.75028),
            New("Euromast", "Rotterdam", DutchProvince.SouthHolland, BuildingType.Tower, 1960, 184.5, null, 51.90539, 4.46658),
            New("De Rotterdam", "Rotterdam", DutchProvince.SouthHolland, BuildingType.Office, 2013, 149, 44, 51.90667, 4.48806),
            New("Maastoren", "Rotterdam", DutchProvince.SouthHolland, BuildingType.Office, 2010, 164.8, 44, 51.90861, 4.49333),
            New("Markthal", "Rotterdam", DutchProvince.SouthHolland, BuildingType.Landmark, 2014, 40, 16, 51.92011, 4.48695),
            New("Peace Palace", "The Hague", DutchProvince.SouthHolland, BuildingType.Palace, 1913, 80, null, 52.08660, 4.29550),
            New("Binnenhof", "The Hague", DutchProvince.SouthHolland, BuildingType.CityHall, 1230, null, null, 52.07964, 4.31337),
            New("Mauritshuis", "The Hague", DutchProvince.SouthHolland, BuildingType.Museum, 1644, null, null, 52.08039, 4.31431),
            New("Nieuwe Kerk", "Delft", DutchProvince.SouthHolland, BuildingType.Church, 1496, 108.75, null, 52.01214, 4.36019),
            New("Town Hall of Gouda", "Gouda", DutchProvince.SouthHolland, BuildingType.CityHall, 1450, null, null, 52.01167, 4.71056),
            New("Pieterskerk", "Leiden", DutchProvince.SouthHolland, BuildingType.Church, 1121, null, null, 52.15750, 4.48778),
            New("Dom Tower", "Utrecht", DutchProvince.Utrecht, BuildingType.Tower, 1382, 112.3, null, 52.09061, 5.12141),
            New("Rietveld Schröder House", "Utrecht", DutchProvince.Utrecht, BuildingType.Museum, 1924, null, 2, 52.08533, 5.14760),
            New("Utrecht Centraal", "Utrecht", DutchProvince.Utrecht, BuildingType.Station, 1843, null, null, 52.08932, 5.10997),
            New("Onze Lieve Vrouwetoren", "Amersfoort", DutchProvince.Utrecht, BuildingType.Tower, 1470, 98.33, null, 52.15517, 5.38720),
            New("Het Loo Palace", "Apeldoorn", DutchProvince.Gelderland, BuildingType.Palace, 1686, null, null, 52.23417, 5.94583),
            New("Kröller-Müller Museum", "Otterlo", DutchProvince.Gelderland, BuildingType.Museum, 1938, null, null, 52.09583, 5.81694),
            New("GelreDome", "Arnhem", DutchProvince.Gelderland, BuildingType.Stadium, 1998, null, null, 51.96250, 5.89306),
            New("Peperbus", "Zwolle", DutchProvince.Overijssel, BuildingType.Tower, 1463, 75, null, 52.51217, 6.08981),
            New("Sassenpoort", "Zwolle", DutchProvince.Overijssel, BuildingType.Landmark, 1406, null, null, 52.50972, 6.09528),
            New("De Grolsch Veste", "Enschede", DutchProvince.Overijssel, BuildingType.Stadium, 1998, null, null, 52.23667, 6.83750),
            New("Rijksmuseum Twenthe", "Enschede", DutchProvince.Overijssel, BuildingType.Museum, 1930, null, null, 52.22778, 6.89694),
            New("Almere Centrum Station", "Almere", DutchProvince.Flevoland, BuildingType.Station, 1987, null, null, 52.37500, 5.21778),
            New("Lelystad Airport", "Lelystad", DutchProvince.Flevoland, BuildingType.Station, 1973, null, null, 52.45528, 5.52306),
            New("Museum Schokland", "Schokland", DutchProvince.Flevoland, BuildingType.Museum, 1947, null, null, 52.63433, 5.77746),
            New("Martinitoren", "Groningen", DutchProvince.Groningen, BuildingType.Tower, 1482, 97, null, 53.21917, 6.56806),
            New("Groninger Museum", "Groningen", DutchProvince.Groningen, BuildingType.Museum, 1994, null, null, 53.21194, 6.56556),
            New("Academiegebouw", "Groningen", DutchProvince.Groningen, BuildingType.Landmark, 1909, null, null, 53.21913, 6.56286),
            New("Menkemaborg", "Uithuizen", DutchProvince.Groningen, BuildingType.Palace, 1614, null, null, 53.40496, 6.68387),
            New("Fries Museum", "Leeuwarden", DutchProvince.Friesland, BuildingType.Museum, 2013, null, null, 53.19972, 5.79444),
            New("Achmeatoren", "Leeuwarden", DutchProvince.Friesland, BuildingType.Office, 2002, 115, 26, 53.19806, 5.79194),
            New("Blokhuispoort", "Leeuwarden", DutchProvince.Friesland, BuildingType.Landmark, 1877, null, null, 53.19972, 5.80028),
            New("Eise Eisinga Planetarium", "Franeker", DutchProvince.Friesland, BuildingType.Museum, 1781, null, null, 53.18738, 5.54375),
            New("Drents Museum", "Assen", DutchProvince.Drenthe, BuildingType.Museum, 1854, null, null, 52.99333, 6.56417),
            New("Dwingeloo Radio Observatory", "Dwingeloo", DutchProvince.Drenthe, BuildingType.Landmark, 1956, 25, null, 52.81194, 6.39611),
            New("Atlas Theater", "Emmen", DutchProvince.Drenthe, BuildingType.Landmark, 2016, null, null, 52.78241, 6.89156),
            New("National Prison Museum", "Veenhuizen", DutchProvince.Drenthe, BuildingType.Museum, 1823, null, null, 53.04045, 6.38967),
            New("Middelburg Town Hall", "Middelburg", DutchProvince.Zeeland, BuildingType.CityHall, 1452, null, null, 51.49889, 3.61083),
            New("Middelburg Abbey", "Middelburg", DutchProvince.Zeeland, BuildingType.Church, 1123, null, null, 51.50028, 3.61556),
            New("Sint-Lievensmonstertoren", "Zierikzee", DutchProvince.Zeeland, BuildingType.Tower, 1454, 62, null, 51.65028, 3.91444),
            New("Watersnoodmuseum", "Ouwerkerk", DutchProvince.Zeeland, BuildingType.Museum, 2001, null, null, 51.61750, 3.98194),
            New("St. John's Cathedral", "'s-Hertogenbosch", DutchProvince.NorthBrabant, BuildingType.Church, 1220, 73, null, 51.68806, 5.30833),
            New("Evoluon", "Eindhoven", DutchProvince.NorthBrabant, BuildingType.Landmark, 1966, null, null, 51.44361, 5.44694),
            New("Philips Stadion", "Eindhoven", DutchProvince.NorthBrabant, BuildingType.Stadium, 1913, null, null, 51.44170, 5.46740),
            New("Grote Kerk", "Breda", DutchProvince.NorthBrabant, BuildingType.Church, 1547, 97, null, 51.58889, 4.77528),
            New("Westpoint Tower", "Tilburg", DutchProvince.NorthBrabant, BuildingType.Office, 2004, 142, 47, 51.56013, 5.06515),
            New("Basilica of Saint Servatius", "Maastricht", DutchProvince.Limburg, BuildingType.Church, 1039, null, null, 50.84882, 5.68720),
            New("Maastricht City Hall", "Maastricht", DutchProvince.Limburg, BuildingType.CityHall, 1664, null, null, 50.85139, 5.69111),
            New("Bonnefanten Museum", "Maastricht", DutchProvince.Limburg, BuildingType.Museum, 1995, null, null, 50.84253, 5.70198),
            New("Glaspaleis", "Heerlen", DutchProvince.Limburg, BuildingType.Landmark, 1935, null, 5, 50.88792, 5.97934),
            New("Valkenburg Castle", "Valkenburg aan de Geul", DutchProvince.Limburg, BuildingType.Landmark, 1115, null, null, 50.86194, 5.83083),
        ];

        for (var index = 0; index < buildings.Count; index++)
        {
            buildings[index].Id = new Guid(index + 1, 0, 0, new byte[8]);
        }

        return buildings;
    }

    private static Building New(string name, string city, DutchProvince province, BuildingType type, int? year, double? height, int? floors, double latitude, double longitude) => new()
    {
        Name = name,
        City = city,
        Province = province,
        Type = type,
        Year = year,
        Height = height,
        Floors = floors,
        Latitude = latitude,
        Longitude = longitude
    };
}
