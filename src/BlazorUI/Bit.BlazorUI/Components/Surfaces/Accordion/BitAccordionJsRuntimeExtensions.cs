namespace Bit.BlazorUI;

internal static class BitAccordionJsRuntimeExtensions
{
    internal static ValueTask BitAccordionWaitForTransitions(this IJSRuntime jsRuntime, ElementReference content)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Accordion.waitForTransitions", content);
    }
}
