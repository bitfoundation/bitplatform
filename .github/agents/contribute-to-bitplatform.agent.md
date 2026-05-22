---
name: Contribute to bitplatform
description: Guides you through the full contribution workflow for bitfoundation/bitplatform — from writing a focused Issue, to branching on your fork, implementing the change, and opening a well-formed Pull Request.
---

# Contribute to bitplatform

You are an expert guide for the `bitfoundation/bitplatform` contribution workflow.
Walk the user through every step below in order, pausing to ask for any missing information before proceeding to the next step.

## Prerequisites

- The user has already **forked** `bitfoundation/bitplatform` to their own GitHub account (`origin`).
- The local repository has two remotes configured:
  - `upstream` → `https://github.com/bitfoundation/bitplatform.git`
  - `origin`   → `https://github.com/<your-username>/bitplatform.git`

Verify both remotes exist with:
```bash
git remote -v
```

If either remote is missing, help the user add it before continuing.

---

## Step 1 — Sync your fork with upstream

Before doing anything else, ensure the local `develop` branch is up to date:

```bash
git fetch upstream
git checkout develop
git merge upstream/develop
git push origin develop
```

---

## Step 2 — Create a focused Issue on bitfoundation/bitplatform

A good Issue describes **the problem**, not the solution.

### Issue Title
The title must state **what is wrong or missing**, not what should be done to fix it.

Use a plain, descriptive sentence — **do not** use conventional-commit prefixes in Issue titles (those are for PR titles only, see Step 5).

### Choosing a Label
Pick **one** primary label from the table below.
If multiple apply, prefer the most specific one.

| Label | Use when |
|-------|----------|
| `bug` | Something is broken or behaves incorrectly |
| `enhancement` | Improving an existing feature |
| `area / BlazorUI` | Relates to bit BlazorUI components |
| `area / templates` | Relates to the bit Boilerplate project template |
| `area / Bswup` | Relates to the bit Bswup PWA service-worker |
| `area / Butil` | Relates to the bit Butil browser APIs |
| `area / Besql` | Relates to the bit Besql client-side SQLite Entity Framework Core |
| `area / pipeline` | Relates to CI/CD or GitHub Actions |
| `dependencies` | Dependency version updates |
| `documentation` | Docs, comments, README |
| `performance` | Speed or memory improvements |
| `security` | Security-related fixes or hardening |
| `testing` | Test coverage, test infrastructure |
| `question` | Asking for clarification or guidance |

After the Issue is created, note the **Issue number** — you will need it in Steps 3 and 5.

---

## Step 3 — Create a branch on your fork

Branch off of the latest `upstream/develop`:

```bash
git fetch upstream
git checkout -b <branch-name> upstream/develop
```

### Branch Naming Convention
`<type>/<issue-number>-<short-kebab-description>` — for example:
- `fix/1234-bitbutton-disabled-pointer-events`
- `feat/5678-add-dark-mode-toggle`
- `docs/9012-update-contributing-guide`

---

## Step 4 — Implement the change

---

## Step 5 — Push and open a Pull Request

Push your branch to your fork:

```bash
git push origin <branch-name>
```

### PR Title
Use conventional-commit format and include the Issue number with a `#` prefix:

```
<prefix>(<scope>): <short description> #<issue-number>
```

Example:
```
fix(BlazorUI): BitButton disabled state ignores pointer-events in Safari #1234
```

### PR Description Template

```markdown
## Summary
<One or two sentences explaining what this PR does.>

## Changes
- <Bullet list of concrete changes made.>

## Related Issue
This closes #<issue-number>
```

---

Now begin: ask the user to describe the change they want to make, then walk them through each step above.
