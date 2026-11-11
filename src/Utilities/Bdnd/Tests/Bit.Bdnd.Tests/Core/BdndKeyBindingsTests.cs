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

        StringAssert.Contains(error.Message, "\"Escape\"");
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
