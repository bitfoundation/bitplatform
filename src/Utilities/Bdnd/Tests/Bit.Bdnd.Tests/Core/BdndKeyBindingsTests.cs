using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Bit.Bdnd.Tests.Core;

/// <summary>Key bindings a manager accepts: every key doing one thing, and a drag always possible from the keyboard.</summary>
[TestClass]
public class BdndKeyBindingsTests
{
    [TestMethod]
    public void TheDefaultsAreTheWaiAriaKeys()
    {
        var keys = BdndKeyBindings.Default;

        Assert.AreSequenceEqual(new[] { " ", "Enter" }, keys.PickUp);
        Assert.AreSequenceEqual(new[] { " ", "Enter" }, keys.Drop);
        Assert.AreSequenceEqual(new[] { "Escape", "Esc" }, keys.Cancel);
        Assert.AreSequenceEqual(new[] { "ArrowUp" }, keys.Up);
        Assert.AreSequenceEqual(new[] { "ArrowDown" }, keys.Down);
        Assert.AreSequenceEqual(new[] { "ArrowLeft" }, keys.Left);
        Assert.AreSequenceEqual(new[] { "ArrowRight" }, keys.Right);
        Assert.AreSequenceEqual(new[] { "Home" }, keys.First);
        Assert.AreSequenceEqual(new[] { "End" }, keys.Last);
        Assert.AreSame(keys, new BdndDragDropManager().KeyBindings);
    }

    [TestMethod]
    public void AManagerTakesKeysThatEachDoOneThing()
    {
        var manager = new BdndDragDropManager();
        var keys = new BdndKeyBindings { PickUp = ["Enter"], Drop = ["Enter", "d"], Cancel = [], First = [], Last = [] };

        manager.KeyBindings = keys;

        Assert.AreSame(keys, manager.KeyBindings);
        Assert.IsNotNull(manager.KeyMap);
        Assert.IsEmpty(manager.KeyMap["cancel"]);
    }

    [TestMethod]
    public void AKeyBoundToTwoCommandsOfADragIsRefused()
    {
        var manager = new BdndDragDropManager();

        var error = Assert.ThrowsExactly<ArgumentException>(() => manager.KeyBindings = new BdndKeyBindings { Down = ["ArrowDown", "Escape"] });

        Assert.Contains("\"Escape\"", error.Message);
        Assert.AreSame(BdndKeyBindings.Default, manager.KeyBindings);
    }

    [TestMethod]
    public void PickingUpSharesItsKeysWithDroppingOnly()
    {
        var manager = new BdndDragDropManager();

        manager.KeyBindings = new BdndKeyBindings { PickUp = [" ", "Enter", "p"] };

        Assert.ThrowsExactly<ArgumentException>(() => manager.KeyBindings = new BdndKeyBindings { Drop = [" ", "Home"] });
    }

    [TestMethod]
    public void PickingUpAndDroppingNeedAKeyAndNoKeyIsEmpty()
    {
        var manager = new BdndDragDropManager();

        Assert.ThrowsExactly<ArgumentException>(() => manager.KeyBindings = new BdndKeyBindings { PickUp = [] });
        Assert.ThrowsExactly<ArgumentException>(() => manager.KeyBindings = new BdndKeyBindings { Drop = [] });
        Assert.ThrowsExactly<ArgumentException>(() => manager.KeyBindings = new BdndKeyBindings { Up = [""] });
        Assert.ThrowsExactly<ArgumentException>(() => manager.KeyBindings = new BdndKeyBindings { Up = null! });
        Assert.ThrowsExactly<ArgumentNullException>(() => manager.KeyBindings = null!);
    }

