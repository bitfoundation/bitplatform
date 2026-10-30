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
<!--#if (aspire == true && redis == true)-->

## Published Redis runs without persistence

- **Symptom:** in the Docker Compose project `aspire publish` writes, the persistent Redis that holds Hangfire's jobs and the distributed lock runs with `appendonly no`, so a restart of its container loses them. Running the app with `aspire start` is not affected.
- **Cause:** Aspire publishes Redis as `/bin/sh -c "redis-server ..."` and writes every `WithArgs` value after that string, where the shell takes them as its own positional parameters and `redis-server` never sees them.
- **Workaround:** `PublishAsComposeRedis` in `src/Server/Boilerplate.Server.AppHost/Infrastructure/Extensions/DockerComposePublishingExtensions.cs` folds the arguments into the `-c` string, the way run mode does.
- **Upstream:** https://github.com/microsoft/aspire/issues/21400 (Aspire 13.6.4, reported 2026-10-29).
- **Remove when:** Aspire's published Redis passes `WithArgs` on to `redis-server`. To check, remove `PublishAsComposeRedis`, publish and deploy, then `CONFIG GET appendonly` on the persistent Redis must still answer `yes`.

## DbGate can't open Redis

- **Symptom:** DbGate's Redis connections never open. In run mode its log says `self-signed certificate`; on the server it speaks TLS to a Redis that has none. A password with a `/`, `?`, `#` or `%` in it breaks the connection as well.
- **Cause:** the `WithDbGate` of CommunityToolkit.Aspire.Hosting.Redis.Extensions writes DbGate's Redis URL by hand: always `rediss://`, and with the password as it is, unencoded. In run mode Aspire serves Redis over TLS with the developer certificate, which DbGate doesn't trust, and the published Redis has no TLS at all.
- **Workaround:** `ConfigureDbGate` in `src/Server/Boilerplate.Server.AppHost/Infrastructure/Extensions/IDistributedApplicationBuilderExtensions.cs` turns DbGate's certificate check off in run mode only (`NODE_TLS_REJECT_UNAUTHORIZED=0`), and `ConfigureDockerComposePublishing` in `src/Server/Boilerplate.Server.AppHost/Infrastructure/Extensions/DockerComposePublishingExtensions.cs` rewrites the published URLs to `redis://`. The deploy skill (`.github/agents/deploy.agent.md`) and `.docs/16` keep a deployment's Redis passwords to letters and digits, which the passwords Aspire generates in run mode already are.
- **Upstream:** https://github.com/CommunityToolkit/Aspire/issues/2343 (CommunityToolkit.Aspire 13.6.1, reported 2026-10-30), after https://github.com/CommunityToolkit/Aspire/issues/1548 was closed as stale without a fix.
- **Remove when:** the toolkit takes the URL from the Redis resource, with its scheme and an encoded password, and DbGate trusts Aspire's certificate. To check, remove the code above, then open each Redis in DbGate with `aspire start`, and on a test server with a Redis password that has a `/` in it. The letters and digits rule stays even then, since StackExchange.Redis can't take a comma in a password; only its mention of DbGate goes.
<!--#endif-->
