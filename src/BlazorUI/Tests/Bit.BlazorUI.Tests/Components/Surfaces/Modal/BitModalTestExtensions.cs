using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

internal static class BitModalTestExtensions
{
    /// <summary>
    /// Presses Escape inside the Modal whose root the selector finds, the way the browser delivers a press no layer
    /// inside the Modal answered first: the Modal's own handler reports the key (OnEscapeKeyDown), and the script,
    /// having decided the press is the Modal's, asks it to be dismissed (Utils.watchEscape calling OnEscape).
    /// </summary>
    public static Task PressEscape(this IRenderedComponent<IComponent> rendered, string selector = ".bit-mdl")
    {
        var root = rendered.Find(selector);

        root.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        var modal = FindModal(rendered, root.Id);

        return modal.InvokeAsync(() => modal.Instance._OnEscape());
    }

    /// <summary>
    /// The script's half of a press only: what it calls for an Escape no layer inside the Modal answered first.
    /// </summary>
    public static Task ForwardEscape(this IRenderedComponent<IComponent> rendered, string selector = ".bit-mdl")
    {
        var modal = FindModal(rendered, rendered.Find(selector).Id);

        return modal.InvokeAsync(() => modal.Instance._OnEscape());
    }

    private static IRenderedComponent<BitModal> FindModal(IRenderedComponent<IComponent> rendered, string? rootId)
    {
        if (rendered is IRenderedComponent<BitModal> self && RootIdOf(self) == rootId) return self;

        return rendered.FindComponents<BitModal>().Last(m => RootIdOf(m) == rootId);
    }

    private static string? RootIdOf(IRenderedComponent<BitModal> modal) => modal.Nodes.OfType<IElement>().FirstOrDefault()?.Id;
}
