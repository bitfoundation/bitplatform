import { customProperties } from './frames';

/** The part of an axe-core violation the checks report. */
type AxeResult = { id: string; impact: string | null; help: string; helpUrl: string; nodes: { target: string[] }[] };

/** The load of axe-core, which happens the first time the checks run. */
let axeLoading: Promise<void> | null = null;

/** Loads axe-core into this page once. */
function loadAxe() {
    if ((window as any).axe) return Promise.resolve();

    axeLoading ??= new Promise<void>((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'scripts/axe.min.js';
        script.onload = () => resolve();
        // A failed load is forgotten, so the next run tries again.
        script.onerror = () => {
            axeLoading = null;
            script.remove();
            reject(new Error('The accessibility checker could not be loaded.'));
        };
        document.head.appendChild(script);
    });

    return axeLoading;
}

/** Runs axe-core's accessibility rules on this page, most severe first, and the search for colors outside the theme. */
export async function runChecks() {
    await loadAxe();

    // The Workbench's own overlay is left out of the check.
    const results = await (window as any).axe.run({ exclude: [['.wb-layer']] }, { resultTypes: ['violations'] });
    const order = ['critical', 'serious', 'moderate', 'minor'];

    const accessibility = (results.violations as AxeResult[])
        .map(violation => ({
            rule: violation.id,
            impact: violation.impact ?? 'minor',
            help: violation.help,
            helpUrl: violation.helpUrl,
            count: violation.nodes.length,
            targets: violation.nodes.slice(0, 5).map(node => node.target.join(' '))
        }))
        .sort((a, b) => order.indexOf(a.impact) - order.indexOf(b.impact));

    return { accessibility, theme: checkTheme() };
}

/** Finds the text, background and border colors that match no theme color, grouped and counted, 30 at most. */
function checkTheme() {
    const themeColors = new Set<string>();
    const tints: Rgba[] = [];
    const neutrals: Rgba[] = [[1, 1, 1, 1], [0, 0, 0, 1]];
    const probe = document.createElement('span');
    probe.style.display = 'none';
    document.body.appendChild(probe);

    // Turns any color value into its computed form through a hidden probe element.
    const resolve = (value: string) => {
        probe.style.color = '';
        probe.style.color = value;
        return getComputedStyle(probe).color;
    };

    // Each theme color counts as is; the background and foreground ones are also neutrals a tint can be mixed with.
    for (const name of customProperties(document, name => name.startsWith('--bit-clr-'))) {
        const color = resolve(`var(${name})`);
        const parsed = parseColor(color);

        themeColors.add(color);

        if (!parsed) continue;

        tints.push(parsed);
        if (name.startsWith('--bit-clr-bg-') || name.startsWith('--bit-clr-fg-')) neutrals.push(parsed);
    }

    const findings = new Map<string, { property: string; color: string; count: number; example: string }>();
    // bit components style themselves, so their elements are left out.
    const isOwnedByComponent = (element: Element) => Array.from(element.classList).some(name => name.startsWith('bit-'));

    // Only visible elements outside media, native pickers and nested frames are checked.
    for (const element of Array.from(document.body.querySelectorAll('*'))) {
        if (element.closest('.wb-layer, svg, canvas, iframe, img, video, select, option') || isOwnedByComponent(element)) continue;

        const rect = element.getBoundingClientRect();
        if (rect.width === 0 || rect.height === 0) continue;

        const style = getComputedStyle(element);
        if (style.visibility === 'hidden') continue;

        const parent = element.parentElement ? getComputedStyle(element.parentElement) : null;
        const hasText = Array.from(element.childNodes).some(node => node.nodeType === Node.TEXT_NODE && node.textContent?.trim());

        // A text color counts only where the element sets its own, so inherited text isn't counted again.
        const checks: [string, string][] = [];
        if (hasText && style.color !== parent?.color) checks.push(['color', style.color]);
        if (style.backgroundColor !== 'rgba(0, 0, 0, 0)') checks.push(['background-color', style.backgroundColor]);
        if (parseFloat(style.borderTopWidth) > 0 && style.borderTopStyle !== 'none') checks.push(['border-color', style.borderTopColor]);

        // A color the page puts in --data-color stands for data and is not a finding.
        const dataColor = style.getPropertyValue('--data-color').trim();
        const dataValue = dataColor ? resolve(dataColor) : null;

        for (const [property, color] of checks) {
            if (themeColors.has(color) || color === 'rgba(0, 0, 0, 0)' || color === dataValue || isThemeMix(color, tints, neutrals)) continue;

            const key = `${property}|${color}`;
            const finding = findings.get(key);

            if (finding) {
                finding.count++;
            } else {
                findings.set(key, { property, color, count: 1, example: describe(element) });
            }
        }
    }

    probe.remove();

    return Array.from(findings.values()).sort((a, b) => b.count - a.count).slice(0, 30);
}

