# Specs

This folder says what the product does and why, in plain words, next to the code. Everyone on the team and every AI tool works from the same decisions here, instead of from someone's chat history. Write in the language the team works in.

## Feature specs

One file per feature area, named after it, such as `orders.md`, with these sections, leaving out any that would be empty:

- **Purpose**: what the feature is for, and who uses it.
- **Requirements**: what the app does, each with acceptance criteria written as outcomes someone can check, such as "When a customer cancels an order that has already shipped, the app refuses and says why". The feature's tests check these.
- **Decisions**: what was decided and why, with the options that were turned down.
- **Open questions**: what is not decided yet.

A feature spec describes the product as its branch has it. A change that alters a feature updates its spec in the same pull request, so the review shows the old and the new behavior side by side.

## Change plans

A larger change gets one file in `changes/`, named with the date it started and a short name, such as `changes/YYYY-MM-DD-order-cancellation.md`:

- the feature specs it adds or changes
- the design: the parts it touches, the approach, the trade-offs and risks
- the tasks, as a checklist ticked as each one is done
- any change to the plan, with its reason

A plan stays after its pull request merges, as the record of how and why the change was made.
