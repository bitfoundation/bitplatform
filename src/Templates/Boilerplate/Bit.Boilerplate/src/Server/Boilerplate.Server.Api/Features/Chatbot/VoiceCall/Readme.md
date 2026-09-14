# Voice calls: the story of one call

The AI chat panel has a phone button. This document follows one call from that tap to the final hang-up. For each step it names the code involved and explains the design choices that aren't obvious from the code.

## The cast

| Who | Where | What it does |
|---|---|---|
| **The client** | `AppAiChatPanel.razor.Voice.cs` (browser or hybrid WebView) | Opens the microphone, sends a WebRTC offer, plays the model's voice and shows the transcript. |
| **The controller** | `ChatbotController.StartVoiceCall` | Checks the caller and builds the instructions. |
| **The runner** | `VoiceCallRunner` (singleton) | Creates the call, stays on it, runs tools and always hangs up. |
| **The provider client** | `OpenAIRealtimeCallClient` | Makes the three HTTP/WebSocket requests to OpenAI. |
| **OpenAI Realtime** | `api.openai.com/v1` | Hosts the call, handles the audio and decides when to call a tool. |
| **The tab's SignalR connection** | `AppHub` | Receives commands from tools that act on the running app. |

The design comes down to one rule: **the audio goes directly between the client and OpenAI, while the key, the prompt and the tools stay on the server.**

## Act 1: Dialing

**The user taps the phone button.** The button is shown only if the platform supports both WebRTC and `mediaDevices`, which requires a secure context. `ToggleVoiceCall` asks the user to sign in if they aren't (a convenience only; the server enforces sign-in in the next step). It then asks for microphone permission and stops dictation and read aloud, because only one of the three runs at a time.

**The client prepares an offer.** Using Butil's `WebRtc`, it opens the microphone, creates a peer connection, adds the microphone track and creates the `oai-events` data channel. Both are added *before* `createOffer`, because the offer describes them. The model's voice will play through a hidden `<audio autoplay>` element. No JavaScript in this project is involved.

**The offer goes to our API, not to OpenAI.** `POST api/v1/Chatbot/StartVoiceCall` carries only the SDP offer, the culture, the time zone, the device platform and the conversation so far. The client never has a key, a prompt or a tool list, not even a short-lived one.

**The controller checks the caller.** `[Authorize]` rejects anonymous callers before anything reaches OpenAI. The `speech` rate limit allows 10 calls per minute per user, and the global limiter allows 100 per minute per IP. The controller then composes the instructions from three parts:

- the tenant's Support system prompt (the same one the text chat uses)
- a `### Voice call:` section that turns off the markdown, link, image and heading rules, since nothing spoken is shown as text, and says an approval takes a tap on the screen
- the variables, which come from the client and are therefore cleaned first: `SanitizeVariable` for the device, `KnownTimeZoneId` for the time zone, and `EscapeVariable` for the email and URL

The controller also looks up the session's most recently connected SignalR connection id and hashes the user id into the `OpenAI-Safety-Identifier`.

