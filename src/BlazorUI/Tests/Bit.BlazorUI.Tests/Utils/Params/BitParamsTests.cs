using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

[TestClass]
public class BitParamsTests : BunitTestContext
{
    [TestMethod]
    public void ShouldCascadeParametersToChildren()
    {
        var parameters = new List<IBitComponentParams>
        {
            new FakeParamsA(),
            new FakeParamsB()
        };

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, parameters);
            builder.AddChildContent(childBuilder =>
            {
                childBuilder.OpenComponent<ParamsConsumer>(0);
                childBuilder.CloseComponent();
            });
        });

        component.MarkupMatches("1-Hello");
    }

    [TestMethod]
    public void ShouldProvideValuesWithNamesAndWatchThem()
    {
        var param = new FakeParamsA();

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [param]);
            builder.AddChildContent("<div>content</div>");
        });

        var provider = component.FindComponent<BitCascadingValueProvider>().Instance;
        var value = provider.Values!.Single(v => v.Name == param.Name);

        Assert.IsInstanceOfType<FakeParamsA>(value.Value);
        Assert.AreEqual(typeof(FakeParamsA), value.ValueType);
        Assert.IsFalse(value.IsFixed);
    }

    [TestMethod]
    public void ShouldSkipNullParameters()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [null!, new FakeParamsB()]);
            builder.AddChildContent(childBuilder =>
            {
                childBuilder.OpenComponent<ParamsConsumer>(0);
                childBuilder.CloseComponent();
            });
        });

        var provider = component.FindComponent<BitCascadingValueProvider>().Instance;
        var values = provider.Values!.Where(v => v.Value is IBitComponentParams).ToList();

        Assert.HasCount(1, values);
        Assert.AreEqual("B", values[0].Name);
        component.MarkupMatches("-Hello");
    }

    [TestMethod]
    public void ShouldRenderChildContentWhenParametersNull()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.AddChildContent("<p>no-params</p>");
        });

        component.MarkupMatches("<p>no-params</p>");
    }

    [TestMethod]
    public void ShouldRenderChildContentWhenParametersEmpty()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, []);
            builder.AddChildContent("<p>empty</p>");
        });

        component.MarkupMatches("<p>empty</p>");
    }

    [TestMethod]
    public void ShouldRenderEmptyWhenNoChildAndNoParameters()
    {
        var component = RenderComponent<BitParams>();

        component.MarkupMatches(string.Empty);
    }

    [TestMethod]
    public async Task ShouldIgnoreUnknownParametersAndNotThrow()
    {
        var component = RenderComponent<BitParams>();

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { "Unknown", 1 },
            { nameof(BitParams.Parameters), new[] { new FakeParamsA() } }
        });

        await component.InvokeAsync(() => component.Instance.SetParametersAsync(parameters));

        Assert.AreEqual(1, component.Instance.Parameters?.Count());
    }

    [TestMethod]
    public void ShouldUpdateParametersOnRerender()
    {
        var first = new FakeParamsA();
        var second = new FakeParamsB();

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [first]);
            builder.AddChildContent(childBuilder =>
            {
                childBuilder.OpenComponent<ParamsConsumer>(0);
                childBuilder.CloseComponent();
            });
        });

        component.Render(builder => builder.Add(p => p.Parameters, [second]));

        component.MarkupMatches("-Hello");
    }

    [TestMethod]
    public void NestedParamsShouldOnlyReplaceWhatTheyProvide()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new FakeMergeParams { Number = 1, Text = "outer" }, new FakeParamsB()]);
            builder.AddChildContent<BitParams>(inner =>
            {
                inner.Add(p => p.Parameters, [new FakeMergeParams { Text = "inner" }]);
                inner.AddChildContent<MergeConsumer>();
            });
        });

        component.MarkupMatches("1|inner||Hello");
    }

    [TestMethod]
    public void LaterParamsOfTheSameTypeShouldOnlyReplaceWhatTheyProvide()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new FakeMergeParams { Number = 1, Text = "first" }, new FakeMergeParams { Text = "second" }]);
            builder.AddChildContent<MergeConsumer>();
        });

        component.MarkupMatches("1|second||");
    }

    [TestMethod]
    public void NestedParamsShouldMergeDictionaries()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new FakeMergeParams { Attributes = new() { ["a"] = 1, ["b"] = 1 } }]);
            builder.AddChildContent<BitParams>(inner =>
            {
                inner.Add(p => p.Parameters, [new FakeMergeParams { Attributes = new() { ["b"] = 2, ["c"] = 2 } }]);
                inner.AddChildContent<MergeConsumer>();
            });
        });

        component.MarkupMatches("||a,b,c|");
    }

    [TestMethod]
    public void IsolatedParamsShouldHideEverythingTheAncestorsCarry()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new FakeMergeParams { Number = 1, Text = "outer" }, new FakeParamsB()]);
            builder.AddChildContent<BitParams>(inner =>
            {
                inner.Add(p => p.Isolated, true);
                inner.Add(p => p.Parameters, [new FakeMergeParams { Text = "inner" }]);
                inner.AddChildContent<MergeConsumer>();
            });
        });

        component.MarkupMatches("|inner||");
    }

    [TestMethod]
    public void IsolatedParamsShouldHideTheGrandparentsToo()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new FakeMergeParams { Number = 1 }]);
            builder.AddChildContent<BitParams>(middle =>
            {
                middle.Add(p => p.Parameters, [new FakeParamsB()]);
                middle.AddChildContent<BitParams>(inner =>
                {
                    inner.Add(p => p.Isolated, true);
                    inner.AddChildContent<MergeConsumer>();
                });
            });
        });

        component.MarkupMatches("|||");
    }

    [TestMethod]
    public void AGrandchildShouldMergeWithEveryAncestor()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new FakeMergeParams { Number = 1 }]);
            builder.AddChildContent<BitParams>(middle =>
            {
                middle.Add(p => p.Parameters, [new FakeParamsB()]);
                middle.AddChildContent<BitParams>(inner =>
                {
                    inner.Add(p => p.Parameters, [new FakeMergeParams { Text = "inner" }]);
                    inner.AddChildContent<MergeConsumer>();
                });
            });
        });

        component.MarkupMatches("1|inner||Hello");
    }

    [TestMethod]
    public void AChangedParamsObjectShouldReachAConsumerItsParentDoesNotRender()
    {
        var @params = new FakeMergeParams { Number = 1 };

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [@params]);
            builder.AddChildContent<StaticMergeHost>();
        });

        component.MarkupMatches("1|||");

        @params.Number = 2;
        component.Render(builder => builder.Add(p => p.Parameters, [@params]));

        component.MarkupMatches("2|||");

        component.Render(builder => builder.Add(p => p.Parameters, [new FakeMergeParams { Number = 3 }]));

        component.MarkupMatches("3|||");
    }

    [TestMethod]
    public void AnEntryAddedToADictionaryShouldReachTheConsumers()
    {
        var @params = new FakeMergeParams { Attributes = new() { ["a"] = 1 } };

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [@params]);
            builder.AddChildContent<StaticMergeHost>();
        });

        @params.Attributes["b"] = 2;
        component.Render(builder => builder.Add(p => p.Parameters, [@params]));

        component.MarkupMatches("||a,b|");
    }

    [TestMethod]
    public void AnUnchangedRenderShouldNotReRenderTheConsumers()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new FakeMergeParams { Number = 1, Attributes = new() { ["a"] = 1 } }]);
            builder.AddChildContent<StaticMergeHost>();
        });

        var consumer = component.FindComponent<MergeConsumer>().Instance;
        var renders = consumer.RenderCount;

        // A new list of new objects that carry the same values is no change.
        component.Render(builder => builder.Add(p => p.Parameters, [new FakeMergeParams { Number = 1, Attributes = new() { ["a"] = 1 } }]));

        Assert.AreEqual(renders, consumer.RenderCount);
        component.MarkupMatches("1||a|");
    }

    [TestMethod]
    public void AnUnchangedRenderShouldStillRefreshTheContent()
    {
        var @params = new[] { new FakeMergeParams { Number = 1 } };

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, @params);
            builder.AddChildContent("<p>first</p>");
        });

        component.Render(builder =>
        {
            builder.Add(p => p.Parameters, @params);
            builder.AddChildContent("<p>second</p>");
        });

        component.MarkupMatches("<p>second</p>");
    }

    [TestMethod]
    public void AChangeInTheOuterParamsShouldReachTheNestedMerge()
    {
        var outer = new FakeMergeParams { Number = 1 };

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [outer]);
            builder.AddChildContent<StaticNestedHost>();
        });

        component.MarkupMatches("1|inner||");

        outer.Number = 2;
        component.Render(builder => builder.Add(p => p.Parameters, [outer]));

        component.MarkupMatches("2|inner||");
    }

    [TestMethod]
    public void AChangedParamsObjectShouldReachABitComponent()
    {
        var @params = new BitButtonParams { Size = BitSize.Small };

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [@params]);
            builder.AddChildContent<StaticButtonHost>();
        });

        Assert.IsTrue(component.Find("button").ClassList.Contains("bit-btn-sm"));

        @params.Size = BitSize.Large;
        component.Render(builder => builder.Add(p => p.Parameters, [@params]));

        Assert.IsTrue(component.Find("button").ClassList.Contains("bit-btn-lg"));
    }

    [TestMethod]
    public void ComponentBaseParamsShouldLeaveIsEnabledUnset()
    {
        Assert.IsNull(new BitButtonParams().IsEnabled);
    }

    [TestMethod]
    public void IsEnabledShouldDisableEveryComponentAndGiveItBack()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.IsEnabled, false);
            builder.AddChildContent<StaticFormHost>();
        });

        Assert.IsFalse(component.FindComponent<BitButton>().Instance.IsEnabled);
        Assert.IsFalse(component.FindComponent<BitCheckbox>().Instance.IsEnabled);
        Assert.IsTrue(component.FindComponent<BitNumberField<int>>().Instance.IsEnabled, "its own IsEnabled wins");
        Assert.IsTrue(component.Find("button").ClassList.Contains("bit-dis"));

        component.Render(builder => builder.Add(p => p.IsEnabled, null));

        Assert.IsTrue(component.FindComponent<BitButton>().Instance.IsEnabled);
        Assert.IsTrue(component.FindComponent<BitCheckbox>().Instance.IsEnabled);
        Assert.IsFalse(component.Find("button").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void ReadOnlyShouldLockEveryInputAndGiveItBack()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.ReadOnly, true);
            builder.AddChildContent<StaticFormHost>();
        });

        Assert.IsTrue(component.FindComponent<BitCheckbox>().Instance.ReadOnly);
        Assert.IsFalse(component.FindComponent<BitNumberField<int>>().Instance.ReadOnly, "its own ReadOnly wins");

        component.Render(builder => builder.Add(p => p.ReadOnly, false));

        Assert.IsFalse(component.FindComponent<BitCheckbox>().Instance.ReadOnly);
    }

    [TestMethod]
    public void ANestedScopeShouldEnableAPartAgain()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.IsEnabled, false);
            builder.AddChildContent<BitParams>(inner =>
            {
                inner.Add(p => p.IsEnabled, true);
                inner.AddChildContent<StaticFormHost>();
            });
        });

        Assert.IsTrue(component.FindComponent<BitButton>().Instance.IsEnabled);
    }

    [TestMethod]
    public void ANestedScopeShouldInheritIsEnabledUnlessIsolated()
    {
        var inherited = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.IsEnabled, false);
            builder.AddChildContent<BitParams>(inner =>
            {
                inner.Add(p => p.Parameters, [new FakeParamsB()]);
                inner.AddChildContent<StaticFormHost>();
            });
        });

        Assert.IsFalse(inherited.FindComponent<BitButton>().Instance.IsEnabled);

        var isolated = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.IsEnabled, false);
            builder.AddChildContent<BitParams>(inner =>
            {
                inner.Add(p => p.Isolated, true);
                inner.AddChildContent<StaticFormHost>();
            });
        });

        Assert.IsTrue(isolated.FindComponent<BitButton>().Instance.IsEnabled);
    }

    [TestMethod]
    public void DirShouldReachEveryComponentUnlessIsolated()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Dir, BitDir.Rtl);
            builder.AddChildContent<StaticFormHost>();
        });

        Assert.AreEqual(BitDir.Rtl, component.FindComponent<BitButton>().Instance.Dir);
        Assert.IsTrue(component.Find("button").ClassList.Contains("bit-rtl"));

        component.Render(builder => builder.Add(p => p.Dir, BitDir.Ltr));

        Assert.AreEqual(BitDir.Ltr, component.FindComponent<BitButton>().Instance.Dir);

        var isolated = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Dir, BitDir.Rtl);
            builder.AddChildContent<BitParams>(inner =>
            {
                inner.Add(p => p.Isolated, true);
                inner.AddChildContent<StaticFormHost>();
            });
        });

        Assert.IsNull(isolated.FindComponent<BitButton>().Instance.Dir);
    }

    [TestMethod]
    public void AComponentParamsObjectShouldWinOverTheScope()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.IsEnabled, false);
            builder.Add(p => p.Parameters, [new BitButtonParams { IsEnabled = true }]);
            builder.AddChildContent<StaticFormHost>();
        });

        Assert.IsTrue(component.FindComponent<BitButton>().Instance.IsEnabled);
        Assert.IsFalse(component.FindComponent<BitCheckbox>().Instance.IsEnabled);
    }



    private sealed class StaticNestedHost : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BitParams>(0);
            builder.AddComponentParameter(1, nameof(BitParams.Parameters), new IBitComponentParams[] { new FakeMergeParams { Text = "inner" } });
            builder.AddComponentParameter(2, nameof(BitParams.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<StaticMergeHost>(0);
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class StaticFormHost : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BitButton>(0);
            builder.AddComponentParameter(1, nameof(BitButton.Title), "Save");
            builder.CloseComponent();

            builder.OpenComponent<BitCheckbox>(2);
            builder.AddComponentParameter(3, nameof(BitCheckbox.Label), "Agree");
            builder.CloseComponent();

            builder.OpenComponent<BitNumberField<int>>(4);
            builder.AddComponentParameter(5, nameof(BitNumberField<int>.IsEnabled), true);
            builder.AddComponentParameter(6, nameof(BitNumberField<int>.ReadOnly), false);
            builder.CloseComponent();
        }
    }

    private sealed class StaticButtonHost : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BitButton>(0);
            builder.AddComponentParameter(1, nameof(BitButton.Title), "Save");
            builder.CloseComponent();
        }
    }
}
