using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// Where a component sits as a reader sees it: the render mode that starts at it, the parent that re-rendered it, and
/// what it was handed by DI. The demo's dashboard showed all three wrong in a real browser while every unit test passed.
/// </summary>
[TestClass]
public class ComponentContextTests : BunitContext
{
    public ComponentContextTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
        });
        Services.AddScoped<SessionWithSecrets>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    private IRenderedComponent<BevToolsPanel> RenderPanel()
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true, Visibility = "visible" });
        module.Setup<string?>("loadPrefs", _ => true).SetResult(null);
        return Render<BevToolsPanel>();
    }

#if NET9_0_OR_GREATER
    /// <summary>
    /// An interactive renderer answers every component it runs with its own mode, and bUnit, like the prerendering
    /// renderer, answers a child with its parent's: only the component the mode was assigned to starts a subtree.
    /// </summary>
    [TestMethod]
    public void A_render_mode_is_named_where_it_starts_and_inherited_below_it()
    {
        Render<ParentComponent>(ps => ps.SetAssignedRenderMode(RenderMode.InteractiveServer));
        Session.Components.Refresh(force: true);

        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        var child = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));
        Assert.AreEqual("InteractiveServer", parent.RenderModeName);
        Assert.IsNull(child.RenderModeName, "the child inherits the mode; it does not start a subtree of its own");

        var panel = RenderPanel();
        panel.InvokeAsync(() => panel.Instance.SelectComponent(child.InstanceId));
        var inspector = panel.Find(".bev-right").TextContent;
        Assert.Contains("from", inspector, StringComparison.Ordinal);
        Assert.DoesNotContain("assigned here", inspector, StringComparison.Ordinal);
        Assert.ContainsSingle(r => r.TextContent.Contains('⚡'), panel.FindAll(".bev-tree-row"));
    }
#endif

    /// <summary>
    /// <c>&lt;CascadingValue&gt;&lt;CascadingChild /&gt;&lt;/CascadingValue&gt;</c>: the renderer's parent is the
    /// CascadingValue, which the tree hides; the component that wrote both, and re-rendered, is the parent a reader sees.
    /// No refresh runs between the renders: the CascadingValue, which never renders through BevTools, is linked to its
    /// own parent when its child is, or the walk up would stop at it and call the render a StateHasChanged.
    /// </summary>
    [TestMethod]
    public void A_component_inside_a_cascading_value_names_the_component_that_rendered_it_as_its_parent()
    {
        var cut = Render<CascadingHost>();
        cut.InvokeAsync(() => cut.Instance.Bump());

        var child = Session.Components.All.Single(r => r.Type == typeof(CascadingChild));
        Assert.IsTrue(child.Parent!.IsHidden, "the renderer's parent is the hidden CascadingValue");
        Assert.AreEqual(BevRenderCause.ParentRender, child.LastRender!.Cause, child.LastRender.CauseDetail);
        Assert.AreEqual("Parent CascadingHost re-rendered (parameters re-applied)", child.LastRender.CauseDetail);

        var panel = RenderPanel();
        panel.InvokeAsync(() => panel.Instance.SelectComponent(child.InstanceId));
        Assert.AreEqual("CascadingHost", panel.Find(".bev-right .bev-toolbar .bev-link").TextContent);
    }

    [TestMethod]
    public void Injected_services_can_be_expanded_and_keep_their_secrets_redacted()
    {
        Render<ServiceUser>();
        var record = Session.Components.All.Single(r => r.Type == typeof(ServiceUser));
        var panel = RenderPanel();
        panel.InvokeAsync(() => panel.Instance.SelectComponent(record.InstanceId));

        panel.FindAll(".bev-vt-row").First(r => r.TextContent.Contains("UserSession:", StringComparison.Ordinal)).QuerySelector(".bev-caret")!.Click();

        var rows = panel.FindAll(".bev-vt-row").Select(r => r.TextContent).ToList();
        Assert.Contains(r => r.Contains("UserName:", StringComparison.Ordinal) && r.Contains("ada", StringComparison.Ordinal), rows);
        Assert.Contains(r => r.Contains("AccessToken:", StringComparison.Ordinal) && r.Contains("«redacted»", StringComparison.Ordinal), rows);
        Assert.Contains(r => r.Contains("Nickname:", StringComparison.Ordinal) && r.Contains("«redacted»", StringComparison.Ordinal), rows);
        Assert.DoesNotContain("token-value", panel.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("by-attribute", panel.Markup, StringComparison.Ordinal);
    }

    public sealed class CascadingHost : ComponentBase
    {
        private readonly HostSettings _settings = new();

        public void Bump()
        {
            _settings.Version++;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<HostSettings>>(0);
            builder.AddComponentParameter(1, "Value", _settings);
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<CascadingChild>(3);
                b.AddComponentParameter(4, nameof(CascadingChild.Title), "same");
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    /// <summary>One settings object for the whole subtree, changed in place: the demo's DashboardSettings.</summary>
    public sealed class HostSettings
    {
        public int Version { get; set; }
    }

    /// <summary>The demo's StatsCard, reduced: a cascading parameter makes every render of its parent re-apply its parameters.</summary>
    public sealed class CascadingChild : ComponentBase
    {
        [Parameter] public string? Title { get; set; }

        [CascadingParameter] public HostSettings? Settings { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Title + " v" + Settings?.Version);
    }

    public sealed class SessionWithSecrets
    {
        public string UserName { get; set; } = "ada";

        public string AccessToken { get; set; } = "token-value";

        [BevToolsSensitive]
        public string Nickname { get; set; } = "by-attribute";
    }

    public sealed class ServiceUser : ComponentBase
    {
        [Inject] public SessionWithSecrets UserSession { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, UserSession.UserName);
    }
}
