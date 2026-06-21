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
}
