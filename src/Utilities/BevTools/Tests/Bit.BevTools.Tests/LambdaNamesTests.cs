using Microsoft.AspNetCore.Components;

namespace Bit.BevTools.Tests;

/// <summary>
/// An inline handler such as <c>@onclick="() => Load(4000)"</c> reaches BevTools as a compiled name,
/// <c>&lt;BuildRenderTree&gt;b__0_1</c>. The name a developer recognises is the method it calls - when the IL proves
/// there is exactly one.
/// </summary>
[TestClass]
public class LambdaNamesTests
{
    [TestMethod]
    public void A_lambda_that_calls_one_component_method_is_named_after_it()
    {
        var page = new OrdersPage();

        Assert.AreEqual("Load", BevToolsLambdaTargets.CalledMethod(page.LoadSlowly.Method)?.Name, "captures this");
        Assert.AreEqual("Load", BevToolsLambdaTargets.CalledMethod(page.LoadWith(7).Method)?.Name, "a closure over a local");
        Assert.AreEqual("LoadAsync", BevToolsLambdaTargets.CalledMethod(page.LoadAwaited.Method)?.Name, "an async lambda, through its state machine");
        Assert.AreEqual("OrdersPage.Load (lambda)", BevToolsEventCallbacks.Method(page.LoadSlowly));
    }

    [TestMethod]
    public void A_lambda_without_exactly_one_component_method_keeps_its_place()
    {
        var page = new OrdersPage();

        Assert.IsNull(BevToolsLambdaTargets.CalledMethod(page.LoadTwice.Method), "two methods: which one is the handler is not for BevTools to guess");
        Assert.IsNull(BevToolsLambdaTargets.CalledMethod(page.Count.Method), "a property is not a handler");
        Assert.IsNull(BevToolsLambdaTargets.CalledMethod(page.Refresh.Method), "StateHasChanged is the framework's");
        Assert.AreEqual("lambda in OrdersPage..ctor", BevToolsEventCallbacks.Method(page.LoadTwice));
    }

    [TestMethod]
    public void A_UI_event_reported_by_a_compiled_lambda_name_is_titled_by_the_method_it_calls()
    {
        var page = new OrdersPage();
        var type = typeof(OrdersPage).FullName!;

        Assert.AreEqual("OrdersPage.Load (lambda)", BevToolsLambdaTargets.DescribeEventHandler(type, page.LoadSlowly.Method.Name));
        Assert.AreEqual("OrdersPage.Load (lambda)", BevToolsLambdaTargets.DescribeEventHandler(type, page.LoadWith(1).Method.Name));
        Assert.AreEqual("lambda in OrdersPage..ctor", BevToolsLambdaTargets.DescribeEventHandler(type, page.Count.Method.Name));
        Assert.IsNull(BevToolsLambdaTargets.DescribeEventHandler(type, "IncrementCount"), "an ordinary method keeps its own name");
        Assert.AreEqual("lambda in Missing.BuildRenderTree", BevToolsLambdaTargets.DescribeEventHandler("App.Missing", "<BuildRenderTree>b__0_1"), "a type that cannot be found");
    }

    /// <summary>
    /// <c>@bind</c> (and <c>@bind:get</c>/<c>@bind:set</c>) hands the element a lambda of the framework's binder, which
    /// converts the value and calls the setter the component wrote. Named by its compiled name, every bound input read
    /// "lambda in FilterBar.CreateBinderCoreAsync": the component's type with a method of the framework's.
    /// </summary>
    [TestMethod]
    public void A_bound_input_is_named_after_the_setter_the_component_wrote()
    {
        var box = new BoundBox();

        // What the Razor compiler emits for @bind:get="Query" @bind:set="OnQuery" and for @bind="Query".
        var withSetter = BevToolsBlazorReflection.EventCallbackDelegate(EventCallback.Factory.CreateBinder(box,
            Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.CreateInferredBindSetter(callback: box.OnQuery, value: box.Query), box.Query ?? ""))!;
        var assignment = BevToolsBlazorReflection.EventCallbackDelegate(EventCallback.Factory.CreateBinder(box, value => box.Query = value, box.Query ?? ""))!;

        Assert.AreEqual("BoundBox.OnQuery (@bind)", BevToolsEventCallbacks.Method(withSetter));
        Assert.EndsWith(" (@bind)", BevToolsEventCallbacks.Method(assignment));
        Assert.DoesNotContain("CreateBinderCore", BevToolsEventCallbacks.Method(assignment), StringComparison.Ordinal);

        // A UI event carries only the method's compiled name, which cannot say which setter: the component is named.
        Assert.AreEqual("BoundBox (@bind)", BevToolsLambdaTargets.DescribeEventHandler(typeof(BoundBox).FullName!, withSetter.Method.Name));
    }

    public sealed class BoundBox
    {
        public string? Query { get; set; }

