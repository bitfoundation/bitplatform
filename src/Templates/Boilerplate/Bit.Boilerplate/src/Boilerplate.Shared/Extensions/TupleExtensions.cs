using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

/// <summary>
/// This code was inspired by a post by 'Federico Alterio', found here:
/// https://www.codeproject.com/Tips/5367698/Awaiting-a-Tuple-in-Csharp
/// 
/// In essence, this code empowers the concurrent execution of multiple asynchronous tasks,
/// promoting a streamlined and efficient workflow across various functionalities.
/// 
/// As an alternative to the following code:
/// public class MyPage : AppComponentBase
/// {
///     CategoryDto[] categories;
///     ProductDto[] products;
///     protected override async Task OnInitAsync()
///     {
///         categories = await HttpClient.GetJsonAsync("api/categories"); // Tooks 150ms
///         products = await HttpClient.GetJsonAsync("api/products"); // Tooks 150ms
///         // The total time is 300ms 😖
///     }
/// }
/// 
/// You can now employ the following concise approach:
/// public class MyPage : AppComponentBase
/// {
///     CategoryDto[] categories;
///     ProductDto[] products;
///     protected override async Task OnInitAsync()
///     {
///         (categories, products) = await (HttpClient.GetJsonAsync("api/categories"), HttpClient.GetJsonAsync("api/products")); // Tooks 150ms
///         // The total time is 150ms 👍
///     }
/// }
/// </summary>
public static class TupleExtensions
{
    public static TaskAwaiter<(T1, T2)> GetAwaiter<T1, T2>(this (Task<T1>, Task<T2>) tuple)
    {
        async Task<(T1, T2)> UnifyTasks()
        {
            var (task1, task2) = tuple;
            await Task.WhenAll(task1, task2);
            return (task1.Result, task2.Result);
        }

        return UnifyTasks().GetAwaiter();
    }

    public static TaskAwaiter<(T1, T2, T3)> GetAwaiter<T1, T2, T3>(this (Task<T1>, Task<T2>, Task<T3>) tuple)
    {
        async Task<(T1, T2, T3)> UnifyTasks()
        {
            var (task1, task2, task3) = tuple;
            await Task.WhenAll(task1, task2, task3);
            return (task1.Result, task2.Result, task3.Result);
        }

        return UnifyTasks().GetAwaiter();
    }

    public static TaskAwaiter<(T1, T2, T3, T4)> GetAwaiter<T1, T2, T3, T4>(this (Task<T1>, Task<T2>, Task<T3>, Task<T4>) tuple)
    {
        async Task<(T1, T2, T3, T4)> UnifyTasks()
        {
            var (task1, task2, task3, task4) = tuple;
            await Task.WhenAll(task1, task2, task3, task4);
            return (task1.Result, task2.Result, task3.Result, task4.Result);
        }

        return UnifyTasks().GetAwaiter();
    }

    public static TaskAwaiter<(T1, T2, T3, T4, T5)> GetAwaiter<T1, T2, T3, T4, T5>(this (Task<T1>, Task<T2>, Task<T3>, Task<T4>, Task<T5>) tuple)
    {
        async Task<(T1, T2, T3, T4, T5)> UnifyTasks()
        {
            var (task1, task2, task3, task4, task5) = tuple;
            await Task.WhenAll(task1, task2, task3, task4, task5);
            return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result);
        }

        return UnifyTasks().GetAwaiter();
    }

    public static TaskAwaiter<(T1, T2, T3, T4, T5, T6)> GetAwaiter<T1, T2, T3, T4, T5, T6>(this (Task<T1>, Task<T2>, Task<T3>, Task<T4>, Task<T5>, Task<T6>) tuple)
    {
        async Task<(T1, T2, T3, T4, T5, T6)> UnifyTasks()
        {
            var (task1, task2, task3, task4, task5, task6) = tuple;
            await Task.WhenAll(task1, task2, task3, task4, task5, task6);
            return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result, task6.Result);
        }

        return UnifyTasks().GetAwaiter();
    }

    public static TaskAwaiter<(T1, T2, T3, T4, T5, T6, T7)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7>(this (Task<T1>, Task<T2>, Task<T3>, Task<T4>, Task<T5>, Task<T6>, Task<T7>) tuple)
    {
        async Task<(T1, T2, T3, T4, T5, T6, T7)> UnifyTasks()
        {
            var (task1, task2, task3, task4, task5, task6, task7) = tuple;
            await Task.WhenAll(task1, task2, task3, task4, task5, task6, task7);
            return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result, task6.Result, task7.Result);
        }

        return UnifyTasks().GetAwaiter();
    }
}
