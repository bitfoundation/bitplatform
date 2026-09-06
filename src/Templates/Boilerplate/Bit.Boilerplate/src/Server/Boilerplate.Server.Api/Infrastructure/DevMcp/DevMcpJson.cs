namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

internal static class DevMcpJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(object value) => JsonSerializer.Serialize(value, Options);
}
