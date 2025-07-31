//+:cnd:noEmit

declare class BitTheme { static init(options: BitThemeOptions): void; };

interface BitThemeOptions {
    system?: boolean;
    persist?: boolean;
    theme?: string | null;
    default?: string | null;
    darkTheme?: string | null;
    lightTheme?: string | null;
    onChange?: onThemeChangeType;
}

type onThemeChangeType = (newThemeName: string, oldThemeName: string) => void;

(function () {
    BitTheme?.init({
        system: true,
        persist: true,
        onChange: (newTheme: string, oldTheme: string) => {
            document.body.classList.add('theme-' + newTheme);
            document.body.classList.remove('theme-' + oldTheme);

            const primaryBgColor = getComputedStyle(document.documentElement).getPropertyValue('--bit-clr-bg-pri');
            if (!primaryBgColor) return;

            document.querySelector('meta[name=theme-color]')?.setAttribute('content', primaryBgColor);
        }
    });
}());
