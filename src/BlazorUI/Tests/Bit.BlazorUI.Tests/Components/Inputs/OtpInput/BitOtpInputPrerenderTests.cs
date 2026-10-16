using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.OtpInput;

/// <summary>
/// What an OTP input sends before the page is interactive (a prerender, or a static SSR page): no OnAfterRender runs,
/// so neither the javascript side nor the auto focus is set up, and the inputs still have to be there with the code
/// they were given.
/// </summary>
[TestClass]
public class BitOtpInputPrerenderTests
{
    [TestMethod]
    public async Task BitOtpInputShouldPrerenderItsInputsWithoutTheBrowser()
    {
        var html = await Prerenderer.RenderAsync<BitOtpInput>(new Dictionary<string, object?>
        {
            [nameof(BitOtpInput.Length)] = 4,
            [nameof(BitOtpInput.Label)] = "Verification code",
            [nameof(BitOtpInput.DefaultValue)] = "12",
            [nameof(BitOtpInput.AutoFocus)] = true,
            [nameof(BitOtpInput.Separator)] = "-",
        });

        Assert.AreEqual(4, Regex.Matches(html, "bit-otp-inp").Count);
        Assert.AreEqual(3, Regex.Matches(html, "bit-otp-sep").Count);
        StringAssert.Contains(html, "Verification code");
        StringAssert.Contains(html, "value=\"1\"");
        StringAssert.Contains(html, "value=\"2\"");
        StringAssert.Contains(html, "role=\"group\"");
    }

    [TestMethod]
    public async Task BitOtpInputShouldPrerenderTheHiddenFieldOfANamedCode()
    {
        var html = await Prerenderer.RenderAsync<BitOtpInput>(new Dictionary<string, object?>
        {
            [nameof(BitOtpInput.Length)] = 4,
            [nameof(BitOtpInput.Name)] = "otp",
            [nameof(BitOtpInput.DefaultValue)] = "1234",
        });

        // A plain form posts the whole code as one named field, which a static page has to carry as well.
        StringAssert.Contains(html, "type=\"hidden\"");
        StringAssert.Contains(html, "name=\"otp\"");
        StringAssert.Contains(html, "value=\"1234\"");
    }
}
