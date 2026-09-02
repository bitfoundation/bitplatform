---
name: localize-strings
description: Moves hardcoded user-facing strings out of Razor/C# files into AppStrings.resx and rewrites the call sites to use IStringLocalizer with nameof(AppStrings.Key). Use only when explicitly asked to localize or apply translations.
---

<!-- Bridge stub. Visual Studio's agent picker (and the @ menu) only lists .github/agents/*.agent.md
     files - it discovers the Agent Skills in .agents/skills/ but never lists them for manual
     invocation. This file mirrors the canonical skill's name/description and delegates to it.
     Edit the canonical file, never this one. -->

Read `.agents/skills/localize-strings/SKILL.md` (relative to the repository root) and follow it exactly.
