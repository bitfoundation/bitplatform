using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

[TestClass]
public class TsPromiseMethodScannerTests
{
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
    public void CollectFromSource_DetectsPromiseWithNestedGenericTypeArguments()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                public static loadMap(): Promise<Map<string, number[]>> { return Promise.resolve(new Map()); }
                public static loadState(): Promise<{ ok: boolean }> { return Promise.resolve({ ok: true }); }
                public static invoke<T extends Map<string, unknown>>(id: string): Promise<T> { return Promise.reject(id); }
                public static sync(): Map<string, number[]> { return new Map(); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsTrue(promiseMethods.Contains("Sample.loadMap"),
            "A top-level Promise must be detected when its type argument is itself generic.");
        Assert.IsTrue(promiseMethods.Contains("Sample.loadState"),
            "A top-level Promise must be detected when its type argument is an object-literal type.");
        Assert.IsTrue(promiseMethods.Contains("Sample.invoke"),
            "A generic method whose type parameters nest a generic constraint must still be read as a method.");
        Assert.IsFalse(promiseMethods.Contains("Sample.sync"),
            "A nested generic return type without Promise must not be flagged.");
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
    public void CollectFromSource_IgnoresSynchronousMethods()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                public static setup(id: string, element: HTMLElement) { Sample._items[id] = element; }
                public static getWidth(element: HTMLElement): number { return element.clientWidth; }
                public static describe(): string { return 'sample'; }
                public static dispose(id: string): void { delete Sample._items[id]; }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.AreEqual(0, promiseMethods.Count,
            "A method that is neither async nor annotated with Promise must not be flagged.");
    }

    [TestMethod]
    public void CollectFromSource_DoesNotDetectUnannotatedPromiseReturn()
    {
        // The contract is header-only: a method that returns a promise without being declared async or
        // annotated ": Promise<...>" is not detected. This pins that limit so it is not mistaken for a bug.
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                public static loadViaFetch(url: string) { return fetch(url); }
                private static loadAsync(): Promise<void> { return Promise.resolve(); }
                public static wrap() { return Sample.loadAsync(); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsTrue(promiseMethods.Contains("Sample.loadAsync"));
        Assert.IsFalse(promiseMethods.Contains("Sample.loadViaFetch"));
        Assert.IsFalse(promiseMethods.Contains("Sample.wrap"));
    }

    [TestMethod]
    public void CollectFromSource_IgnoresCommentedOutSignatures()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                // public static async ghostAsync() { await Promise.resolve(); }
                /* public static realGhost(): Promise<void> { return Promise.resolve(); } */
                public static loadAsync(): Promise<void> { return Promise.resolve(); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsTrue(promiseMethods.Contains("Sample.loadAsync"));
        Assert.IsFalse(promiseMethods.Contains("Sample.ghostAsync"),
            "A signature inside a line comment must not be treated as a real declaration.");
        Assert.IsFalse(promiseMethods.Contains("Sample.realGhost"),
            "A signature inside a block comment must not be treated as a real declaration.");
    }

    [TestMethod]
    public void CollectFromSource_IgnoresSignaturesInsideStringLiterals()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                public static describe(): string {
                  return "class Ghost { static async hauntAsync() { await Promise.resolve(); } }";
                }
                public static template(name: string): string {
                  return `class ${name} { static spookAsync(): Promise<void> { return Promise.resolve(); } }`;
                }
                public static quote(): string {
                  return 'it\'s a "quoted" { brace';
                }
                public static async realAsync() { await Promise.resolve(); }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsFalse(promiseMethods.Contains("Ghost.hauntAsync"),
            "A class/method signature embedded in a string literal must not be treated as a real declaration.");
        Assert.IsFalse(promiseMethods.Contains("Sample.spookAsync"),
            "A signature embedded in a template literal must not be treated as a real declaration.");
        Assert.IsFalse(promiseMethods.Contains("Sample.describe"));
        Assert.IsFalse(promiseMethods.Contains("Sample.template"));
        Assert.IsFalse(promiseMethods.Contains("Sample.quote"),
            "An escaped quote and a brace inside a literal must not desynchronize the scan.");
        Assert.IsTrue(promiseMethods.Contains("Sample.realAsync"),
            "A real declaration after string literals with braces must still be found.");
    }

    [TestMethod]
    public void CollectFromSource_IgnoresArrowFunctionPropertiesAndNestedFunctions()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                private static onResize = async () => { await Promise.resolve(); };
                private static handlers: { [id: string]: () => Promise<void> } = {};
                public static setup(id: string) {
                  const load = async (url: string) => { await fetch(url); };
                  async function nested(): Promise<void> { await load(id); }
                  Sample.handlers[id] = () => nested();
                }
                public static async teardown(id: string) { delete Sample.handlers[id]; }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsFalse(promiseMethods.Contains("Sample.onResize"),
            "An arrow-function property is not a method.");
        Assert.IsFalse(promiseMethods.Contains("Sample.handlers"));
        Assert.IsFalse(promiseMethods.Contains("Sample.setup"),
            "Async closures and nested functions inside a synchronous method must not flag the method.");
        Assert.IsFalse(promiseMethods.Contains("Sample.load"));
        Assert.IsFalse(promiseMethods.Contains("Sample.nested"));
        Assert.IsTrue(promiseMethods.Contains("Sample.teardown"));
    }

    [TestMethod]
    public void CollectFromSource_IgnoresStaticGettersAndSetters()
    {
        var ts = """
            namespace BitBlazorUI {
              class Sample {
                private static _ready: Promise<void> = Promise.resolve();
                public static get ready(): Promise<void> { return Sample._ready; }
                public static set ready(value: Promise<void>) { Sample._ready = value; }
                public static async wait() { await Sample.ready; }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsFalse(promiseMethods.Contains("Sample.ready"),
            "A static getter or setter is not an invocable method.");
        Assert.IsFalse(promiseMethods.Contains("Sample.get"));
        Assert.IsFalse(promiseMethods.Contains("Sample.set"));
        Assert.IsTrue(promiseMethods.Contains("Sample.wait"));
    }

    [TestMethod]
    public void CollectFromSource_AttributesMethodsToTheirOwnClassAndSkipsBodilessSignatures()
    {
        var ts = """
            namespace BitBlazorUI {
              class First {
                public static load(id: string): Promise<void>;
                public static load(id: number): Promise<void>;
                public static load(id: any): Promise<void> { return Promise.resolve(); }
              }
              class Second {
                public static load(id: string): void { }
              }
            }
            """;

        var promiseMethods = TsPromiseMethodScanner.CollectFromSource(ts);

        Assert.IsTrue(promiseMethods.Contains("First.load"));
        Assert.IsFalse(promiseMethods.Contains("Second.load"),
            "A same-named synchronous method in another class must be attributed to that class.");
    }
}
