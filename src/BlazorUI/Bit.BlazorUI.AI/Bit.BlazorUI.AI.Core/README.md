### bit BlazorUI AI Core: what every bit BlazorUI AI package builds on
[![NuGet Version](https://img.shields.io/nuget/v/Bit.BlazorUI.AI.Core.svg?style=flat)](https://www.nuget.org/packages/Bit.BlazorUI.AI.Core/) ![Nuget](https://img.shields.io/nuget/dt/Bit.BlazorUI.AI.Core.svg)

The UI-free core of [bit BlazorUI AI](https://www.nuget.org/packages/Bit.BlazorUI.AI/):

- **The transport contract**: `IBitAiTransport` streams a `BitAiRequest` as `BitAiEvent`s; `IBitAiApprovalTransport`
  carries the user's decision on a tool call the other side holds.
- **The wire format**: `BitAiWireSerializer` reads and writes the events and the request as JSON, through
  source-generated metadata (safe to trim and to compile ahead of time). It is versioned and only grows.
- **The conversation model and the chat session**: `BitAiConversation`, `BitAiMessage` and its parts, the streaming
  reducer, tools and approvals, persistence (`BitAiConversationSerializer`).
- **The client transports that need nothing beyond .NET**: `BitAiSseTransport` (server-sent events) and
  `BitAiWebSocketTransport` (a WebSocket per reply).

It has no UI and no ASP.NET Core dependency. A Blazor app gets it with `Bit.BlazorUI.AI`; install it on its own in a
server or a plain .NET client that only speaks the wire format. The server ends are in `Bit.BlazorUI.AI.Server`.