/** A color as red, green, blue and alpha, each from 0 to 1. */
type Rgba = [number, number, number, number];

/** Parses a computed rgb(), rgba() or color(srgb ...) value. */
export function parseColor(value: string): Rgba | null {
    const legacy = value.match(/^rgba?\(([^)]+)\)$/);
    if (legacy) {
        const [r, g, b, a = 1] = legacy[1].split(/[\s,/]+/).filter(Boolean).map(Number);
        return [r / 255, g / 255, b / 255, a];
    }

    const modern = value.match(/^color\(srgb ([^)]+)\)$/);
    if (modern) {
        const [r, g, b, a = 1] = modern[1].split(/[\s/]+/).filter(Boolean).map(Number);
        return [r, g, b, a];
    }

    return null;
}

/** Whether a color is a theme color, or a theme color mixed with white, black or a theme background or foreground. */
function isThemeMix(value: string, tints: Rgba[], neutrals: Rgba[]) {
    const color = parseColor(value);
    if (!color) return false;

    const near = (x: number[], y: number[]) => Math.abs(x[0] - y[0]) < 0.02 && Math.abs(x[1] - y[1]) < 0.02 && Math.abs(x[2] - y[2]) < 0.02;

    return tints.some(tint => near(tint, color) || neutrals.some(neutral => {
        const delta = [tint[0] - neutral[0], tint[1] - neutral[1], tint[2] - neutral[2]];
        const length = delta[0] * delta[0] + delta[1] * delta[1] + delta[2] * delta[2];
        if (length < 0.0001) return false;

        // Projects the color onto the line from the neutral to the tint, and checks that it lands on that line.
        const t = ((color[0] - neutral[0]) * delta[0] + (color[1] - neutral[1]) * delta[1] + (color[2] - neutral[2]) * delta[2]) / length;

        return t >= 0 && t <= 1 && near([neutral[0] + delta[0] * t, neutral[1] + delta[1] * t, neutral[2] + delta[2] * t], color);
    }));
}

/** A short label for an element: its tag, two classes and the start of its text. */
function describe(element: Element) {
    const classes = Array.from(element.classList).filter(name => !name.startsWith('b-')).slice(0, 2);
    const text = (element as HTMLElement).innerText?.trim().replace(/\s+/g, ' ').slice(0, 40);

    return `${element.tagName.toLowerCase()}${classes.map(name => `.${name}`).join('')}${text ? ` “${text}”` : ''}`;
}

/** Scrolls to an element and flashes an outline around it; false when the selector finds nothing. */
export function highlight(selector: string) {
    let element: Element | null = null;

    try {
        element = document.querySelector(selector);
    } catch {
    }

    if (!element) return false;

    element.scrollIntoView({ block: 'center', behavior: 'instant' as ScrollBehavior });
    (element as HTMLElement).animate([{ outline: '3px solid var(--bit-clr-err)', outlineOffset: '2px' }, { outline: '3px solid transparent', outlineOffset: '2px' }], { duration: 1600, iterations: 2 });

    return true;
}
