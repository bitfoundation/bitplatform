namespace Bit.BlazorUI;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "<Pending>")]
public static class BitCss
{
    public static class Color
    {
        public static class Primary
        {
            public const string Main = "bit-css-clr-primary-main";
            public const string Dark = "bit-css-clr-primary-dark";
            public const string Light = "bit-css-clr-primary-light";

        }
        public static class Secondary
        {
            public const string Main = "bit-css-clr-secondary-main";
            public const string Dark = "bit-css-clr-secondary-dark";
            public const string Light = "bit-css-clr-secondary-light";

        }

        public static class Foreground
        {
            public const string Primary = "bit-css-clr-fg-primary";
            public const string Secondary = "bit-css-clr-fg-secondary";
            public const string Disabled = "bit-css-clr-fg-disabled";
        }

        public static class Background
        {
            public const string Primary = "bit-css-clr-bg-primary";
            public const string Secondary = "bit-css-clr-bg-secondary";
            public const string Disabled = "bit-css-clr-bg-disabled";
        }

        public static class Border
        {
            public const string Primary = "bit-css-clr-brd-primary";
            public const string Secondary = "bit-css-clr-brd-secondary";
            public const string Disabled = "bit-css-clr-brd-disabled";
        }

        public static class Action
        {
            public static class Hover
            {
                public static class Primary
                {
                    public const string Main = "bit-css-clr-action-hover-primary-main";
                    public const string Dark = "bit-css-clr-action-hover-primary-dark";
                    public const string Light = "bit-css-clr-action-hover-primary-light";
                }
                public static class Secondary
                {
                    public const string Main = "bit-css-clr-action-hover-secondary-main";
                    public const string Dark = "bit-css-clr-action-hover-secondary-dark";
                    public const string Light = "bit-css-clr-action-hover-secondary-light";
                }

                public static class Foreground
                {
                    public const string Primary = "bit-css-clr-action-hover-foreground-primary";
                    public const string Secondary = "bit-css-clr-action-hover-foreground-secondary";
                }

                public static class Background
                {
                    public const string Primary = "bit-css-clr-action-hover-background-primary";
                    public const string Secondary = "bit-css-clr-action-hover-background-secondary";
                }

                public static class Border
                {
                    public const string Primary = "bit-css-clr-action-hover-border-primary";
                    public const string Secondary = "bit-css-clr-action-hover-border-secondary";
                }
            }
            public static class Active
            {
                public static class Primary
                {
                    public const string Main = "bit-css-clr-action-active-primary-main";
                    public const string Dark = "bit-css-clr-action-active-primary-dark";
                    public const string Light = "bit-css-clr-action-active-primary-light";
                }
                public static class Secondary
                {
                    public const string Main = "bit-css-clr-action-active-secondary-main";
                    public const string Dark = "bit-css-clr-action-active-secondary-dark";
                    public const string Light = "bit-css-clr-action-active-secondary-light";
                }

                public static class Foreground
                {
                    public const string Primary = "bit-css-clr-action-active-foreground-primary";
                    public const string Secondary = "bit-css-clr-action-active-foreground-secondary";
                }

                public static class Background
                {
                    public const string Primary = "bit-css-clr-action-active-background-primary";
                    public const string Secondary = "bit-css-clr-action-active-background-secondary";
                }

                public static class Border
                {
                    public const string Primary = "bit-css-clr-action-active-border-primary";
                    public const string Secondary = "bit-css-clr-action-active-border-secondary";
                }
            }
        }

        public static class State
        {
            public const string Info = "bit-css-clr-sta-info";
            public const string InfoBg = "bit-css-clr-sta-info-bg";

            public const string Success = "bit-css-clr-sta-success";
            public const string SuccessBg = "bit-css-clr-sta-success-bg";

            public const string Warning = "bit-css-clr-sta-warning";
            public const string WarningBg = "bit-css-clr-sta-warning-bg";

            public const string SevereWarning = "bit-css-clr-sta-severe-warning";
            public const string SevereWarningBg = "bit-css-clr-sta-severe-warning-bg";

            public const string Error = "bit-css-clr-sta-error";
            public const string ErrorBg = "bit-css-clr-sta-error-bg";
        }
    }
}
