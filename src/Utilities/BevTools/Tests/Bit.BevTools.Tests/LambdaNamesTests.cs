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

        Assert.AreEqual("Load", BevLambdaTargets.CalledMethod(page.LoadSlowly.Method)?.Name, "captures this");
        Assert.AreEqual("Load", BevLambdaTargets.CalledMethod(page.LoadWith(7).Method)?.Name, "a closure over a local");
        Assert.AreEqual("LoadAsync", BevLambdaTargets.CalledMethod(page.LoadAwaited.Method)?.Name, "an async lambda, through its state machine");
        Assert.AreEqual("OrdersPage.Load (lambda)", BevEventCallbacks.Method(page.LoadSlowly));
    }

    [TestMethod]
    public void A_lambda_without_exactly_one_component_method_keeps_its_place()
    {
        var page = new OrdersPage();

        Assert.IsNull(BevLambdaTargets.CalledMethod(page.LoadTwice.Method), "two methods: which one is the handler is not for BevTools to guess");
        Assert.IsNull(BevLambdaTargets.CalledMethod(page.Count.Method), "a property is not a handler");
        Assert.IsNull(BevLambdaTargets.CalledMethod(page.Refresh.Method), "StateHasChanged is the framework's");
        Assert.AreEqual("lambda in OrdersPage..ctor", BevEventCallbacks.Method(page.LoadTwice));
    }

    [TestMethod]
    public void A_UI_event_reported_by_a_compiled_lambda_name_is_titled_by_the_method_it_calls()
    {
        var page = new OrdersPage();
        var type = typeof(OrdersPage).FullName!;

        Assert.AreEqual("OrdersPage.Load (lambda)", BevLambdaTargets.DescribeEventHandler(type, page.LoadSlowly.Method.Name));
        Assert.AreEqual("OrdersPage.Load (lambda)", BevLambdaTargets.DescribeEventHandler(type, page.LoadWith(1).Method.Name));
        Assert.AreEqual("lambda in OrdersPage..ctor", BevLambdaTargets.DescribeEventHandler(type, page.Count.Method.Name));
        Assert.IsNull(BevLambdaTargets.DescribeEventHandler(type, "IncrementCount"), "an ordinary method keeps its own name");
        Assert.AreEqual("lambda in Missing.BuildRenderTree", BevLambdaTargets.DescribeEventHandler("App.Missing", "<BuildRenderTree>b__0_1"), "a type that cannot be found");
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
        Assert.AreEqual("Open", BevLambdaTargets.CalledMethod(order)?.Name, "the method itself is known exactly");
        Assert.AreEqual("Show", BevLambdaTargets.CalledMethod(customer)?.Name);
        Assert.AreEqual("lambda in OrdersPage.Row", BevLambdaTargets.DescribeEventHandler(type, order.Name), "by name alone it could be either");

        // A base class's lambda runs under the derived type's name, and the derived type has one of the same name.
        var special = new SpecialOrderList();
        var inherited = special.Inherited().Method;
        var own = special.Handler().Method;
        Assert.AreEqual(inherited.Name, own.Name, "precondition: the compiler gave both the same name");
        Assert.AreEqual("lambda in SpecialOrderList.Handler", BevLambdaTargets.DescribeEventHandler(typeof(SpecialOrderList).FullName!, own.Name));
    }

    [TestMethod]
    public void A_second_call_into_application_code_makes_the_name_a_guess()
    {
        var page = new OrdersPage();

        Assert.IsNull(BevLambdaTargets.CalledMethod(page.DeleteThenLoad.Method), "a service call and a component method");
        Assert.IsNull(BevLambdaTargets.CalledMethod(page.DeleteOnly.Method), "the only call is not the component's");
        Assert.IsNull(BevLambdaTargets.CalledMethod(page.WithLocalFunction.Method), "a local function is application code too");
    }

    [TestMethod]
    public void Hot_reload_forgets_every_name()
    {
        var page = new OrdersPage();
        BevLambdaTargets.DescribeEventHandler(typeof(OrdersPage).FullName!, page.LoadSlowly.Method.Name);
        Assert.IsGreaterThan(0, BevLambdaTargets.CachedCount);

        Assert.Contains(a => a.HandlerType == typeof(BevLambdaTargets), typeof(BevLambdaTargets).Assembly.GetCustomAttributes(typeof(System.Reflection.Metadata.MetadataUpdateHandlerAttribute), false).Cast<System.Reflection.Metadata.MetadataUpdateHandlerAttribute>());
        typeof(BevLambdaTargets).GetMethod("ClearCache", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.Invoke(null, [null]);

        Assert.AreEqual(0, BevLambdaTargets.CachedCount);
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
