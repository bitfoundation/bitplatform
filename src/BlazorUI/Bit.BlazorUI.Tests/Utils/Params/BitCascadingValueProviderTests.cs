using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

[TestClass]
public partial class BitCascadingValueProviderTests : BunitTestContext
{
    [TestMethod]
    public void ShouldProvideCascadingValuesFromEnumerable()
    {
        var cascadingValues = new List<BitCascadingValue?>
        {
            null,
            new("hi", "Greeting"),
            new(7)
        };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, cascadingValues!);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var consumer = component.FindComponent<CascadingConsumer>().Instance;

        Assert.AreEqual(7, consumer.Number);
        Assert.AreEqual("hi", consumer.Greeting);
    }

    [TestMethod]
    public void ShouldProvideCascadingValuesFromValueList()
    {
        var cascadingValues = new BitCascadingValueList();
        cascadingValues.Add(3, isFixed: true);
        cascadingValues.Add("message", "Greeting");

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.ValueList, cascadingValues);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var intCascade = component.FindComponent<CascadingValue<int>>().Instance;
        var stringCascade = component.FindComponent<CascadingValue<string>>().Instance;

        Assert.AreEqual(3, intCascade.Value);
        Assert.IsTrue(intCascade.IsFixed);

        Assert.AreEqual("message", stringCascade.Value);
        Assert.IsFalse(stringCascade.IsFixed);
        Assert.AreEqual("Greeting", stringCascade.Name);
    }

    [TestMethod]
    public void ShouldRenderChildContentWhenNoValuesAreProvided()
    {
        var childContentRendered = false;

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.AddChildContent(builder =>
            {
                childContentRendered = true;
                builder.AddContent(0, "plain-child");
            });
        });

        Assert.IsTrue(childContentRendered);
        component.MarkupMatches("plain-child");
    }

    [TestMethod]
    public void ShouldRenderEmptyWhenNothingProvided()
    {
        var component = RenderComponent<BitCascadingValueProvider>();

        component.MarkupMatches(string.Empty);
    }

    [TestMethod]
    public void ShouldRenderChildContentWhenValueListIsEmpty()
    {
        var childContentRendered = false;

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.ValueList, new BitCascadingValueList());
            parameters.AddChildContent(builder =>
            {
                childContentRendered = true;
                builder.AddContent(0, "empty-list-child");
            });
        });

        Assert.IsTrue(childContentRendered);
        component.MarkupMatches("empty-list-child");
    }

    [TestMethod]
    public void ShouldPreferValuesParameterOverValueList()
    {
        var values = new List<BitCascadingValue> { new("from-values", "Greeting") };
        var valueList = new BitCascadingValueList
        {
            { "from-value-list", "Greeting" }
        };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, values);
            parameters.Add(p => p.ValueList, valueList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var consumer = component.FindComponent<CascadingConsumer>().Instance;

        Assert.AreEqual("from-values", consumer.Greeting);
    }

    [TestMethod]
    public void ShouldSkipNullEntriesAndStillCascadeOthers()
    {
        var cascadingValues = new List<BitCascadingValue?>
        {
            new(5),
            null,
            new("hello", "Greeting")
        };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, cascadingValues!);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var consumer = component.FindComponent<CascadingConsumer>().Instance;

        Assert.AreEqual(5, consumer.Number);
        Assert.AreEqual("hello", consumer.Greeting);
    }

    [TestMethod]
    public void ShouldCascadeWhenFirstEntryIsNull()
    {
        var cascadingValues = new List<BitCascadingValue?>
        {
            null,
            new("greet", "Greeting"),
            new(11)
        };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, cascadingValues!);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var consumer = component.FindComponent<CascadingConsumer>().Instance;

        Assert.AreEqual(11, consumer.Number);
        Assert.AreEqual("greet", consumer.Greeting);
    }

    [TestMethod]
    public void ShouldRespectOrderWhenDuplicateNamesExist()
    {
        var cascadingValues = new List<BitCascadingValue?>
        {
            new("first", "Greeting"),
            new("second", "Greeting")
        };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, cascadingValues!);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var consumer = component.FindComponent<CascadingConsumer>().Instance;

        Assert.AreEqual("second", consumer.Greeting);
    }

    [TestMethod]
    public void ShouldAllowNullValuesWhenTypeProvided()
    {
        var cascadingValues = new BitCascadingValueList();
        cascadingValues.Add<string?>(null, "Greeting");
        cascadingValues.Add<int?>(null);

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.ValueList, cascadingValues);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<NullableConsumer>(0);
                builder.CloseComponent();
            });
        });

        var stringCascade = component.FindComponent<CascadingValue<string>>().Instance;
        var intCascade = component.FindComponent<CascadingValue<int?>>().Instance;
        var consumer = component.FindComponent<NullableConsumer>().Instance;

        Assert.IsNull(stringCascade.Value);
        Assert.IsNull(intCascade.Value);
        Assert.AreEqual("Greeting", stringCascade.Name);
        Assert.IsNull(consumer.NullableNumber);
        component.MarkupMatches("-");
    }

    [TestMethod]
    public void ShouldHandleManyCascadingValues()
    {
        var cascadingValues = Enumerable.Range(0, 50).Select(i => new BitCascadingValue(i)).ToList();

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

        Assert.AreEqual(49, consumer.Number);
    }

    [TestMethod]
    public void ShouldDefaultIsFixedToFalse()
    {
        var cascadingValues = new List<BitCascadingValue?>
        {
            new(1),
            new("msg", "Greeting")
        };

        var component = RenderComponent<BitCascadingValueProvider>(parameters =>
        {
            parameters.Add(p => p.Values, cascadingValues!);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<CascadingConsumer>(0);
                builder.CloseComponent();
            });
        });

        var intCascade = component.FindComponent<CascadingValue<int>>().Instance;
        var stringCascade = component.FindComponent<CascadingValue<string>>().Instance;

        Assert.IsFalse(intCascade.IsFixed);
        Assert.IsFalse(stringCascade.IsFixed);
    }
}
