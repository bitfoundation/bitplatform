using System;
using Xamarin.Forms;

[assembly: XmlnsDefinition("https://bit-framework.com", "Bit")]
[assembly: XmlnsDefinition("https://bit-framework.com", "Bit.CSharpClient.Controls")]
[assembly: XmlnsDefinition("https://bit-framework.com", "Bit.View")]
[assembly: XmlnsDefinition("https://bit-framework.com", "Bit.View.Contracts")]
[assembly: XmlnsDefinition("https://bit-framework.com", "Bit.View.Props")]

namespace Bit
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class IgnoreMeInNavigationStatckAttribute : Attribute
    {

    }

    public static class BitCSharpClientControls
    {
        public static void Init()
        {

        }
    }
}
