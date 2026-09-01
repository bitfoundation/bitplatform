@AGENTS.md

# Claude Code

`AGENTS.md` at the repository root is the single source of truth for all coding conventions, project structure, technology stack and behavioral directives. The `@AGENTS.md` import above already inlined its full content into this session - do not read that file again.

Agent Skills are canonical in `.agents/skills/` (see `AGENTS.md` section 7 for the list). Claude Code only discovers skills under `.claude/skills/`, so that folder holds one thin bridge stub per skill which delegates to the canonical file. When changing a skill, edit the file in `.agents/skills/` and keep only the frontmatter of the matching stub in sync - never fork the body.
