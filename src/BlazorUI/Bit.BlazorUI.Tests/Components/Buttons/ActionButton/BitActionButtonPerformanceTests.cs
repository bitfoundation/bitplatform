using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ActionButton;

/// <summary>
/// Performance tests for the BitActionButton component.
/// These tests measure memory footprint and render/re-render speed
/// with varying component counts to identify performance characteristics.
/// </summary>
[TestClass]
public class BitActionButtonPerformanceTests : BunitTestContext
{
    // Define component counts for different test scenarios
    private static readonly int[] ComponentCounts = [1, 10, 50, 100, 500];

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void BitActionButton_MemoryFootprint_SingleComponent(int count)
    {
        // Force garbage collection before measurement
        ForceGarbageCollection();

        var memoryBefore = GC.GetAllocatedBytesForCurrentThread();

        var components = new List<IRenderedComponent<BitActionButton>>(count);

        for (int i = 0; i < count; i++)
        {
            var component = RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Button {i}");
                }));
            });
            components.Add(component);
        }

        var memoryAfter = GC.GetAllocatedBytesForCurrentThread();

        var memoryUsed = memoryAfter - memoryBefore;
        var memoryPerComponent = memoryUsed / count;

        // Log the results for analysis
        Debug.WriteLine($"[Memory Test] Count: {count}, Total Memory Used: {memoryUsed:N0} bytes, Per Component: {memoryPerComponent:N0} bytes");

        // Assert that memory usage is within reasonable bounds
        // Note: First component includes bUnit framework overhead (~1.5MB), subsequent ones are much smaller
        // These thresholds are adjusted based on baseline measurements
        var threshold = count == 1 ? 5_000_000 : 500_000; // Higher threshold for single component due to framework overhead

        AssertPerformanceThreshold(memoryPerComponent, threshold,
            $"Memory per component ({memoryPerComponent:N0} bytes) exceeds threshold of {threshold:N0} bytes");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void BitActionButton_MemoryFootprint_WithAllFeatures(int count)
    {
        ForceGarbageCollection();

        var memoryBefore = GC.GetAllocatedBytesForCurrentThread();

        var components = new List<IRenderedComponent<BitActionButton>>(count);

        for (int i = 0; i < count; i++)
        {
            var component = RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.Title, $"Button Title {i}");
                parameters.Add(p => p.AriaLabel, $"Aria Label {i}");
                parameters.Add(p => p.AriaDescription, $"Aria Description {i}");
                parameters.Add(p => p.Color, BitColor.Primary);
                parameters.Add(p => p.IsEnabled, true);
                parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs _) => { }));
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Button {i}");
                }));
            });
            components.Add(component);
        }

        var memoryAfter = GC.GetAllocatedBytesForCurrentThread();
        var memoryUsed = memoryAfter - memoryBefore;
        var memoryPerComponent = memoryUsed / count;

        Debug.WriteLine($"[Memory Test - Full Features] Count: {count}, Total Memory Used: {memoryUsed:N0} bytes, Per Component: {memoryPerComponent:N0} bytes");

        // Note: First component includes bUnit framework overhead, subsequent ones are much smaller
        var threshold = count == 1 ? 5_000_000 : 500_000;

        AssertPerformanceThreshold(memoryPerComponent, threshold,
            $"Memory per component with all features ({memoryPerComponent:N0} bytes) exceeds threshold of {threshold:N0} bytes");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void BitActionButton_MemoryFootprint_AsLink(int count)
    {
        ForceGarbageCollection();

        var memoryBefore = GC.GetAllocatedBytesForCurrentThread();

        var components = new List<IRenderedComponent<BitActionButton>>(count);

        for (int i = 0; i < count; i++)
        {
            var component = RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Link");
                parameters.Add(p => p.Href, $"https://example.com/{i}");
                parameters.Add(p => p.Target, "_blank");
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Link {i}");
                }));
            });
            components.Add(component);
        }

        var memoryAfter = GC.GetAllocatedBytesForCurrentThread();
        var memoryUsed = memoryAfter - memoryBefore;
        var memoryPerComponent = memoryUsed / count;

        Debug.WriteLine($"[Memory Test - Link Mode] Count: {count}, Total Memory Used: {memoryUsed:N0} bytes, Per Component: {memoryPerComponent:N0} bytes");

        // Note: First component includes bUnit framework overhead, subsequent ones are much smaller
        var threshold = count == 1 ? 5_000_000 : 500_000;

        AssertPerformanceThreshold(memoryPerComponent, threshold,
            $"Memory per component in link mode ({memoryPerComponent:N0} bytes) exceeds threshold of {threshold:N0} bytes");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void BitActionButton_InitialRenderSpeed_Simple(int count)
    {
        // Warm-up render
        RenderComponent<BitActionButton>();

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < count; i++)
        {
            RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Button {i}");
                }));
            });
        }

        stopwatch.Stop();

        var totalMs = stopwatch.Elapsed.TotalMilliseconds;
        var perComponentMs = totalMs / count;

        Debug.WriteLine($"[Render Speed - Simple] Count: {count}, Total Time: {totalMs:F2}ms, Per Component: {perComponentMs:F4}ms");

        // Assert render time is within acceptable bounds
        // Adjust threshold based on baseline measurements
        AssertPerformanceThreshold(perComponentMs, 50,
            $"Render time per component ({perComponentMs:F4}ms) exceeds threshold of 50ms");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void BitActionButton_InitialRenderSpeed_WithAllFeatures(int count)
    {
        // Warm-up render
        RenderComponent<BitActionButton>();

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < count; i++)
        {
            RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.Title, $"Button Title {i}");
                parameters.Add(p => p.AriaLabel, $"Aria Label {i}");
                parameters.Add(p => p.AriaDescription, $"Aria Description {i}");
                parameters.Add(p => p.Color, BitColor.Primary);
                parameters.Add(p => p.IsEnabled, true);
                parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs _) => { }));
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Button {i}");
                }));
            });
        }

        stopwatch.Stop();

        var totalMs = stopwatch.Elapsed.TotalMilliseconds;
        var perComponentMs = totalMs / count;

        Debug.WriteLine($"[Render Speed - Full Features] Count: {count}, Total Time: {totalMs:F2}ms, Per Component: {perComponentMs:F4}ms");

        AssertPerformanceThreshold(perComponentMs, 75,
            $"Render time per component with all features ({perComponentMs:F4}ms) exceeds threshold of 75ms");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void BitActionButton_InitialRenderSpeed_LoadingState(int count)
    {
        // Warm-up render
        RenderComponent<BitActionButton>();

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < count; i++)
        {
            RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.IsLoading, true);
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Loading Button {i}");
                }));
            });
        }

        stopwatch.Stop();

        var totalMs = stopwatch.Elapsed.TotalMilliseconds;
        var perComponentMs = totalMs / count;

        Debug.WriteLine($"[Render Speed - Loading State] Count: {count}, Total Time: {totalMs:F2}ms, Per Component: {perComponentMs:F4}ms");

        AssertPerformanceThreshold(perComponentMs, 50,
            $"Render time per component in loading state ({perComponentMs:F4}ms) exceeds threshold of 50ms");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void BitActionButton_ReRenderSpeed_ParameterChange(int count)
    {
        var components = new List<IRenderedComponent<BitActionButton>>(count);

        // Initial render
        for (int i = 0; i < count; i++)
        {
            var component = RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.IsEnabled, true);
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Button {i}");
                }));
            });
            components.Add(component);
        }

        // Measure re-render time
        var stopwatch = Stopwatch.StartNew();

        foreach (var component in components)
        {
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.IsEnabled, false);
            });
        }

        stopwatch.Stop();

        var totalMs = stopwatch.Elapsed.TotalMilliseconds;
        var perComponentMs = totalMs / count;

        Debug.WriteLine($"[Re-render Speed - Parameter Change] Count: {count}, Total Time: {totalMs:F2}ms, Per Component: {perComponentMs:F4}ms");

        AssertPerformanceThreshold(perComponentMs, 25,
            $"Re-render time per component ({perComponentMs:F4}ms) exceeds threshold of 25ms");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void BitActionButton_ReRenderSpeed_IconChange(int count)
    {
        var components = new List<IRenderedComponent<BitActionButton>>(count);

        // Initial render
        for (int i = 0; i < count; i++)
        {
            var component = RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Button {i}");
                }));
            });
            components.Add(component);
        }

        // Measure re-render time
        var stopwatch = Stopwatch.StartNew();

        foreach (var component in components)
        {
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.IconName, "Edit");
            });
        }

        stopwatch.Stop();

        var totalMs = stopwatch.Elapsed.TotalMilliseconds;
        var perComponentMs = totalMs / count;

        Debug.WriteLine($"[Re-render Speed - Icon Change] Count: {count}, Total Time: {totalMs:F2}ms, Per Component: {perComponentMs:F4}ms");

        AssertPerformanceThreshold(perComponentMs, 25,
            $"Re-render time per component on icon change ({perComponentMs:F4}ms) exceeds threshold of 25ms");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void BitActionButton_ReRenderSpeed_LoadingStateToggle(int count)
    {
        var components = new List<IRenderedComponent<BitActionButton>>(count);

        // Initial render (not loading)
        for (int i = 0; i < count; i++)
        {
            var component = RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.IsLoading, false);
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Button {i}");
                }));
            });
            components.Add(component);
        }

        // Measure re-render time when toggling loading state
        var stopwatch = Stopwatch.StartNew();

        foreach (var component in components)
        {
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.IsLoading, true);
            });
        }

        stopwatch.Stop();

        var totalMs = stopwatch.Elapsed.TotalMilliseconds;
        var perComponentMs = totalMs / count;

        Debug.WriteLine($"[Re-render Speed - Loading Toggle] Count: {count}, Total Time: {totalMs:F2}ms, Per Component: {perComponentMs:F4}ms");

        AssertPerformanceThreshold(perComponentMs, 30,
            $"Re-render time per component on loading toggle ({perComponentMs:F4}ms) exceeds threshold of 30ms");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void BitActionButton_ReRenderSpeed_ContentChange(int count)
    {
        var components = new List<IRenderedComponent<BitActionButton>>(count);

        // Initial render
        for (int i = 0; i < count; i++)
        {
            var component = RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Original Text {i}");
                }));
            });
            components.Add(component);
        }

        // Measure re-render time with content change
        var stopwatch = Stopwatch.StartNew();

        int j = 0;
        foreach (var component in components)
        {
            var index = j++;
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Updated Text {index}");
                }));
            });
        }

        stopwatch.Stop();

        var totalMs = stopwatch.Elapsed.TotalMilliseconds;
        var perComponentMs = totalMs / count;

        Debug.WriteLine($"[Re-render Speed - Content Change] Count: {count}, Total Time: {totalMs:F2}ms, Per Component: {perComponentMs:F4}ms");

        AssertPerformanceThreshold(perComponentMs, 25,
            $"Re-render time per component on content change ({perComponentMs:F4}ms) exceeds threshold of 25ms");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void BitActionButton_ReRenderSpeed_MultipleParameterChanges(int count)
    {
        var components = new List<IRenderedComponent<BitActionButton>>(count);

        // Initial render
        for (int i = 0; i < count; i++)
        {
            var component = RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.Title, $"Title {i}");
                parameters.Add(p => p.IsEnabled, true);
                parameters.Add(p => p.Color, BitColor.Primary);
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Button {i}");
                }));
            });
            components.Add(component);
        }

        // Measure re-render time with multiple parameter changes
        var stopwatch = Stopwatch.StartNew();

        int j = 0;
        foreach (var component in components)
        {
            var index = j++;
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.IconName, "Edit");
                parameters.Add(p => p.Title, $"Updated Title {index}");
                parameters.Add(p => p.IsEnabled, false);
                parameters.Add(p => p.Color, BitColor.Secondary);
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Updated Button {index}");
                }));
            });
        }

        stopwatch.Stop();

        var totalMs = stopwatch.Elapsed.TotalMilliseconds;
        var perComponentMs = totalMs / count;

        Debug.WriteLine($"[Re-render Speed - Multiple Changes] Count: {count}, Total Time: {totalMs:F2}ms, Per Component: {perComponentMs:F4}ms");

        AssertPerformanceThreshold(perComponentMs, 35,
            $"Re-render time per component with multiple changes ({perComponentMs:F4}ms) exceeds threshold of 35ms");
    }

    [TestMethod]
    public void BitActionButton_ScalabilityTest_RenderTimeGrowth()
    {
        var renderTimes = new Dictionary<int, double>();

        // Warm-up
        RenderComponent<BitActionButton>();

        foreach (var count in ComponentCounts)
        {
            // Create fresh context for each test
            TearDown();
            Setup();

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                RenderComponent<BitActionButton>(parameters =>
                {
                    parameters.Add(p => p.IconName, "Add");
                    parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                    {
                        builder.AddContent(0, $"Button {i}");
                    }));
                });
            }

            stopwatch.Stop();
            renderTimes[count] = stopwatch.Elapsed.TotalMilliseconds / count;
        }

        // Log all results
        Debug.WriteLine("=== Scalability Test Results ===");
        foreach (var kvp in renderTimes)
        {
            Debug.WriteLine($"Count: {kvp.Key}, Avg Render Time: {kvp.Value:F4}ms");
        }

        // Verify that render time doesn't grow disproportionately
        // The ratio between largest and smallest should be reasonable
        var minTime = renderTimes[ComponentCounts[0]];
        var maxTime = renderTimes[ComponentCounts[^1]];
        var ratio = maxTime / minTime;

        Debug.WriteLine($"Render time ratio (max/min): {ratio:F2}");

        // Allow for some growth but it shouldn't be exponential
        AssertPerformanceThreshold(ratio, 10,
            $"Render time growth ratio ({ratio:F2}) suggests potential performance issues at scale");
    }

    [TestMethod]
    public void BitActionButton_ScalabilityTest_MemoryGrowth()
    {
        var memoryUsage = new Dictionary<int, long>();

        foreach (var count in ComponentCounts)
        {
            // Create fresh context for each test
            TearDown();
            Setup();

            ForceGarbageCollection();
            var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

            var components = new List<IRenderedComponent<BitActionButton>>(count);
            for (int i = 0; i < count; i++)
            {
                var component = RenderComponent<BitActionButton>(parameters =>
                {
                    parameters.Add(p => p.IconName, "Add");
                    parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                    {
                        builder.AddContent(0, $"Button {i}");
                    }));
                });
                components.Add(component);
            }

            var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
            memoryUsage[count] = (memoryAfter - memoryBefore) / count;
        }

        // Log all results
        Debug.WriteLine("=== Memory Scalability Test Results ===");

        foreach (var kvp in memoryUsage)
        {
            Debug.WriteLine($"Count: {kvp.Key}, Avg Memory Per Component: {kvp.Value:N0} bytes");
        }

        // Verify memory usage per component remains relatively stable
        var minMemory = memoryUsage[ComponentCounts[0]];
        var maxMemory = memoryUsage[ComponentCounts[^1]];

        // Memory per component shouldn't vary dramatically with scale
        // Some variation is expected due to shared resources
        if (maxMemory >= minMemory * 2 && maxMemory >= 100_000)
        {
            Assert.Fail($"Memory per component varies significantly at scale (min: {minMemory:N0}, max: {maxMemory:N0})");
        }
    }

    [TestMethod]
    public void BitActionButton_StressTest_RapidReRenders()
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IconName, "Add");
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.AddContent(0, "Stress Test Button");
            }));
        });

        const int rerenderCount = 1000;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < rerenderCount; i++)
        {
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.IsEnabled, i % 2 == 0);
            });
        }

        stopwatch.Stop();

        var totalMs = stopwatch.Elapsed.TotalMilliseconds;
        var perRenderMs = totalMs / rerenderCount;

        Debug.WriteLine($"[Stress Test - Rapid Re-renders] Total: {rerenderCount}, Total Time: {totalMs:F2}ms, Per Render: {perRenderMs:F4}ms");

        AssertPerformanceThreshold(perRenderMs, 10,
            $"Average re-render time ({perRenderMs:F4}ms) exceeds stress test threshold of 10ms");
    }

    [TestMethod]
    public void BitActionButton_StressTest_ManyComponents()
    {
        const int componentCount = 500;

        ForceGarbageCollection();
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        var stopwatch = Stopwatch.StartNew();

        var components = new List<IRenderedComponent<BitActionButton>>(componentCount);
        for (int i = 0; i < componentCount; i++)
        {
            var component = RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.Title, $"Button {i}");
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, $"Button {i}");
                }));
            });
            components.Add(component);
        }

        var renderTime = stopwatch.Elapsed.TotalMilliseconds;

        var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        var memoryUsed = memoryAfter - memoryBefore;

        Debug.WriteLine($"[Stress Test - Many Components] Count: {componentCount}");
        Debug.WriteLine($"  Total Render Time: {renderTime:F2}ms");
        Debug.WriteLine($"  Per Component: {renderTime / componentCount:F4}ms");
        Debug.WriteLine($"  Total Memory: {memoryUsed:N0} bytes");
        Debug.WriteLine($"  Per Component: {memoryUsed / componentCount:N0} bytes");

        // Verify test completed successfully - all components were rendered
        if (components.Count != componentCount)
        {
            Assert.Fail($"Expected {componentCount} components but got {components.Count}");
        }

        AssertPerformanceThreshold(renderTime, 30000, "Rendering took too long");
    }

    [TestMethod]
    public void BitActionButton_StressTest_AlternatingStates()
    {
        const int componentCount = 100;
        const int iterations = 10;

        var components = new List<IRenderedComponent<BitActionButton>>(componentCount);

        // Initial render
        for (int i = 0; i < componentCount; i++)
        {
            var component = RenderComponent<BitActionButton>(parameters =>
            {
                parameters.Add(p => p.IconName, "Add");
                parameters.Add(p => p.IsLoading, false);
                parameters.Add(p => p.IsEnabled, true);
            });
            components.Add(component);
        }

        var stopwatch = Stopwatch.StartNew();

        // Alternate between states
        for (int iter = 0; iter < iterations; iter++)
        {
            bool isLoading = iter % 2 == 0;
            bool isEnabled = iter % 3 != 0;

            foreach (var component in components)
            {
                component.SetParametersAndRender(parameters =>
                {
                    parameters.Add(p => p.IsLoading, isLoading);
                    parameters.Add(p => p.IsEnabled, isEnabled);
                });
            }
        }

        stopwatch.Stop();

        var totalOperations = componentCount * iterations;
        var totalMs = stopwatch.Elapsed.TotalMilliseconds;
        var perOperationMs = totalMs / totalOperations;

        Debug.WriteLine($"[Stress Test - Alternating States]");
        Debug.WriteLine($"  Total Operations: {totalOperations}");
        Debug.WriteLine($"  Total Time: {totalMs:F2}ms");
        Debug.WriteLine($"  Per Operation: {perOperationMs:F4}ms");

        AssertPerformanceThreshold(perOperationMs, 15,
            $"Average operation time ({perOperationMs:F4}ms) exceeds threshold of 15ms");
    }



    private static void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static void AssertPerformanceThreshold(double actual, double threshold, string message)
    {
        if (actual >= threshold)
        {
            Assert.Fail(message);
        }
    }

    private static void AssertPerformanceThreshold(long actual, long threshold, string message)
    {
        if (actual >= threshold)
        {
            Assert.Fail(message);
        }
    }
}
