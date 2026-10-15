namespace Bit.Belemetry;

/// <summary>The kind of Blazor host Belemetry runs in, which decides its defaults.</summary>
public enum BelemetryHostKind
{
    /// <summary>Detect it: WebAssembly in the browser, Server when ASP.NET Core hosting is registered, Hybrid otherwise.</summary>
    Auto,

    /// <summary>Blazor WebAssembly (standalone, or the client half of a Blazor Web App). The .NET pipeline runs in the browser.</summary>
    WebAssembly,

    /// <summary>An ASP.NET Core host (Blazor Server, static SSR, the server half of Auto). The .NET pipeline is off unless <see cref="BelemetryServerOptions.CollectDotnetTelemetry"/> is set.</summary>
    Server,

    /// <summary>Blazor Hybrid (MAUI, WPF, WinForms). The .NET pipeline runs in the app process and browser telemetry is relayed to it.</summary>
    Hybrid,
}
