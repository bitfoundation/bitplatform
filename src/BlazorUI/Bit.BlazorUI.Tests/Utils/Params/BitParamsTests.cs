using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
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
    public void ShouldProvideValuesWithNamesAndFixedFlag()
    {
        var param = new FakeParamsA();

        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [param]);
            builder.AddChildContent("<div>content</div>");
        });

        var provider = component.FindComponent<BitCascadingValueProvider>().Instance;
        var value = provider.Values?.Single();

        Assert.IsNotNull(value);
        Assert.AreEqual(param, value!.Value);
        Assert.AreEqual(param.Name, value.Name);
        Assert.IsTrue(value.IsFixed);
    }

    [TestMethod]
    public void ShouldSkipNullParameters()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [null!, new FakeParamsB()]);
            builder.AddChildContent("<span>child</span>");
        });

        var provider = component.FindComponent<BitCascadingValueProvider>().Instance;
        var values = provider.Values?.ToList();

        Assert.IsNotNull(values);
        Assert.HasCount(1, values);
        Assert.AreEqual("B", values[0].Name);
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

        component.SetParametersAndRender(builder => builder.Add(p => p.Parameters, [second]));

        component.MarkupMatches("-Hello");
    }
}
