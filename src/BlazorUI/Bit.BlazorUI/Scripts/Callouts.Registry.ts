namespace BitBlazorUI {
    export namespace Callouts {
        export const DEFAULT_CALLOUT: BitCallout = { calloutId: '' };

        // The callouts that are open, outermost first. It is a stack rather than a single entry because a
        // callout can be opened from inside another one - a dropdown in a filter panel, a menu on a card in
        // a popover - and the outer one has to stay open underneath it instead of being taken over.
        export let _stack: BitCallout[] = [];
        // The inputs each open callout was positioned with, so that it can be laid out again without its
        // component having to hand them over a second time.
        export const _params: Map<string, BitCalloutParams> = new Map();

        // The innermost open callout, which is the one the page-level handlers speak for. It reads as the
        // single open callout it used to be for everything that never nests. A getter, defined on the namespace
        // below, since a namespace declares none of its own.
        export declare const current: BitCallout;

        // True when the node lives inside the anchor component of one of the open callouts (e.g. the
        // SearchBox input that owns the suggestion callout). Used so that a scroll/resize while that input
        // is focused - typically caused by the on-screen keyboard moving the page - re-anchors the callout
        // to the component's new position instead of dismissing it.
        // `scope` narrows it to the components inside that element, for a surface that asks whether a popup
        // of its own content owns the key rather than one of a component the surface itself sits inside.
        export function componentContains(node: Node | null, scope?: Node | null): boolean {
            if (node == null) return false;

            return Callouts._stack.some(entry => {
                const componentId = Callouts._params.get(entry.calloutId)?.componentId;
                const component = componentId ? document.getElementById(componentId) : null;
                if (component == null || (scope != null && scope.contains(component) === false)) return false;

                return component.contains(node);
            });
        }

        // True when one of the open callouts was opened from a component inside the given element - a dropdown
        // or a menu inside a modal, whose popup is relocated to the body and so is no longer inside it itself.
        export function isOpenedFrom(root: Element | null): boolean {
            if (root == null) return false;

            return Callouts._stack.some(entry => {
                const componentId = Callouts._params.get(entry.calloutId)?.componentId ?? entry.componentId;
                const component = componentId ? document.getElementById(componentId) : null;
                return component != null && root.contains(component);
            });
        }

        // True when the node lives inside one of the open callouts. A scroll that started in there is the
        // user reading the callout rather than the page moving out from under it.
        export function calloutContains(node: Node | null): boolean {
            if (node == null) return false;

            return Callouts._stack.some(entry => document.getElementById(entry.calloutId)?.contains(node) ?? false);
        }
    }

    // bit-trim: on-demand
    // Configurable, so the bundle evaluated a second time (a page that links it twice) defines it again rather than
    // throwing, the way a class declared again simply replaces the first.
    Object.defineProperty(Callouts, 'current', {
        configurable: true,
        enumerable: true,
        get: (): BitCallout => Callouts._stack.length > 0
            ? Callouts._stack[Callouts._stack.length - 1]
            : Callouts.DEFAULT_CALLOUT,
    });

    export interface BitCallout {
        calloutId: string;
        componentId?: string;
        overlayId?: string;
        arrowId?: string;
        noDismiss?: boolean;
        // Whether a scroll or a resize of the page leaves the callout open, following its component. A
        // callout that is not dismissed from outside at all is not dismissed by these either.
        noScrollDismiss?: boolean;
        dotnetObj?: DotNetObject;
        scrollContainerId?: string;
        responsiveMode?: BitResponsiveMode;
        // Whether the toggle registered the focus trap of the callout, which is then its to take back.
        trapFocus?: boolean;
        // Whether the toggle put the callout into the tab order after its trigger, which is then its to take back.
        tabOut?: boolean;
        // Whether the toggle watches the Escapes of the component's root and callout (Utils.watchNestedEscape),
        // which is then its to take back.
        nestedEscape?: boolean;
    }

    export interface BitCalloutParams {
        componentId: string;
        calloutId: string;
        overlayId: string;
        responsiveMode: BitResponsiveMode;
        dropDirection: BitDropDirection;
        isRtl: boolean | null;
        scrollContainerId: string;
        scrollOffset: number;
        headerId: string;
        footerId: string;
        setCalloutWidth: boolean;
        fixedCalloutWidth: boolean;
        maxWindowWidth: number;
        maxHeight: number;
        arrowId: string;
        gap: number;
        preferredSide: string;
        alignment: string;
        noFlip: boolean;
        collisionPadding: number;
        alignmentOffset: number;
        arrowPadding: number;
        rootId: string;
        // The center of the component on the screen when the callout was last placed against it (see recordAnchor).
        anchorX?: number;
        anchorY?: number;
    }
}
