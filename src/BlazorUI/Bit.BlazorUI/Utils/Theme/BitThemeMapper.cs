namespace Bit.BlazorUI;

internal static class BitThemeMapper
{
    internal static Dictionary<string, string> MapToCssVariables(BitTheme bitTheme)
    {
        var result = new Dictionary<string, string>();

        if (bitTheme is null) return result;

        addCssVar("--bit-clr-pri", bitTheme.Color.Primary.Main);
        addCssVar("--bit-clr-pri-hover", bitTheme.Color.Primary.MainHover);
        addCssVar("--bit-clr-pri-active", bitTheme.Color.Primary.MainActive);
        addCssVar("--bit-clr-pri-dark", bitTheme.Color.Primary.Dark);
        addCssVar("--bit-clr-pri-dark-hover", bitTheme.Color.Primary.DarkHover);
        addCssVar("--bit-clr-pri-dark-active", bitTheme.Color.Primary.DarkActive);
        addCssVar("--bit-clr-pri-light", bitTheme.Color.Primary.Light);
        addCssVar("--bit-clr-pri-light-hover", bitTheme.Color.Primary.LightHover);
        addCssVar("--bit-clr-pri-light-active", bitTheme.Color.Primary.LightActive);
        addCssVar("--bit-clr-pri-text", bitTheme.Color.Primary.Text);

        addCssVar("--bit-clr-sec", bitTheme.Color.Secondary.Main);
        addCssVar("--bit-clr-sec-hover", bitTheme.Color.Secondary.MainHover);
        addCssVar("--bit-clr-sec-active", bitTheme.Color.Secondary.MainActive);
        addCssVar("--bit-clr-sec-dark", bitTheme.Color.Secondary.Dark);
        addCssVar("--bit-clr-sec-dark-hover", bitTheme.Color.Secondary.DarkHover);
        addCssVar("--bit-clr-sec-dark-active", bitTheme.Color.Secondary.DarkActive);
        addCssVar("--bit-clr-sec-light", bitTheme.Color.Secondary.Light);
        addCssVar("--bit-clr-sec-light-hover", bitTheme.Color.Secondary.LightHover);
        addCssVar("--bit-clr-sec-light-active", bitTheme.Color.Secondary.LightActive);
        addCssVar("--bit-clr-sec-text", bitTheme.Color.Secondary.Text);

        addCssVar("--bit-clr-ter", bitTheme.Color.Tertiary.Main);
        addCssVar("--bit-clr-ter-hover", bitTheme.Color.Tertiary.MainHover);
        addCssVar("--bit-clr-ter-active", bitTheme.Color.Tertiary.MainActive);
        addCssVar("--bit-clr-ter-dark", bitTheme.Color.Tertiary.Dark);
        addCssVar("--bit-clr-ter-dark-hover", bitTheme.Color.Tertiary.DarkHover);
        addCssVar("--bit-clr-ter-dark-active", bitTheme.Color.Tertiary.DarkActive);
        addCssVar("--bit-clr-ter-light", bitTheme.Color.Tertiary.Light);
        addCssVar("--bit-clr-ter-light-hover", bitTheme.Color.Tertiary.LightHover);
        addCssVar("--bit-clr-ter-light-active", bitTheme.Color.Tertiary.LightActive);
        addCssVar("--bit-clr-ter-text", bitTheme.Color.Tertiary.Text);

        addCssVar("--bit-clr-inf", bitTheme.Color.Info.Main);
        addCssVar("--bit-clr-inf-hover", bitTheme.Color.Info.MainHover);
        addCssVar("--bit-clr-inf-active", bitTheme.Color.Info.MainActive);
        addCssVar("--bit-clr-inf-dark", bitTheme.Color.Info.Dark);
        addCssVar("--bit-clr-inf-dark-hover", bitTheme.Color.Info.DarkHover);
        addCssVar("--bit-clr-inf-dark-active", bitTheme.Color.Info.DarkActive);
        addCssVar("--bit-clr-inf-light", bitTheme.Color.Info.Light);
        addCssVar("--bit-clr-inf-light-hover", bitTheme.Color.Info.LightHover);
        addCssVar("--bit-clr-inf-light-active", bitTheme.Color.Info.LightActive);
        addCssVar("--bit-clr-inf-text", bitTheme.Color.Info.Text);

        addCssVar("--bit-clr-suc", bitTheme.Color.Success.Main);
        addCssVar("--bit-clr-suc-hover", bitTheme.Color.Success.MainHover);
        addCssVar("--bit-clr-suc-active", bitTheme.Color.Success.MainActive);
        addCssVar("--bit-clr-suc-dark", bitTheme.Color.Success.Dark);
        addCssVar("--bit-clr-suc-dark-hover", bitTheme.Color.Success.DarkHover);
        addCssVar("--bit-clr-suc-dark-active", bitTheme.Color.Success.DarkActive);
        addCssVar("--bit-clr-suc-light", bitTheme.Color.Success.Light);
        addCssVar("--bit-clr-suc-light-hover", bitTheme.Color.Success.LightHover);
        addCssVar("--bit-clr-suc-light-active", bitTheme.Color.Success.LightActive);
        addCssVar("--bit-clr-suc-text", bitTheme.Color.Success.Text);

        addCssVar("--bit-clr-wrn", bitTheme.Color.Warning.Main);
        addCssVar("--bit-clr-wrn-hover", bitTheme.Color.Warning.MainHover);
        addCssVar("--bit-clr-wrn-active", bitTheme.Color.Warning.MainActive);
        addCssVar("--bit-clr-wrn-dark", bitTheme.Color.Warning.Dark);
        addCssVar("--bit-clr-wrn-dark-hover", bitTheme.Color.Warning.DarkHover);
        addCssVar("--bit-clr-wrn-dark-active", bitTheme.Color.Warning.DarkActive);
        addCssVar("--bit-clr-wrn-light", bitTheme.Color.Warning.Light);
        addCssVar("--bit-clr-wrn-light-hover", bitTheme.Color.Warning.LightHover);
        addCssVar("--bit-clr-wrn-light-active", bitTheme.Color.Warning.LightActive);
        addCssVar("--bit-clr-wrn-text", bitTheme.Color.Warning.Text);

        addCssVar("--bit-clr-swr", bitTheme.Color.SevereWarning.Main);
        addCssVar("--bit-clr-swr-hover", bitTheme.Color.SevereWarning.MainHover);
        addCssVar("--bit-clr-swr-active", bitTheme.Color.SevereWarning.MainActive);
        addCssVar("--bit-clr-swr-dark", bitTheme.Color.SevereWarning.Dark);
        addCssVar("--bit-clr-swr-dark-hover", bitTheme.Color.SevereWarning.DarkHover);
        addCssVar("--bit-clr-swr-dark-active", bitTheme.Color.SevereWarning.DarkActive);
        addCssVar("--bit-clr-swr-light", bitTheme.Color.SevereWarning.Light);
        addCssVar("--bit-clr-swr-light-hover", bitTheme.Color.SevereWarning.LightHover);
        addCssVar("--bit-clr-swr-light-active", bitTheme.Color.SevereWarning.LightActive);
        addCssVar("--bit-clr-swr-text", bitTheme.Color.SevereWarning.Text);

        addCssVar("--bit-clr-err", bitTheme.Color.Error.Main);
        addCssVar("--bit-clr-err-hover", bitTheme.Color.Error.MainHover);
        addCssVar("--bit-clr-err-active", bitTheme.Color.Error.MainActive);
        addCssVar("--bit-clr-err-dark", bitTheme.Color.Error.Dark);
        addCssVar("--bit-clr-err-dark-hover", bitTheme.Color.Error.DarkHover);
        addCssVar("--bit-clr-err-dark-active", bitTheme.Color.Error.DarkActive);
        addCssVar("--bit-clr-err-light", bitTheme.Color.Error.Light);
        addCssVar("--bit-clr-err-light-hover", bitTheme.Color.Error.LightHover);
        addCssVar("--bit-clr-err-light-active", bitTheme.Color.Error.LightActive);
        addCssVar("--bit-clr-err-text", bitTheme.Color.Error.Text);

        addCssVar("--bit-clr-fg-pri", bitTheme.Color.Foreground.Primary);
        addCssVar("--bit-clr-fg-pri-hover", bitTheme.Color.Foreground.PrimaryHover);
        addCssVar("--bit-clr-fg-pri-active", bitTheme.Color.Foreground.PrimaryActive);
        addCssVar("--bit-clr-fg-sec", bitTheme.Color.Foreground.Secondary);
        addCssVar("--bit-clr-fg-sec-hover", bitTheme.Color.Foreground.SecondaryHover);
        addCssVar("--bit-clr-fg-sec-active", bitTheme.Color.Foreground.SecondaryActive);
        addCssVar("--bit-clr-fg-ter", bitTheme.Color.Foreground.Tertiary);
        addCssVar("--bit-clr-fg-ter-hover", bitTheme.Color.Foreground.TertiaryHover);
        addCssVar("--bit-clr-fg-ter-active", bitTheme.Color.Foreground.TertiaryActive);
        addCssVar("--bit-clr-fg-dis", bitTheme.Color.Foreground.Disabled);

        addCssVar("--bit-clr-bg-pri", bitTheme.Color.Background.Primary);
        addCssVar("--bit-clr-bg-pri-hover", bitTheme.Color.Background.PrimaryHover);
        addCssVar("--bit-clr-bg-pri-active", bitTheme.Color.Background.PrimaryActive);
        addCssVar("--bit-clr-bg-sec", bitTheme.Color.Background.Secondary);
        addCssVar("--bit-clr-bg-sec-hover", bitTheme.Color.Background.SecondaryHover);
        addCssVar("--bit-clr-bg-sec-active", bitTheme.Color.Background.SecondaryActive);
        addCssVar("--bit-clr-bg-ter", bitTheme.Color.Background.Tertiary);
        addCssVar("--bit-clr-bg-ter-hover", bitTheme.Color.Background.TertiaryHover);
        addCssVar("--bit-clr-bg-ter-active", bitTheme.Color.Background.TertiaryActive);
        addCssVar("--bit-clr-bg-dis", bitTheme.Color.Background.Disabled);
        addCssVar("--bit-clr-bg-overlay", bitTheme.Color.Background.Overlay);

        addCssVar("--bit-clr-brd-pri", bitTheme.Color.Border.Primary);
        addCssVar("--bit-clr-brd-pri-hover", bitTheme.Color.Border.PrimaryHover);
        addCssVar("--bit-clr-brd-pri-active", bitTheme.Color.Border.PrimaryActive);
        addCssVar("--bit-clr-brd-sec", bitTheme.Color.Border.Secondary);
        addCssVar("--bit-clr-brd-sec-hover", bitTheme.Color.Border.SecondaryHover);
        addCssVar("--bit-clr-brd-sec-active", bitTheme.Color.Border.SecondaryActive);
        addCssVar("--bit-clr-brd-ter", bitTheme.Color.Border.Tertiary);
        addCssVar("--bit-clr-brd-ter-hover", bitTheme.Color.Border.TertiaryHover);
        addCssVar("--bit-clr-brd-ter-active", bitTheme.Color.Border.TertiaryActive);
        addCssVar("--bit-clr-brd-dis", bitTheme.Color.Border.Disabled);

        addCssVar("--bit-clr-req", bitTheme.Color.Required);

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
        addCssVar("--bit-zin-base", bitTheme.ZIndex.Base);

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

        addCssVar("--bit-tpg-caption1-margin", bitTheme.Typography.Caption1.Margin);
        addCssVar("--bit-tpg-caption1-font-weight", bitTheme.Typography.Caption1.FontWeight);
        addCssVar("--bit-tpg-caption1-font-size", bitTheme.Typography.Caption1.FontSize);
        addCssVar("--bit-tpg-caption1-line-height", bitTheme.Typography.Caption1.LineHeight);
        addCssVar("--bit-tpg-caption1-letter-spacing", bitTheme.Typography.Caption1.LetterSpacing);

        addCssVar("--bit-tpg-caption2-margin", bitTheme.Typography.Caption2.Margin);
        addCssVar("--bit-tpg-caption2-font-weight", bitTheme.Typography.Caption2.FontWeight);
        addCssVar("--bit-tpg-caption2-font-size", bitTheme.Typography.Caption2.FontSize);
        addCssVar("--bit-tpg-caption2-line-height", bitTheme.Typography.Caption2.LineHeight);
        addCssVar("--bit-tpg-caption2-letter-spacing", bitTheme.Typography.Caption2.LetterSpacing);

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
        result.Color.Primary.MainHover = bitTheme.Color.Primary.MainHover ?? other.Color.Primary.MainHover;
        result.Color.Primary.MainActive = bitTheme.Color.Primary.MainActive ?? other.Color.Primary.MainActive;
        result.Color.Primary.Dark = bitTheme.Color.Primary.Dark ?? other.Color.Primary.Dark;
        result.Color.Primary.DarkHover = bitTheme.Color.Primary.DarkHover ?? other.Color.Primary.DarkHover;
        result.Color.Primary.DarkActive = bitTheme.Color.Primary.DarkActive ?? other.Color.Primary.DarkActive;
        result.Color.Primary.Light = bitTheme.Color.Primary.Light ?? other.Color.Primary.Light;
        result.Color.Primary.LightHover = bitTheme.Color.Primary.LightHover ?? other.Color.Primary.LightHover;
        result.Color.Primary.LightActive = bitTheme.Color.Primary.LightActive ?? other.Color.Primary.LightActive;
        result.Color.Primary.Text = bitTheme.Color.Primary.Text ?? other.Color.Primary.Text;

        result.Color.Secondary.Main = bitTheme.Color.Secondary.Main ?? other.Color.Secondary.Main;
        result.Color.Secondary.MainHover = bitTheme.Color.Secondary.MainHover ?? other.Color.Secondary.MainHover;
        result.Color.Secondary.MainActive = bitTheme.Color.Secondary.MainActive ?? other.Color.Secondary.MainActive;
        result.Color.Secondary.Dark = bitTheme.Color.Secondary.Dark ?? other.Color.Secondary.Dark;
        result.Color.Secondary.DarkHover = bitTheme.Color.Secondary.DarkHover ?? other.Color.Secondary.DarkHover;
        result.Color.Secondary.DarkActive = bitTheme.Color.Secondary.DarkActive ?? other.Color.Secondary.DarkActive;
        result.Color.Secondary.Light = bitTheme.Color.Secondary.Light ?? other.Color.Secondary.Light;
        result.Color.Secondary.LightHover = bitTheme.Color.Secondary.LightHover ?? other.Color.Secondary.LightHover;
        result.Color.Secondary.LightActive = bitTheme.Color.Secondary.LightActive ?? other.Color.Secondary.LightActive;
        result.Color.Secondary.Text = bitTheme.Color.Secondary.Text ?? other.Color.Secondary.Text;

        result.Color.Tertiary.Main = bitTheme.Color.Tertiary.Main ?? other.Color.Tertiary.Main;
        result.Color.Tertiary.MainHover = bitTheme.Color.Tertiary.MainHover ?? other.Color.Tertiary.MainHover;
        result.Color.Tertiary.MainActive = bitTheme.Color.Tertiary.MainActive ?? other.Color.Tertiary.MainActive;
        result.Color.Tertiary.Dark = bitTheme.Color.Tertiary.Dark ?? other.Color.Tertiary.Dark;
        result.Color.Tertiary.DarkHover = bitTheme.Color.Tertiary.DarkHover ?? other.Color.Tertiary.DarkHover;
        result.Color.Tertiary.DarkActive = bitTheme.Color.Tertiary.DarkActive ?? other.Color.Tertiary.DarkActive;
        result.Color.Tertiary.Light = bitTheme.Color.Tertiary.Light ?? other.Color.Tertiary.Light;
        result.Color.Tertiary.LightHover = bitTheme.Color.Tertiary.LightHover ?? other.Color.Tertiary.LightHover;
        result.Color.Tertiary.LightActive = bitTheme.Color.Tertiary.LightActive ?? other.Color.Tertiary.LightActive;
        result.Color.Tertiary.Text = bitTheme.Color.Tertiary.Text ?? other.Color.Tertiary.Text;

        result.Color.Info.Main = bitTheme.Color.Info.Main ?? other.Color.Info.Main;
        result.Color.Info.MainHover = bitTheme.Color.Info.MainHover ?? other.Color.Info.MainHover;
        result.Color.Info.MainActive = bitTheme.Color.Info.MainActive ?? other.Color.Info.MainActive;
        result.Color.Info.Dark = bitTheme.Color.Info.Dark ?? other.Color.Info.Dark;
        result.Color.Info.DarkHover = bitTheme.Color.Info.DarkHover ?? other.Color.Info.DarkHover;
        result.Color.Info.DarkActive = bitTheme.Color.Info.DarkActive ?? other.Color.Info.DarkActive;
        result.Color.Info.Light = bitTheme.Color.Info.Light ?? other.Color.Info.Light;
        result.Color.Info.LightHover = bitTheme.Color.Info.LightHover ?? other.Color.Info.LightHover;
        result.Color.Info.LightActive = bitTheme.Color.Info.LightActive ?? other.Color.Info.LightActive;
        result.Color.Info.Text = bitTheme.Color.Info.Text ?? other.Color.Info.Text;

        result.Color.Success.Main = bitTheme.Color.Success.Main ?? other.Color.Success.Main;
        result.Color.Success.MainHover = bitTheme.Color.Success.MainHover ?? other.Color.Success.MainHover;
        result.Color.Success.MainActive = bitTheme.Color.Success.MainActive ?? other.Color.Success.MainActive;
        result.Color.Success.Dark = bitTheme.Color.Success.Dark ?? other.Color.Success.Dark;
        result.Color.Success.DarkHover = bitTheme.Color.Success.DarkHover ?? other.Color.Success.DarkHover;
        result.Color.Success.DarkActive = bitTheme.Color.Success.DarkActive ?? other.Color.Success.DarkActive;
        result.Color.Success.Light = bitTheme.Color.Success.Light ?? other.Color.Success.Light;
        result.Color.Success.LightHover = bitTheme.Color.Success.LightHover ?? other.Color.Success.LightHover;
        result.Color.Success.LightActive = bitTheme.Color.Success.LightActive ?? other.Color.Success.LightActive;
        result.Color.Success.Text = bitTheme.Color.Success.Text ?? other.Color.Success.Text;

        result.Color.Warning.Main = bitTheme.Color.Warning.Main ?? other.Color.Warning.Main;
        result.Color.Warning.MainHover = bitTheme.Color.Warning.MainHover ?? other.Color.Warning.MainHover;
        result.Color.Warning.MainActive = bitTheme.Color.Warning.MainActive ?? other.Color.Warning.MainActive;
        result.Color.Warning.Dark = bitTheme.Color.Warning.Dark ?? other.Color.Warning.Dark;
        result.Color.Warning.DarkHover = bitTheme.Color.Warning.DarkHover ?? other.Color.Warning.DarkHover;
        result.Color.Warning.DarkActive = bitTheme.Color.Warning.DarkActive ?? other.Color.Warning.DarkActive;
        result.Color.Warning.Light = bitTheme.Color.Warning.Light ?? other.Color.Warning.Light;
        result.Color.Warning.LightHover = bitTheme.Color.Warning.LightHover ?? other.Color.Warning.LightHover;
        result.Color.Warning.LightActive = bitTheme.Color.Warning.LightActive ?? other.Color.Warning.LightActive;
        result.Color.Warning.Text = bitTheme.Color.Warning.Text ?? other.Color.Warning.Text;

        result.Color.SevereWarning.Main = bitTheme.Color.SevereWarning.Main ?? other.Color.SevereWarning.Main;
        result.Color.SevereWarning.MainHover = bitTheme.Color.SevereWarning.MainHover ?? other.Color.SevereWarning.MainHover;
        result.Color.SevereWarning.MainActive = bitTheme.Color.SevereWarning.MainActive ?? other.Color.SevereWarning.MainActive;
        result.Color.SevereWarning.Dark = bitTheme.Color.SevereWarning.Dark ?? other.Color.SevereWarning.Dark;
        result.Color.SevereWarning.DarkHover = bitTheme.Color.SevereWarning.DarkHover ?? other.Color.SevereWarning.DarkHover;
        result.Color.SevereWarning.DarkActive = bitTheme.Color.SevereWarning.DarkActive ?? other.Color.SevereWarning.DarkActive;
        result.Color.SevereWarning.Light = bitTheme.Color.SevereWarning.Light ?? other.Color.SevereWarning.Light;
        result.Color.SevereWarning.LightHover = bitTheme.Color.SevereWarning.LightHover ?? other.Color.SevereWarning.LightHover;
        result.Color.SevereWarning.LightActive = bitTheme.Color.SevereWarning.LightActive ?? other.Color.SevereWarning.LightActive;
        result.Color.SevereWarning.Text = bitTheme.Color.SevereWarning.Text ?? other.Color.SevereWarning.Text;

        result.Color.Error.Main = bitTheme.Color.Error.Main ?? other.Color.Error.Main;
        result.Color.Error.MainHover = bitTheme.Color.Error.MainHover ?? other.Color.Error.MainHover;
        result.Color.Error.MainActive = bitTheme.Color.Error.MainActive ?? other.Color.Error.MainActive;
        result.Color.Error.Dark = bitTheme.Color.Error.Dark ?? other.Color.Error.Dark;
        result.Color.Error.DarkHover = bitTheme.Color.Error.DarkHover ?? other.Color.Error.DarkHover;
        result.Color.Error.DarkActive = bitTheme.Color.Error.DarkActive ?? other.Color.Error.DarkActive;
        result.Color.Error.Light = bitTheme.Color.Error.Light ?? other.Color.Error.Light;
        result.Color.Error.LightHover = bitTheme.Color.Error.LightHover ?? other.Color.Error.LightHover;
        result.Color.Error.LightActive = bitTheme.Color.Error.LightActive ?? other.Color.Error.LightActive;
        result.Color.Error.Text = bitTheme.Color.Error.Text ?? other.Color.Error.Text;

        result.Color.Foreground.Primary = bitTheme.Color.Foreground.Primary ?? other.Color.Foreground.Primary;
        result.Color.Foreground.PrimaryHover = bitTheme.Color.Foreground.PrimaryHover ?? other.Color.Foreground.PrimaryHover;
        result.Color.Foreground.PrimaryActive = bitTheme.Color.Foreground.PrimaryActive ?? other.Color.Foreground.PrimaryActive;
        result.Color.Foreground.Secondary = bitTheme.Color.Foreground.Secondary ?? other.Color.Foreground.Secondary;
        result.Color.Foreground.SecondaryHover = bitTheme.Color.Foreground.SecondaryHover ?? other.Color.Foreground.SecondaryHover;
        result.Color.Foreground.SecondaryActive = bitTheme.Color.Foreground.SecondaryActive ?? other.Color.Foreground.SecondaryActive;
        result.Color.Foreground.Tertiary = bitTheme.Color.Foreground.Tertiary ?? other.Color.Foreground.Tertiary;
        result.Color.Foreground.TertiaryHover = bitTheme.Color.Foreground.TertiaryHover ?? other.Color.Foreground.TertiaryHover;
        result.Color.Foreground.TertiaryActive = bitTheme.Color.Foreground.TertiaryActive ?? other.Color.Foreground.TertiaryActive;
        result.Color.Foreground.Disabled = bitTheme.Color.Foreground.Disabled ?? other.Color.Foreground.Disabled;

        result.Color.Background.Primary = bitTheme.Color.Background.Primary ?? other.Color.Background.Primary;
        result.Color.Background.PrimaryHover = bitTheme.Color.Background.PrimaryHover ?? other.Color.Background.PrimaryHover;
        result.Color.Background.PrimaryActive = bitTheme.Color.Background.PrimaryActive ?? other.Color.Background.PrimaryActive;
        result.Color.Background.Secondary = bitTheme.Color.Background.Secondary ?? other.Color.Background.Secondary;
        result.Color.Background.SecondaryHover = bitTheme.Color.Background.SecondaryHover ?? other.Color.Background.SecondaryHover;
        result.Color.Background.SecondaryActive = bitTheme.Color.Background.SecondaryActive ?? other.Color.Background.SecondaryActive;
        result.Color.Background.Tertiary = bitTheme.Color.Background.Tertiary ?? other.Color.Background.Tertiary;
        result.Color.Background.TertiaryHover = bitTheme.Color.Background.TertiaryHover ?? other.Color.Background.TertiaryHover;
        result.Color.Background.TertiaryActive = bitTheme.Color.Background.TertiaryActive ?? other.Color.Background.TertiaryActive;
        result.Color.Background.Disabled = bitTheme.Color.Background.Disabled ?? other.Color.Background.Disabled;
        result.Color.Background.Overlay = bitTheme.Color.Background.Overlay ?? other.Color.Background.Overlay;

        result.Color.Border.Primary = bitTheme.Color.Border.Primary ?? other.Color.Border.Primary;
        result.Color.Border.PrimaryHover = bitTheme.Color.Border.PrimaryHover ?? other.Color.Border.PrimaryHover;
        result.Color.Border.PrimaryActive = bitTheme.Color.Border.PrimaryActive ?? other.Color.Border.PrimaryActive;
        result.Color.Border.Secondary = bitTheme.Color.Border.Secondary ?? other.Color.Border.Secondary;
        result.Color.Border.SecondaryHover = bitTheme.Color.Border.SecondaryHover ?? other.Color.Border.SecondaryHover;
        result.Color.Border.SecondaryActive = bitTheme.Color.Border.SecondaryActive ?? other.Color.Border.SecondaryActive;
        result.Color.Border.Tertiary = bitTheme.Color.Border.Tertiary ?? other.Color.Border.Tertiary;
        result.Color.Border.TertiaryHover = bitTheme.Color.Border.TertiaryHover ?? other.Color.Border.TertiaryHover;
        result.Color.Border.TertiaryActive = bitTheme.Color.Border.TertiaryActive ?? other.Color.Border.TertiaryActive;
        result.Color.Border.Disabled = bitTheme.Color.Border.Disabled ?? other.Color.Border.Disabled;

        result.Color.Required = bitTheme.Color.Required ?? other.Color.Required;

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
        result.ZIndex.Base = bitTheme.ZIndex.Base ?? other.ZIndex.Base;

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

        result.Typography.Caption1.Margin = bitTheme.Typography.Caption1.Margin ?? other.Typography.Caption1.Margin;
        result.Typography.Caption1.FontWeight = bitTheme.Typography.Caption1.FontWeight ?? other.Typography.Caption1.FontWeight;
        result.Typography.Caption1.FontSize = bitTheme.Typography.Caption1.FontSize ?? other.Typography.Caption1.FontSize;
        result.Typography.Caption1.LineHeight = bitTheme.Typography.Caption1.LineHeight ?? other.Typography.Caption1.LineHeight;
        result.Typography.Caption1.LetterSpacing = bitTheme.Typography.Caption1.LetterSpacing ?? other.Typography.Caption1.LetterSpacing;

        result.Typography.Caption2.Margin = bitTheme.Typography.Caption2.Margin ?? other.Typography.Caption2.Margin;
        result.Typography.Caption2.FontWeight = bitTheme.Typography.Caption2.FontWeight ?? other.Typography.Caption2.FontWeight;
        result.Typography.Caption2.FontSize = bitTheme.Typography.Caption2.FontSize ?? other.Typography.Caption2.FontSize;
        result.Typography.Caption2.LineHeight = bitTheme.Typography.Caption2.LineHeight ?? other.Typography.Caption2.LineHeight;
        result.Typography.Caption2.LetterSpacing = bitTheme.Typography.Caption2.LetterSpacing ?? other.Typography.Caption2.LetterSpacing;

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
