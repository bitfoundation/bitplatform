namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant.Data;

public sealed class AssistantTool(BitAiToolDefinition definition, Func<CancellationToken, Task<BitAiToolOutcome>> run) : IBitAiTool
{
    public BitAiToolDefinition Definition => definition;

    public async ValueTask<BitAiToolOutcome> InvokeAsync(BitAiToolInvocation invocation, CancellationToken cancellationToken = default) => await run(cancellationToken);
}