        public void OnQuery(string? value) => Query = value;
    }

    [TestMethod]
    public void A_compiled_name_that_more_than_one_lambda_carries_is_not_named()
    {
        var page = new OrdersPage();
        var type = typeof(OrdersPage).FullName!;

        // Two overloads capture their parameter: each closure class has its own <Row>b__0.
        var order = page.Row(1001).Method;
        var customer = page.Row("ada").Method;
        Assert.AreEqual(order.Name, customer.Name, "precondition: the compiler gave both the same name");
        Assert.AreEqual("Open", BevToolsLambdaTargets.CalledMethod(order)?.Name, "the method itself is known exactly");
        Assert.AreEqual("Show", BevToolsLambdaTargets.CalledMethod(customer)?.Name);
        Assert.AreEqual("lambda in OrdersPage.Row", BevToolsLambdaTargets.DescribeEventHandler(type, order.Name), "by name alone it could be either");

        // A base class's lambda runs under the derived type's name, and the derived type has one of the same name.
        var special = new SpecialOrderList();
        var inherited = special.Inherited().Method;
        var own = special.Handler().Method;
        Assert.AreEqual(inherited.Name, own.Name, "precondition: the compiler gave both the same name");
        Assert.AreEqual("lambda in SpecialOrderList.Handler", BevToolsLambdaTargets.DescribeEventHandler(typeof(SpecialOrderList).FullName!, own.Name));
    }

    [TestMethod]
    public void A_second_call_into_application_code_makes_the_name_a_guess()
    {
        var page = new OrdersPage();

        Assert.IsNull(BevToolsLambdaTargets.CalledMethod(page.DeleteThenLoad.Method), "a service call and a component method");
        Assert.IsNull(BevToolsLambdaTargets.CalledMethod(page.DeleteOnly.Method), "the only call is not the component's");
        Assert.IsNull(BevToolsLambdaTargets.CalledMethod(page.WithLocalFunction.Method), "a local function is application code too");
    }

    [TestMethod]
    public void Hot_reload_forgets_every_name()
    {
        var page = new OrdersPage();
        BevToolsLambdaTargets.DescribeEventHandler(typeof(OrdersPage).FullName!, page.LoadSlowly.Method.Name);
        Assert.IsGreaterThan(0, BevToolsLambdaTargets.CachedCount);

        Assert.Contains(a => a.HandlerType == typeof(BevToolsLambdaTargets), typeof(BevToolsLambdaTargets).Assembly.GetCustomAttributes(typeof(System.Reflection.Metadata.MetadataUpdateHandlerAttribute), false).Cast<System.Reflection.Metadata.MetadataUpdateHandlerAttribute>());
        typeof(BevToolsLambdaTargets).GetMethod("ClearCache", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.Invoke(null, [null]);

        Assert.AreEqual(0, BevToolsLambdaTargets.CachedCount);
    }

    public sealed class OrdersApi
    {
        public void Delete(int id) => _ = id;
    }

    // Handler is the first method of both, so both lambdas are <Handler>b__0_0.
    public class OrderListBase : ComponentBase
    {
        public virtual Action Handler() => () => Delete();

        protected void Delete() => StateHasChanged();
    }

    public sealed class SpecialOrderList : OrderListBase
    {
        public override Action Handler() => () => Archive();

        public Action Inherited() => base.Handler();

        private void Archive() => StateHasChanged();
    }

    private sealed class OrdersPage : ComponentBase
    {
        public OrdersPage()
        {
            LoadSlowly = () => Load(4000);
            LoadTwice = () =>
            {
                Load(1);
                Clear();
            };
            Count = () => Loaded++;
            Refresh = () => StateHasChanged();
            LoadAwaited = async () => await LoadAsync();
            DeleteThenLoad = () =>
            {
                _api.Delete(7);
                Load(2);
            };
            DeleteOnly = () => _api.Delete(7);
            WithLocalFunction = () =>
            {
                Local();
                Load(3);
            };

            void Local() => Loaded++;
        }

        private readonly OrdersApi _api = new();

        public int Loaded { get; private set; }

        public Action DeleteThenLoad { get; }

        public Action DeleteOnly { get; }

        public Action WithLocalFunction { get; }

        public Action Row(int order) => () => Open(order);

        public Action Row(string customer) => () => Show(customer);

        private void Open(int order) => Loaded = order;

        private void Show(string customer) => Loaded = customer.Length;

        public Action LoadSlowly { get; }

        public Action LoadTwice { get; }

        public Action Count { get; }

        public Action Refresh { get; }

        public Func<Task> LoadAwaited { get; }

        public Action LoadWith(int delay) => () => Load(delay);

        private void Load(int delay) => Loaded += delay;

        private Task LoadAsync() => Task.CompletedTask;

        private void Clear() => Loaded = 0;
    }
}
