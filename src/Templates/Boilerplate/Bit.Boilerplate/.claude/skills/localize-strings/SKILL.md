---
name: localize-strings
description: Moves hardcoded user-facing strings out of Razor/C# files into AppStrings.resx and rewrites the call sites to use IStringLocalizer with nameof(AppStrings.Key). Use when the user explicitly asks to localize a page or component, extract hardcoded text, apply translations, fix missing resource strings, or says "run resx" / "move these strings to resx".
---

<!-- Bridge stub. Claude Code only discovers skills under .claude/skills/, so this file mirrors
     the frontmatter of the canonical skill and delegates to it. Edit the canonical file, not
     this one - only the frontmatter above needs to stay in sync. Tools that read both folders
     see the same name here and land on the same instructions either way. -->

Read `.agents/skills/localize-strings/SKILL.md` (relative to the repository root) and follow it exactly.
