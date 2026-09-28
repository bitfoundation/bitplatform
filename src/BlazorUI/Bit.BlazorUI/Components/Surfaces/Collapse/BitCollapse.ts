namespace BitBlazorUI {
    export class Collapse {
        // The time in ms left until the transition the collapse is playing has finished, delay included, as the
        // browser is playing it: the parameters, the public --bit-Collapse-duration variable, the motion tokens
        // of the theme preset and the reduced motion preference all end up in what the browser plays, which is
        // the one place where the stylesheet and the component can agree on when the transition is over.
        // Reading what is left rather than the whole duration keeps the time the render took to get here, and
        // the call to take this reading, from being waited out a second time. The transitions are the root's
        // (the size) and the content region's (the fade and the visibility); nothing inside the content counts.
        // null means there is nothing to read, and the component falls back to its own estimate.
        public static getRemainingTransitionTime(root: HTMLElement): number | null {
            if (!root || !(root instanceof Element)) return null;

            try {
                const content = root.querySelector(':scope > .bit-col-con');

                if (typeof root.getAnimations !== 'function') {
                    return Collapse.getTransitionTime(root, content);
                }

                // getAnimations brings the style up to date first, so a transition the last render started is
                // already in the list. One that has already finished - collapsed by the reduced motion
                // preference, or on a collapse that is not displayed at all - is not, which leaves nothing to wait.
                const animations = [...root.getAnimations(), ...(content ? content.getAnimations() : [])];
                let longest = 0;

                for (const animation of animations) {
                    if (typeof CSSTransition !== 'undefined' && !(animation instanceof CSSTransition)) continue;

                    const timing = animation.effect?.getComputedTiming();

                    if (!timing) continue;

                    const end = Number(timing.endTime ?? 0);
                    const current = Number(timing.localTime ?? 0);

                    if (!isFinite(end)) continue;

                    longest = Math.max(longest, end - current);
                }

                return Math.max(0, longest);
            } catch {
                return null;
            }
        }

        // The whole of the transition, delay included, as the computed style resolves it: the fallback for a
        // browser that cannot list what it is playing. The longest of the listed transitions is taken, since
        // the size and the fade share the same pace.
        private static getTransitionTime(root: Element, content: Element | null): number | null {
            let longest: number | null = null;

            for (const element of content ? [root, content] : [root]) {
                const style = getComputedStyle(element);
                const durations = Collapse.parseTimes(style.transitionDuration);
                const delays = Collapse.parseTimes(style.transitionDelay);

                if (durations.length === 0) continue;

                // The browser repeats the shorter of the two lists to the length of the longer one.
                const count = Math.max(durations.length, delays.length);

                for (let i = 0; i < count; i++) {
                    const duration = durations[i % durations.length];
                    const delay = delays.length > 0 ? delays[i % delays.length] : 0;
                    longest = Math.max(longest ?? 0, Math.max(0, duration) + Math.max(0, delay));
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
