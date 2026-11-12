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

        Assert.HasCount(4, Regex.Matches(html, "bit-otp-inp"));
        Assert.HasCount(3, Regex.Matches(html, "bit-otp-sep"));
        Assert.Contains("Verification code", html);
        Assert.Contains("value=\"1\"", html);
        Assert.Contains("value=\"2\"", html);
        Assert.Contains("role=\"group\"", html);
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
        Assert.Contains("type=\"hidden\"", html);
        Assert.Contains("name=\"otp\"", html);
        Assert.Contains("value=\"1234\"", html);
    }
}
