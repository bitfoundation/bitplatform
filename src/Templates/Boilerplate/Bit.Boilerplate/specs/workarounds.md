# Workarounds

Code that works around a defect in a library, a tool or the platform this app builds on, until the defect is fixed where it lives. Each entry says what goes wrong, where the workaround lives, the upstream report, and how to tell the workaround can go. When an upgrade brings the fix, remove the workaround and its entry together.

<!-- One section per workaround:

## What goes wrong, as a user or a developer sees it

- **Symptom:** what goes wrong, and where.
- **Cause:** the defect, in the library's own terms.
- **Workaround:** what it does, and the files it lives in.
- **Upstream:** the report, or why there is none yet.
- **Remove when:** how to tell the fix has arrived, and how to check it.
-->

## The landing page's chat doesn't show that it's listening

- **Symptom:** after the microphone of the landing page's chat is pressed, nothing on the page says that it's listening: the button only turns a darker shade, as any pressed button does, and stays so until the visitor stops it or speaks.
- **Cause:** `BitAiVoiceInput` (Bit.BlazorUI.AI 11.0.0-pre-19) marks a recording for the app's own transcriber, with a countdown and a class on its root, but the browser's own speech recognition, which the landing page dictates with, gets only the checked state of its `BitToggleButton`. That it listens, and that the browser refused the microphone, is said only in its visually hidden live region.
- **Workaround:** `LandingChatSurface` (`src/LandingPage/Boilerplate.LandingPage.Client/Components/Chat/LandingChatSurface.razor.cs` and its `.razor.scss`) gives the checked microphone an `lp-chat-dictating` class through `BitAiPromptInputParams.Classes.VoiceInput.Button.Checked`, which paints it in the page's accent and pulses it, or rings it under reduced motion.
- **Upstream:** https://github.com/bitfoundation/bitplatform/issues/14996 (Bit.BlazorUI.AI 11.0.0-pre-19, reported 2026-11-13).
- **Remove when:** `BitAiVoiceInput` shows that the browser's speech recognition is listening, and that it failed. To check, remove the class and its rule, then press the microphone on the landing page.
<!--#if (aspire == true)-->

## `dotnet run` doesn't start the app host

- **Symptom:** `dotnet run` of the app host stops at once with "The --no-build option cannot be used when watch mode is enabled."
- **Cause:** Aspire 17 runs an app host's `dotnet run` as `aspire run --project <app host> --no-build`, and the Aspire CLI refuses `--no-build` while `aspire.config.json` turns `defaultWatchEnabled` on, as this project's does.
- **Workaround:** `ASPIRE_SUPPRESS_CLI_RUN_HOOK` in `src/Server/Boilerplate.Server.AppHost/Boilerplate.Server.AppHost.csproj` turns that hand off, so `dotnet run` starts the app host itself, as it did before Aspire 17. `aspire start` keeps watch mode.
- **Upstream:** not reported yet (Aspire 17.0.0, found 2026-11-18).
- **Remove when:** `dotnet run` of an app host works with watch mode on. To check, remove the property, then `dotnet run` the app host must log "Distributed application started".
<!--#endif-->
<!--#if (aspire == true && redis == true)-->

## Published Redis runs without persistence

- **Symptom:** in the Docker Compose project `aspire publish` writes, the persistent Redis that holds Hangfire's jobs and the distributed lock runs with `appendonly no`, so a restart of its container loses them. Running the app with `aspire start` is not affected.
- **Cause:** Aspire publishes Redis as `/bin/sh -c "redis-server ..."` and writes every `WithArgs` value after that string, where the shell takes them as its own positional parameters and `redis-server` never sees them.
- **Workaround:** `PublishAsComposeRedis` in `src/Server/Boilerplate.Server.AppHost/Infrastructure/Extensions/DockerComposePublishingExtensions.cs` folds the arguments into the `-c` string, the way run mode does.
- **Upstream:** https://github.com/microsoft/aspire/issues/21400 (Aspire 13.6.4, reported 2026-10-29), fixed by https://github.com/microsoft/aspire/pull/21447 for Aspire 17.1.
- **Remove when:** Aspire's published Redis passes `WithArgs` on to `redis-server`. To check, remove `PublishAsComposeRedis`, publish and deploy, then `CONFIG GET appendonly` on the persistent Redis must still answer `yes`.

## DbGate can't open Redis

- **Symptom:** DbGate's Redis connections never open. In run mode its log says `self-signed certificate`; on the server it speaks TLS to a Redis that has none. A password with a `/`, `?`, `#` or `%` in it breaks the connection as well.
- **Cause:** the `WithDbGate` of CommunityToolkit.Aspire.Hosting.Redis.Extensions writes DbGate's Redis URL by hand: always `rediss://`, and with the password as it is, unencoded. In run mode Aspire serves Redis over TLS with the developer certificate, which DbGate doesn't trust, and the published Redis has no TLS at all.
- **Workaround:** `ConfigureDbGate` in `src/Server/Boilerplate.Server.AppHost/Infrastructure/Extensions/IDistributedApplicationBuilderExtensions.cs` turns DbGate's certificate check off in run mode only (`NODE_TLS_REJECT_UNAUTHORIZED=0`), and `ConfigureDockerComposePublishing` in `src/Server/Boilerplate.Server.AppHost/Infrastructure/Extensions/DockerComposePublishingExtensions.cs` rewrites the published URLs to `redis://`. `bit pipeline` creates a deployment's Redis passwords from letters and digits, and the deploy skill (`.github/agents/deploy.agent.md`) and `.docs/16` keep the ones set by hand to the same, which the passwords Aspire generates in run mode already are.
- **Upstream:** https://github.com/CommunityToolkit/Aspire/issues/2343 (CommunityToolkit.Aspire 13.6.1, reported 2026-10-30), after https://github.com/CommunityToolkit/Aspire/issues/1548 was closed as stale without a fix. Fixed by https://github.com/CommunityToolkit/Aspire/pull/2371, in the release after 13.6.1.
- **Remove when:** the toolkit takes the URL from the Redis resource, with its scheme and an encoded password, and DbGate trusts Aspire's certificate. To check, remove the code above, then open each Redis in DbGate with `aspire start`, and on a test server with a Redis password that has a `/` in it. The letters and digits rule stays even then, since StackExchange.Redis can't take a comma in a password; only its mention of DbGate goes.
<!--#endif-->
