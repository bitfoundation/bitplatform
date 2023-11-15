namespace Bit.Websites.Careers.Server;

public class AppSettings
{
    public HealthCheckSettings HealthCheckSettings { get; set; } = default!;
}

public class HealthCheckSettings
{
    public bool EnableHealthChecks { get; set; }
}
