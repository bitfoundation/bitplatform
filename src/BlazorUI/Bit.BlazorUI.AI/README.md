### bit BlazorUI AI: The AI components package of bit BlazorUI
[![NuGet Version](https://img.shields.io/nuget/v/Bit.BlazorUI.AI.svg?style=flat)](https://www.nuget.org/packages/Bit.BlazorUI.AI/) ![Nuget](https://img.shields.io/nuget/dt/Bit.BlazorUI.AI.svg)


**bit BlazorUI AI** offers the UI of AI apps for Blazor: conversations, streaming, tool calling, agents, citations,
artifacts, structured output and generative UI - as one coherent set of components rather than a pile of them.

**Why choose bit BlazorUI AI components?**
- **Native C# and Blazor throughout**: no JavaScript AI library is wrapped, no provider SDK is referenced, and no
  component knows which model produced what it is drawing.
- **Built for streaming**: Markdown that is still being written, JSON that is not valid yet, tool calls that have to be
  shown, approved, run and answered, and transcripts of hundreds of messages that never re-render the world per token.
- **Part of bit BlazorUI**: built on its components, styled by its theme tokens - dark and light, the Fluent 2,
  Material and Cupertino presets and right-to-left come for free.
- **Universal Support**: Compatible with all interactive Blazor modes - **Server**, **WASM**, and **Hybrid**.

### Getting Started

To use bit BlazorUI AI components, follow these steps:

1. Install the `Bit.BlazorUI.AI` nuget package.

using command line:

```
dotnet add package Bit.BlazorUI.AI
```

or using Package Manager Console:

```
Install-Package Bit.BlazorUI.AI
```

2. Register its services, together with the transport that talks to your model (in every `Program.cs` that renders
components):

```csharp
builder.Services.AddBitBlazorUIAIServices(ai => ai.UseTransport<MyTransport>());
```

3. In the default document (`App.razor` or `index.html`), add the `Bit.BlazorUI.AI` CSS file reference after the core
one:

```
<link rel="stylesheet" href="_content/Bit.BlazorUI.AI/styles/bit.blazorui.ai.css" />
```

4. In the default document (`App.razor` or `index.html`), add the `Bit.BlazorUI.AI` JS file reference after the core
one:

```
<script src="_content/Bit.BlazorUI.AI/scripts/bit.blazorui.ai.js"></script>
```

5. In the `_Imports.razor`, add the using `Bit.BlazorUI` to make it available throughout the project.

```
@using Bit.BlazorUI;
```

6. Start using BlazorUI AI components following its documents: https://blazorui.bitplatform.dev

### The one interface you implement

Everything the components do is downstream of a single boundary. Implement it over a provider SDK, an HTTP endpoint, a
local model, an orchestrator, or a fixture in a test:

```csharp
public sealed class MyTransport : IBitAiTransport
{
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(
        BitAiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new BitAiMessageStartedEvent();

        await foreach (var token in _model.StreamAsync(request, cancellationToken))
        {
            yield return new BitAiTextDeltaEvent(token);
        }

        yield return new BitAiUsageUpdatedEvent(new BitAiTokenUsage { OutputTokens = 128 });
        yield return new BitAiMessageCompletedEvent();
    }
}
```

Then a working chat - transcript, composer, streaming Markdown, tool cards, error recovery, keyboard and screen-reader
support - is one line:

```razor
<BitAiChat />
```
