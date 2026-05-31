using Bit.Butil.E2ETests.Infrastructure;
using NUnit.Framework;

namespace Bit.Butil.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class StorageTests : ButilPageTest
{
    [Test]
    public async Task LocalStorage_RoundTrips_StringValues()
    {
        await ClickAndExpectAsync("ls-clear", "ls:clear");
        await ClickAndExpectAsync("ls-set", "ls:set");
        await ClickAndExpectAsync("ls-get", "ls:get:butil-e2e-value");
    }

    [Test]
    public async Task LocalStorage_RoundTrips_TypedPayload_ViaJsonGenerics()
    {
        await ClickAndExpectAsync("ls-clear", "ls:clear");
        await ClickAndExpectAsync("ls-typed-set", "ls:typed-set");
        await ClickAndExpectAsync("ls-typed-get", "ls:typed-get:42/answer");
    }

    [Test]
    public async Task SessionStorage_RoundTrips_StringValues()
    {
        await ClickAndExpectAsync("ss-clear", "ss:clear");
        await ClickAndExpectAsync("ss-set", "ss:set");
        await ClickAndExpectAsync("ss-get", "ss:get:butil-e2e-svalue");
    }
}
