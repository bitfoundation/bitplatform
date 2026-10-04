using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.SnackBar;

[TestClass]
public class BitSnackBarParamsTests : BunitTestContext
{
    [TestMethod]
    public async Task BitSnackBarShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitSnackBarCascadingParamsTest>();

        var snackBars = component.FindComponents<BitSnackBar>();

        Assert.AreEqual(2, snackBars.Count);

        await snackBars[0].Instance.Show("first");
        await snackBars[1].Instance.Show("second");

        // The first snack bar takes everything from the cascading parameters.
        var first = snackBars[0].Find(".bit-snb");
        Assert.IsTrue(first.ClassList.Contains("bit-snb-tcn"));
        Assert.IsTrue(first.ClassList.Contains("cascaded-root"));

        var firstStyle = first.GetAttribute("style")!;
        StringAssert.Contains(firstStyle, "margin: 1px;");
        StringAssert.Contains(firstStyle, "--bit-snb-off:2rem");
        StringAssert.Contains(firstStyle, "--bit-snb-max-w:20rem");
        StringAssert.Contains(firstStyle, "--bit-snb-dur-full:0ms");

        var firstItem = snackBars[0].Find(".bit-snb-itm");
        foreach (var cls in new[] { "bit-snb-lg", "bit-snb-otl", "cascaded-item" })
        {
            Assert.IsTrue(firstItem.ClassList.Contains(cls), cls);
        }
        Assert.AreEqual(1, snackBars[0].FindAll(".bit-snb-ico").Count);
        Assert.AreEqual(1, snackBars[0].FindAll(".bit-snb-prb").Count);
        Assert.AreEqual("Dismiss", snackBars[0].Find(".bit-snb-cbt").GetAttribute("aria-label"));

        // The second one sets its own position, size and label, which the cascading parameters must not overwrite.
        var second = snackBars[1].Find(".bit-snb");
        Assert.IsTrue(second.ClassList.Contains("bit-snb-bst"));
        Assert.IsFalse(second.ClassList.Contains("bit-snb-tcn"));

        var secondItem = snackBars[1].Find(".bit-snb-itm");
        Assert.IsTrue(secondItem.ClassList.Contains("bit-snb-sm"));
        Assert.IsTrue(secondItem.ClassList.Contains("bit-snb-otl"));
        Assert.AreEqual("Close it", snackBars[1].Find(".bit-snb-cbt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task BitSnackBarShouldTakeCascadedBehaviorParams()
    {
        var component = RenderComponent<CascadingValue<BitSnackBarParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitSnackBarParams.ParamName);
            parameters.Add(p => p.Value, new BitSnackBarParams { Persistent = true, MaxItems = 1, OverflowBehavior = BitSnackBarOverflowBehavior.Skip });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitSnackBar>(0);
                builder.CloseComponent();
            }));
        });

        var snackBar = component.FindComponent<BitSnackBar>();

        await snackBar.Instance.Show("first");
        await snackBar.Instance.Show("second");

        // A cascaded MaxItems and Skip keep the second one out, and a cascaded Persistent takes the dismiss button away.
        Assert.AreEqual(1, snackBar.Instance.Items.Count);
        Assert.AreEqual("first", snackBar.Instance.Items[0].Title);
        Assert.AreEqual(0, snackBar.FindAll(".bit-snb-cbt").Count);
    }

    [TestMethod]
    public void BitSnackBarStylesheetShouldOnlyReadItsPublicVariables()
    {
        var stylesheet = ReadStylesheet();

        // A public variable declared by the component would stop the value set on an ancestor from inheriting.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"--bit-SnackBar-[\w-]+\s*:"), "A public variable is declared by the stylesheet.");

        var documented = Regex.Matches(stylesheet, @"^//   (--bit-SnackBar-[\w-]+)", RegexOptions.Multiline)
                              .Select(m => m.Groups[1].Value)
                              .ToArray();

        Assert.IsTrue(documented.Length > 0);

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name},", $"{name} is documented but never read with a fallback.");
        }

        var read = Regex.Matches(stylesheet, @"var\((--bit-SnackBar-[\w-]+),").Select(m => m.Groups[1].Value).Distinct();

        foreach (var name in read)
        {
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented.");
        }
    }

    [TestMethod]
    public void BitSnackBarParamsShouldCarryEveryPlainParameterOfTheComponent()
    {
        // A parameter added to the component without its counterpart here is one a BitParams cascade silently
        // ignores. Callbacks and templates are deliberately left out: they belong to the code around one host - and
        // so does ServiceHost, which marks one host out of the others rather than being something they share.
        var paramsProperties = typeof(BitSnackBarParams).GetProperties().Select(p => p.Name).ToHashSet();
        paramsProperties.Add(nameof(BitSnackBar.ServiceHost));

        var missing = typeof(BitSnackBar).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                         .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                         .Where(p => p.PropertyType.Name.StartsWith("EventCallback") is false)
                                         .Where(p => p.PropertyType.Name.StartsWith("RenderFragment") is false)
                                         .Select(p => p.Name)
                                         .Where(n => paramsProperties.Contains(n) is false)
                                         .ToList();

        CollectionAssert.AreEqual(new List<string>(), missing, string.Join(", ", missing));
    }

    [TestMethod]
    public void BitSnackBarParamsShouldApplyEveryPropertyItCarries()
    {
        // The other direction: a property added here but forgotten in UpdateParameters is accepted by the cascade
        // and then never reaches the snack bar.
        var snackBarParams = new BitSnackBarParams();
        var snackBar = new BitSnackBar();

        var properties = typeof(BitSnackBarParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                  .Where(p => p.CanWrite)
                                                  .ToArray();

        foreach (var property in properties)
        {
            var target = typeof(BitSnackBar).GetProperty(property.Name)!;

            property.SetValue(snackBarParams, SampleValue(property.PropertyType, target.GetValue(snackBar)));
        }

        snackBarParams.UpdateParameters(snackBar);

        foreach (var property in properties)
        {
            var target = typeof(BitSnackBar).GetProperty(property.Name)!;

            Assert.AreEqual(property.GetValue(snackBarParams), target.GetValue(snackBar), $"{property.Name} is not applied.");
        }

        // A value unlike the component's default, so the assertion above cannot pass by coincidence.
        static object SampleValue(Type type, object? current)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying == typeof(bool)) return current is true ? false : true;
            if (underlying == typeof(int)) return 7;
            if (underlying == typeof(string)) return "sample";
            if (underlying == typeof(TimeSpan)) return TimeSpan.FromSeconds(7);
            if (underlying == typeof(string[])) return new[] { "F8" };
            if (underlying == typeof(BitIconInfo)) return BitIconInfo.Css("sample");
            if (underlying == typeof(BitSnackBarClassStyles)) return new BitSnackBarClassStyles();
            if (underlying.IsEnum)
            {
                var values = Enum.GetValues(underlying);
                return values.GetValue(values.Length - 1)!;
            }

            throw new NotSupportedException($"Add a sample value for {type}.");
        }
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Notifications", "SnackBar", "BitSnackBar.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
