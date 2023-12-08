namespace Microsoft.AspNetCore.Mvc;

[AttributeUsage(AttributeTargets.Method)]
internal class HttpGetAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class HttpPostAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class HttpPutAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class HttpDeleteAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class HttpPatchAttribute : Attribute { }
