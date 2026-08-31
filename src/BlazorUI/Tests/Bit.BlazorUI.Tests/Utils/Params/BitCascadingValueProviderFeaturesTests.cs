using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

[TestClass]
public partial class BitCascadingValueProviderFeaturesTests : BunitTestContext
{
    [TestMethod]
    public void ShouldSkipDisabledValues()
    {
        var cascadingValues = new List<BitCascadingValue>
        {
            new(5),
            new("hidden", "Greeting") { Enabled = false }
        };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, cascadingValues);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var consumer = component.FindComponent<CascadingConsumer>().Instance;

        Assert.AreEqual(5, consumer.Number);
        Assert.IsNull(consumer.Greeting);
        Assert.AreEqual(1, component.FindComponents<CascadingValue<int>>().Count);
        Assert.AreEqual(0, component.FindComponents<CascadingValue<string>>().Count);
    }

    [TestMethod]
    public void ShouldRenderChildContentWhenEveryValueIsDisabled()
    {
        var cascadingValues = new List<BitCascadingValue>
        {
            new(5) { Enabled = false },
            new("hidden", "Greeting") { Enabled = false }
        };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, cascadingValues);
            parameters.AddChildContent(builder => builder.AddContent(0, "all-disabled-child"));
        });

        component.MarkupMatches("all-disabled-child");
    }

    [TestMethod]
    public void ShouldToggleAValueOnAndOffThroughTheEnabledProperty()
    {
        var greeting = new BitCascadingValue("hello", "Greeting") { Enabled = false };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { greeting });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.IsNull(component.FindComponent<CascadingConsumer>().Instance.Greeting);

        greeting.Enabled = true;
        component.Render();

        Assert.AreEqual("hello", component.FindComponent<CascadingConsumer>().Instance.Greeting);
    }

    [TestMethod]
    public void ShouldCombineTheValueListAndTheValuesParameters()
    {
        var valueList = new BitCascadingValueList();
        valueList.Add(9);

        var values = new List<BitCascadingValue> { new("from-values", "Greeting") };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.ValueList, valueList);
            parameters.Add(p => p.Values, values);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var consumer = component.FindComponent<CascadingConsumer>().Instance;

        Assert.AreEqual(9, consumer.Number);
        Assert.AreEqual("from-values", consumer.Greeting);
    }

    [TestMethod]
    public void ShouldUpdateTheConsumerWhenACascadedValueChanges()
    {
        var number = new BitCascadingValue(1);

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { number });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual(1, component.FindComponent<CascadingConsumer>().Instance.Number);

        number.Value = 2;
        component.Render();

        Assert.AreEqual(2, component.FindComponent<CascadingConsumer>().Instance.Number);
    }

    [TestMethod]
    public void ShouldCascadeAValueAsTheExplicitlyProvidedValueType()
    {
        var cascadingValues = new List<BitCascadingValue>
        {
            new(new CascadingDemoServiceDecorator(), typeof(ICascadingDemoService))
        };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, cascadingValues);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingServiceConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.IsNotNull(component.FindComponent<CascadingValue<ICascadingDemoService>>().Instance);
        component.MarkupMatches("demo-service");
    }

    [TestMethod]
    public void ShouldSupplyANullableValueTypeParameterWhenTheValueTypeIsNullable()
    {
        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { BitCascadingValue.From<int?>(5) });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<NullableConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.IsNotNull(component.FindComponent<CascadingValue<int?>>().Instance);
        Assert.AreEqual(5, component.FindComponent<NullableConsumer>().Instance.NullableNumber);
    }

    [TestMethod]
    public void ShouldCascadeANullValueWithAnExplicitValueType()
    {
        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { BitCascadingValue.From<int?>(null) });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<NullableConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.IsNotNull(component.FindComponent<CascadingValue<int?>>().Instance);
        Assert.IsNull(component.FindComponent<NullableConsumer>().Instance.NullableNumber);
    }

    [TestMethod]
    public void ShouldShadowAnOuterValueWithANullOfTheSameValueType()
    {
        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { new(5, typeof(int?)) });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCascadingValueProvider>(0);
                builder.AddComponentParameter(1, nameof(BitCascadingValueProvider.Values),
                    new List<BitCascadingValue> { BitCascadingValue.From<int?>(null) });
                builder.AddComponentParameter(2, nameof(BitCascadingValueProvider.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<NullableConsumer>(0);
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            });
        });

        Assert.IsNull(component.FindComponent<NullableConsumer>().Instance.NullableNumber);
    }

    [TestMethod]
    public void ShouldShadowAnOuterProviderValueFromAnInnerProvider()
    {
        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { new("outer", "Greeting"), new(1) });
            parameters.AddChildContent(BuildNestedProvider(new BitCascadingValue("inner", "Greeting")));
        });

        var consumer = component.FindComponent<CascadingConsumer>().Instance;

        Assert.AreEqual("inner", consumer.Greeting);
        Assert.AreEqual(1, consumer.Number);
    }

    [TestMethod]
    public void ShouldLetAnOuterProviderValueShowThroughWhenTheInnerValueIsDisabled()
    {
        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { new("outer", "Greeting") });
            parameters.AddChildContent(BuildNestedProvider(new BitCascadingValue("inner", "Greeting") { Enabled = false }));
        });

        Assert.AreEqual("outer", component.FindComponent<CascadingConsumer>().Instance.Greeting);
    }

    [TestMethod]
    public void ShouldRenderNothingWhenThereIsNoChildContentButThereAreValues()
    {
        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { new(5) });
        });

        component.MarkupMatches(string.Empty);
        Assert.AreEqual(1, component.FindComponents<CascadingValue<int>>().Count);
    }

    [TestMethod]
    public void ShouldThrowWhenCreateCascadingValueGetsANullBuilder()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => BitCascadingValueProvider.CreateCascadingValue(null!, 0, new BitCascadingValue(5), _ => { }));
    }

    [TestMethod]
    public void ShouldRenderOnlyTheLastOfTheValuesSharingAValueTypeAndAName()
    {
        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue>
            {
                new("first", "Greeting"),
                new("second", "GREETING"),
                new(1),
                new(2)
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var consumer = component.FindComponent<CascadingConsumer>().Instance;

        Assert.AreEqual("second", consumer.Greeting);
        Assert.AreEqual(2, consumer.Number);
        Assert.AreEqual(1, component.FindComponents<CascadingValue<string>>().Count);
        Assert.AreEqual(1, component.FindComponents<CascadingValue<int>>().Count);
    }

    [TestMethod]
    public void ShouldKeepTheValuesOfTheSameTypeThatCarryDifferentNames()
    {
        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue>
            {
                new("greeting", "Greeting"),
                new("farewell", "Farewell"),
                new("unnamed")
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual("greeting", component.FindComponent<CascadingConsumer>().Instance.Greeting);
        Assert.AreEqual(3, component.FindComponents<CascadingValue<string>>().Count);
    }

    [TestMethod]
    public void ShouldDropTheValueListEntryThatTheValuesParameterShadows()
    {
        var valueList = new BitCascadingValueList { { "from-value-list", "Greeting" } };
        var values = new List<BitCascadingValue> { new("from-values", "Greeting") };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.ValueList, valueList);
            parameters.Add(p => p.Values, values);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual("from-values", component.FindComponent<CascadingConsumer>().Instance.Greeting);
        Assert.AreEqual(1, component.FindComponents<CascadingValue<string>>().Count);
    }

    [TestMethod]
    public void ShouldCascadeAMixOfFixedAndNonFixedValues()
    {
        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue>
            {
                BitCascadingValue.Fixed(5),
                new("hi", "Greeting")
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var consumer = component.FindComponent<CascadingConsumer>().Instance;

        Assert.AreEqual(5, consumer.Number);
        Assert.AreEqual("hi", consumer.Greeting);
        Assert.IsTrue(component.FindComponent<CascadingValue<int>>().Instance.IsFixed);
        Assert.IsFalse(component.FindComponent<CascadingValue<string>>().Instance.IsFixed);
    }

    [TestMethod]
    public void ShouldNotRunTheFactoryOfADisabledOrShadowedLazyValue()
    {
        var calls = 0;
        var disabled = BitCascadingValue.Lazy(() => { calls++; return "disabled"; }, "Greeting", false, false);
        var shadowed = BitCascadingValue.Lazy(() => { calls++; return "shadowed"; }, "Greeting");
        var winner = BitCascadingValue.Lazy(() => { calls++; return "winner"; }, "Greeting");

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { disabled, shadowed, winner });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual("winner", component.FindComponent<CascadingConsumer>().Instance.Greeting);
        Assert.AreEqual(1, calls);
        Assert.IsFalse(disabled.IsValueCreated);
        Assert.IsFalse(shadowed.IsValueCreated);
        Assert.IsTrue(winner.IsValueCreated);
    }

    [TestMethod]
    public void ShouldRefreshTheConsumersWhenACascadedValueIsAssigned()
    {
        var number = new BitCascadingValue(1);

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { number });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual(1, component.FindComponent<CascadingConsumer>().Instance.Number);

        number.Value = 2;

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindComponent<CascadingConsumer>().Instance.Number));
    }

    [TestMethod]
    public void ShouldRefreshTheConsumersWhenADisabledValueIsEnabled()
    {
        var greeting = new BitCascadingValue("hello", "Greeting") { Enabled = false };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { new(1), greeting });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.IsNull(component.FindComponent<CascadingConsumer>().Instance.Greeting);

        greeting.Enabled = true;

        component.WaitForAssertion(() => Assert.AreEqual("hello", component.FindComponent<CascadingConsumer>().Instance.Greeting));
    }

    [TestMethod]
    public void ShouldRefreshTheConsumersWhenACascadedObjectIsMutatedInPlace()
    {
        var service = new MutableCascadingDemoService();
        var cascadingValue = BitCascadingValue.From<ICascadingDemoService>(service);

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { cascadingValue });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingServiceConsumer>(0);
                builder.CloseComponent();
            });
        });

        component.MarkupMatches("initial");

        service.Name = "mutated";
        cascadingValue.NotifyChanged();

        component.WaitForAssertion(() => component.MarkupMatches("mutated"));
    }

    [TestMethod]
    public void ShouldReCreateTheCascadingValueWhenIsFixedIsToggled()
    {
        var greeting = new BitCascadingValue("hello", "Greeting");

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { greeting });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        Assert.IsFalse(component.FindComponent<CascadingValue<string>>().Instance.IsFixed);

        greeting.IsFixed = true;

        component.WaitForAssertion(() => Assert.IsTrue(component.FindComponent<CascadingValue<string>>().Instance.IsFixed));
        Assert.AreEqual("hello", component.FindComponent<CascadingConsumer>().Instance.Greeting);

        greeting.IsFixed = false;

        component.WaitForAssertion(() => Assert.IsFalse(component.FindComponent<CascadingValue<string>>().Instance.IsFixed));
        Assert.AreEqual("hello", component.FindComponent<CascadingConsumer>().Instance.Greeting);
    }

    [TestMethod]
    public void ShouldStopListeningToTheValuesOnceDisposed()
    {
        var number = new BitCascadingValue(1);

        RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, new List<BitCascadingValue> { number });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        Context.DisposeComponentsAsync().GetAwaiter().GetResult();

        number.Value = 2;
        number.NotifyChanged();

        Assert.AreEqual(2, number.Value);
    }

    private static RenderFragment BuildNestedProvider(BitCascadingValue innerValue)
    {
        return builder =>
        {
            builder.OpenComponent<BitCascadingValueProvider>(0);
            builder.AddComponentParameter(1, nameof(BitCascadingValueProvider.Values), new List<BitCascadingValue> { innerValue });
            builder.AddComponentParameter(2, nameof(BitCascadingValueProvider.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<CascadingConsumer>(0);
                b.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }
}
