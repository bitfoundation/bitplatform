namespace Bit.Belemetry;

/// <summary>
/// Fills in the resource attributes a server can tell about where it runs - what the OpenTelemetry host, process,
/// container and Azure resource detectors would add - from environment variables and <c>/proc</c>, once at start-up.
/// </summary>
/// <remarks>
/// Servers only. A device (Hybrid) or a browser tab is a person's machine: its name, user and container are theirs,
/// not the app's, and would turn into identifiers. Nothing here makes a network call (the Azure VM metadata service
/// would), so detection costs a few environment reads and, on Linux, one small file.
/// </remarks>
internal static class BelemetryResourceDetector
{
    /// <summary>
    /// The instance id the platform already assigned this process, when it has one: App Service's and Container
    /// Apps' names survive restarts, which a random id per process would not, so Application Insights keeps counting
    /// the same instance.
    /// </summary>
    public static string? GetPlatformInstanceId()
    {
        return GetEnv("WEBSITE_INSTANCE_ID") ?? GetEnv("CONTAINER_APP_REPLICA_NAME");
    }

    /// <summary>Adds the host, process, container and cloud attributes that can be found without a network call.</summary>
    /// <param name="attributes">The resource under construction; nothing already set is overwritten.</param>
    /// <param name="useEnvironment">Whether the cloud platforms' environment variables are read.</param>
    public static void Detect(BelemetryAttributes attributes, bool useEnvironment)
    {
        attributes.SetIfAbsent(BelemetrySemanticConventions.HostName, Environment.MachineName);
        attributes.SetIfAbsent(BelemetrySemanticConventions.ProcessPid, Environment.ProcessId);

        if (Environment.ProcessPath is { Length: > 0 } processPath)
        {
            attributes.SetIfAbsent(BelemetrySemanticConventions.ProcessExecutableName, Path.GetFileNameWithoutExtension(processPath));
        }

        try
        {
            attributes.SetIfAbsent(BelemetrySemanticConventions.ProcessOwner, Environment.UserName);
        }
        catch
        {
            // Some sandboxes refuse the lookup; the owner is a nicety.
        }

        if (OperatingSystem.IsLinux() && ReadContainerId() is { } containerId)
        {
            attributes.SetIfAbsent(BelemetrySemanticConventions.ContainerId, containerId);
        }

        if (useEnvironment) DetectAzure(attributes);
    }

    private static void DetectAzure(BelemetryAttributes attributes)
    {
        if (GetEnv("WEBSITE_SITE_NAME") is { } siteName)
        {
            // App Service and Functions share the variables; Functions adds its own runtime version.
            var functions = GetEnv("FUNCTIONS_EXTENSION_VERSION") is not null;
            attributes.SetIfAbsent(BelemetrySemanticConventions.CloudProvider, "azure");
            attributes.SetIfAbsent(BelemetrySemanticConventions.CloudPlatform, functions ? "azure_functions" : "azure_app_service");
            attributes.SetIfAbsent(BelemetrySemanticConventions.CloudRegion, GetEnv("REGION_NAME"));
            attributes.SetIfAbsent("azure.app.service.stamp", GetEnv("WEBSITE_HOME_STAMPNAME"));
            attributes.SetIfAbsent(BelemetrySemanticConventions.HostId, GetEnv("WEBSITE_INSTANCE_ID"));
            attributes.SetIfAbsent(BelemetrySemanticConventions.CloudResourceId, AppServiceResourceId(siteName, GetEnv("WEBSITE_RESOURCE_GROUP"), GetEnv("WEBSITE_OWNER_NAME"), functions));
            return;
        }

        if (GetEnv("CONTAINER_APP_NAME") is { } appName)
        {
            attributes.SetIfAbsent(BelemetrySemanticConventions.CloudProvider, "azure");
            attributes.SetIfAbsent(BelemetrySemanticConventions.CloudPlatform, "azure_container_apps");
            attributes.SetIfAbsent("azure.container_apps.app", appName);
            attributes.SetIfAbsent("azure.container_apps.revision", GetEnv("CONTAINER_APP_REVISION"));
            attributes.SetIfAbsent(BelemetrySemanticConventions.HostId, GetEnv("CONTAINER_APP_REPLICA_NAME"));
        }
    }

    /// <summary>
    /// The ARM id of an App Service site. <c>WEBSITE_OWNER_NAME</c> is <c>{subscription}+{resource group}-{region}webspace</c>,
    /// so the subscription is what precedes its first <c>+</c>.
    /// </summary>
    internal static string? AppServiceResourceId(string siteName, string? resourceGroup, string? ownerName, bool functions)
    {
        if (resourceGroup is null || ownerName is null) return null;
        var plus = ownerName.IndexOf('+', StringComparison.Ordinal);
        if (plus <= 0) return null;
        var subscription = ownerName[..plus];
        return $"/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.Web/sites/{siteName}";
    }

    /// <summary>
    /// The container id from the mount of the cgroup (cgroup v2 names the container in the mount source) or, failing
    /// that, from the cgroup paths (v1 ends them with the id). Null outside a container, or when the files are
    /// unreadable.
    /// </summary>
    internal static string? ReadContainerId()
    {
        try
        {
            if (File.Exists("/proc/self/mountinfo") && FindContainerId(File.ReadLines("/proc/self/mountinfo"), mountInfo: true) is { } fromMounts) return fromMounts;
            if (File.Exists("/proc/self/cgroup")) return FindContainerId(File.ReadLines("/proc/self/cgroup"), mountInfo: false);
        }
        catch
        {
            // A restricted /proc is not worth failing start-up over.
        }

        return null;
    }

    internal static string? FindContainerId(IEnumerable<string> lines, bool mountInfo)
    {
        foreach (var line in lines)
        {
            // mountinfo: "... /docker/containers/<id>/hostname /etc/hostname ..."; cgroup v1: "12:memory:/docker/<id>".
            if (mountInfo && line.Contains("/hostname ", StringComparison.Ordinal) is false && line.Contains("/containers/", StringComparison.Ordinal) is false) continue;

            foreach (var segment in line.Split('/'))
            {
                var candidate = segment;
                var dot = candidate.IndexOf('.', StringComparison.Ordinal);
                if (dot > 0) candidate = candidate[..dot]; // "<id>.scope" under systemd.
                var dash = candidate.LastIndexOf('-');
                if (dash >= 0 && candidate.Length - dash - 1 == 64) candidate = candidate[(dash + 1)..]; // "docker-<id>".
                if (IsContainerId(candidate)) return candidate;
            }
        }

        return null;
    }

    private static bool IsContainerId(string value)
    {
        if (value.Length != 64) return false;
        foreach (var c in value)
        {
            if (char.IsAsciiHexDigitLower(c) is false && char.IsAsciiDigit(c) is false) return false;
        }
        return true;
    }

    private static string? GetEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
