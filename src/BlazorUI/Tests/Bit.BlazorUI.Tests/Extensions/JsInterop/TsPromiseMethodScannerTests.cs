using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

[TestClass]
public class TsPromiseMethodScannerTests
{
    [TestMethod]
    public void BodyScan_IgnoresPromiseReturnInsideClosureWithControlFlowBlock()
    {
        var body = """
            {
              const cb = () => {
                if (x) {
                  doThing();
                }
                return fetch(url);
              };
              return 1;
            }
            """;

        Assert.IsFalse(TsPromiseMethodScanner.BodyHasDirectTopLevelPromiseReturn(body));
    }

    [TestMethod]
    public void BodyScan_DetectsTopLevelFetchReturnInsideControlFlowBlock()
    {
        var body = """
            {
              if (x) {
                return fetch(url);
              }
              return 1;
            }
            """;

        Assert.IsTrue(TsPromiseMethodScanner.BodyHasDirectTopLevelPromiseReturn(body));
    }

    [TestMethod]
    public void BodyScan_IgnoresLocalVariableIndirection()
    {
        var body = """
            {
              const p = fetch(url);
              return p;
            }
            """;

        Assert.IsFalse(TsPromiseMethodScanner.BodyHasDirectTopLevelPromiseReturn(body));
    }

    [TestMethod]
    public void BodyScan_ReturnObjectLiteralDoesNotDesyncBraceStack()
    {
        var body = """
            {
              return { ok: true };
              return fetch(url);
            }
            """;

        Assert.IsTrue(TsPromiseMethodScanner.BodyHasDirectTopLevelPromiseReturn(body));
    }

    [TestMethod]
    public void BodyScan_TreatsReturnFollowedByNewlineAsVoidReturn()
    {
        // Automatic Semicolon Insertion: `return` then a newline parses as `return;`, so the fetch(...) on
        // the next line is a separate (unreachable) statement, not the returned value. The method returns
        // void, so it must NOT be classified as promise-returning.
        var body = """
            {
              return
              fetch(url);
            }
            """;

        Assert.IsFalse(TsPromiseMethodScanner.BodyHasDirectTopLevelPromiseReturn(body));
    }

    [TestMethod]
    public void CollectFromSource_DetectsAsyncModifier()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                public static async loadAsync() { await Promise.resolve(); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsTrue(promiseMethods.Contains("Sample.loadAsync"));
    }

    [TestMethod]
    public void CollectFromSource_DetectsPromiseReturnTypeAnnotation()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                public static loadAsync(): Promise<void> { return Promise.resolve(); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsTrue(promiseMethods.Contains("Sample.loadAsync"));
    }

    [TestMethod]
    public void CollectFromSource_PropagatesSameClassDelegationFromAnnotatedCallee()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                private static loadAsync(): Promise<void> { return Promise.resolve(); }
                public static setup() { return Sample.loadAsync('x'); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsTrue(promiseMethods.Contains("Sample.loadAsync"));
        Assert.IsTrue(promiseMethods.Contains("Sample.setup"));
    }

    [TestMethod]
    public void CollectFromSource_DetectsUnannotatedDirectFetchReturn()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                public static loadViaFetch(url: string) { return fetch(url); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsTrue(promiseMethods.Contains("Sample.loadViaFetch"));
    }

    [TestMethod]
    public void CollectFromSource_PropagatesTwoHopDelegationFromAnnotatedSource()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                private static loadAsync(): Promise<void> { return Promise.resolve(); }
                private static wrap() { return Sample.loadAsync(); }
                public static setup() { return Sample.wrap(); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsTrue(promiseMethods.Contains("Sample.loadAsync"));
        Assert.IsTrue(promiseMethods.Contains("Sample.wrap"),
            "First-hop wrapper of an explicit Promise source must be detected.");
        Assert.IsTrue(promiseMethods.Contains("Sample.setup"),
            "Second-hop wrapper must be detected via transitive delegation through the fixpoint.");
    }

    [TestMethod]
    public void CollectFromSource_DoesNotPropagateDelegationToBodyScannedCallee()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                private static loadViaFetch(url: string) { return fetch(url); }
                public static setup(url: string) { return Sample.loadViaFetch(url); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsTrue(promiseMethods.Contains("Sample.loadViaFetch"));
        Assert.IsFalse(promiseMethods.Contains("Sample.setup"),
            "Delegation fixpoint must not chain from body-scanned methods to avoid compounding heuristic false positives.");
    }

    [TestMethod]
    public void CollectFromSource_IgnoresPromiseNestedInsideObjectLiteralReturnType()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                public static makeState(): { pending: Promise<void> } { return { pending: Promise.resolve() }; }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsFalse(promiseMethods.Contains("Sample.makeState"),
            "A Promise nested inside an object-literal return type must not flag the method as promise-returning.");
    }

    [TestMethod]
    public void CollectFromSource_IgnoresPromiseNestedInsideReturnedFunctionType()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                public static makeLoader(): () => Promise<void> { return () => Promise.resolve(); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsFalse(promiseMethods.Contains("Sample.makeLoader"),
            "A method returning a function that returns a Promise does not itself return a Promise.");
    }

    [TestMethod]
    public void CollectFromSource_IgnoresPromiseNestedInsideGenericArgumentReturnType()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                public static collect(): Array<Promise<void>> { return []; }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsFalse(promiseMethods.Contains("Sample.collect"),
            "A Promise nested inside a generic argument must not flag the method as promise-returning.");
    }

    [TestMethod]
    public void CollectFromSource_DetectsTopLevelPromiseWithNestedTypeArgument()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                public static loadAsync(): Promise<{ ok: boolean }> { return Promise.resolve({ ok: true }); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsTrue(promiseMethods.Contains("Sample.loadAsync"),
            "A top-level Promise return type must still be detected even when its type argument is complex.");
    }
}
