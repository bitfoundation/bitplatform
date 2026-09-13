using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class StorageTests : ButilPageTest
{
    [TestMethod]
    public async Task LocalStorage_RoundTrips_StringValues()
    {
        await ClickAndExpectAsync("ls-clear", "ls:clear");
        await ClickAndExpectAsync("ls-set", "ls:set");
        await ClickAndExpectAsync("ls-get", "ls:get:butil-e2e-value");
    }

    [TestMethod]
    public async Task LocalStorage_RoundTrips_TypedPayload_ViaJsonGenerics()
    {
        await ClickAndExpectAsync("ls-clear", "ls:clear");
        await ClickAndExpectAsync("ls-typed-set", "ls:typed-set");
        await ClickAndExpectAsync("ls-typed-get", "ls:typed-get:42/answer");
    }

    [TestMethod]
    public async Task SessionStorage_RoundTrips_StringValues()
    {
        await ClickAndExpectAsync("ss-clear", "ss:clear");
        await ClickAndExpectAsync("ss-set", "ss:set");
        await ClickAndExpectAsync("ss-get", "ss:get:butil-e2e-svalue");
    }

    [TestMethod]
    public async Task LocalStorage_RemoveItem_Removes_The_Key()
    {
        await ClickAndExpectAsync("ls-remove", "ls:removed:True");
    }

    [TestMethod]
    public async Task LocalStorage_Reports_Length_Key_And_ContainsKey()
    {
        // After clear + two known writes: length is 2, the first key is "alpha",
        // ContainsKey("alpha") is true and ContainsKey("ghost") is false.
        await ClickAndExpectAsync("ls-meta", "ls:meta:2/alpha/True/False");
    }
}
