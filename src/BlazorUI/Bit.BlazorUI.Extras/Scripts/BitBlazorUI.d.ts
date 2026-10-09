// The members of the core library's scripts (bit.blazorui.js) the scripts of this package call. Both bundles
// add to the one global BitBlazorUI namespace, and the core one is loaded by every app this package runs in -
// its components are built out of core ones, which cannot run without it - so they are called rather than
// copied, and a fix made to them reaches this package as well. Only what is used here is declared;
// BitFocusablesContractTests fails on a member declared here that the core Utils no longer has.
declare namespace BitBlazorUI {
    class Utils {
        static isFocusableElement(el: HTMLElement): boolean;
        static getFocusables(container: ParentNode): HTMLElement[];
        static firstFocusable(container: ParentNode, skip?: (el: HTMLElement) => boolean): HTMLElement | null;
        static findFocusable(scope: ParentNode, from: Node, forward: boolean, skip?: (el: HTMLElement) => boolean): HTMLElement | null;
    }
}
