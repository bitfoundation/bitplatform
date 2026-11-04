# Bit.BevTools.Belemetry

bit [Belemetry](../../Belemetry/)'s production diagnostics inside [BevTools](../README.md).

```csharp
builder.Services.AddBitBelemetryServices(o => o.Diagnostics.Enabled = true);
builder.Services.AddBitBevToolsBelemetry();
```

- **Support panel**: Belemetry's diagnostic timeline and exception groups for this session (this circuit on Blazor
  Server); a failure's component opens the component tree at its live instance; a trace shows its events; **Capture**
  previews and exports a support bundle; **Import** validates a bundle from elsewhere and shows its report.
- **Component context in captures**: Belemetry's captures gain each involved component's ancestry and render count from
  BevTools' tree (`IBelemetryComponentContextProvider`). Belemetry never walks the renderer itself.
- **BevTools stays off the timeline**: a click on BevTools' own UI (the Support panel's Capture button, a tab) is not
  recorded as the application's activity (`IBelemetryDiagnosticComponentFilter`, by BevTools' exact-namespace rule).

Both libraries work without this package. BevTools is a development tool: in production, Belemetry's captures carry the
component types the framework reported, and say the hierarchy is unavailable.
