---
name: deploy
description: Publishes this project to the user's own Linux VM, with a Test environment deployed from `develop` and Production from `main`. It sets up Docker and a self-hosted GitHub Actions runner on the VM, finds the external services the project depends on and helps the user get each one, stores their keys as GitHub secrets, builds the app's images on GitHub-hosted runners into GitHub Container Registry and has the VM's runner bring them up on Docker Compose, and serves the app on the user's own domain through a free Cloudflare Tunnel, with no inbound port open on the VM. Use when the user asks to deploy, publish, host or release the app, to set up a test or production environment, a server, a runner, secrets or a domain, or says "run deploy".
---

Read `.github/agents/deploy.agent.md` (relative to the repository root) and follow it exactly.
