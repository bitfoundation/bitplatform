declare class BitTheme { static init(options: any): void; };

// https://github.com/dotnet/aspnetcore/issues/18902#issuecomment-585987887
declare module DotNet {
    function invokeMethod<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): T;
    function invokeMethodAsync<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): Promise<T>;
    interface DotNetObject {
        invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
        invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    }
}

class App {
    public static applyBodyElementClasses(cssClasses: string[], cssVariables: any): void {
        cssClasses?.forEach(c => document.body.classList.add(c));
        Object.keys(cssVariables).forEach(key => document.body.style.setProperty(key, cssVariables[key]));
    }

    public static getPlatform(): string {
        return (navigator as any).userAgentData?.platform || navigator?.platform;
    }
}


BitTheme.init({
    system: true,
    onChange: (newTheme: string, oldThem: string) => {
        const primaryBackgroundColor = getComputedStyle(document.documentElement).getPropertyValue('--bit-clr-bg-pri');
        const isDark = newTheme === 'dark';
        if (isDark) {
            document.body.classList.add('theme-dark');
            document.body.classList.remove('theme-light');
        } else {
            document.body.classList.add('theme-light');
            document.body.classList.remove('theme-dark');
        }

        document.querySelector("meta[name=theme-color]")!.setAttribute('content', primaryBackgroundColor);

        invokeMauiApplyTheme(primaryBackgroundColor, isDark);
    }
});

async function invokeMauiApplyTheme(primaryBackgroundColor: string, isDark: boolean) {
    try {
        await DotNet.invokeMethodAsync('Boilerplate.Client.Maui', 'ApplyTheme', primaryBackgroundColor, isDark);
    }
    catch (e: any) {
        const errorMessage = e.toString();
        if (errorMessage.includes('No call dispatcher has been set')) {
            setTimeout(() => invokeMauiApplyTheme(primaryBackgroundColor, isDark), 50);
        }
        else if (errorMessage.includes('There is no loaded assembly with the name')) {
            // This method call only works on Maui project, so let's ignore it for web and windows projects.
        }
        else {
            console.error(e);
        }
    }
}
