using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers the normalization the settings object applies on assignment, so an out-of-range value a
/// consumer sets never reaches the grid maths downstream.
/// </summary>
[TestClass]
public class BitFullCalendarSettingsTests
{
    [TestMethod]
    public void DefaultsShouldDescribeAFullDayWithHalfHourSlots()
    {
        var settings = new BitFullCalendarSettings();

        Assert.AreEqual(0, settings.VisibleStartHour);
        Assert.AreEqual(24, settings.VisibleEndHour);
        Assert.AreEqual(30, settings.SlotDurationMinutes);
        Assert.AreEqual(8, settings.StartOfDayHour);
        Assert.AreEqual(3, settings.MaxEventsPerDayCell);
        Assert.IsTrue(settings.AllowEventOverlap);
        Assert.IsFalse(settings.RequireEventDescription);
        Assert.IsFalse(settings.ShowWeekNumbers);
        Assert.IsTrue(settings.ShowCurrentTimeIndicator);
        Assert.IsNull(settings.HiddenDays);
        Assert.IsNull(settings.FirstDayOfWeek);
    }

    [TestMethod]
    [DataRow(-5, 0)]
    [DataRow(0, 0)]
    [DataRow(23, 23)]
    [DataRow(99, 23)]
    public void VisibleStartHourShouldClampTo0Through23(int assigned, int expected)
    {
        Assert.AreEqual(expected, new BitFullCalendarSettings { VisibleStartHour = assigned }.VisibleStartHour);
    }

    [TestMethod]
    [DataRow(-5, 1)]
    [DataRow(1, 1)]
    [DataRow(24, 24)]
    [DataRow(99, 24)]
    public void VisibleEndHourShouldClampTo1Through24(int assigned, int expected)
    {
        Assert.AreEqual(expected, new BitFullCalendarSettings { VisibleEndHour = assigned }.VisibleEndHour);
    }

    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(23, 23)]
    [DataRow(40, 23)]
    public void StartOfDayHourShouldClampTo0Through23(int assigned, int expected)
    {
        Assert.AreEqual(expected, new BitFullCalendarSettings { StartOfDayHour = assigned }.StartOfDayHour);
    }

    [TestMethod]
    [DataRow(5, 5)]
    [DataRow(15, 15)]
    [DataRow(30, 30)]
    [DataRow(60, 60)]
    public void SlotDurationShouldKeepAnAcceptedValue(int assigned, int expected)
    {
        Assert.AreEqual(expected, new BitFullCalendarSettings { SlotDurationMinutes = assigned }.SlotDurationMinutes);
    }

    [TestMethod]
    [DataRow(0, 5)]
    [DataRow(1, 5)]
    [DataRow(7, 6)]
    [DataRow(13, 12)]
    [DataRow(45, 60)]
    [DataRow(1000, 60)]
    public void SlotDurationShouldRoundAnUnsupportedValueToTheNearestAcceptedOne(int assigned, int expected)
    {
        Assert.AreEqual(expected, new BitFullCalendarSettings { SlotDurationMinutes = assigned }.SlotDurationMinutes);
    }

    [TestMethod]
    public void SlotDurationShouldAlwaysDivideTheHour()
    {
        for (var minutes = -10; minutes <= 120; minutes++)
        {
            var normalized = new BitFullCalendarSettings { SlotDurationMinutes = minutes }.SlotDurationMinutes;
            Assert.AreEqual(0, 60 % normalized, $"{minutes} normalized to {normalized}, which does not divide an hour");
        }
    }

    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [DataRow(4, 4)]
    [DataRow(10, 10)]
    [DataRow(99, 10)]
    public void MaxEventsPerDayCellShouldClampTo1Through10(int assigned, int expected)
    {
        Assert.AreEqual(expected, new BitFullCalendarSettings { MaxEventsPerDayCell = assigned }.MaxEventsPerDayCell);
    }

    [TestMethod]
    public void HiddenDaysAndFirstDayOfWeekShouldRoundTrip()
    {
        var settings = new BitFullCalendarSettings
        {
            HiddenDays = [DayOfWeek.Friday, DayOfWeek.Saturday],
            FirstDayOfWeek = DayOfWeek.Sunday
        };

        Assert.AreEqual(2, settings.HiddenDays!.Count);
        Assert.AreEqual(DayOfWeek.Sunday, settings.FirstDayOfWeek);
    }

    [TestMethod]
    public void BusinessHourDefaultsShouldDescribeANineToFiveWeek()
    {
        var settings = new BitFullCalendarSettings();

        Assert.IsNull(settings.BusinessDays, "null means the built-in Monday-to-Friday week");
        Assert.AreEqual(9, settings.BusinessStartHour);
        Assert.AreEqual(17, settings.BusinessEndHour);
        Assert.IsFalse(settings.HighlightBusinessHours);
        Assert.IsFalse(settings.RestrictToBusinessHours);
    }

    [TestMethod]
    [DataRow(-5, 0)]
    [DataRow(0, 0)]
    [DataRow(9, 9)]
    [DataRow(23, 23)]
    [DataRow(30, 23)]
    public void BusinessStartHourShouldClampTo0Through23(int assigned, int expected)
    {
        Assert.AreEqual(expected, new BitFullCalendarSettings { BusinessStartHour = assigned }.BusinessStartHour);
    }

    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [DataRow(17, 17)]
    [DataRow(24, 24)]
    [DataRow(48, 24)]
    public void BusinessEndHourShouldClampTo1Through24(int assigned, int expected)
    {
        Assert.AreEqual(expected, new BitFullCalendarSettings { BusinessEndHour = assigned }.BusinessEndHour);
    }

    [TestMethod]
    public void MonthGridDefaultsShouldKeepTheGridAsItAlwaysWas()
    {
        var settings = new BitFullCalendarSettings();

        Assert.IsFalse(settings.FixedWeekCount);
        Assert.IsTrue(settings.ShowNonCurrentDates);
        Assert.IsFalse(settings.NavLinks);
    }
}
