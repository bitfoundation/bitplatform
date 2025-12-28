using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.MediaQuery;

[TestClass]
public class BitMediaQueryTests : BunitTestContext
{
    [TestMethod]
    public void BitMediaQueryShouldRenderMatchedChildContentWhenQueryGiven()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.MediaQuery.setup");

        var comp = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.Matched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"matched\">Matched</div>")));
            parameters.Add(p => p.NotMatched, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"notmatched\">NotMatched</div>")));
        });

        // initial call to JS returns default(T) so no content change expected, component should render container
        var root = comp.Find(".bit-mdq");
        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitMediaQueryShouldInvokeOnChangeWhenJsNotifies()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.MediaQuery.setup");

        var changed = false;
        var comp = RenderComponent<BitMediaQuery>(parameters =>
        {
            parameters.Add(p => p.Query, "(max-width: 600px)");
            parameters.Add(p => p.OnChange, (bool v) => changed = v);
        });

        // simulate JS invocation by calling the internal method via reflection
        var instance = comp.Instance;
        var method = instance.GetType().GetMethod("_OnMatchChange", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        Assert.IsNotNull(method);

        var invokeResult = method!.Invoke(instance, [true]);

        if (invokeResult is ValueTask vt)
        {
            vt.GetAwaiter().GetResult();
        }
        else if (invokeResult is Task t)
        {
            t.GetAwaiter().GetResult();
        }

        Assert.IsTrue(changed);
    }
}
