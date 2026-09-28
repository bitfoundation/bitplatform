namespace BitBlazorUI {
    export class Collapse {
        // The time in ms the transition the root is playing takes to finish, delay included, as the browser
        // resolved it: the parameters, the public --bit-Collapse-duration variable, the motion tokens of the
        // theme preset and the reduced motion preference all end up in the computed style, which is the one
        // place where the stylesheet and the component can agree on when the transition is over. The longest
        // of the listed transitions is taken, since the size and the fade share the same pace. null means
        // there is nothing to read, and the component falls back to its own estimate.
        public static getTransitionTime(root: HTMLElement): number | null {
            if (!root || !(root instanceof Element)) return null;

            try {
                const style = getComputedStyle(root);
                const durations = Collapse.parseTimes(style.transitionDuration);
                const delays = Collapse.parseTimes(style.transitionDelay);

                if (durations.length === 0) return null;

                // The browser repeats the shorter of the two lists to the length of the longer one.
                const count = Math.max(durations.length, delays.length);
                let longest = 0;

                for (let i = 0; i < count; i++) {
                    const duration = durations[i % durations.length];
                    const delay = delays.length > 0 ? delays[i % delays.length] : 0;
                    longest = Math.max(longest, Math.max(0, duration) + Math.max(0, delay));
                }

                return longest;
            } catch {
                return null;
            }
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
