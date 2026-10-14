# Bit.BlazorUI E2E Tests

End-to-end tests of every non-legacy component of `Bit.BlazorUI` and `Bit.BlazorUI.Extras`, driven in a real browser
with Playwright against a Blazor Server host that renders each component in every configuration its features need.
Where the unit tests (`Tests/Bit.BlazorUI.Tests`) pin the markup a component renders, these pin what a user gets: the
scripts, the stylesheets, the keyboard, the pointer, the focus, the layout and the round trips, in every engine.

## Structure

```
E2E/
├── Bit.BlazorUI.Tests.E2E.TestHost/        # Blazor Server app hosting one page per component
│   ├── Components/
│   │   ├── App.razor, Routes.razor
│   │   ├── Shared/E2EStatus.razor          # the status line every page carries ("Ready" once interactive)
│   │   ├── Shared/E2ESection.razor         # one titled, id-ed section per feature
│   │   └── Pages/<Category>/Bit<Component>E2E.razor   # @page "/e2e/<component>"
│   ├── Program.cs                          # services, upload endpoints, /api/server-errors
│   └── wwwroot/                            # the host's own stylesheet and the sample files pages serve
│
└── Bit.BlazorUI.Tests.E2E/                 # the Playwright + MSTest project
    ├── E2ETestBase.cs                      # host and browser lifecycle, OpenAsync, error capture
    ├── E2ETestRun.cs                       # run-wide cleanup and the parallelization setting
    └── <Category>/Bit<Component>E2ETests.cs
```

## Running

Build the solution once (`dotnet build src/BlazorUI/Bit.BlazorUI.slnx`), install the Playwright browsers once
(`pwsh Bit.BlazorUI.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install`), then in `Bit.BlazorUI.Tests.E2E`:

```bash
dotnet test -f net10.0
```

The first test starts the host (`dotnet run` of the TestHost on `http://localhost:5290`) and the run stops it at the
end. To pay the host's build once across runs, or to debug a page by hand, start it yourself and the run attaches to
it:

```bash
dotnet run --project Bit.BlazorUI.Tests.E2E.TestHost --framework net10.0 --urls http://localhost:5290
```

| Variable | Meaning |
|----------|---------|
| `BROWSER` | `chromium` (default), `edge`, `firefox` or `webkit` |
| `HEADED=1` | watch the browser |
| `E2E_HOST_URL` | the host to drive instead of `http://localhost:5290` |

Filter as usual: `dotnet test -f net10.0 --filter "FullyQualifiedName~BitButtonE2ETests"`. Test classes run four at
a time (`E2ETestRun.cs`); the tests of a class run in order. The whole suite is about 6,800 tests and takes roughly
a quarter of an hour on Chromium. On a loaded machine a Blazor Server circuit can time out or a fast keystroke can be
lost; a test that fails only in the full run and passes alone is load, not a regression.

If `dotnet test` reports that zero tests ran, run the built assembly directly:

```bash
E2E_HOST_URL=http://localhost:5290 dotnet exec bin/Debug/net10.0/Bit.BlazorUI.Tests.E2E.dll --filter "FullyQualifiedName~BitButtonE2ETests"
```

## Writing a page and its tests

- One page per component at `/e2e/<component>` (`Pages/<Category>/Bit<Component>E2E.razor`), made of `E2ESection`s,
  one per feature, each with a stable `Id` the tests reach it by, and ending with `<E2EStatus />` (or one with a
  `Status` the page sets when something of its own is done). Everything the tests read back - a bound value, how often
  an event fired, what it fired with - is written into the page as text in an element with an id, never inferred.
- One test class per component (`<Category>/Bit<Component>E2ETests.cs : E2ETestBase`), one test per feature, named
  for what the user does and sees. A test opens the page with `OpenAsync("/e2e/<component>")` and drives the real
  component: real clicks, real keys, real focus, the computed styles of the real stylesheet.
- A test fails at cleanup on any uncaught page error or console error its page raised; a test whose subject is an
  error sets `AllowConsoleErrors` (or `AllowPageErrors`). `AssertNoServerErrorsAsync` reads what the host logged for
  the circuits of the pages this test visited (the host records which pages each circuit was on), so a neighbouring
  test's error never fails it.
- Read state through Playwright's auto-waiting assertions (`Expect(...)`), never a one-off read right after an
  action: every change is a round trip to the server, and a transition may still be running.
- A category whose tests share helpers keeps them in a file of its own (`ButtonsE2EHelpers.cs`,
  `NotificationsE2EHelpers.cs`, `BitDataGridE2ETestBase.cs`, `BitDropdownE2ETestBase.cs`, `BitDatePickersE2ETestBase.cs`).
- An engine-specific gap is skipped with `Assert.Inconclusive` behind `IsFirefox` / `IsWebKit`, with the reason.
