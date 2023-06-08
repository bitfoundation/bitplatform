namespace Bit.BlazorUI;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "<Pending>")]
public static class BitCss
{
    public static class Color
    {
        public static class Primary
        {
            public const string Main = "bit-css-color-primary-main";
            public const string Dark = "bit-css-color-primary-dark";
            public const string Light = "bit-css-color-primary-light";

        }
        public static class Secondary
        {
            public const string Main = "bit-css-color-secondary-main";
            public const string Dark = "bit-css-color-secondary-dark";
            public const string Light = "bit-css-color-secondary-light";

        }
    }

    public static class Foreground
    {
        public const string Primary = "bit-css-fg-primary";
        public const string Secondary = "bit-css-fg-secondary";
        public const string Disabled = "bit-css-fg-disabled";
    }

    public static class Background
    {
        public const string Primary = "bit-css-bg-primary";
        public const string Secondary = "bit-css-bg-secondary";
        public const string Disabled = "bit-css-bg-disabled";
    }

    public static class Border
    {
        public const string Primary = "bit-css-brd-primary";
        public const string Secondary = "bit-css-brd-secondary";
        public const string Disabled = "bit-css-brd-disabled";
    }
}