**The runner creates the call.** It opens a DI scope that lives for the whole call, points the scoped `AppChatbot` at the SignalR connection, and takes `GetAIFunctions()`, the same tools the text chat uses. `POST /realtime/calls` sends the offer together with the whole `session`: model, instructions, voice, transcription model and language (the user's culture), `tool_choice: auto` and each tool's JSON schema. OpenAI returns the SDP answer in the body and the call id in the `Location` header.

**The runner joins before it answers.** It opens the sideband WebSocket (`wss://…/realtime?call_id=…`) *before* returning the answer. A call with no server on it could not run tools or be ended. The server connects out to OpenAI; nothing connects in, so no inbound endpoint is needed. If this step fails, the runner hangs up the call it already created.

**The conversation so far goes in first.** So the model carries on instead of starting over, the runner filters the history with `AppChatbot.BelievableHistory`, exactly as the text chat does: finished messages only, the newest 40, and an assistant answer without a valid signature goes in as the user's. Each message becomes a `conversation.item.create` (`input_text` for the user, `output_text` for the assistant) sent before the answer is returned. Images are skipped. In a call this is consistency rather than protection: the caller can add any item over the data channel anyway.

**The runner starts the call's lifetime.** A `CancellationTokenSource` ends the call after `MaxCallDuration`. Storing it in `callsByUser` cancels the user's previous call, because every open call is billed by the minute. The event loop then runs on its own, detached from the HTTP request.

**The answer reaches the client.** The response carries the SDP answer and `MaxDuration`. If the user already hung up while the server was dialing, the answer is ignored. Otherwise `setRemoteDescription` completes the connection, and a countdown shows how much time is left. The server is what hangs up; the countdown running out only ends the client's side.

## Act 2: Talking

Audio travels over WebRTC (DTLS-SRTP) **directly between the client and OpenAI**. Our server handles no audio at all: no relay, no bandwidth, and no recording on our side.

OpenAI sends the same event stream to the client's data channel and to the server's sideband. The client uses it only for the transcript:

- `conversation.item.input_audio_transcription.delta` / `.completed` for the user's side (only when `RealtimeTranscriptionModel` is set). A separate model writes it, so it can differ from what the realtime model understood, and nothing corrects it afterwards
- `response.output_audio_transcript.delta` / `.done` for the assistant's side

Finished bubbles are saved to the chat history **unsigned**. So a spoken answer can't be read aloud by `SynthesizeSpeech`, and goes back as the user's words, not the assistant's, when that history is sent to the text chat or to a later call. The server can't sign them either: the caller controls the call's instructions (See "What the guards don't cover"), so a signature would vouch for whatever the caller made the model say.

**Typing during a call.** A message typed (or a suggestion tapped) while a call is open goes into the call over the same data channel, not to the text chat: a `conversation.item.create` with `input_text`, then `response.create`. It cuts the model short, as speaking over it would: `response.cancel` while a response is active, and `output_audio_buffer.clear` while its audio is still playing, which can outlast the response. The provider refuses a new response while another is active, so the panel sends once the cancelled `response.done` arrives, with the send button loading. The model answers by voice as usual. The attach button is disabled during a call, and signatures and read aloud don't apply to spoken answers.

## Act 3: A tool call

The user says *"switch to dark mode"*.

1. OpenAI sends `response.done` on the sideband, with a `function_call { call_id, name, arguments }` in its output. The client receives the same event and ignores it. The runner acts on only two event types: `response.done` and `error`.
2. `InvokeTool` looks up the name in the server's own list. **A name that isn't there is not run**; the model gets the output `There is no tool named 'X'.` This matters because the client shares the call and could use `session.update` to declare tools of its own.
3. The tool runs as the caller. The original request is over, so the runner gives the scope a synthetic `HttpContext` with the caller's cloned user, the request's scheme and host, and `X-Origin` only when that origin is trusted.
4. Tools that act on the running app (`NavigateToPage`, `SetApplicationTheme`, `SetApplicationCulture`, `CheckLastError`, `ClearAppFiles`, `ShowSignInModal`, and the ones showing cards or suggestions) use SignalR to reach the tab. `ClearAppFiles` waits there for the user to tap its approval card, and the call's events wait with it, for at most the call's remaining time. Tools that stay on the server skip this step.
5. Each result goes back as a `conversation.item.create` with a `function_call_output`. After all of them the runner sends a single `response.create`. Without it the model never speaks the result. A response that had already spoken and only called `ShowFollowUpSuggestions` gets none, or the model would talk about the suggestions it just showed. A cancelled response runs none of its calls: the user cut it short, and their arguments may be half written.

If a tool throws, the model hears `The X tool failed.` and a warning is logged. Several calls in one response run one after another over the same socket, whose sends are serialized.

## Act 4: Hanging up

The client ends its side when:

- the user taps the button again
- dictation or read aloud starts
- the component is disposed
- the `oai-events` data channel closes, which is how OpenAI hanging up reaches the client at once
- the countdown runs out, in case the client never notices the hang-up
- the peer connection reports `failed` or `closed`

The last three also show *"The voice call ended."* Closing the panel doesn't end the call: the floating chat button turns into a call button that leads back to it.

Closing the connection ends the call at OpenAI, which closes the sideband.

The runner's loop also ends when `MaxCallDuration` is reached or when the same user starts a newer call. In every case, the `finally` block does the following:

1. decrements `chatbot.active_voice_calls`
2. removes the user's entry, but only if it still belongs to this call
3. **posts `/hangup`**
4. disposes the sideband, the scope and the token source

Step 3 is what guarantees billing stops, even when a client disappears without closing the call.

## What the guards don't cover

- **Callers can change their own call.** Over the data channel, the client can change instructions or voice with `session.update`, add fake tool outputs, or ask for more responses. Only which tools actually run is fixed on the server. The effect stays inside the caller's own call and its time limit.
- **Limits apply per server instance.** Rate-limit counters and `callsByUser` live in process memory. With N instances, one user can hold N calls. To cap the total bill, set a spend limit on the OpenAI key.
- **App-side tools may reach a different tab.** They go to the session's most recently connected tab, which may not be the tab on the call. If the session has no connection, they report failure.
- **Hanging up while dialing leaves a call open.** The call already exists at OpenAI. The runner keeps it until OpenAI closes it or the time limit ends it.

## Configuration

All settings are under `AI:OpenAI`:

| Setting | Effect |
|---|---|
| `RealtimeApiKey` | Turns the feature on; without it the runner isn't registered. Keep it in user-secrets or a vault. |
| `RealtimeEndpoint` | Defaults to `https://api.openai.com/v1`. The sideband URL is built from it (`http` becomes `ws`, `https` becomes `wss`). |
| `RealtimeModel`, `RealtimeVoice` | Sent in `session`. |
| `RealtimeTranscriptionModel` | Transcribes the user's side. Leave it empty to show only the assistant's side. |
| `RealtimeMaxCallDuration` | Hard limit per call; zero means 2 minutes. |

Spelling hints for words the transcriber wouldn't know go in `keywords` in `VoiceCallRunner.CreateSession`.

The web app also needs `Permissions-Policy: microphone=(self)`. Android needs `AppWebChromeClient` to grant microphone access to the WebView, and iOS/macOS need `NSMicrophoneUsageDescription`.
