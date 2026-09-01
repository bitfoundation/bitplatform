@./AGENTS.md

# Gemini

`AGENTS.md` at the repository root is the single source of truth for all coding conventions, project structure, technology stack and behavioral directives. The `@./AGENTS.md` import above already inlined its full content into this session - do not read that file again. (Google Antigravity reads `AGENTS.md` directly and does not need this file.)

Agent Skills are canonical in `.agents/skills/` (see `AGENTS.md` section 7 for the list). Antigravity and recent Gemini CLI builds discover them there natively; `.gemini/commands/` additionally ships one command shim per skill so each one is always invocable as `/<skill-name>` in Gemini CLI and Gemini Code Assist agent mode. When changing a skill, edit the canonical file in `.agents/skills/` - the shims only inject it.
