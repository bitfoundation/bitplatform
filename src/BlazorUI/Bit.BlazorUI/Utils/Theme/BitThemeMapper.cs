namespace Bit.BlazorUI;

internal static class BitThemeMapper
{
    internal static Dictionary<string, string> MapToCssVariables(BitTheme bitTheme)
    {
        var result = new Dictionary<string, string>();

        if (bitTheme is null) return result;

        addCssVar("--bit-clr-pri", bitTheme.Color.Primary.Main);
        addCssVar("--bit-clr-pri-dark", bitTheme.Color.Primary.Dark);
        addCssVar("--bit-clr-pri-light", bitTheme.Color.Primary.Light);
        addCssVar("--bit-clr-pri-text", bitTheme.Color.Primary.Text);

        addCssVar("--bit-clr-sec", bitTheme.Color.Secondary.Main);
        addCssVar("--bit-clr-sec-dark", bitTheme.Color.Secondary.Dark);
        addCssVar("--bit-clr-sec-light", bitTheme.Color.Secondary.Light);
        addCssVar("--bit-clr-sec-text", bitTheme.Color.Secondary.Text);

        addCssVar("--bit-clr-fg-pri", bitTheme.Color.Foreground.Primary);
        addCssVar("--bit-clr-fg-sec", bitTheme.Color.Foreground.Secondary);
        addCssVar("--bit-clr-fg-dis", bitTheme.Color.Foreground.Disabled);

        addCssVar("--bit-clr-bg-pri", bitTheme.Color.Background.Primary);
        addCssVar("--bit-clr-bg-sec", bitTheme.Color.Background.Secondary);
        addCssVar("--bit-clr-bg-dis", bitTheme.Color.Background.Disabled);
        addCssVar("--bit-clr-bg-overlay", bitTheme.Color.Background.Overlay);

        addCssVar("--bit-clr-brd-pri", bitTheme.Color.Border.Primary);
        addCssVar("--bit-clr-brd-sec", bitTheme.Color.Border.Secondary);
        addCssVar("--bit-clr-brd-dis", bitTheme.Color.Border.Disabled);

        addCssVar("--bit-clr-pri-hover", bitTheme.Color.Action.Hover.Primary);
        addCssVar("--bit-clr-pri-dark-hover", bitTheme.Color.Action.Hover.PrimaryDark);
        addCssVar("--bit-clr-pri-light-hover", bitTheme.Color.Action.Hover.PrimaryLight);
        addCssVar("--bit-clr-sec-hover", bitTheme.Color.Action.Hover.Secondary);
        addCssVar("--bit-clr-sec-dark-hover", bitTheme.Color.Action.Hover.SecondaryDark);
        addCssVar("--bit-clr-sec-light-hover", bitTheme.Color.Action.Hover.SecondaryLight);

        addCssVar("--bit-clr-pri-active", bitTheme.Color.Action.Active.Primary);
        addCssVar("--bit-clr-pri-dark-active", bitTheme.Color.Action.Active.PrimaryDark);
        addCssVar("--bit-clr-pri-light-active", bitTheme.Color.Action.Active.PrimaryLight);
        addCssVar("--bit-clr-sec-active", bitTheme.Color.Action.Active.Secondary);
        addCssVar("--bit-clr-sec-dark-active", bitTheme.Color.Action.Active.SecondaryDark);
        addCssVar("--bit-clr-sec-light-active", bitTheme.Color.Action.Active.SecondaryLight);

        addCssVar("--bit-clr-fg-pri-hover", bitTheme.Color.Action.Hover.Foreground.Primary!);
        addCssVar("--bit-clr-fg-sec-hover", bitTheme.Color.Action.Hover.Foreground.Secondary!);
        addCssVar("--bit-clr-fg-pri-active", bitTheme.Color.Action.Active.Foreground.Primary!);
        addCssVar("--bit-clr-fg-sec-active", bitTheme.Color.Action.Active.Foreground.Secondary!);

        addCssVar("--bit-clr-bg-pri-hover", bitTheme.Color.Action.Hover.Background.Primary!);
        addCssVar("--bit-clr-bg-sec-hover", bitTheme.Color.Action.Hover.Background.Secondary!);
        addCssVar("--bit-clr-bg-pri-active", bitTheme.Color.Action.Active.Background.Primary!);
        addCssVar("--bit-clr-bg-sec-active", bitTheme.Color.Action.Active.Background.Secondary!);

        addCssVar("--bit-clr-brd-pri-hover", bitTheme.Color.Action.Hover.Border.Primary!);
        addCssVar("--bit-clr-brd-sec-hover", bitTheme.Color.Action.Hover.Border.Secondary!);
        addCssVar("--bit-clr-brd-pri-active", bitTheme.Color.Action.Active.Border.Primary!);
        addCssVar("--bit-clr-brd-sec-active", bitTheme.Color.Action.Active.Border.Secondary!);

        addCssVar("--bit-clr-inf", bitTheme.Color.State.Info);
        addCssVar("--bit-clr-inf-bg", bitTheme.Color.State.InfoBackground);
        addCssVar("--bit-clr-suc", bitTheme.Color.State.Success);
        addCssVar("--bit-clr-suc-bg", bitTheme.Color.State.SuccessBackground);
        addCssVar("--bit-clr-wrn", bitTheme.Color.State.Warning);
        addCssVar("--bit-clr-wrn-bg", bitTheme.Color.State.WarningBackground);
        addCssVar("--bit-clr-swr", bitTheme.Color.State.SevereWarning);
        addCssVar("--bit-clr-swr-bg", bitTheme.Color.State.SevereWarningBackground);
        addCssVar("--bit-clr-err", bitTheme.Color.State.Error);
        addCssVar("--bit-clr-err-bg", bitTheme.Color.State.ErrorBackground);
        addCssVar("--bit-clr-req", bitTheme.Color.State.Required);

        addCssVar("--bit-clr-ntr-white", bitTheme.Color.Neutral.White);
        addCssVar("--bit-clr-ntr-black", bitTheme.Color.Neutral.Black);
        addCssVar("--bit-clr-ntr-gray10", bitTheme.Color.Neutral.Gray10);
        addCssVar("--bit-clr-ntr-gray20", bitTheme.Color.Neutral.Gray20);
        addCssVar("--bit-clr-ntr-gray30", bitTheme.Color.Neutral.Gray30);
        addCssVar("--bit-clr-ntr-gray40", bitTheme.Color.Neutral.Gray40);
        addCssVar("--bit-clr-ntr-gray50", bitTheme.Color.Neutral.Gray50);
        addCssVar("--bit-clr-ntr-gray60", bitTheme.Color.Neutral.Gray60);
        addCssVar("--bit-clr-ntr-gray70", bitTheme.Color.Neutral.Gray70);
        addCssVar("--bit-clr-ntr-gray80", bitTheme.Color.Neutral.Gray80);
        addCssVar("--bit-clr-ntr-gray90", bitTheme.Color.Neutral.Gray90);
        addCssVar("--bit-clr-ntr-gray100", bitTheme.Color.Neutral.Gray100);
        addCssVar("--bit-clr-ntr-gray110", bitTheme.Color.Neutral.Gray110);
        addCssVar("--bit-clr-ntr-gray120", bitTheme.Color.Neutral.Gray120);
        addCssVar("--bit-clr-ntr-gray130", bitTheme.Color.Neutral.Gray130);
        addCssVar("--bit-clr-ntr-gray140", bitTheme.Color.Neutral.Gray140);
        addCssVar("--bit-clr-ntr-gray150", bitTheme.Color.Neutral.Gray150);
        addCssVar("--bit-clr-ntr-gray160", bitTheme.Color.Neutral.Gray160);
        addCssVar("--bit-clr-ntr-gray170", bitTheme.Color.Neutral.Gray170);
        addCssVar("--bit-clr-ntr-gray180", bitTheme.Color.Neutral.Gray180);
        addCssVar("--bit-clr-ntr-gray190", bitTheme.Color.Neutral.Gray190);
        addCssVar("--bit-clr-ntr-gray200", bitTheme.Color.Neutral.Gray200);
        addCssVar("--bit-clr-ntr-gray210", bitTheme.Color.Neutral.Gray210);
        addCssVar("--bit-clr-ntr-gray220", bitTheme.Color.Neutral.Gray220);

        addCssVar("--bit-shd-cal", bitTheme.BoxShadow.Callout);
        addCssVar("--bit-shd-1", bitTheme.BoxShadow.S1);
        addCssVar("--bit-shd-2", bitTheme.BoxShadow.S2);
        addCssVar("--bit-shd-3", bitTheme.BoxShadow.S3);
        addCssVar("--bit-shd-4", bitTheme.BoxShadow.S4);
        addCssVar("--bit-shd-5", bitTheme.BoxShadow.S5);
        addCssVar("--bit-shd-6", bitTheme.BoxShadow.S6);
        addCssVar("--bit-shd-7", bitTheme.BoxShadow.S7);
        addCssVar("--bit-shd-8", bitTheme.BoxShadow.S8);
        addCssVar("--bit-shd-9", bitTheme.BoxShadow.S9);
        addCssVar("--bit-shd-10", bitTheme.BoxShadow.S10);
        addCssVar("--bit-shd-11", bitTheme.BoxShadow.S11);
        addCssVar("--bit-shd-12", bitTheme.BoxShadow.S12);
        addCssVar("--bit-shd-13", bitTheme.BoxShadow.S13);
        addCssVar("--bit-shd-14", bitTheme.BoxShadow.S14);
        addCssVar("--bit-shd-15", bitTheme.BoxShadow.S15);
        addCssVar("--bit-shd-16", bitTheme.BoxShadow.S16);
        addCssVar("--bit-shd-17", bitTheme.BoxShadow.S17);
        addCssVar("--bit-shd-18", bitTheme.BoxShadow.S18);
        addCssVar("--bit-shd-19", bitTheme.BoxShadow.S19);
        addCssVar("--bit-shd-20", bitTheme.BoxShadow.S20);
        addCssVar("--bit-shd-21", bitTheme.BoxShadow.S21);
        addCssVar("--bit-shd-22", bitTheme.BoxShadow.S22);
        addCssVar("--bit-shd-23", bitTheme.BoxShadow.S23);
        addCssVar("--bit-shd-24", bitTheme.BoxShadow.S24);

        addCssVar("--bit-spa-scaling-factor", bitTheme.Spacing.ScalingFactor);

        addCssVar("--bit-zin-snackbar", bitTheme.ZIndex.Snackbar);
        addCssVar("--bit-zin-modal", bitTheme.ZIndex.Modal);
        addCssVar("--bit-zin-callout", bitTheme.ZIndex.Callout);
        addCssVar("--bit-zin-overlay", bitTheme.ZIndex.Overlay);

        addCssVar("--bit-shp-brd-radius", bitTheme.Shape.BorderRadius);
        addCssVar("--bit-shp-brd-width", bitTheme.Shape.BorderWidth);
        addCssVar("--bit-shp-brd-style", bitTheme.Shape.BorderStyle);

        addCssVar("--bit-tpg-font-family", bitTheme.Typography.FontFamily);
        addCssVar("--bit-tpg-font-weight", bitTheme.Typography.FontWeight);
        addCssVar("--bit-tpg-line-height", bitTheme.Typography.LineHeight);
        addCssVar("--bit-tpg-gutter-size", bitTheme.Typography.GutterSize);

        addCssVar("--bit-tpg-body1-margin", bitTheme.Typography.Body1.Margin);
        addCssVar("--bit-tpg-body1-font-weight", bitTheme.Typography.Body1.FontWeight);
        addCssVar("--bit-tpg-body1-font-size", bitTheme.Typography.Body1.FontSize);
        addCssVar("--bit-tpg-body1-line-height", bitTheme.Typography.Body1.LineHeight);
        addCssVar("--bit-tpg-body1-letter-spacing", bitTheme.Typography.Body1.LetterSpacing);

        addCssVar("--bit-tpg-body2-margin", bitTheme.Typography.Body2.Margin);
        addCssVar("--bit-tpg-body2-font-weight", bitTheme.Typography.Body2.FontWeight);
        addCssVar("--bit-tpg-body2-font-size", bitTheme.Typography.Body2.FontSize);
        addCssVar("--bit-tpg-body2-line-height", bitTheme.Typography.Body2.LineHeight);
        addCssVar("--bit-tpg-body2-letter-spacing", bitTheme.Typography.Body2.LetterSpacing);

        addCssVar("--bit-tpg-button-margin", bitTheme.Typography.Button.Margin);
        addCssVar("--bit-tpg-button-font-weight", bitTheme.Typography.Button.FontWeight);
        addCssVar("--bit-tpg-button-font-size", bitTheme.Typography.Button.FontSize);
        addCssVar("--bit-tpg-button-line-height", bitTheme.Typography.Button.LineHeight);
        addCssVar("--bit-tpg-button-letter-spacing", bitTheme.Typography.Button.LetterSpacing);
        addCssVar("--bit-tpg-button-text-transform", bitTheme.Typography.Button.TextTransform);
        addCssVar("--bit-tpg-button-display", bitTheme.Typography.Button.Display);

        addCssVar("--bit-tpg-caption-margin", bitTheme.Typography.Caption.Margin);
        addCssVar("--bit-tpg-caption-font-weight", bitTheme.Typography.Caption.FontWeight);
        addCssVar("--bit-tpg-caption-font-size", bitTheme.Typography.Caption.FontSize);
        addCssVar("--bit-tpg-caption-line-height", bitTheme.Typography.Caption.LineHeight);
        addCssVar("--bit-tpg-caption-letter-spacing", bitTheme.Typography.Caption.LetterSpacing);

        addCssVar("--bit-tpg-h1-margin", bitTheme.Typography.H1.Margin);
        addCssVar("--bit-tpg-h1-font-weight", bitTheme.Typography.H1.FontWeight);
        addCssVar("--bit-tpg-h1-font-size", bitTheme.Typography.H1.FontSize);
        addCssVar("--bit-tpg-h1-line-height", bitTheme.Typography.H1.LineHeight);
        addCssVar("--bit-tpg-h1-letter-spacing", bitTheme.Typography.H1.LetterSpacing);

        addCssVar("--bit-tpg-h2-margin", bitTheme.Typography.H2.Margin);
        addCssVar("--bit-tpg-h2-font-weight", bitTheme.Typography.H2.FontWeight);
        addCssVar("--bit-tpg-h2-font-size", bitTheme.Typography.H2.FontSize);
        addCssVar("--bit-tpg-h2-line-height", bitTheme.Typography.H2.LineHeight);
        addCssVar("--bit-tpg-h2-letter-spacing", bitTheme.Typography.H2.LetterSpacing);

        addCssVar("--bit-tpg-h3-margin", bitTheme.Typography.H3.Margin);
        addCssVar("--bit-tpg-h3-font-weight", bitTheme.Typography.H3.FontWeight);
        addCssVar("--bit-tpg-h3-font-size", bitTheme.Typography.H3.FontSize);
        addCssVar("--bit-tpg-h3-line-height", bitTheme.Typography.H3.LineHeight);
        addCssVar("--bit-tpg-h3-letter-spacing", bitTheme.Typography.H3.LetterSpacing);

        addCssVar("--bit-tpg-h3-margin", bitTheme.Typography.H3.Margin);
        addCssVar("--bit-tpg-h3-font-weight", bitTheme.Typography.H3.FontWeight);
        addCssVar("--bit-tpg-h3-font-size", bitTheme.Typography.H3.FontSize);
        addCssVar("--bit-tpg-h3-line-height", bitTheme.Typography.H3.LineHeight);
        addCssVar("--bit-tpg-h3-letter-spacing", bitTheme.Typography.H3.LetterSpacing);

        addCssVar("--bit-tpg-h4-margin", bitTheme.Typography.H4.Margin);
        addCssVar("--bit-tpg-h4-font-weight", bitTheme.Typography.H4.FontWeight);
        addCssVar("--bit-tpg-h4-font-size", bitTheme.Typography.H4.FontSize);
        addCssVar("--bit-tpg-h4-line-height", bitTheme.Typography.H4.LineHeight);
        addCssVar("--bit-tpg-h4-letter-spacing", bitTheme.Typography.H4.LetterSpacing);

        addCssVar("--bit-tpg-h5-margin", bitTheme.Typography.H5.Margin);
        addCssVar("--bit-tpg-h5-font-weight", bitTheme.Typography.H5.FontWeight);
        addCssVar("--bit-tpg-h5-font-size", bitTheme.Typography.H5.FontSize);
        addCssVar("--bit-tpg-h5-line-height", bitTheme.Typography.H5.LineHeight);
        addCssVar("--bit-tpg-h5-letter-spacing", bitTheme.Typography.H5.LetterSpacing);

        addCssVar("--bit-tpg-h6-margin", bitTheme.Typography.H6.Margin);
        addCssVar("--bit-tpg-h6-font-weight", bitTheme.Typography.H6.FontWeight);
        addCssVar("--bit-tpg-h6-font-size", bitTheme.Typography.H6.FontSize);
        addCssVar("--bit-tpg-h6-line-height", bitTheme.Typography.H6.LineHeight);
        addCssVar("--bit-tpg-h6-letter-spacing", bitTheme.Typography.H6.LetterSpacing);

        addCssVar("--bit-tpg-inherit-margin", bitTheme.Typography.Inherit.Margin);
        addCssVar("--bit-tpg-inherit-font-family", bitTheme.Typography.Inherit.FontFamily);
        addCssVar("--bit-tpg-inherit-font-weight", bitTheme.Typography.Inherit.FontWeight);
        addCssVar("--bit-tpg-inherit-font-size", bitTheme.Typography.Inherit.FontSize);
        addCssVar("--bit-tpg-inherit-line-height", bitTheme.Typography.Inherit.LineHeight);
        addCssVar("--bit-tpg-inherit-letter-spacing", bitTheme.Typography.Inherit.LetterSpacing);

        addCssVar("--bit-tpg-overline-margin", bitTheme.Typography.Overline.Margin);
        addCssVar("--bit-tpg-overline-font-weight", bitTheme.Typography.Overline.FontWeight);
        addCssVar("--bit-tpg-overline-font-size", bitTheme.Typography.Overline.FontSize);
        addCssVar("--bit-tpg-overline-line-height", bitTheme.Typography.Overline.LineHeight);
        addCssVar("--bit-tpg-overline-letter-spacing", bitTheme.Typography.Overline.LetterSpacing);
        addCssVar("--bit-tpg-overline-text-transform", bitTheme.Typography.Overline.TextTransform);
        addCssVar("--bit-tpg-overline-display", bitTheme.Typography.Overline.Display);

        addCssVar("--bit-tpg-subtitle1-margin", bitTheme.Typography.Subtitle1.Margin);
        addCssVar("--bit-tpg-subtitle1-font-weight", bitTheme.Typography.Subtitle1.FontWeight);
        addCssVar("--bit-tpg-subtitle1-font-size", bitTheme.Typography.Subtitle1.FontSize);
        addCssVar("--bit-tpg-subtitle1-line-height", bitTheme.Typography.Subtitle1.LineHeight);
        addCssVar("--bit-tpg-subtitle1-letter-spacing", bitTheme.Typography.Subtitle1.LetterSpacing);

        addCssVar("--bit-tpg-subtitle2-margin", bitTheme.Typography.Subtitle2.Margin);
        addCssVar("--bit-tpg-subtitle2-font-weight", bitTheme.Typography.Subtitle2.FontWeight);
        addCssVar("--bit-tpg-subtitle2-font-size", bitTheme.Typography.Subtitle2.FontSize);
        addCssVar("--bit-tpg-subtitle2-line-height", bitTheme.Typography.Subtitle2.LineHeight);
        addCssVar("--bit-tpg-subtitle2-letter-spacing", bitTheme.Typography.Subtitle2.LetterSpacing);

        return result;

        void addCssVar(string key, string? value) { if (value is not null) result!.Add(key, value); }
    }

