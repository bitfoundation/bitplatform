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

        // The names this script holds the registration of, and the createEventArgs an app registered for one of them
        // after it did (see wrapRegistration).
        private static readonly _ownedEvents: string[] = [];
        private static readonly _appEventArgs: { [name: string]: ((e: Event) => any) | null } = {};

        // The script is normally loaded right after Blazor's own, which is what defines window.Blazor, so the
        // registration is tried at once. A page that loads Blazor later - after this script in the markup, injected by
        // a loader, started by hand - is caught the moment it assigns window.Blazor, before anything (a JS
        // initializer, an app's own registration) has had a chance to run against it; the document's own load
        // events are a last resort for a Blazor defined some other way.
        public static init() {
            Events.register();

            if (Events._isRegistered) return;

            Events.awaitBlazor();

            document.addEventListener('DOMContentLoaded', () => Events.register(), { once: true });
            window.addEventListener('load', () => Events.register(), { once: true });
        }

        public static register(blazor?: any) {
            if (Events._isRegistered) return;

            blazor = blazor || (window as any).Blazor;
            if (!blazor || typeof blazor.registerCustomEventType !== 'function') return;

            Events._isRegistered = true;

            // Read without instanceof, which is bound to this window's Element and so misses an element of
            // another document (an iframe, a popup window) that Blazor renders into.
            const targetId = (e: Event) => {
                const target = e.target as Element | null;
                return target && target.nodeType === 1 ? target.id : '';
            };

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

            Events.wrapRegistration(blazor);
        }

        // Defines window.Blazor as a property that registers the events as soon as it is assigned, and turns back
        // into the plain property it would have been the moment it has.
        private static awaitBlazor() {
            const descriptor = Object.getOwnPropertyDescriptor(window, 'Blazor');
            if (descriptor && descriptor.configurable === false) return;

            let value: any = descriptor ? descriptor.value : undefined;

            Object.defineProperty(window, 'Blazor', {
                configurable: true,
                enumerable: true,
                get: () => value,
                set: (blazor: any) => {
                    value = blazor;
                    Events.register(blazor);
                    if (Events._isRegistered === false) return;

                    Object.defineProperty(window, 'Blazor', { value, writable: true, configurable: true, enumerable: true });
                }
            });
        }

        private static registerEvent(blazor: any, name: string, ownEventArgs: (e: any) => object) {
            try {
                // The arguments are put together at dispatch time, so an app that registers the same event after this
                // script has still gets its own fields to its handlers (see wrapRegistration).
                blazor.registerCustomEventType(name, {
                    createEventArgs: (e: Event) => {
                        const appEventArgs = Events._appEventArgs[name];
                        return appEventArgs ? { ...ownEventArgs(e), ...appEventArgs(e) } : ownEventArgs(e);
                    }
                });

                Events._ownedEvents.push(name);
            } catch {
                // Blazor takes one registration per event name and throws on a second one. A name that is already
                // taken - by the app, or by a later version of Blazor itself - keeps the registration it has.
            }
        }

        // Blazor takes one registration per event name and throws on a second one, so an app registering one of
        // these events itself - after this script has - would have its registration throw, and its handlers handed
        // this script's arguments in place of its own. Its registration is taken in instead: its createEventArgs
        // runs beside this script's, its fields winning a clash, so its handlers and the library's both get theirs.
        // A second registration of the same name by the app, or one that names a browserEventName, still goes to
        // Blazor and throws the way it always would.
        private static wrapRegistration(blazor: any) {
            const registerCustomEventType = blazor.registerCustomEventType;

            try {
                blazor.registerCustomEventType = function (eventName: string, options: any) {
                    const isOwned = Events._ownedEvents.indexOf(eventName) !== -1;
                    const isTaken = Object.prototype.hasOwnProperty.call(Events._appEventArgs, eventName);

                    if (isOwned && isTaken === false && options && !options.browserEventName) {
                        Events._appEventArgs[eventName] = typeof options.createEventArgs === 'function' ? options.createEventArgs : null;
                        return;
                    }

                    return registerCustomEventType.apply(this, arguments as any);
                };
            } catch {
                // A Blazor object that cannot be written to keeps its own registration, throwing as it always has.
            }
        }
    }
}
