# Accessibility verification

How a change to a bit BlazorUI component is checked with assistive technologies (AT), which screen reader and browser
pairs it is checked on, and how the result is recorded. Automated tests catch a lot (the markup tests of
`Tests/Bit.BlazorUI.Tests`, the keyboard, focus and layout tests of `Tests/E2E`, and the axe-core scan of the E2E
suites that have taken it up) but none of them hears what a screen reader says. This page covers the part that has to be
done by hand.

Nothing here claims WCAG conformance. A record says what was checked, on what, by whom and when, and nothing more.

## When a manual check is required

- **Always** for a change to a composite widget's roles, keyboard model, focus handling or announcements. A composite
  widget is one with a keyboard model of its own: grid and treegrid (`BitDataGrid`), the date grids (`BitCalendar`,
  `BitDatePicker`, `BitDateRangePicker`, `BitFullCalendar`), menus (`BitMenuButton`, `BitDropMenu`), listbox and
  combobox (`BitDropdown`, `BitSearchBox`), tabs (`BitPivot`), the disclosure navigation of `BitNav`, dialogs
  (`BitDialog`, `BitModal`, `BitPanel`, `BitCallout`), the editors (`BitRichTextEditor`, `BitMarkdownEditor`) and
  `BitPdfViewer`.
- **Always** before a component is marked remediated in the accessibility work (W2 of the improvement plan).
- **Recommended** for any other change a user of AT would notice: a new accessible name, a new live-region
  announcement, a different focus target.
- **Not needed** for a change with no rendered effect and no effect on the accessibility tree (refactoring, a test, a
  style change that leaves the content, color, focus, motion and what is exposed to AT alone). A style change can
  reach the tree: `display`, `visibility` and generated `content` all change what AT is given.

## The matrix

| Tier | Screen reader | Browser | Platform | Why |
|---|---|---|---|---|
| 1 | NVDA (current release) | Firefox **and** Chrome | Windows | The most used free screen reader. Firefox and Chrome expose the page through different accessibility trees, so both are run |
| 1 | JAWS (current release) | Chrome | Windows | The most used commercial screen reader; its virtual cursor reads tables, grids and forms its own way |
| 1 | VoiceOver | Safari | macOS (current) | The only screen reader on macOS; Safari is the browser VoiceOver is tuned for |
| 1 | VoiceOver | Safari | iOS / iPadOS (current) | Touch exploration, the rotor and swipe gestures: required for the touch-aware components (`BitPanel`, `BitSwipeTrap`, `BitPullToRefresh`, `BitNavBar`, the `Responsive` popups) |
| 2 | TalkBack | Chrome | Android (current) | Touch exploration on Android; required for the touch-aware components, recommended otherwise |
| 2 | Narrator | Edge | Windows | The built-in Windows screen reader. Also what reaches a Blazor Hybrid app on Windows (WebView2) |

- **Tier 1** is required wherever a manual check is required (above). A run on fewer pairs is recorded as partial,
  and the record lists the pairs it did not reach.
- **Tier 2** is required for the touch-aware components and recommended for the rest.
- **Blazor Hybrid:** a MAUI Blazor app renders in WebView2 on Windows, WKWebView on iOS and macOS, and the Android
  WebView. A component whose behaviour differs there (gestures, the safe areas, the virtual keyboard) is also checked in
  a Hybrid app with the platform's screen reader.
- **Versions:** the current release of each screen reader and browser at the time of the run, written into the
  record. An older pair is checked only to reproduce a reported issue.

Beside the screen readers, every manual check covers:

- **Keyboard only**, with no screen reader running: every action reachable, the focus always visible, never lost or
  trapped.
- **Forced colors**: Windows contrast themes (a light and a dark one) in Edge or Chrome.
- **Zoom and reflow**: 200% browser zoom, and a 320 CSS-pixel-wide viewport (400% zoom of a 1280-pixel window).
- **Reduced motion**: the OS setting on, which the components read through `prefers-reduced-motion`.
- **Right-to-left**, for a component with a direction of its own (arrows, placement, swipe): the same checks with
  `Dir="BitDir.Rtl"`.

## What to check with each screen reader

1. **Name, role, state:** what is announced when the focus lands on each part (the name, the role, and every state:
   expanded, selected, checked, pressed, disabled, invalid, busy, current).
2. **Reading:** the browse or virtual mode reads the component in a sensible order. On touch (VoiceOver on iOS,
   TalkBack) it is read by swiping through it and by exploring it with a finger, and both follow the same order. A
   table or grid is read with its headers. A part hidden from AT (`aria-hidden`, `hidden`, `display: none`, `inert`)
   is not read; text hidden only visually and written for screen reader users is read, and that is no failure.
3. **Operation:** every action the component offers works in focus or forms mode with the keys its pattern defines
   (the WAI-ARIA Authoring Practices pattern linked from the record). On touch, every action works with the screen
   reader's gestures.
4. **Changes:** what changes without the focus moving is announced once, through a live region or a state change.
   That covers a sort, a filter, a loading state, an error or a count.
5. **Focus:** opening a popup or dialog moves the focus into it. A combobox whose list is driven from its input
   (`BitSearchBox`) keeps the DOM focus on the input instead: `aria-activedescendant` names the active option, and
   the option is announced as it changes. Closing returns the focus to where it came from, and a removed element hands
   the focus on instead of dropping it on the page.

## Recording a run

- One file per component in a `records/` folder beside this page, named after the component (`records/BitDataGrid.md`;
  the folder starts with the first record), copied from [the template](manual-verification-template.md). A later run
  adds a dated section at the top of the same file, so the history stays in one place.
- Every check is marked *Pass*, *Fail*, *N/A* or *Not run*. *Not run* is never left blank or written as *Pass*.
- Every *Fail* links the issue that tracks it. A failure that is accepted for now (a screen reader bug, a limitation
  of the pattern) says why.
- A screen reader's own bug is recorded as such, with the screen reader's version and, where one exists, its issue.
  The component is not changed to work around one screen reader at the cost of the others.

## Regression policy

- An accessibility regression is a bug of the same severity as a functional one. It covers a lost or wrong name, role
  or state; a key that stopped working; focus lost, trapped or not restored; an announcement lost or repeated; a
  contrast or forced-colors failure; and motion that ignores reduced motion. It is fixed forward, not suppressed.
- The fix comes with the smallest automated test that would have caught it: a markup test for a name, role or state,
  an E2E test for keyboard, focus or layout, and the axe scan for what its rules cover. A regression only a screen
  reader shows is fixed with a manual record that reproduces it before and passes after.
- An axe suppression in the E2E suite follows the suppression policy written beside the scan (one rule, one selector,
  a reason, and the issue that tracks the fix).
- A change that alters what AT users get on purpose is listed in `BreakingChanges.md` when an app or its tests could
  depend on the old behaviour (a role, a name, a tab stop).
