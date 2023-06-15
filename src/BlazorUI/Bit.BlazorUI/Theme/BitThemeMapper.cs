using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Theme;
internal static class BitThemeMapper
{
    internal static Dictionary<string, string> MapToCssVariables(BitTheme bitTheme)
    {
        var result = new Dictionary<string, string>();

        if (bitTheme is null) return result;

        addCssVar("--bit-clr-primary-main", bitTheme.Color.Primary.Main);
        addCssVar("--bit-clr-primary-dark", bitTheme.Color.Primary.Dark);
        addCssVar("--bit-clr-primary-light", bitTheme.Color.Primary.Light);
        addCssVar("--bit-clr-primary-text", bitTheme.Color.Primary.Text);

        addCssVar("--bit-clr-secondary-main", bitTheme.Color.Secondary.Main);
        addCssVar("--bit-clr-secondary-dark", bitTheme.Color.Secondary.Dark);
        addCssVar("--bit-clr-secondary-light", bitTheme.Color.Secondary.Light);
        addCssVar("--bit-clr-secondary-text", bitTheme.Color.Secondary.Text);

        addCssVar("--bit-clr-fg-primary", bitTheme.Color.Foreground.Primary);
        addCssVar("--bit-clr-fg-secondary", bitTheme.Color.Foreground.Secondary);
        addCssVar("--bit-clr-fg-disabled", bitTheme.Color.Foreground.Disabled);

        addCssVar("--bit-clr-bg-primary", bitTheme.Color.Background.Primary);
        addCssVar("--bit-clr-bg-secondary", bitTheme.Color.Background.Secondary);
        addCssVar("--bit-clr-bg-disabled", bitTheme.Color.Background.Disabled);
        addCssVar("--bit-clr-bg-overlay", bitTheme.Color.Background.Overlay);

        addCssVar("--bit-clr-brd-primary", bitTheme.Color.Border.Primary);
        addCssVar("--bit-clr-brd-secondary", bitTheme.Color.Border.Secondary);
        addCssVar("--bit-clr-brd-disabled", bitTheme.Color.Border.Disabled);

        addCssVar("--bit-clr-act-hover-pri", bitTheme.Color.Action.Hover.Primary);
        addCssVar("--bit-clr-act-hover-pri-dark", bitTheme.Color.Action.Hover.PrimaryDark);
        addCssVar("--bit-clr-act-hover-pri-light", bitTheme.Color.Action.Hover.PrimaryLight);
        addCssVar("--bit-clr-act-hover-sec", bitTheme.Color.Action.Hover.Secondary);
        addCssVar("--bit-clr-act-hover-sec-dark", bitTheme.Color.Action.Hover.SecondaryDark);
        addCssVar("--bit-clr-act-hover-sec-light", bitTheme.Color.Action.Hover.SecondaryLight);

        addCssVar("--bit-clr-act-active-pri", bitTheme.Color.Action.Active.Primary);
        addCssVar("--bit-clr-act-active-pri-dark", bitTheme.Color.Action.Active.PrimaryDark);
        addCssVar("--bit-clr-act-active-pri-light", bitTheme.Color.Action.Active.PrimaryLight);
        addCssVar("--bit-clr-act-active-sec", bitTheme.Color.Action.Active.Secondary);
        addCssVar("--bit-clr-act-active-sec-dark", bitTheme.Color.Action.Active.SecondaryDark);
        addCssVar("--bit-clr-act-active-sec-light", bitTheme.Color.Action.Active.SecondaryLight);

        addCssVar("--bit-clr-act-hover-fg-pri", bitTheme.Color.Action.Hover.Foreground.Primary!);
        addCssVar("--bit-clr-act-hover-fg-sec", bitTheme.Color.Action.Hover.Foreground.Secondary!);
        addCssVar("--bit-clr-act-active-fg-pri", bitTheme.Color.Action.Active.Foreground.Primary!);
        addCssVar("--bit-clr-act-active-fg-sec", bitTheme.Color.Action.Active.Foreground.Secondary!);

        addCssVar("--bit-clr-act-hover-bg-pri", bitTheme.Color.Action.Hover.Background.Primary!);
        addCssVar("--bit-clr-act-hover-bg-sec", bitTheme.Color.Action.Hover.Background.Secondary!);
        addCssVar("--bit-clr-act-active-bg-pri", bitTheme.Color.Action.Active.Background.Primary!);
        addCssVar("--bit-clr-act-active-bg-sec", bitTheme.Color.Action.Active.Background.Secondary!);

        addCssVar("--bit-clr-act-hover-brd-pri", bitTheme.Color.Action.Hover.Border.Primary!);
        addCssVar("--bit-clr-act-hover-brd-sec", bitTheme.Color.Action.Hover.Border.Secondary!);
        addCssVar("--bit-clr-act-active-brd-pri", bitTheme.Color.Action.Active.Border.Primary!);
        addCssVar("--bit-clr-act-active-brd-sec", bitTheme.Color.Action.Active.Border.Secondary!);

        addCssVar("--bit-clr-sta-info", bitTheme.Color.State.Info);
        addCssVar("--bit-clr-sta-info-bg", bitTheme.Color.State.InfoBackground);
        addCssVar("--bit-clr-sta-success", bitTheme.Color.State.Success);
        addCssVar("--bit-clr-sta-success-bg", bitTheme.Color.State.SuccessBackground);
        addCssVar("--bit-clr-sta-warning", bitTheme.Color.State.Warning);
        addCssVar("--bit-clr-sta-warning-bg", bitTheme.Color.State.WarningBackground);
        addCssVar("--bit-clr-sta-severe-warning", bitTheme.Color.State.SevereWarning);
        addCssVar("--bit-clr-sta-severe-warning-bg", bitTheme.Color.State.SevereWarningBackground);
        addCssVar("--bit-clr-sta-error", bitTheme.Color.State.Error);
        addCssVar("--bit-clr-sta-error-bg", bitTheme.Color.State.ErrorBackground);
        addCssVar("--bit-clr-sta-req", bitTheme.Color.State.Required);

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
}
