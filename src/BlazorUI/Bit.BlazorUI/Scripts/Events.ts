namespace BitBlazorUI {
    // Hands Blazor the arguments of the CSS animation and transition events, which it dispatches to the
    // @onanimationend / @ontransitionend directives of Bit.BlazorUI.Events.EventHandlers but has no arguments of its own for: an event
    // it knows nothing about reaches .NET as an empty object, which would leave a handler unable to tell which
    // animation ended, or whether it was its own element's or one bubbled up from the content.
    // The events are registered under their own names rather than as aliases of them, so a directive is listened for
    // the same way whether or not the registration has happened yet - an event that fires before it simply arrives
    // with its fields empty.
    export class Events {
        private static _isRegistered = false;

        private static readonly _animationEvents = ['animationstart', 'animationend', 'animationiteration', 'animationcancel'];
        private static readonly _transitionEvents = ['transitionrun', 'transitionstart', 'transitionend', 'transitioncancel'];

        // The script is normally loaded right after Blazor's own, which is what defines window.Blazor, so the
        // registration is tried at once; a page that loads it ahead of Blazor's (in the head, say) is caught up with
        // once the document has been parsed, by which time every classic and deferred script on it has run.
        public static init() {
            Events.register();

            if (Events._isRegistered) return;

            document.addEventListener('DOMContentLoaded', () => Events.register(), { once: true });
            window.addEventListener('load', () => Events.register(), { once: true });
        }

        public static register() {
            if (Events._isRegistered) return;

            const blazor = (window as any).Blazor;
            if (!blazor || typeof blazor.registerCustomEventType !== 'function') return;

            Events._isRegistered = true;

            const targetId = (e: Event) => (e.target instanceof Element ? e.target.id : '') || '';

            const animationArgs = (e: AnimationEvent) => ({
                animationName: e.animationName || '',
                elapsedTime: e.elapsedTime || 0,
                pseudoElement: e.pseudoElement || '',
                targetId: targetId(e)
            });

            const transitionArgs = (e: TransitionEvent) => ({
                propertyName: e.propertyName || '',
                elapsedTime: e.elapsedTime || 0,
                pseudoElement: e.pseudoElement || '',
                targetId: targetId(e)
            });

            Events._animationEvents.forEach(name => Events.registerEvent(blazor, name, animationArgs));
            Events._transitionEvents.forEach(name => Events.registerEvent(blazor, name, transitionArgs));
        }

        private static registerEvent(blazor: any, name: string, createEventArgs: (e: any) => object) {
            try {
                blazor.registerCustomEventType(name, { createEventArgs });
            } catch {
                // Blazor takes one registration per event name and throws on a second one. A name that is already
                // taken - by the app, or by a later version of Blazor itself - keeps the registration it has.
            }
        }
    }
}