    [TestMethod]
    [DataRow("Tab")]
    [DataRow("tab")]
    public void TabCannotBeBoundSoTheFocusIsNeverTrapped(string tab)
    {
        var manager = new BdndDragDropManager();

        var error = Assert.ThrowsExactly<ArgumentException>(() => manager.KeyBindings = new BdndKeyBindings { Down = ["ArrowDown", tab] });
        Assert.ThrowsExactly<ArgumentException>(() => manager.KeyBindings = new BdndKeyBindings { PickUp = [tab] });

        Assert.Contains("Tab", error.Message);
        Assert.AreSame(BdndKeyBindings.Default, manager.KeyBindings);
    }

    [TestMethod]
    public void ALetterInEitherCaseIsOneKey()
    {
        // the module matches a character whatever its case, so "k" and "K" would both go up and down
        var manager = new BdndDragDropManager();

        Assert.ThrowsExactly<ArgumentException>(() => manager.KeyBindings = new BdndKeyBindings { Up = ["ArrowUp", "k"], Down = ["ArrowDown", "K"] });
        manager.KeyBindings = new BdndKeyBindings { Up = ["ArrowUp", "k", "K"] };
    }

    [TestMethod]
    public void TheModulesDefaultKeysAreTheseDefaults()
    {
        // the module keeps a copy of the defaults, so that a source sends nothing for them: the two must not drift apart
        var script = File.ReadAllText(Path.Combine(Path.GetDirectoryName(ThisFile())!, "..", "..", "..", "Bit.Bdnd", "Scripts", "bit-bdnd.ts"));
        var table = Regex.Match(script, @"const defaultKeys: Record<KeyCommand, string\[\]> = \{(?<body>[^}]*)\};");
        Assert.IsTrue(table.Success, "defaultKeys in bit-bdnd.ts");
        var module = Regex.Matches(table.Groups["body"].Value, @"(?<command>\w+): \[(?<keys>[^\]]*)\]")
            .ToDictionary(m => m.Groups["command"].Value, m => Regex.Matches(m.Groups["keys"].Value, "'([^']*)'").Select(k => k.Groups[1].Value).ToArray());

        Assert.IsTrue(BdndKeyBindings.SameMap(BdndKeyBindings.DefaultMap, module),
            string.Join("; ", module.Select(pair => $"{pair.Key}: {string.Join(",", pair.Value)}")));
    }

    private static string ThisFile([CallerFilePath] string path = "") => path;

    [TestMethod]
    public void TheInstructionsFollowTheKeys()
    {
        var manager = new BdndDragDropManager();
        var before = manager.InstructionsId;

        manager.KeyBindings = new BdndKeyBindings { Drop = ["d"] };

        Assert.Contains("D to drop", manager.InstructionsText);
        Assert.AreNotEqual(before, manager.InstructionsId);

        manager.KeyBindings = BdndKeyBindings.Default;
        Assert.AreEqual(before, manager.InstructionsId);
        Assert.Contains("Space or Enter to drop", manager.InstructionsText);
    }

    [TestMethod]
    public void ChangingTheKeysNotifiesTheSources()
    {
        var manager = new BdndDragDropManager();
        var notified = 0;
        manager.StateChanged += () => notified++;
        var keys = new BdndKeyBindings { Last = ["End", "G"] };

        manager.KeyBindings = keys;
        manager.KeyBindings = keys;

        Assert.AreEqual(1, notified);
    }

    [TestMethod]
    public void TheSameKeysInANewInstanceNotifyNothing()
    {
        var manager = new BdndDragDropManager();
        var notified = 0;
        manager.StateChanged += () => notified++;

        manager.KeyBindings = new BdndKeyBindings { Last = ["End", "G"] };
        var map = manager.KeyMap;
        manager.KeyBindings = new BdndKeyBindings { Last = ["End", "G"] };
        manager.KeyBindings = new BdndKeyBindings(); // the defaults, from the defaults' map: a change
        manager.KeyBindings = BdndKeyBindings.Default;

        Assert.AreEqual(2, notified);
        Assert.IsNull(manager.KeyMap);
        Assert.IsNotNull(map);
    }
}
