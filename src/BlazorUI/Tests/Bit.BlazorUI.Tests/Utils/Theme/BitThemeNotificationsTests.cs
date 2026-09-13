using System;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeNotificationsTests : BunitTestContext
{
    [TestMethod]
    public void SubscribingToThemeChangedAloneRegistersTheJsNotifier()
    {
        Services.AddBitBlazorUIServices();
        var notifications = Services.GetRequiredService<BitThemeNotifications>();

        // An app that only ever subscribes - without touching BitThemeManager - must still get
        // JS-driven theme-change events wired up (this used to silently produce no notifications).
        notifications.ThemeChanged += (_, _) => { };

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.registerDotNetNotifier", calledTimes: 1);
    }

    [TestMethod]
    public void RepeatedSubscriptionsRegisterTheJsNotifierOnlyOnce()
    {
        Services.AddBitBlazorUIServices();
        var notifications = Services.GetRequiredService<BitThemeNotifications>();

        notifications.ThemeChanged += (_, _) => { };
        notifications.ThemeChanged += (_, _) => { };
        notifications.ThemeChanged += (_, _) => { };

        // The subscribe trigger funnels into the manager's idempotent registration, so extra
        // subscriptions must not produce extra JS round-trips.
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.registerDotNetNotifier", calledTimes: 1);
    }

    [TestMethod]
    public void SubscribeWithoutDiRegistrationTriggerIsANoOp()
    {
        // A hand-constructed instance has no RegistrationTrigger; subscribing must not throw.
        var notifications = new BitThemeNotifications();

        notifications.ThemeChanged += (_, _) => { };
    }
}
