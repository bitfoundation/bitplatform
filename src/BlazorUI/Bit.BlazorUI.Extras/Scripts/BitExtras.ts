namespace BitBlazorUI {
    export class BitExtras {
        public static applyRootClasses(cssClasses: string[], cssVariables: any) {
            cssClasses?.forEach(c => document.documentElement.classList.add(c));
            Object.keys(cssVariables).forEach(key => document.documentElement.style.setProperty(key, cssVariables[key]));
        }
    }
}