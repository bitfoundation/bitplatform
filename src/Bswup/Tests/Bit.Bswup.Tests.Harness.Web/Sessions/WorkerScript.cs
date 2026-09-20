using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Bit.Bswup.Tests.Harness.Web;

/// <summary>
/// Writes an app's service-worker.js for a session: the <c>self.*</c> settings, then the import of the Bswup
/// worker - the file an app ships, generated instead of copied so each test can configure it. The first line
/// names the deployment version, so publishing a new version changes the script's bytes exactly like a real
/// release does (the byte comparison is how a browser detects an update).
/// </summary>
public static partial class WorkerScript
{
    public static string Build(HarnessSession session, HostedApp app, string mode)
    {
        var options = session.Options;
        var script = new StringBuilder();
        script.Append("// Bit.Bswup harness service worker - session '").Append(session.Id)
              .Append("', app '").Append(app.Name)
              .Append("', version ").Append(session.Version).Append('.').AppendLine();

        if (options.Worker == HarnessWorkers.Cleanup)
        {
            script.AppendLine("self.importScripts('_content/Bit.Bswup/bit-bswup.sw-cleanup.js');");
            return script.ToString();
        }

        var settings = app.DefaultWorkerSettings(mode);
        foreach (var (name, value) in options.WorkerSettings ?? [])
        {
            if (value is null) settings.Remove(name);
            else settings[name] = value.DeepClone();
        }

        foreach (var (name, value) in settings)
        {
            if (SettingName().IsMatch(name) is false)
                throw new InvalidOperationException($"'{name}' is not a valid service-worker setting name.");

            script.Append("self.").Append(name).Append(" = ");
            WriteValue(script, value);
            script.AppendLine(";");
        }

        script.AppendLine("self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');");
        return script.ToString();
    }

    private static void WriteValue(StringBuilder script, JsonNode? node)
    {
        switch (node)
        {
            case null:
                script.Append("null");
                break;

            case JsonObject regex when regex.TryGetPropertyValue("$regex", out var source):
                script.Append("new RegExp(")
                      .Append(JsonSerializer.Serialize(source?.GetValue<string>() ?? string.Empty))
                      .Append(", ")
                      .Append(JsonSerializer.Serialize(regex["flags"]?.GetValue<string>() ?? string.Empty))
                      .Append(')');
                break;

            case JsonObject obj:
                script.Append('{');
                var first = true;
                foreach (var (name, value) in obj)
                {
                    if (first is false) script.Append(", ");
                    first = false;
                    script.Append(JsonSerializer.Serialize(name)).Append(": ");
                    WriteValue(script, value);
                }
                script.Append('}');
                break;

            case JsonArray array:
                script.Append('[');
                for (var i = 0; i < array.Count; i++)
                {
                    if (i > 0) script.Append(", ");
                    WriteValue(script, array[i]);
                }
                script.Append(']');
                break;

            default:
                script.Append(node.ToJsonString());
                break;
        }
    }

    [GeneratedRegex(@"^[A-Za-z_$][\w$]*$")]
    private static partial Regex SettingName();
}
