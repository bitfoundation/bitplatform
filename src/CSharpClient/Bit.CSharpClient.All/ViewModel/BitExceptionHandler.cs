#define Debug

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bit.ViewModel
{
    public class BitExceptionHandler
    {
        public static BitExceptionHandler Current { get; set; } = new BitExceptionHandler();

        public virtual void OnExceptionReceived(Exception exp, IDictionary<string, string> properties = null)
        {
            properties = properties ?? new Dictionary<string, string>();

            if (exp != null)
            {
                Debug.WriteLine($"DateTime: {DateTime.Now.ToLongTimeString()} Message: {exp}", category: "ApplicationException");
            }
        }
    }
}
