---
name: localize-strings
description: Moves hardcoded user-facing strings out of Razor/C# files into AppStrings.resx, rewrites the call sites to use IStringLocalizer with nameof(AppStrings.Key), and fills in the new keys for every other language the project ships. Use when explicitly asked to localize or apply translations, or when the changes are about to be pushed.
---

# Move user-facing text into the resource files

This is the pass `AGENTS.md` section 5 leaves for later: it moves the English literals written through the `IStringLocalizer` indexer into `AppStrings.resx` and into every other language the project ships.

## What moves

- Every literal passed to the `Localizer` indexer, and every raw user-visible string, in the files the user named; without that, in the changes on the current branch.
- Not CSS classes or ids, configuration keys, URLs and routes, technical constants such as file extensions or MIME types, or log messages.

## Keys

- Reuse a key when `AppStrings.resx` already holds the same text.
- Otherwise add one with a short name that says what the text is for; related keys share a prefix, as the existing `SignIn*` and `Email*` ones do. A placeholder such as `{0}` stays in the value, and the call site keeps passing its argument.
- Each call site becomes `Localizer[nameof(AppStrings.Key)]`, and the strongly typed class is regenerated with the command in `AGENTS.md` section 4. An attribute takes a constant instead, so a DTO's `[Display(Name = "Delivery date")]` becomes `[Display(Name = nameof(AppStrings.DeliveryDate))]`.

## Every language

`AppStrings.resx` holds the English text. `Bit.ResxTranslator.json` lists every other language the project ships, each with its own `AppStrings.<language>.resx`, and a key missing there shows in English to that language's users.

Run `bit translate` from the project folder. It fills in every key the other languages lack, keeps the translations they already have, and uses the AI provider set in `Bit.ResxTranslator.json`. Its API key belongs in the `OpenAI__ApiKey` environment variable, not in the file. If no key is set, ask the user to set it in their environment, never to paste it into the conversation, or whether you should write the translations yourself.

## Done means

- No literal from the list above is left in the files in scope.
- `bit translate --check` reports no missing translation.
- The solution builds, and the pages show the same English text as before.
