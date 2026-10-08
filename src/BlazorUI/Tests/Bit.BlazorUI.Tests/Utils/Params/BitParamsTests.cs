using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public void ANestedObjectChangedInPlaceShouldReachABitComponent()
    {
        var @params = new BitButtonParams { Classes = new() { Root = "first" } };

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [@params]);
            builder.AddChildContent<StaticButtonHost>();
        });

        Assert.IsTrue(component.Find("button").ClassList.Contains("first"));

        @params.Classes.Root = "second";
        component.Render(builder => builder.Add(p => p.Parameters, [@params]));

        Assert.IsTrue(component.Find("button").ClassList.Contains("second"));
        Assert.IsFalse(component.Find("button").ClassList.Contains("first"));
    }

    [TestMethod]
    public void AnUnchangedParamsObjectShouldNotRebuildTheStringsOfABitComponent()
    {
        var @params = new BitButtonParams { Size = BitSize.Small, Classes = new() { Root = "first" }, Styles = new() { Root = "color:red" } };

        // A content fragment counts as a possible change, so the button renders again whenever the content does.
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [@params]);
            builder.AddChildContent(content =>
            {
                content.OpenComponent<BitButton>(0);
                content.AddComponentParameter(1, nameof(BitButton.ChildContent), (RenderFragment)(text => text.AddContent(0, "Save")));
                content.CloseComponent();
            });
        });

        var rendered = component.FindComponent<BitButton>();
        var renders = rendered.RenderCount;
        var classes = rendered.Instance.ClassBuilder.Value;
        var styles = rendered.Instance.StyleBuilder.Value;

        // The host renders again with the very same params: the button renders again with it, and must not pay for
        // its class and style strings when the cascade has nothing new to say.
        component.Render(builder => builder.Add(p => p.Parameters, [@params]));

        Assert.IsTrue(rendered.RenderCount > renders);
        Assert.AreSame(classes, rendered.Instance.ClassBuilder.Value);
        Assert.AreSame(styles, rendered.Instance.StyleBuilder.Value);
    }

    [TestMethod]
    public void ComponentBaseParamsShouldLeaveDisabledUnset()
    {
        Assert.IsNull(new BitButtonParams().Disabled);
    }

    [TestMethod]
    public void AParameterTheParamsObjectStopsSupplyingShouldGoBack()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new BitButtonParams { Variant = BitVariant.Outline, Class = "shared", Disabled = true, Dir = BitDir.Rtl }]);
            builder.AddChildContent<StaticButtonHost>();
        });

        var button = component.FindComponent<BitButton>().Instance;

        Assert.AreEqual(BitVariant.Outline, button.Variant);
        Assert.IsTrue(button.Disabled);
        Assert.IsTrue(component.Find("button").ClassList.Contains("shared"));
        Assert.IsTrue(component.Find("button").ClassList.Contains("bit-rtl"));

        component.Render(builder => builder.Add(p => p.Parameters, [new BitButtonParams { Class = "shared" }]));

        Assert.IsNull(button.Variant);
        Assert.IsFalse(button.Disabled);
        Assert.IsNull(button.Dir);
        Assert.IsTrue(component.Find("button").ClassList.Contains("shared"), "a parameter still supplied stays");
        Assert.IsFalse(component.Find("button").ClassList.Contains("bit-dis"));
        Assert.IsFalse(component.Find("button").ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void EveryParameterShouldGoBackOnceTheParamsObjectIsGone()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new BitButtonParams { Variant = BitVariant.Outline, Class = "shared" },
                                            new BitCheckboxParams { Size = BitSize.Large },
                                            new BitNumberFieldParams { Size = BitSize.Large }]);
            builder.AddChildContent<StaticInputsHost>();
        });

        Assert.AreEqual(BitSize.Large, component.FindComponent<BitCheckbox>().Instance.Size);
        Assert.AreEqual(BitSize.Large, component.FindComponent<BitNumberField<int>>().Instance.Size);

        var button = component.FindComponent<BitButton>().Instance;
        var checkbox = component.FindComponent<BitCheckbox>().Instance;
        var numberField = component.FindComponent<BitNumberField<int>>().Instance;

        component.Render(builder => builder.Add(p => p.Parameters, null));

        Assert.AreSame(button, component.FindComponent<BitButton>().Instance, "the content is kept, not built again");
        Assert.AreSame(checkbox, component.FindComponent<BitCheckbox>().Instance);
        Assert.AreSame(numberField, component.FindComponent<BitNumberField<int>>().Instance);
        Assert.AreEqual(BitVariant.Text, button.Variant);
        Assert.IsNull(button.Class);
        Assert.IsNull(checkbox.Size);
        Assert.IsNull(numberField.Size);
    }

    [TestMethod]
    public void ARestoredParameterShouldRunItsSetupHook()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new BitNumberFieldParams { Min = "5" }]);
            builder.AddChildContent<StaticInputsHost>();
        });

        Assert.AreEqual("5", component.Find("input[role=spinbutton]").GetAttribute("aria-valuemin"));

        component.Render(builder => builder.Add(p => p.Parameters, [new BitNumberFieldParams()]));

        Assert.IsNull(component.FindComponent<BitNumberField<int>>().Instance.Min);
        Assert.IsFalse(component.Find("input[role=spinbutton]").HasAttribute("aria-valuemin"), "OnSetMin ran again, so the bound is gone");
    }

    [TestMethod]
    public void AParameterTheMarkupLetsGoOfShouldGoBackToWhatTheMarkupLeft()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new BitButtonParams { Color = BitColor.Error }]);
            builder.AddChildContent<SwitchableButtonHost>(host => host.Add(p => p.Color, BitColor.Tertiary));
        });

        var button = component.FindComponent<BitButton>().Instance;

        Assert.AreEqual(BitColor.Tertiary, button.Color, "its own Color wins");

        // Only the host renders, so the params object stays the very same one.
        component.FindComponent<SwitchableButtonHost>().Render(host => host.Add(p => p.Color, null));

        Assert.AreEqual(BitColor.Error, button.Color);

        component.Render(builder => builder.Add(p => p.Parameters, [new BitButtonParams()]));

        Assert.AreEqual(BitColor.Tertiary, button.Color);
    }

    [TestMethod]
    public void TogglingIsolatedShouldKeepTheContent()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new BitButtonParams { Variant = BitVariant.Outline }]);
            builder.AddChildContent<BitParams>(inner =>
            {
                inner.Add(p => p.Isolated, false);
                inner.AddChildContent<StaticButtonHost>();
            });
        });

        var button = component.FindComponent<BitButton>().Instance;
        var inner = component.FindComponent<BitParams>();

        inner.Render(builder => builder.Add(p => p.Isolated, true));

        Assert.AreSame(button, component.FindComponent<BitButton>().Instance);
        Assert.IsNull(button.Variant);

        inner.Render(builder => builder.Add(p => p.Isolated, false));

        Assert.AreSame(button, component.FindComponent<BitButton>().Instance);
        Assert.AreEqual(BitVariant.Outline, button.Variant);
    }

    [TestMethod]
    public void ASequenceProducedOnEnumerationShouldNotBeEnumerated()
    {
        var enumerations = 0;

        IEnumerable<int> Numbers()
        {
            enumerations++;
            yield return 1;
        }

        var @params = new FakeMergeParams { Number = 1, Numbers = Numbers() };

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [@params]);
            builder.AddChildContent<StaticMergeHost>();
        });

        component.Render(builder => builder.Add(p => p.Parameters, [@params]));
        component.Render(builder => builder.Add(p => p.Parameters, [@params]));

        Assert.AreEqual(0, enumerations);
    }

    [TestMethod]
    public void ACollectionBehindAReadOnlyPropertyChangedInPlaceShouldReachTheConsumers()
    {
        var @params = new FakeMergeParams { Number = 1, Nested = new() };

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [@params]);
            builder.AddChildContent<StaticMergeHost>();
        });

        var consumer = component.FindComponent<MergeConsumer>().Instance;
        var renders = consumer.RenderCount;

        component.Render(builder => builder.Add(p => p.Parameters, [@params]));

        Assert.AreEqual(renders, consumer.RenderCount);

        @params.Nested.Tags.Add("new");
        component.Render(builder => builder.Add(p => p.Parameters, [@params]));

        Assert.AreEqual(renders + 1, consumer.RenderCount);
    }

    [TestMethod]
    public void TheGeneratedCodeShouldReadTheParamsObjectDirectly()
    {
        var property = typeof(BitButton).GetProperty("CascadedParams", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.AreEqual(typeof(BitButton), property?.DeclaringType);
    }

    [TestMethod]
    public void TheParametersOfATextInputShouldCountAsSetByTheMarkup()
    {
        var component = RenderComponent<BitTextField>(parameters => parameters.Add(p => p.Immediate, true));

        var isSetByMarkup = typeof(BitComponentBase).GetMethod("IsSetByMarkup", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.IsTrue((bool)isSetByMarkup.Invoke(component.Instance, [nameof(BitTextField.Immediate)])!);
        Assert.IsFalse((bool)isSetByMarkup.Invoke(component.Instance, [nameof(BitTextField.DebounceTime)])!);
    }

    [TestMethod]
    public void AnIsolatedScopeShouldPutBackWhatTheOuterOneSupplied()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new BitButtonParams { Variant = BitVariant.Outline }]);
            builder.AddChildContent<BitParams>(inner =>
            {
                inner.Add(p => p.Isolated, false);
                inner.AddChildContent<StaticButtonHost>();
            });
        });

        var button = component.FindComponent<BitButton>().Instance;

        Assert.AreEqual(BitVariant.Outline, button.Variant);

        component.FindComponent<BitParams>().Render(inner => inner.Add(p => p.Isolated, true));

        Assert.IsNull(button.Variant);
    }

    [TestMethod]
    public void AParameterTheMarkupSetsShouldNotBePutBack()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new BitButtonParams { Variant = BitVariant.Outline, Color = BitColor.Error }]);
            builder.AddChildContent<StaticInputsHost>();
        });

        var button = component.FindComponent<BitButton>().Instance;

        Assert.AreEqual(BitVariant.Text, button.Variant, "its own Variant wins");
        Assert.AreEqual(BitColor.Error, button.Color);

        component.Render(builder => builder.Add(p => p.Parameters, [new BitButtonParams()]));

        Assert.AreEqual(BitVariant.Text, button.Variant);
        Assert.IsNull(button.Color);
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

    private sealed class StaticInputsHost : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BitButton>(0);
            builder.AddComponentParameter(1, nameof(BitButton.Variant), BitVariant.Text);
            builder.CloseComponent();

            builder.OpenComponent<BitCheckbox>(2);
            builder.CloseComponent();

            builder.OpenComponent<BitNumberField<int>>(3);
            builder.CloseComponent();
        }
    }

    private sealed class SwitchableButtonHost : ComponentBase
    {
        [Parameter] public BitColor? Color { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BitButton>(0);
            builder.AddComponentParameter(1, nameof(BitButton.Title), "Save");
            if (Color is not null)
            {
                builder.AddComponentParameter(2, nameof(BitButton.Color), Color);
            }
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
