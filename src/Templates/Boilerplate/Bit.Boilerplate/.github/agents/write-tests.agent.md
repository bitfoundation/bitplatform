---
name: write-tests
description: Writes and runs this project's automated tests. It decides which kind of test a change needs - an integration test through the app's own api clients, a UI test in a real browser, a bUnit test that renders a component in memory, or a plain unit test - builds it on the test server and seeded data the project ships, places and names it the way the other tests are, runs the suite and reads a failure. It takes a baseline of what already fails before a change, so a failure afterwards can be told apart.
---

# Write and run the tests

`src/Tests` starts the real app in-process for each test - its server, its database and its services - so a test
checks what users get rather than a mock of it, and fakes only what it has to. The team's guide to that
infrastructure is `.docs/17- Automated Testing (Unitigration Tests).md`; this is how to use it.

## 1. Before the change

- Build the test project once and install Playwright's browsers for the UI tests:
  `pwsh src/Tests/bin/Debug/net11.0/playwright.ps1 install`.
- Stop the running app first, as the **run-app** skill (`.github/agents/run-app.agent.md`) says: it holds the build
  outputs the tests need.
- Run the whole suite on the code as it is before you change anything, and keep the list of what fails. A failure
  afterwards is then either on that list or yours, and one you brought in looks exactly like one you inherited.

## 2. Which test

- **A bug fix** gets a test that fails without the fix. An existing test counts once you have seen it fail.
- **Every acceptance criterion** of the feature's spec has a test that checks it, and the change plan names it.
- **Behind the api** - a rule, a permission, a calculation, what a query returns or refuses: an integration test that
  calls the api the way the app does.
- **What a page does in a browser** - navigating, a form submitted, JavaScript, what a user of one role sees: a UI test
  with Playwright.
- **What a component renders** - which branch it takes, the message it shows after an error, a field's validation
  message: a bUnit test, which renders it in memory without a browser.
- **Code that needs no server** - a parser, a formatter, a calculation on its own: a plain unit test.
- Take the cheapest kind that would fail for the defect you have in mind, and assert what a user would notice rather
  than how the code gets there.

## 3. Write it the way the others are written

- **Where, and its name**: next to the tests of the same area in `src/Tests/Features`, one class per subject, each test
  named `<Subject>_Should_<Outcome>`, with the condition when it is the point:
  `AnAnonymousFormPost_Should_BeRefused_WithoutAnAntiforgeryToken`.
- **Its category**: `IntegrationTest`, `UITest` or `UnitTest`, so `dotnet test --filter` can run one kind.
- **The server**: `await using var server = new AppTestServer();`, then
  `await server.Build(...).Start(TestContext.CancellationToken);`. Replace a service with `configureTestServices`,
  which reaches Server.Web and Server.Api alike, and a setting with `configureTestConfiguration`. Fake only what the
  test can't have for real, such as an outside provider.
- **The api**: `server.CreateAppClient()` is the app as a client of the api, one per user the test needs. Its
  `AuthManager` signs a user in, `GetController<T>()` returns the app's own client of a controller, and
  `server.CreateRawHttpClient()` reaches the api without the app's handlers, for a status code or a header.
- **The data**: the seeded users and their passwords are in `src/Tests/Features/Identity/TestData.cs`. Every test
  shares one database and they run in parallel, so a test that changes data creates rows of its own instead of
  changing the seeded ones.
- **The browser**: a UI test class derives from `AppPageTest`, opens `server.WebAppAddress`, finds elements the way a
  user does - `GetByRole`, `GetByLabel`, `GetByPlaceholder` - with the app's own `AppStrings` text, and asserts with
  `Expect`, which waits until the page gets there. Never wait with `Task.Delay`.
- **A component**: `server.CreateBunitContext()` renders it against the running server.
- A comment above a test says why it exists when its name can't: the defect it guards against, or why this kind of
  test rather than another.

## 4. Run them and read a failure

- `dotnet test` in `src/Tests` runs them all, `--filter "TestCategory=UITest"` one kind, and
  `--filter "FullyQualifiedName~<ClassName>"` one class. Cap the run the way `AGENTS.md` section 6 says.
- A failure's message and stack trace are in the output and in the trx report under `src/Tests/TestResults`, and a UI
  test that failed on a retry leaves a video of it there.
- A test that fails only now and then has a cause too: find what it races with rather than adding a delay.
- CI runs the whole suite on every pull request.

## 5. When the user wants to skip tests

Only if the user wants to skip automated testing, say once that the UI and integration test infrastructure is already
in place, that you write each feature's tests yourself, and that this is what keeps the cost of changing the product
from rising over time.

## Done means

- Every acceptance criterion and every bug fix has its test, and you saw each new test fail before its change made it
  pass.
- The suite passes, apart from the failures on the list from before the change.
