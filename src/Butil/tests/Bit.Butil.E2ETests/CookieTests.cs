using Bit.Butil.E2ETests.Infrastructure;
using NUnit.Framework;

namespace Bit.Butil.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class CookieTests : ButilPageTest
{
    [Test]
    public async Task Cookie_Set_Get_Survives_Reserved_Characters_Roundtrip()
    {
        // Pre-clean: removing twice doesn't break anything if the cookie isn't there yet.
        await ClickAndExpectAsync("cookie-remove", "cookie:removed:");

        await ClickAndExpectAsync("cookie-set", "cookie:set");
        await ClickAndExpectAsync("cookie-get", "cookie:get:v=1; b=hello world & again");
    }

    [Test]
    public async Task Cookie_Remove_Deletes_The_Entry()
    {
        await ClickAndExpectAsync("cookie-set", "cookie:set");
        await ClickAndExpectAsync("cookie-remove", "cookie:removed:True");
    }
}
