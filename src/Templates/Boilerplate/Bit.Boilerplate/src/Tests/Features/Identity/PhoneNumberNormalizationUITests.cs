using Hangfire;
using PhoneNumbers;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Infrastructure.Services;
using Boilerplate.Tests.Infrastructure.Components;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest")]
public partial class PhoneNumberNormalizationUITests : AppPageTest
{
    /// <summary>
    /// The Sign in page accepts a phone number in any human format, but the server must always text the one-time code to
    /// the canonical E.164 number: whatever the visitor types, <c>PhoneService.NormalizePhoneNumber</c> (libphonenumber,
    /// See <c>IdentityController.SendOtp</c>) runs before <c>PhoneService.SendSms</c> is ever called.
    /// <list type="number">
    /// <item>Replace the real <c>PhoneService</c> with a subclass that overrides only <c>SendSms</c> to record every
    /// (message, phone-number) pair into a static collector and deliver nothing - no Hangfire job, no Twilio - while
    /// still using the real <c>NormalizePhoneNumber</c>, which is exactly the behavior under test.</item>
    /// <item>For three brand-new random US numbers, each typed in a different de-normalized format (parentheses + dash,
    /// dots, spaces), switch to the Phone tab and request the code. A brand-new number makes the server register the
    /// (still unconfirmed) account, text the confirmation code and reveal the OTP panel.</item>
    /// <item>Assert every number handed to <c>SendSms</c> arrived already normalized to E.164 - a leading "+", digits
    /// only, none of the punctuation that was typed.</item>
    /// <item>For the last number, finish signing in: read the 6 digit code straight from the captured SMS body (the SMS
    /// is the only place a phone code is delivered), type it into the OTP panel and land on the home page, signed in.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task SignIn_Should_NormalizePhoneNumber_BeforeSendingSms()
    {
        // A shared static collector can never mix in another test's calls because every assertion below filters by this
        // run's own unique random numbers; clearing it up-front just keeps the failure messages readable.
        CapturingPhoneService.SentMessages.Clear();

        await using var server = new AppTestServer(Context);
        await server.Build(services =>
        {
            // Swap the real PhoneService (registered as AddScoped<PhoneService> in Program.Services) for the capturing
            // subclass, only for this test's server. IdentityController injects the concrete PhoneService, so the service
            // type stays PhoneService and only the implementation becomes the fake.
            services.RemoveAll<PhoneService>();
            services.AddScoped<PhoneService, CapturingPhoneService>();
        }).Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        // Three brand-new, unique US numbers (random area code + exchange, three consecutive subscriber numbers), each
        // written in a different de-normalized format. All three must normalize to "+1" + the ten digits. Random keeps
        // them unique per run so a process-wide collector can never confuse them with another run's calls.
        var random = Random.Shared;
        var areaCode = random.Next(200, 1000);    // 3 digits, first digit 2-9
        var exchange = random.Next(200, 1000);    // 3 digits, first digit 2-9
        var subscriber = random.Next(1000, 9000); // 4 digit base; +0/+1/+2 keeps all three distinct and still 4 digits

        var attempts = new[]
        {
            (Typed: $"({areaCode}) {exchange}-{subscriber + 0:D4}", Normalized: $"+1{areaCode}{exchange}{subscriber + 0:D4}"),
            (Typed: $"{areaCode}.{exchange}.{subscriber + 1:D4}",   Normalized: $"+1{areaCode}{exchange}{subscriber + 1:D4}"),
            (Typed: $"{areaCode} {exchange} {subscriber + 2:D4}",   Normalized: $"+1{areaCode}{exchange}{subscriber + 2:D4}"),
        };

        // Canonical E.164: a leading '+' and digits only. Fails on any space, parenthesis, dash or dot, so matching it
        // proves the number was normalized (not merely passed through).
        var e164 = new Regex(@"^\+[1-9]\d{6,14}$");

        foreach (var attempt in attempts)
        {
            // Each request starts from a fresh Sign in page. A hard reload is the simplest way back to the phone form:
            // the OTP panel is treated as a modal, but its NavigationLock only intercepts internal Blazor navigation
            // (SignInPanel.razor), so a full page navigation resets the panel cleanly.
            await Page.GotoAsync(new Uri(serverAddress, PageUrls.SignIn).ToString(),
                new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Switch from the default Email tab to the Phone tab (BitPivot HeaderOnly renders each header as role="tab").
            await Page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.Phone, Exact = true }).ClickAsync();

            // Type the de-normalized number into the BitPhoneInput's number box (its <input type="tel"> carries the
            // placeholder) and ask for the code. Send OTP stays disabled until the debounced value commits, so
            // Playwright's actionability wait covers the 500ms debounce.
            await Page.GetByPlaceholder(AppStrings.PhoneNumberPlaceholder).FillAsync(attempt.Typed);
            await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SendOtpButtonText }).ClickAsync();

            // The server registers the new account, texts the confirmation code (captured synchronously by the fake) and
            // answers "not confirmed", which reveals the OTP panel - a reliable signal that SendSms has already run.
            await Page.Locator(".bit-otp-inp").First.WaitForAsync();

            var sms = await WaitForSmsTo(attempt.Normalized, TestContext.CancellationToken);

            Assert.AreEqual(attempt.Normalized, sms.PhoneNumber,
                $"The server should have normalized '{attempt.Typed}' to E.164 before calling SendSms.");
            Assert.MatchesRegex(e164, sms.PhoneNumber,
                "SendSms must receive a canonical E.164 number (leading '+', digits only, no formatting).");
        }

        // The OTP panel for the last number is still on screen. Complete the sign-in using the code from its SMS body.
        var lastSms = await WaitForSmsTo(attempts[^1].Normalized, TestContext.CancellationToken);

        // The confirmation SMS reads "{code} is your code in Boilerplate.\n@host #code" (See ConfirmPhoneTokenShortText),
        // so the code is the first 6 digit run in the body.
        var otpCode = Regex.Match(lastSms.MessageText, @"\d{6}").Value;
        Assert.MatchesRegex(new Regex(@"^\d{6}$"), otpCode,
            "The confirmation SMS should start with a 6 digit code.");

        await BitOtpInputUtils.FillOtpInputs(Page, otpCode);

        // Filling the last digit confirms the phone number, signs the account in and redirects to the home page.
        await Expect(Page).ToHaveURLAsync(serverAddress.ToString());
        await Expect(Page.Locator(".bit-prs.persona").First).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }

    /// <summary>
    /// Polls the static collector for the newest <c>SendSms</c> call addressed to <paramref name="phoneNumber"/>. The
    /// call is normally already recorded (SendSms runs synchronously inside the request that then reveals the OTP panel);
    /// the short poll only guards against reading a hair too early.
    /// </summary>
    private static async Task<(string MessageText, string PhoneNumber)> WaitForSmsTo(string phoneNumber, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);

        while (true)
        {
            // Newest-first so a freshly requested code wins over any earlier one for the same number.
            var match = CapturingPhoneService.SentMessages.LastOrDefault(sms => sms.PhoneNumber == phoneNumber);
            if (match.PhoneNumber is not null)
                return match;

            if (DateTimeOffset.UtcNow >= deadline)
                throw new InvalidOperationException(
                    $"No SMS was captured for '{phoneNumber}'. Captured numbers: " +
                    $"[{string.Join(", ", CapturingPhoneService.SentMessages.Select(sms => sms.PhoneNumber))}].");

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }
}

