using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>Tests for the drag-edge improvements (plan item 2.5): the DragPropagation flag wiring.</summary>
[TestClass]
public class DragEdgesTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    private static IReadOnlyDictionary<string, object?> AttachedFlags(FakeBmotionInterop interop)
    {
        var call = interop.Calls.First(c => c.Method == "attachEventListeners");
        return (IReadOnlyDictionary<string, object?>)call.Args[1]!;
    }

    [TestMethod]
    public void DragPropagation_True_FlagsPropagation()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Drag, BmDrag.Both)
            .Add(p => p.DragPropagation, true)
            .Add(p => p.ChildContent, Div));

        var flags = AttachedFlags(ctx.Interop);
        Assert.AreEqual(true, flags["drag"]);
        Assert.AreEqual(true, flags["dragPropagation"]);
    }

    [TestMethod]
    public void DragPropagation_DefaultFalse_OmitsFlag()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Drag, BmDrag.Both)
            .Add(p => p.ChildContent, Div));

        var flags = AttachedFlags(ctx.Interop);
        Assert.IsFalse(flags.ContainsKey("dragPropagation"),
            "propagation must default off (nested drags isolated) and not emit the flag");
    }
}