    internal static BitTheme Merge(BitTheme bitTheme, BitTheme other)
    {
        var result = new BitTheme();

        result.Color.Primary.Main = bitTheme.Color.Primary.Main ?? other.Color.Primary.Main;
        result.Color.Primary.Dark = bitTheme.Color.Primary.Dark ?? other.Color.Primary.Dark;
        result.Color.Primary.Light = bitTheme.Color.Primary.Light ?? other.Color.Primary.Light;
        result.Color.Primary.Text = bitTheme.Color.Primary.Text ?? other.Color.Primary.Text;

        result.Color.Secondary.Main = bitTheme.Color.Secondary.Main ?? other.Color.Secondary.Main;
        result.Color.Secondary.Dark = bitTheme.Color.Secondary.Dark ?? other.Color.Secondary.Dark;
        result.Color.Secondary.Light = bitTheme.Color.Secondary.Light ?? other.Color.Secondary.Light;
        result.Color.Secondary.Text = bitTheme.Color.Secondary.Text ?? other.Color.Secondary.Text;

        result.Color.Foreground.Primary = bitTheme.Color.Foreground.Primary ?? other.Color.Foreground.Primary;
        result.Color.Foreground.Secondary = bitTheme.Color.Foreground.Secondary ?? other.Color.Foreground.Secondary;
        result.Color.Foreground.Disabled = bitTheme.Color.Foreground.Disabled ?? other.Color.Foreground.Disabled;

        result.Color.Background.Primary = bitTheme.Color.Background.Primary ?? other.Color.Background.Primary;
        result.Color.Background.Secondary = bitTheme.Color.Background.Secondary ?? other.Color.Background.Secondary;
        result.Color.Background.Disabled = bitTheme.Color.Background.Disabled ?? other.Color.Background.Disabled;
        result.Color.Background.Overlay = bitTheme.Color.Background.Overlay ?? other.Color.Background.Overlay;

        result.Color.Border.Primary = bitTheme.Color.Border.Primary ?? other.Color.Border.Primary;
        result.Color.Border.Secondary = bitTheme.Color.Border.Secondary ?? other.Color.Border.Secondary;
        result.Color.Border.Disabled = bitTheme.Color.Border.Disabled ?? other.Color.Border.Disabled;

        result.Color.Action.Hover.Primary = bitTheme.Color.Action.Hover.Primary ?? other.Color.Action.Hover.Primary;
        result.Color.Action.Hover.PrimaryDark = bitTheme.Color.Action.Hover.PrimaryDark ?? other.Color.Action.Hover.PrimaryDark;
        result.Color.Action.Hover.PrimaryLight = bitTheme.Color.Action.Hover.PrimaryLight ?? other.Color.Action.Hover.PrimaryLight;
        result.Color.Action.Hover.Secondary = bitTheme.Color.Action.Hover.Secondary ?? other.Color.Action.Hover.Secondary;
        result.Color.Action.Hover.SecondaryDark = bitTheme.Color.Action.Hover.SecondaryDark ?? other.Color.Action.Hover.SecondaryDark;
        result.Color.Action.Hover.SecondaryLight = bitTheme.Color.Action.Hover.SecondaryLight ?? other.Color.Action.Hover.SecondaryLight;

        result.Color.Action.Active.Primary = bitTheme.Color.Action.Active.Primary ?? other.Color.Action.Active.Primary;
        result.Color.Action.Active.PrimaryDark = bitTheme.Color.Action.Active.PrimaryDark ?? other.Color.Action.Active.PrimaryDark;
        result.Color.Action.Active.PrimaryLight = bitTheme.Color.Action.Active.PrimaryLight ?? other.Color.Action.Active.PrimaryLight;
        result.Color.Action.Active.Secondary = bitTheme.Color.Action.Active.Secondary ?? other.Color.Action.Active.Secondary;
        result.Color.Action.Active.SecondaryDark = bitTheme.Color.Action.Active.SecondaryDark ?? other.Color.Action.Active.SecondaryDark;
        result.Color.Action.Active.SecondaryLight = bitTheme.Color.Action.Active.SecondaryLight ?? other.Color.Action.Active.SecondaryLight;

        result.Color.Action.Hover.Foreground.Primary = bitTheme.Color.Action.Hover.Foreground.Primary ?? other.Color.Action.Hover.Foreground.Primary;
        result.Color.Action.Hover.Foreground.Secondary = bitTheme.Color.Action.Hover.Foreground.Secondary ?? other.Color.Action.Hover.Foreground.Secondary;
        result.Color.Action.Active.Foreground.Primary = bitTheme.Color.Action.Active.Foreground.Primary ?? other.Color.Action.Active.Foreground.Primary;
        result.Color.Action.Active.Foreground.Secondary = bitTheme.Color.Action.Active.Foreground.Secondary ?? other.Color.Action.Active.Foreground.Secondary;

        result.Color.Action.Hover.Background.Primary = bitTheme.Color.Action.Hover.Background.Primary ?? other.Color.Action.Hover.Background.Primary;
        result.Color.Action.Hover.Background.Secondary = bitTheme.Color.Action.Hover.Background.Secondary ?? other.Color.Action.Hover.Background.Secondary;
        result.Color.Action.Active.Background.Primary = bitTheme.Color.Action.Active.Background.Primary ?? other.Color.Action.Active.Background.Primary;
        result.Color.Action.Active.Background.Secondary = bitTheme.Color.Action.Active.Background.Secondary ?? other.Color.Action.Active.Background.Secondary;

        result.Color.Action.Hover.Border.Primary = bitTheme.Color.Action.Hover.Border.Primary ?? other.Color.Action.Hover.Border.Primary;
        result.Color.Action.Hover.Border.Secondary = bitTheme.Color.Action.Hover.Border.Secondary ?? other.Color.Action.Hover.Border.Secondary;
        result.Color.Action.Active.Border.Primary = bitTheme.Color.Action.Active.Border.Primary ?? other.Color.Action.Active.Border.Primary;
        result.Color.Action.Active.Border.Secondary = bitTheme.Color.Action.Active.Border.Secondary ?? other.Color.Action.Active.Border.Secondary;

        result.Color.State.Info = bitTheme.Color.State.Info ?? other.Color.State.Info;
        result.Color.State.InfoBackground = bitTheme.Color.State.InfoBackground ?? other.Color.State.InfoBackground;
        result.Color.State.Success = bitTheme.Color.State.Success ?? other.Color.State.Success;
        result.Color.State.SuccessBackground = bitTheme.Color.State.SuccessBackground ?? other.Color.State.SuccessBackground;
        result.Color.State.Warning = bitTheme.Color.State.Warning ?? other.Color.State.Warning;
        result.Color.State.WarningBackground = bitTheme.Color.State.WarningBackground ?? other.Color.State.WarningBackground;
        result.Color.State.SevereWarning = bitTheme.Color.State.SevereWarning ?? other.Color.State.SevereWarning;
        result.Color.State.SevereWarningBackground = bitTheme.Color.State.SevereWarningBackground ?? other.Color.State.SevereWarningBackground;
        result.Color.State.Error = bitTheme.Color.State.Error ?? other.Color.State.Error;
        result.Color.State.ErrorBackground = bitTheme.Color.State.ErrorBackground ?? other.Color.State.ErrorBackground;

        result.Color.Neutral.White = bitTheme.Color.Neutral.White ?? other.Color.Neutral.White;
        result.Color.Neutral.Black = bitTheme.Color.Neutral.Black ?? other.Color.Neutral.Black;
        result.Color.Neutral.Gray10 = bitTheme.Color.Neutral.Gray10 ?? other.Color.Neutral.Gray10;
        result.Color.Neutral.Gray20 = bitTheme.Color.Neutral.Gray20 ?? other.Color.Neutral.Gray20;
        result.Color.Neutral.Gray30 = bitTheme.Color.Neutral.Gray30 ?? other.Color.Neutral.Gray30;
        result.Color.Neutral.Gray40 = bitTheme.Color.Neutral.Gray40 ?? other.Color.Neutral.Gray40;
        result.Color.Neutral.Gray50 = bitTheme.Color.Neutral.Gray50 ?? other.Color.Neutral.Gray50;
        result.Color.Neutral.Gray60 = bitTheme.Color.Neutral.Gray60 ?? other.Color.Neutral.Gray60;
        result.Color.Neutral.Gray70 = bitTheme.Color.Neutral.Gray70 ?? other.Color.Neutral.Gray70;
        result.Color.Neutral.Gray80 = bitTheme.Color.Neutral.Gray80 ?? other.Color.Neutral.Gray80;
        result.Color.Neutral.Gray90 = bitTheme.Color.Neutral.Gray90 ?? other.Color.Neutral.Gray90;
        result.Color.Neutral.Gray100 = bitTheme.Color.Neutral.Gray100 ?? other.Color.Neutral.Gray100;
        result.Color.Neutral.Gray110 = bitTheme.Color.Neutral.Gray110 ?? other.Color.Neutral.Gray110;
        result.Color.Neutral.Gray120 = bitTheme.Color.Neutral.Gray120 ?? other.Color.Neutral.Gray120;
        result.Color.Neutral.Gray130 = bitTheme.Color.Neutral.Gray130 ?? other.Color.Neutral.Gray130;
        result.Color.Neutral.Gray140 = bitTheme.Color.Neutral.Gray140 ?? other.Color.Neutral.Gray140;
        result.Color.Neutral.Gray150 = bitTheme.Color.Neutral.Gray150 ?? other.Color.Neutral.Gray150;
        result.Color.Neutral.Gray160 = bitTheme.Color.Neutral.Gray160 ?? other.Color.Neutral.Gray160;
        result.Color.Neutral.Gray170 = bitTheme.Color.Neutral.Gray170 ?? other.Color.Neutral.Gray170;
        result.Color.Neutral.Gray180 = bitTheme.Color.Neutral.Gray180 ?? other.Color.Neutral.Gray180;
        result.Color.Neutral.Gray190 = bitTheme.Color.Neutral.Gray190 ?? other.Color.Neutral.Gray190;
        result.Color.Neutral.Gray200 = bitTheme.Color.Neutral.Gray200 ?? other.Color.Neutral.Gray200;
        result.Color.Neutral.Gray210 = bitTheme.Color.Neutral.Gray210 ?? other.Color.Neutral.Gray210;
        result.Color.Neutral.Gray220 = bitTheme.Color.Neutral.Gray220 ?? other.Color.Neutral.Gray220;

        result.BoxShadow.Callout = bitTheme.BoxShadow.Callout ?? other.BoxShadow.Callout;
        result.BoxShadow.S1 = bitTheme.BoxShadow.S1 ?? other.BoxShadow.S1;
        result.BoxShadow.S2 = bitTheme.BoxShadow.S2 ?? other.BoxShadow.S2;
        result.BoxShadow.S3 = bitTheme.BoxShadow.S3 ?? other.BoxShadow.S3;
        result.BoxShadow.S4 = bitTheme.BoxShadow.S4 ?? other.BoxShadow.S4;
        result.BoxShadow.S5 = bitTheme.BoxShadow.S5 ?? other.BoxShadow.S5;
        result.BoxShadow.S6 = bitTheme.BoxShadow.S6 ?? other.BoxShadow.S6;
        result.BoxShadow.S7 = bitTheme.BoxShadow.S7 ?? other.BoxShadow.S7;
        result.BoxShadow.S8 = bitTheme.BoxShadow.S8 ?? other.BoxShadow.S8;
        result.BoxShadow.S9 = bitTheme.BoxShadow.S9 ?? other.BoxShadow.S9;
        result.BoxShadow.S10 = bitTheme.BoxShadow.S10 ?? other.BoxShadow.S10;
        result.BoxShadow.S11 = bitTheme.BoxShadow.S11 ?? other.BoxShadow.S11;
        result.BoxShadow.S12 = bitTheme.BoxShadow.S12 ?? other.BoxShadow.S12;
        result.BoxShadow.S13 = bitTheme.BoxShadow.S13 ?? other.BoxShadow.S13;
        result.BoxShadow.S14 = bitTheme.BoxShadow.S14 ?? other.BoxShadow.S14;
        result.BoxShadow.S15 = bitTheme.BoxShadow.S15 ?? other.BoxShadow.S15;
        result.BoxShadow.S16 = bitTheme.BoxShadow.S16 ?? other.BoxShadow.S16;
        result.BoxShadow.S17 = bitTheme.BoxShadow.S17 ?? other.BoxShadow.S17;
        result.BoxShadow.S18 = bitTheme.BoxShadow.S18 ?? other.BoxShadow.S18;
        result.BoxShadow.S19 = bitTheme.BoxShadow.S19 ?? other.BoxShadow.S19;
        result.BoxShadow.S20 = bitTheme.BoxShadow.S20 ?? other.BoxShadow.S20;
        result.BoxShadow.S21 = bitTheme.BoxShadow.S21 ?? other.BoxShadow.S21;
        result.BoxShadow.S22 = bitTheme.BoxShadow.S22 ?? other.BoxShadow.S22;
        result.BoxShadow.S23 = bitTheme.BoxShadow.S23 ?? other.BoxShadow.S23;
        result.BoxShadow.S24 = bitTheme.BoxShadow.S24 ?? other.BoxShadow.S24;

        result.Spacing.ScalingFactor = bitTheme.Spacing.ScalingFactor ?? other.Spacing.ScalingFactor;

        result.ZIndex.Snackbar = bitTheme.ZIndex.Snackbar ?? other.ZIndex.Snackbar;
        result.ZIndex.Modal = bitTheme.ZIndex.Modal ?? other.ZIndex.Modal;
        result.ZIndex.Callout = bitTheme.ZIndex.Callout ?? other.ZIndex.Callout;
        result.ZIndex.Overlay = bitTheme.ZIndex.Overlay ?? other.ZIndex.Overlay;

        result.Shape.BorderRadius = bitTheme.Shape.BorderRadius ?? other.Shape.BorderRadius;
        result.Shape.BorderWidth = bitTheme.Shape.BorderWidth ?? other.Shape.BorderWidth;
        result.Shape.BorderStyle = bitTheme.Shape.BorderStyle ?? other.Shape.BorderStyle;

        result.Typography.FontFamily = bitTheme.Typography.FontFamily ?? other.Typography.FontFamily;
        result.Typography.FontWeight = bitTheme.Typography.FontWeight ?? other.Typography.FontWeight;
        result.Typography.LineHeight = bitTheme.Typography.LineHeight ?? other.Typography.LineHeight;
        result.Typography.GutterSize = bitTheme.Typography.GutterSize ?? other.Typography.GutterSize;

        result.Typography.Body1.Margin = bitTheme.Typography.Body1.Margin ?? other.Typography.Body1.Margin;
        result.Typography.Body1.FontWeight = bitTheme.Typography.Body1.FontWeight ?? other.Typography.Body1.FontWeight;
        result.Typography.Body1.FontSize = bitTheme.Typography.Body1.FontSize ?? other.Typography.Body1.FontSize;
        result.Typography.Body1.LineHeight = bitTheme.Typography.Body1.LineHeight ?? other.Typography.Body1.LineHeight;
        result.Typography.Body1.LetterSpacing = bitTheme.Typography.Body1.LetterSpacing ?? other.Typography.Body1.LetterSpacing;

        result.Typography.Body2.Margin = bitTheme.Typography.Body2.Margin ?? other.Typography.Body2.Margin;
        result.Typography.Body2.FontWeight = bitTheme.Typography.Body2.FontWeight ?? other.Typography.Body2.FontWeight;
        result.Typography.Body2.FontSize = bitTheme.Typography.Body2.FontSize ?? other.Typography.Body2.FontSize;
        result.Typography.Body2.LineHeight = bitTheme.Typography.Body2.LineHeight ?? other.Typography.Body2.LineHeight;
        result.Typography.Body2.LetterSpacing = bitTheme.Typography.Body2.LetterSpacing ?? other.Typography.Body2.LetterSpacing;

        result.Typography.Button.Margin = bitTheme.Typography.Button.Margin ?? other.Typography.Button.Margin;
        result.Typography.Button.FontWeight = bitTheme.Typography.Button.FontWeight ?? other.Typography.Button.FontWeight;
        result.Typography.Button.FontSize = bitTheme.Typography.Button.FontSize ?? other.Typography.Button.FontSize;
        result.Typography.Button.LineHeight = bitTheme.Typography.Button.LineHeight ?? other.Typography.Button.LineHeight;
        result.Typography.Button.LetterSpacing = bitTheme.Typography.Button.LetterSpacing ?? other.Typography.Button.LetterSpacing;
        result.Typography.Button.TextTransform = bitTheme.Typography.Button.TextTransform ?? other.Typography.Button.TextTransform;
        result.Typography.Button.Display = bitTheme.Typography.Button.Display ?? other.Typography.Button.Display;

        result.Typography.Caption.Margin = bitTheme.Typography.Caption.Margin ?? other.Typography.Caption.Margin;
        result.Typography.Caption.FontWeight = bitTheme.Typography.Caption.FontWeight ?? other.Typography.Caption.FontWeight;
        result.Typography.Caption.FontSize = bitTheme.Typography.Caption.FontSize ?? other.Typography.Caption.FontSize;
        result.Typography.Caption.LineHeight = bitTheme.Typography.Caption.LineHeight ?? other.Typography.Caption.LineHeight;
        result.Typography.Caption.LetterSpacing = bitTheme.Typography.Caption.LetterSpacing ?? other.Typography.Caption.LetterSpacing;

        result.Typography.H1.Margin = bitTheme.Typography.H1.Margin ?? other.Typography.H1.Margin;
        result.Typography.H1.FontWeight = bitTheme.Typography.H1.FontWeight ?? other.Typography.H1.FontWeight;
        result.Typography.H1.FontSize = bitTheme.Typography.H1.FontSize ?? other.Typography.H1.FontSize;
        result.Typography.H1.LineHeight = bitTheme.Typography.H1.LineHeight ?? other.Typography.H1.LineHeight;
        result.Typography.H1.LetterSpacing = bitTheme.Typography.H1.LetterSpacing ?? other.Typography.H1.LetterSpacing;

        result.Typography.H2.Margin = bitTheme.Typography.H2.Margin ?? other.Typography.H2.Margin;
        result.Typography.H2.FontWeight = bitTheme.Typography.H2.FontWeight ?? other.Typography.H2.FontWeight;
        result.Typography.H2.FontSize = bitTheme.Typography.H2.FontSize ?? other.Typography.H2.FontSize;
        result.Typography.H2.LineHeight = bitTheme.Typography.H2.LineHeight ?? other.Typography.H2.LineHeight;
        result.Typography.H2.LetterSpacing = bitTheme.Typography.H2.LetterSpacing ?? other.Typography.H2.LetterSpacing;

        result.Typography.H3.Margin = bitTheme.Typography.H3.Margin ?? other.Typography.H3.Margin;
        result.Typography.H3.FontWeight = bitTheme.Typography.H3.FontWeight ?? other.Typography.H3.FontWeight;
        result.Typography.H3.FontSize = bitTheme.Typography.H3.FontSize ?? other.Typography.H3.FontSize;
        result.Typography.H3.LineHeight = bitTheme.Typography.H3.LineHeight ?? other.Typography.H3.LineHeight;
        result.Typography.H3.LetterSpacing = bitTheme.Typography.H3.LetterSpacing ?? other.Typography.H3.LetterSpacing;

        result.Typography.H4.Margin = bitTheme.Typography.H4.Margin ?? other.Typography.H4.Margin;
        result.Typography.H4.FontWeight = bitTheme.Typography.H4.FontWeight ?? other.Typography.H4.FontWeight;
        result.Typography.H4.FontSize = bitTheme.Typography.H4.FontSize ?? other.Typography.H4.FontSize;
        result.Typography.H4.LineHeight = bitTheme.Typography.H4.LineHeight ?? other.Typography.H4.LineHeight;
        result.Typography.H4.LetterSpacing = bitTheme.Typography.H4.LetterSpacing ?? other.Typography.H4.LetterSpacing;

        result.Typography.H5.Margin = bitTheme.Typography.H5.Margin ?? other.Typography.H5.Margin;
        result.Typography.H5.FontWeight = bitTheme.Typography.H5.FontWeight ?? other.Typography.H5.FontWeight;
        result.Typography.H5.FontSize = bitTheme.Typography.H5.FontSize ?? other.Typography.H5.FontSize;
        result.Typography.H5.LineHeight = bitTheme.Typography.H5.LineHeight ?? other.Typography.H5.LineHeight;
        result.Typography.H5.LetterSpacing = bitTheme.Typography.H5.LetterSpacing ?? other.Typography.H5.LetterSpacing;

        result.Typography.H6.Margin = bitTheme.Typography.H6.Margin ?? other.Typography.H6.Margin;
        result.Typography.H6.FontWeight = bitTheme.Typography.H6.FontWeight ?? other.Typography.H6.FontWeight;
        result.Typography.H6.FontSize = bitTheme.Typography.H6.FontSize ?? other.Typography.H6.FontSize;
        result.Typography.H6.LineHeight = bitTheme.Typography.H6.LineHeight ?? other.Typography.H6.LineHeight;
        result.Typography.H6.LetterSpacing = bitTheme.Typography.H6.LetterSpacing ?? other.Typography.H6.LetterSpacing;

        result.Typography.Inherit.Margin = bitTheme.Typography.Inherit.Margin ?? other.Typography.Inherit.Margin;
        result.Typography.Inherit.FontFamily = bitTheme.Typography.Inherit.FontFamily ?? other.Typography.Inherit.FontFamily;
        result.Typography.Inherit.FontWeight = bitTheme.Typography.Inherit.FontWeight ?? other.Typography.Inherit.FontWeight;
        result.Typography.Inherit.FontSize = bitTheme.Typography.Inherit.FontSize ?? other.Typography.Inherit.FontSize;
        result.Typography.Inherit.LineHeight = bitTheme.Typography.Inherit.LineHeight ?? other.Typography.Inherit.LineHeight;
        result.Typography.Inherit.LetterSpacing = bitTheme.Typography.Inherit.LetterSpacing ?? other.Typography.Inherit.LetterSpacing;

        result.Typography.Overline.Margin = bitTheme.Typography.Overline.Margin ?? other.Typography.Overline.Margin;
        result.Typography.Overline.FontWeight = bitTheme.Typography.Overline.FontWeight ?? other.Typography.Overline.FontWeight;
        result.Typography.Overline.FontSize = bitTheme.Typography.Overline.FontSize ?? other.Typography.Overline.FontSize;
        result.Typography.Overline.LineHeight = bitTheme.Typography.Overline.LineHeight ?? other.Typography.Overline.LineHeight;
        result.Typography.Overline.LetterSpacing = bitTheme.Typography.Overline.LetterSpacing ?? other.Typography.Overline.LetterSpacing;
        result.Typography.Overline.TextTransform = bitTheme.Typography.Overline.TextTransform ?? other.Typography.Overline.TextTransform;
        result.Typography.Overline.Display = bitTheme.Typography.Overline.Display ?? other.Typography.Overline.Display;

        result.Typography.Subtitle1.Margin = bitTheme.Typography.Subtitle1.Margin ?? other.Typography.Subtitle1.Margin;
        result.Typography.Subtitle1.FontWeight = bitTheme.Typography.Subtitle1.FontWeight ?? other.Typography.Subtitle1.FontWeight;
        result.Typography.Subtitle1.FontSize = bitTheme.Typography.Subtitle1.FontSize ?? other.Typography.Subtitle1.FontSize;
        result.Typography.Subtitle1.LineHeight = bitTheme.Typography.Subtitle1.LineHeight ?? other.Typography.Subtitle1.LineHeight;
        result.Typography.Subtitle1.LetterSpacing = bitTheme.Typography.Subtitle1.LetterSpacing ?? other.Typography.Subtitle1.LetterSpacing;

        result.Typography.Subtitle2.Margin = bitTheme.Typography.Subtitle2.Margin ?? other.Typography.Subtitle2.Margin;
        result.Typography.Subtitle2.FontWeight = bitTheme.Typography.Subtitle2.FontWeight ?? other.Typography.Subtitle2.FontWeight;
        result.Typography.Subtitle2.FontSize = bitTheme.Typography.Subtitle2.FontSize ?? other.Typography.Subtitle2.FontSize;
        result.Typography.Subtitle2.LineHeight = bitTheme.Typography.Subtitle2.LineHeight ?? other.Typography.Subtitle2.LineHeight;
        result.Typography.Subtitle2.LetterSpacing = bitTheme.Typography.Subtitle2.LetterSpacing ?? other.Typography.Subtitle2.LetterSpacing;

        return result;
    }
}
