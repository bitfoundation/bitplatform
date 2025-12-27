using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser-based performance tests for BitActionButton component using Playwright.
/// These tests measure actual rendering performance in a real browser environment.
/// </summary>
[TestClass]
[TestCategory("Performance")]
[TestCategory("Browser")]
public class BitActionButtonBrowserTests : PerformanceTestBase
{
    #region Initial Render Performance Tests

    [TestMethod]
    [TestCategory("InitialRender")]
    public async Task BitActionButton_InitialRender_10Components()
    {
        await TestInitialRender(10, Thresholds.Render10Components);
    }

    [TestMethod]
    [TestCategory("InitialRender")]
    public async Task BitActionButton_InitialRender_100Components()
    {
        await TestInitialRender(100, Thresholds.Render100Components);
    }

    [TestMethod]
    [TestCategory("InitialRender")]
    public async Task BitActionButton_InitialRender_500Components()
    {
        await TestInitialRender(500, Thresholds.Render500Components);
    }

    [TestMethod]
    [TestCategory("InitialRender")]
    public async Task BitActionButton_InitialRender_1000Components()
    {
        await TestInitialRender(1000, Thresholds.Render1000Components);
    }

    private async Task TestInitialRender(int count, double threshold)
    {
        // Navigate to the performance test page
        await Page.GotoAsync($"{BaseUrl}/perf/action-button/{count}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click the render button
        await Page.Locator("#btn-render").ClickAsync();

        // Wait for rendering to complete
        await WaitForRenderComplete();

        // Get and validate the render time
        var renderTime = await GetRenderTime();
        var componentCount = await GetComponentCount();

        Console.WriteLine($"Initial Render - {count} components: {renderTime:F2}ms");

        Assert.AreEqual(count, componentCount, $"Expected {count} components, but got {componentCount}");
        AssertWithinThreshold(renderTime, threshold, $"Initial render time for {count} components");
    }

    #endregion

    #region Re-render Performance Tests

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitActionButton_ReRender_10Components()
    {
        await TestReRender(10, Thresholds.ReRender10Components);
    }

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitActionButton_ReRender_100Components()
    {
        await TestReRender(100, Thresholds.ReRender100Components);
    }

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitActionButton_ReRender_500Components()
    {
        await TestReRender(500, Thresholds.ReRender500Components);
    }

    [TestMethod]
    [TestCategory("ReRender")]
    public async Task BitActionButton_ReRender_1000Components()
    {
        await TestReRender(1000, Thresholds.ReRender1000Components);
    }

    private async Task TestReRender(int count, double threshold)
    {
        // Navigate to the performance test page
        await Page.GotoAsync($"{BaseUrl}/perf/action-button/{count}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // First, render the components
        await Page.Locator("#btn-render").ClickAsync();
        await WaitForRenderComplete();

        // Now trigger a re-render
        await Page.Locator("#btn-rerender").ClickAsync();
        await WaitForReRenderComplete();

        // Get and validate the re-render time
        var reRenderTime = await GetReRenderTime();

        Console.WriteLine($"Re-render - {count} components: {reRenderTime:F2}ms");

        AssertWithinThreshold(reRenderTime, threshold, $"Re-render time for {count} components");
    }

    #endregion

    #region Memory Usage Tests

    [TestMethod]
    [TestCategory("Memory")]
    public async Task BitActionButton_Memory_10Components()
    {
        await TestMemoryUsage(10, Thresholds.Memory10Components);
    }

    [TestMethod]
    [TestCategory("Memory")]
    public async Task BitActionButton_Memory_100Components()
    {
        await TestMemoryUsage(100, Thresholds.Memory100Components);
    }

    [TestMethod]
    [TestCategory("Memory")]
    public async Task BitActionButton_Memory_500Components()
    {
        await TestMemoryUsage(500, Thresholds.Memory500Components);
    }

    [TestMethod]
    [TestCategory("Memory")]
    public async Task BitActionButton_Memory_1000Components()
    {
        await TestMemoryUsage(1000, Thresholds.Memory1000Components);
    }

    private async Task TestMemoryUsage(int count, double threshold)
    {
        // Navigate to the performance test page
        await Page.GotoAsync($"{BaseUrl}/perf/action-button/{count}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Get baseline memory
        var baselineMemory = await GetMemoryUsageMB();

        // Render the components
        await Page.Locator("#btn-render").ClickAsync();
        await WaitForRenderComplete();

        // Get memory after render
        var afterRenderMemory = await GetMemoryUsageMB();
        var memoryIncrease = afterRenderMemory - baselineMemory;

        Console.WriteLine($"Memory - {count} components: Baseline={baselineMemory:F2}MB, After={afterRenderMemory:F2}MB, Increase={memoryIncrease:F2}MB");

        // Note: Memory API may not be available in all browsers
        if (afterRenderMemory > 0)
        {
            AssertWithinThreshold(afterRenderMemory, threshold, $"Memory usage for {count} components");
        }
        else
        {
            Console.WriteLine("Memory measurement not available in this browser");
        }
    }

    #endregion

    #region Scalability Tests

    [TestMethod]
    [TestCategory("Scalability")]
    public async Task BitActionButton_ScalabilityTest_LinearGrowth()
    {
        var counts = new[] { 10, 50, 100, 200, 500 };
        var renderTimes = new List<(int Count, double Time)>();

        foreach (var count in counts)
        {
            await Page.GotoAsync($"{BaseUrl}/perf/action-button/{count}");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Page.Locator("#btn-render").ClickAsync();
            await WaitForRenderComplete();

            var renderTime = await GetRenderTime();
            renderTimes.Add((count, renderTime));

            Console.WriteLine($"Scalability - {count} components: {renderTime:F2}ms");

            // Clear for next iteration
            await Page.Locator("#btn-clear").ClickAsync();
            await Task.Delay(100);
        }

        // Verify roughly linear scaling (not exponential)
        // Time per component should remain relatively stable
        var timePerComponent = renderTimes.Select(r => r.Time / r.Count).ToList();
        var avgTimePerComponent = timePerComponent.Average();
        var maxDeviation = timePerComponent.Max() / avgTimePerComponent;

        Console.WriteLine($"Average time per component: {avgTimePerComponent:F4}ms");
        Console.WriteLine($"Max deviation ratio: {maxDeviation:F2}x");

        // Allow up to 5x deviation (accounting for setup overhead with smaller counts)
        if (maxDeviation > 5)
        {
            Assert.Fail($"Non-linear scaling detected. Max deviation: {maxDeviation:F2}x from average");
        }
    }

    [TestMethod]
    [TestCategory("Scalability")]
    public async Task BitActionButton_MultipleReRendersPerformance()
    {
        const int componentCount = 100;
        const int reRenderCount = 5;

        await Page.GotoAsync($"{BaseUrl}/perf/action-button/{componentCount}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Initial render
        await Page.Locator("#btn-render").ClickAsync();
        await WaitForRenderComplete();

        var reRenderTimes = new List<double>();

        // Multiple re-renders
        for (int i = 0; i < reRenderCount; i++)
        {
            await Page.Locator("#btn-rerender").ClickAsync();
            await WaitForReRenderComplete();

            var reRenderTime = await GetReRenderTime();
            reRenderTimes.Add(reRenderTime);

            Console.WriteLine($"Re-render {i + 1}: {reRenderTime:F2}ms");
        }

        // Re-render times should be consistent
        var avgReRenderTime = reRenderTimes.Average();
        var maxReRenderTime = reRenderTimes.Max();

        Console.WriteLine($"Average re-render time: {avgReRenderTime:F2}ms");
        Console.WriteLine($"Max re-render time: {maxReRenderTime:F2}ms");

        // Max should not be more than 3x average (no degradation)
        if (maxReRenderTime > avgReRenderTime * 3)
        {
            Assert.Fail($"Re-render performance degradation detected. Max ({maxReRenderTime:F2}ms) > 3x average ({avgReRenderTime:F2}ms)");
        }
    }

    #endregion

    #region Stress Tests

    [TestMethod]
    [TestCategory("Stress")]
    public async Task BitActionButton_RapidRenderClearCycles()
    {
        const int componentCount = 100;
        const int cycleCount = 5;

        await Page.GotoAsync($"{BaseUrl}/perf/action-button/{componentCount}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var cycleTimes = new List<double>();

        for (int i = 0; i < cycleCount; i++)
        {
            var cycleStart = DateTime.Now;

            // Render
            await Page.Locator("#btn-render").ClickAsync();
            await WaitForRenderComplete();

            // Clear
            await Page.Locator("#btn-clear").ClickAsync();
            await Page.WaitForFunctionAsync("() => document.getElementById('status')?.innerText === 'Cleared'");

            var cycleTime = (DateTime.Now - cycleStart).TotalMilliseconds;
            cycleTimes.Add(cycleTime);

            Console.WriteLine($"Cycle {i + 1}: {cycleTime:F2}ms");
        }

        var avgCycleTime = cycleTimes.Average();
        Console.WriteLine($"Average cycle time: {avgCycleTime:F2}ms");

        // Should complete within reasonable time
        AssertWithinThreshold(avgCycleTime, 2000, "Average render/clear cycle time");
    }

    [TestMethod]
    [TestCategory("Stress")]
    public async Task BitActionButton_LargeScaleRender_2000Components()
    {
        const int count = 2000;
        const double threshold = 10000; // 10 seconds for 2000 components

        await Page.GotoAsync($"{BaseUrl}/perf/action-button");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Set the count manually
        await Page.Locator("input[type='number']").FillAsync(count.ToString());

        // Render
        await Page.Locator("#btn-render").ClickAsync();
        await WaitForRenderComplete();

        var renderTime = await GetRenderTime();
        var componentCount = await GetComponentCount();

        Console.WriteLine($"Large scale render - {count} components: {renderTime:F2}ms");

        Assert.AreEqual(count, componentCount, $"Expected {count} components, but got {componentCount}");
        AssertWithinThreshold(renderTime, threshold, $"Large scale render time for {count} components");
    }

    #endregion

    #region Visual Verification Tests

    [TestMethod]
    [TestCategory("Visual")]
    public async Task BitActionButton_ComponentsRenderCorrectly()
    {
        const int count = 10;

        await Page.GotoAsync($"{BaseUrl}/perf/action-button/{count}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("#btn-render").ClickAsync();
        await WaitForRenderComplete();

        // Verify the buttons are actually rendered
        var buttons = Page.Locator("#test-container .bit-acb");
        var buttonCount = await buttons.CountAsync();

        Console.WriteLine($"Found {buttonCount} BitActionButton elements in DOM");

        Assert.AreEqual(count, buttonCount, $"Expected {count} BitActionButton elements, but found {buttonCount}");
    }

    [TestMethod]
    [TestCategory("Visual")]
    public async Task BitActionButton_ToggleIsEnabled_UpdatesDOM()
    {
        const int count = 10;

        await Page.GotoAsync($"{BaseUrl}/perf/action-button/{count}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("#btn-render").ClickAsync();
        await WaitForRenderComplete();

        // Get initial state
        var enabledButtons = await Page.Locator("#test-container .bit-acb:not(.bit-dis)").CountAsync();
        Console.WriteLine($"Enabled buttons before toggle: {enabledButtons}");

        // Toggle IsEnabled
        await Page.Locator("#btn-rerender").ClickAsync();
        await WaitForReRenderComplete();

        // Get new state
        var disabledButtons = await Page.Locator("#test-container .bit-acb.bit-dis").CountAsync();
        Console.WriteLine($"Disabled buttons after toggle: {disabledButtons}");

        Assert.AreEqual(count, disabledButtons, "All buttons should be disabled after toggle");
    }

    #endregion
}
