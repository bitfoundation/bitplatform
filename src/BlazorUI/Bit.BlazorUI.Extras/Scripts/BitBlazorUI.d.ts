// The members of the core library's scripts (bit.blazorui.js) the scripts of this package call. Both bundles
// add to the one global BitBlazorUI namespace, and the core one is loaded by every app this package runs in -
// its components are built out of core ones, which cannot run without it - so they are called rather than
// copied, and a fix made to them reaches this package as well. Only what is used here is declared;
// BitFocusablesContractTests fails on a member declared here that the core Utils no longer exports. The publish-time
// trimming reads these too: each one this package's scripts use is kept in the core bundle along with them.
declare namespace BitBlazorUI.Utils {
    function isComposingKey(e: KeyboardEvent | InputEvent): boolean;
    function isFocusableElement(el: HTMLElement): boolean;
    function getFocusables(container: ParentNode): HTMLElement[];
    function firstFocusable(container: ParentNode, skip?: (el: HTMLElement) => boolean): HTMLElement | null;
    function findFocusable(scope: ParentNode, from: Node, forward: boolean, skip?: (el: HTMLElement) => boolean): HTMLElement | null;
    function isInNestedSurface(node: Node | null, boundary: Element): boolean;
    function isRtl(element: HTMLElement): boolean;
}
