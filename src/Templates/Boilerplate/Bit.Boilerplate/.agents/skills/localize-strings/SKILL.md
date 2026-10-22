---
name: localize-strings
description: Moves hardcoded user-facing strings out of Razor/C# files into AppStrings.resx, rewrites the call sites to use IStringLocalizer with nameof(AppStrings.Key), and fills in the new keys for every other language the project ships. Use when the user explicitly asks to localize a page or component, extract hardcoded text, apply translations, fix missing resource strings, or says "run resx" / "move these strings to resx".
---

Read `.github/agents/localize-strings.agent.md` (relative to the repository root) and follow it exactly.
