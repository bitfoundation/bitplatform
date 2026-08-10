using Hangfire;
using PhoneNumbers;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("IntegrationTest")]
public partial class PhoneNumberNormalizationTests
{
    /// <summary>
    /// The Sign in page accepts a phone number in any human format, but the server must always text the one-time code to
    /// the canonical E.164 number: whatever the visitor types, <c>PhoneService.NormalizePhoneNumber</c> (libphonenumber,
    /// See <c>IdentityController.SendOtp</c>) runs before <c>PhoneService.SendSms</c> is ever called.
    /// <list type="number">
    /// <item>Replace the real <c>PhoneService</c> with a subclass that overrides only <c>SendSms</c> to record every
    /// (message, phone-number) pair into a static collector and deliver nothing - no Hangfire job, no Twilio - while
    /// still using the real <c>NormalizePhoneNumber</c>, which is exactly the behavior under test.</item>
    /// <item>For three brand-new random US numbers, each written in a different de-normalized format (parentheses + dash,
    /// dots, spaces), request the code. A brand-new number makes the server register the (still unconfirmed) account,
    /// text the confirmation code and answer "not confirmed" - the very response that reveals the OTP panel in the UI.</item>
    /// <item>Assert every number handed to <c>SendSms</c> arrived already normalized to E.164 - a leading "+", digits
    /// only, none of the punctuation that was typed.</item>
    /// <item>For the last number, finish signing in: read the 6 digit code straight from the captured SMS body (the SMS
    /// is the only place a phone code is delivered) and confirm with the number still in its typed form, which proves the
    /// account really was created against the normalized one.</item>
    /// </list>
    /// <para>
    /// Nothing here is about rendering: what the sign-in form contributes is a phone number in the request body, so the
    /// requests are made directly and no browser is started. The Phone tab's own behavior (its de-bounce, the OTP panel)
    /// is covered by the Playwright sign-in journeys.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task SendOtp_Should_NormalizePhoneNumber_BeforeSendingSms()
    {
        // A shared static collector can never mix in another test's calls because every assertion below filters by this
        // run's own unique random numbers; clearing it up-front just keeps the failure messages readable.
        CapturingPhoneService.SentMessages.Clear();

        await using var server = new AppTestServer();
        await server.Build(services =>
        {
            // Swap the real PhoneService (registered as AddScoped<PhoneService> in Program.Services) for the capturing
            // subclass. IdentityController injects the concrete PhoneService, so the service type stays PhoneService and
            // only the implementation becomes the fake.
            services.RemoveAll<PhoneService>();
            services.AddScoped<PhoneService, CapturingPhoneService>();
        }).Start(TestContext.CancellationToken);

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

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        foreach (var (Typed, Normalized) in attempts)
        {
            // A brand-new number auto-provisions the (still unconfirmed) account, texts the confirmation code and answers
            // "not confirmed". Asserting on that specific key is what proves SendSms really ran on this call rather than
            // the request failing earlier for some unrelated reason.
            var notConfirmed = await Assert.ThrowsExactlyAsync<BadRequestException>(
                () => identityController.SendOtp(new() { PhoneNumber = Typed }, null, TestContext.CancellationToken));

            Assert.AreEqual(nameof(AppStrings.UserIsNotConfirmed), notConfirmed.Key,
                $"Requesting a code for the brand-new number '{Typed}' should have reported the account as unconfirmed.");

            var sms = await WaitForSmsTo(Normalized, TestContext.CancellationToken);

            Assert.AreEqual(Normalized, sms.PhoneNumber,
                $"The server should have normalized '{Typed}' to E.164 before calling SendSms.");
            Assert.MatchesRegex(e164, sms.PhoneNumber,
                "SendSms must receive a canonical E.164 number (leading '+', digits only, no formatting).");
        }

        // Complete the sign-in for the last number using the code from its SMS.
        var lastSms = await WaitForSmsTo(attempts[^1].Normalized, TestContext.CancellationToken);

        // The confirmation SMS reads "{code} is your code in Boilerplate.\n@host #code" (See ConfirmPhoneTokenShortText),
        // so the code is the first 6 digit run in the body.
        var otpCode = Regex.Match(lastSms.MessageText, @"\d{6}").Value;
        Assert.MatchesRegex(new Regex(@"^\d{6}$"), otpCode,
            "The confirmation SMS should start with a 6 digit code.");

        // Confirming with the number still in its typed form is the second half of the proof: the account exists under the
        // normalized number, so this only succeeds because the confirm path normalizes what it is given the very same way.
        var tokens = await identityController.ConfirmPhone(
            new() { Token = otpCode, PhoneNumber = attempts[^1].Typed }, TestContext.CancellationToken);

        Assert.IsFalse(string.IsNullOrEmpty(tokens.AccessToken),
            "Confirming with the code texted to the normalized number should have signed the account in.");
    }

    /// <summary>
    /// Polls the static collector for the newest <c>SendSms</c> call addressed to <paramref name="phoneNumber"/>. The
    /// call is normally already recorded (SendSms runs synchronously inside the request that then reports the account as
    /// unconfirmed); the short poll only guards against reading a hair too early.
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

    public TestContext TestContext { get; set; } = default!;
}

/// <summary>
/// Test double for <see cref="PhoneService"/> that records every <see cref="SendSms"/> call and delivers nothing.
/// Only delivery is faked: <c>NormalizePhoneNumber</c> is left to the real base, so the number reaching <c>SendSms</c>
/// is exactly what the server normalized - which is the behavior under test. Registered per-test via
/// <c>configureTestServices</c> (RemoveAll&lt;PhoneService&gt; then AddScoped&lt;PhoneService, CapturingPhoneService&gt;).
/// </summary>
public partial class CapturingPhoneService(ServerApiSettings appSettings, IBackgroundJobClient backgroundJobClient, IHostEnvironment hostEnvironment, IHttpContextAccessor httpContextAccessor, IStringLocalizer<AppStrings> localizer, ILogger<PhoneService> phoneLogger, PhoneNumberUtil phoneNumberUtil) :
    PhoneService(appSettings, backgroundJobClient, hostEnvironment, httpContextAccessor, localizer, phoneLogger, phoneNumberUtil)
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
