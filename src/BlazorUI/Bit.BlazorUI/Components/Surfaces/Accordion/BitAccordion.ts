namespace BitBlazorUI {
    export class Accordion {
        // Resolves once the transitions of a closing panel have finished, as the browser plays them: the parameters,
        // the public variables, the motion tokens of the theme preset and the reduced motion preference all end up in
        // what it plays. The transitions are the grid row's (the height) and the panel's own (the fade, the padding
        // and the visibility held back for the length of the close); nothing inside the content counts. A transition
        // cut short - the panel opened again - settles the wait as well, and the component tells that apart itself.
        public static async waitForTransitions(content: HTMLElement): Promise<void> {
            if (!content || !(content instanceof Element)) return;

            // .bit-acd-con > .bit-acd-cwr > .bit-acd-cnt
            const row = content.parentElement?.parentElement;
            const elements = row ? [row, content] : [content];

            if (typeof content.getAnimations !== 'function') {
                await new Promise(resolve => setTimeout(resolve, Accordion.getTransitionTime(elements)));
                return;
            }

            // getAnimations brings the style up to date first, so the transitions the last render started are
            // already in the list. One that has already finished - collapsed by the reduced motion preference, or on
            // an accordion that is not displayed at all - is not, which leaves nothing to wait for.
            const transitions = elements
                .flatMap(e => e.getAnimations())
                .filter(a => typeof CSSTransition === 'undefined' || a instanceof CSSTransition);

            await Promise.all(transitions.map(a => a.finished.catch(() => { })));
        }

        // The whole of the transitions, delay included, as the computed style resolves them: the fallback for a
        // browser that cannot list what it is playing.
        private static getTransitionTime(elements: Element[]): number {
            let longest = 0;

            for (const element of elements) {
                const style = getComputedStyle(element);
                const durations = Accordion.parseTimes(style.transitionDuration);
                const delays = Accordion.parseTimes(style.transitionDelay);

                if (durations.length === 0) continue;

                // The browser repeats the shorter of the two lists to the length of the longer one.
                const count = Math.max(durations.length, delays.length);

                for (let i = 0; i < count; i++) {
                    const duration = durations[i % durations.length];
                    const delay = delays.length > 0 ? delays[i % delays.length] : 0;
                    longest = Math.max(longest, Math.max(0, duration) + Math.max(0, delay));
                }
            }

            return longest;
        }

        private static parseTimes(value: string): number[] {
            if (!value) return [];

            return value.split(',').map(v => {
                const time = v.trim();
                const number = parseFloat(time);

                if (isNaN(number)) return 0;

                return time.endsWith('ms') ? number : number * 1000;
            });
        }
    }
}
