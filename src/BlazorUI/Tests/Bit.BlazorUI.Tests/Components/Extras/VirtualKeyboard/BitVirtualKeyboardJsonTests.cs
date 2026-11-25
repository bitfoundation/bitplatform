using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard.BitVirtualKeyboardTestLayouts;

namespace Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard;

[TestClass]
public class BitVirtualKeyboardJsonTests
{
    [TestMethod]
    public void ABuiltInLayoutShouldSurviveARoundTripThroughJson()
    {
        var json = BitVirtualKeyboardLayouts.English.ToJson();

        var back = BitVirtualKeyboardLayout.FromJson(json);

        Assert.AreEqual(json, back.ToJson());
        Assert.AreEqual(Compile(BitVirtualKeyboardLayouts.English).AllKeys.Count(), Compile(back).AllKeys.Count());
    }

    [TestMethod]
    public void AHandWrittenLayoutShouldTakeShorthandRowsEnumsByNameAndAnyCaseOfNames()
    {
        const string json = """
            {
              "id": "pin",
              "NAME": "PIN",
              "direction": "Rtl",
              "keys": { "ok": { "action": "Enter", "width": 2 } },
              "layers": [
                {
                  "id": "base",
                  "rows": [
                    "1 2 3",
                    { "keys": [ { "output": "4" }, { "id": "ok" } ], "align": "Start" }
                  ]
                },
                { "id": "shift", "modifiers": "Shift, AltGr", "rows": [ "\\u{200C} x" ] }
              ]
            }
            """;

        var layout = BitVirtualKeyboardLayout.FromJson(json);

        Assert.AreEqual("pin", layout.Id);
        Assert.AreEqual("PIN", layout.Name);
        Assert.AreEqual(BitDir.Rtl, layout.Direction);
        Assert.AreEqual(BitVirtualKeyboardKeyAction.Enter, layout.Keys["ok"].Action);
        Assert.AreEqual(3, layout.Layers[0].Rows[0].Keys.Count);
        Assert.AreEqual(BitVirtualKeyboardRowAlign.Start, layout.Layers[0].Rows[1].Align);
        Assert.AreEqual(BitVirtualKeyboardModifiers.Shift | BitVirtualKeyboardModifiers.AltGr, layout.Layers[1].Modifiers);
        Assert.AreEqual("\U0000200C", layout.Layers[1].Rows[0].Keys[0].Output);
    }

    [TestMethod]
    public void TheJsonShouldWriteEnumsByNameInCamelCaseAndLeaveUnsetValuesOut()
    {
        var layout = Layout(Layer("base", "a {bksp*1.5}")) with
        {
            Direction = BitDir.Rtl,
            Keys = new() { ["x"] = new() { Action = BitVirtualKeyboardKeyAction.Custom, Command = "go", Variations = new() { [BitVirtualKeyboardPurpose.Email] = new() { Output = "@" } } } },
        };

        using var document = JsonDocument.Parse(layout.ToJson());
        var root = document.RootElement;

        Assert.AreEqual("Rtl", root.GetProperty("direction").GetString());
        Assert.AreEqual("Custom", root.GetProperty("keys").GetProperty("x").GetProperty("action").GetString());
        Assert.IsTrue(root.GetProperty("keys").GetProperty("x").GetProperty("variations").TryGetProperty("Email", out _));
        Assert.IsFalse(root.GetProperty("keys").GetProperty("x").TryGetProperty("output", out _));
        Assert.AreEqual(1.5, root.GetProperty("layers")[0].GetProperty("rows")[0].GetProperty("keys")[1].GetProperty("width").GetDouble());
    }

    [TestMethod]
    public void AMalformedShorthandRowInJsonShouldBeAJsonException()
    {
        Assert.ThrowsExactly<JsonException>(() => BitVirtualKeyboardLayout.FromJson("""{ "id": "x", "name": "x", "layers": [ { "id": "b", "rows": [ "{Bad}" ] } ] }"""));
    }
}