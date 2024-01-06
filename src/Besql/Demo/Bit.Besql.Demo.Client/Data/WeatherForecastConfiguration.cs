using Bit.Besql.Demo.Client.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bit.Besql.Demo.Client.Data;

public class WeatherForecastConfiguration : IEntityTypeConfiguration<WeatherForecast>
{
    public void Configure(EntityTypeBuilder<WeatherForecast> builder)
    {
        builder.HasIndex(w => w.TemperatureC);

        builder
            .HasData([new()
                    {
                        Id = 1,
                        Date = new DateTimeOffset(2024, 1, 1, 10, 10, 10, TimeSpan.Zero),
                        TemperatureC = 30,
                        Summary = "Hot"
                    },
                    new()
                    {
                        Id = 2,
                        Date = new DateTimeOffset(2024, 1, 2, 10, 10, 10, TimeSpan.Zero),
                        TemperatureC = 20,
                        Summary = "Normal"
                    },
                    new()
                    {
                        Id = 3,
                        Date = new DateTimeOffset(2024, 1, 3, 10, 10, 10, TimeSpan.Zero),
                        TemperatureC = 10,
                        Summary = "Cold"
                    }]);
    }
}
