using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bit.Besql.Demo.Client.Model;

public class WeatherForecast
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public int TemperatureC { get; set; }

    [MaxLength(100)]
    public string? Summary { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}