/// <summary>
/// Test double for <see cref="PhoneService"/> that records every <see cref="SendSms"/> call and delivers nothing.
/// Only delivery is faked: <c>NormalizePhoneNumber</c> is left to the real base, so the number reaching <c>SendSms</c>
/// is exactly what the server normalized - which is the behavior under test. Registered per-test via
/// <c>configureTestServices</c> (RemoveAll&lt;PhoneService&gt; then AddScoped&lt;PhoneService, CapturingPhoneService&gt;).
/// </summary>
public partial class CapturingPhoneService(ServerApiSettings appSettings, IBackgroundJobClient backgroundJobClient, IHostEnvironment hostEnvironment, IHttpContextAccessor httpContextAccessor, ILogger<PhoneService> phoneLogger, PhoneNumberUtil phoneNumberUtil) :
    PhoneService(appSettings, backgroundJobClient, hostEnvironment, httpContextAccessor, phoneLogger, phoneNumberUtil)
{
    /// <summary>
    /// Every SendSms call as (message body, phone number). Static because DI owns the resolved instance's lifetime, so a
    /// test cannot hold a reference to the fake; entries are filtered by each test's unique numbers to stay isolated.
    /// </summary>
    public static readonly ConcurrentQueue<(string MessageText, string PhoneNumber)> SentMessages = new();

    public override Task SendSms(string messageText, string phoneNumber)
    {
        SentMessages.Enqueue((messageText, phoneNumber));
        return Task.CompletedTask; // Do not call base: no Hangfire delivery job and no Twilio in tests.
    }
}
