# Coding Style

Always follow the settings defined in the `.editorconfig` file located at the root of this `src` folder.

# CI Workflows

Never put E2E (browser/Playwright, hybrid, publish-gate) tests in a project's main CI workflow (`.github/workflows/bit.ci.<Project>.yml`).
The main workflow runs on pull requests and must only build, run the unit/integration tests and (where it applies) pack.
Put the E2E suites in a separate `.github/workflows/bit.ci.<Project>.e2e.yml` that is triggered by `workflow_dispatch` only,
and point the E2E project's README at that file. Follow `bit.ci.Blex.yml` / `bit.ci.Blex.e2e.yml` and `bit.ci.Bmotion.yml` / `bit.ci.Bmotion.e2e.yml`.
