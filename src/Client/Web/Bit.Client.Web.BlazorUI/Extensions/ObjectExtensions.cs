using System.Text.Json;

namespace Bit.Client.Web.BlazorUI.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// This extension method serializes the object to JSON and then deserialize the JSON to object's type in order to make a deep copy of object, in deep copying the object and its copy will not pointing to the same address in memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The Object that will be copied</param>
        /// <returns>New deep copy of obj</returns>
        public static T? DeepCopy<T>(this T obj)
        {
            var jsonOfObject = JsonSerializer.Serialize(obj);
            var deepCopyOfObject = JsonSerializer.Deserialize<T>(jsonOfObject);
            return deepCopyOfObject;
        }
    }
}
