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
<!--#endif-->
