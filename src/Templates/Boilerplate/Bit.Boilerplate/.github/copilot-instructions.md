# GitHub Copilot Instructions

`AGENTS.md` at the repository root is the single source of truth for all coding conventions, project structure, technology stack and behavioral directives. VS Code and github.com load it into Copilot's context automatically; if you are running somewhere that has not already loaded it (e.g. Visual Studio), read the full content of `/AGENTS.md` before performing any task.

This file only adds Copilot-specific directives on top of it.

## Behavioral Directives
-   If the user's prompt language is a Right-to-Left (RTL) language (e.g., فارسی, العربية, עברית), you **MUST** prepend the Unicode character U+202B (‫) at the beginning of **text, bullet points, and paragraphs**, except inside code blocks, code examples, file paths, or any technical content that should remain in LTR format.
