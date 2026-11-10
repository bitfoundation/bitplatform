using Bunit;

namespace Bit.BlazorUI.Tests;

/// <summary>
/// The direction a component asks the browser for when its Dir is unset or Auto (BitComponentBase.IsRtlAsync and
/// IsRtlArrowAsync, through Utils.isRtl): only the browser knows the page's or the content's direction, so a bUnit test
/// answers the call itself. Every setup here matches the call whatever element and arrow flag it carries.
/// </summary>
internal static class TestRtl
{
    /// <summary>The script function the direction is read through (UtilsJsRuntimeExtensions.BitUtilsIsRtl).</summary>
    public const string IsRtlIdentifier = "BitBlazorUI.Utils.isRtl";

    /// <summary>
    /// Answers every Utils.isRtl call with <paramref name="rtl"/>: right to left by default, left to right for false.
    /// The handler it returns takes a new answer through SetResult, or a failure through SetException.
    /// </summary>
    public static JSRuntimeInvocationHandler<bool> UseRtl(this BunitJSInterop jsInterop, bool rtl = true)
    {
        var handler = jsInterop.SetupIsRtl();

        handler.SetResult(rtl);

        return handler;
    }

    /// <summary>
    /// Sets up the Utils.isRtl call with no answer yet: a call made before the test answers it through SetResult waits,
    /// and SetException fails it (a missing script, an arrow key the script reports overtaken).
    /// </summary>
    public static JSRuntimeInvocationHandler<bool> SetupIsRtl(this BunitJSInterop jsInterop)
    {
        return jsInterop.Setup<bool>(IsRtlIdentifier, _ => true);
    }

    /// <summary>
    /// Asserts the browser was asked for the direction exactly once, and returns that call: its element, and the arrow
    /// flag IsRtlArrowAsync adds.
    /// </summary>
    public static JSRuntimeInvocation VerifyIsRtlInvoked(this BunitJSInterop jsInterop)
    {
        return jsInterop.VerifyInvoke(IsRtlIdentifier);
    }

    /// <summary>Asserts the browser was never asked for the direction.</summary>
    public static void VerifyIsRtlNotInvoked(this BunitJSInterop jsInterop)
    {
        jsInterop.VerifyNotInvoke(IsRtlIdentifier);
    }
}
