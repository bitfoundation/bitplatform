import { customProperties } from './frames';

/** The part of an axe-core violation the checks report. */
type AxeResult = {
    id: string;
    impact: string | null;
    help: string;
    helpUrl: string;
    nodes: { target: string[]; ancestry?: string[]; html?: string; failureSummary?: string }[];
};

/**
 * A press that brings part of the page into sight, with the control's place in the page and its name: picking a tab, or
 * opening what a control expands, like an accordion, a group of a nav or a dropdown.
 */
type Step = { selector: string; kind: 'tab' | 'expand'; label: string };

/** An element an accessibility rule fails on: its place in the page, its markup and what axe-core says is wrong with it. */
type CheckNode = { target: string; html: string; summary: string };

/** An accessibility rule the page breaks in one of the states the checks opened, with where that state is. */
type AccessibilityFinding = {
    rule: string;
    impact: string;
    help: string;
    helpUrl: string;
    count: number;
    nodes: CheckNode[];
    where: string | null;
    steps: Step[];
};

/** A color outside the theme, with an element that uses it and where that element is. */
type ThemeFinding = { property: string; color: string; count: number; example: string; target: string; where: string | null; steps: Step[] };

/** What a run reports as it goes: where it is, and how many states of the page it has checked. */
type CheckOptions = { onProgress?: (where: string, checked: number) => void };

/** The impacts from the most severe down, the order the findings are listed in. */
const impactOrder = ['critical', 'serious', 'moderate', 'minor'];

/** The most states one run checks, how deep it goes into parts inside parts, and how long it goes on before it reports what it found. */
const maxStates = 400;
const maxDepth = 4;
const maxDuration = 8 * 60 * 1000;

/** The load of axe-core, which happens the first time the checks run. */
let axeLoading: Promise<void> | null = null;

/** Whether a run is going through the page's tabs and panels, which the page then shouldn't report to the shell as the user's. */
let isSweeping = false;

/** Whether the checks are going through the page's tabs and panels right now. */
export const isSweepingNow = () => isSweeping;

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

/**
 * Runs axe-core's accessibility rules and the search for colors outside the theme on this page as it is, then on each
 * part it can bring into sight: every tab of each tab list and whatever each control with aria-expanded opens, inside
 * one another, the way the bit BlazorUI components mark them. Each part is closed again, so the page ends as it began.
 * A finding is reported once, where it was first seen, with the presses that lead there.
 */
export async function runChecks(options: CheckOptions = {}) {
    await loadAxe();

    const sweep = new Sweep(options.onProgress);

    isSweeping = true;

    try {
        await sweep.check([], null);
        await sweep.explore(document.body, [], 0);
    } finally {
        isSweeping = false;
    }

    return sweep.result();
}

/** One run of the checks over the states of the page, with what it found so far. */
class Sweep {
    /** The elements already reported, by rule and place, so an element found again in a later state isn't counted twice. */
    private readonly seenNodes = new Set<string>();
    private readonly seenColors = new Set<string>();
    private readonly accessibility = new Map<string, AccessibilityFinding>();
    private readonly theme = new Map<string, ThemeFinding>();
    private readonly started = performance.now();
    private readonly path = location.pathname;
    private checked = 0;
    private isCut = false;

    constructor(private readonly onProgress?: (where: string, checked: number) => void) {
    }

    /** Whether the run has to stop: it reached its limits, or a press took the page somewhere else. */
    private get isOver() {
        const isOver = this.checked >= maxStates || performance.now() - this.started > maxDuration || location.pathname !== this.path;

        this.isCut ||= isOver;

        return isOver;
    }

    /**
     * Checks the page as it is now, reached by the presses of the path; with roots, only the parts of the page that
     * changed. The forced states are inert, so nobody can use them and axe-core leaves them out, the way a screen reader
     * does; they are there for their colors, so their contrast alone is checked, with them made usable for that check.
     */
    async check(path: Step[], roots: Element[] | null) {
        this.checked++;
        this.onProgress?.(whereOf(path) ?? '', this.checked);

        // A color caught halfway through a transition isn't one the page shows, and what the page does once a part has
        // drawn, like forcing the hover of its samples, can start one after the part settled.
        await animationsEnded();

        // The Workbench's own overlay is left out of the check. Targets name an element by its place in the page, as its
        // id can be new each time it is shown.
        const exclude = [['.wb-layer']];
        const options = { resultTypes: ['violations'], ancestry: true };
        const violations: AxeResult[] = (await (window as any).axe.run(roots ? { include: roots, exclude } : { exclude }, options)).violations;

        const forced = Array.from(document.querySelectorAll('[data-wb-state][inert]')).filter(element => !roots || roots.some(root => root.contains(element) || element.contains(root)));

        if (forced.length > 0) {
            forced.forEach(element => element.removeAttribute('inert'));

            try {
                const contrast = { ...options, runOnly: { type: 'rule', values: ['color-contrast'] } };
                violations.push(...(await (window as any).axe.run({ include: forced, exclude }, contrast)).violations);
            } finally {
                forced.forEach(element => element.setAttribute('inert', ''));
            }
        }

        for (const violation of violations) {
            for (const node of violation.nodes) {
                const target = (node.ancestry ?? node.target).join(' ');
                const seen = `${violation.id}|${target}`;

                if (this.seenNodes.has(seen)) continue;
                this.seenNodes.add(seen);

                const steps = stepsTo(query(target), path);
                const where = whereOf(steps);
                const key = `${violation.id}|${where}`;
                let finding = this.accessibility.get(key);

                if (!finding) {
                    finding = { rule: violation.id, impact: violation.impact ?? 'minor', help: violation.help, helpUrl: violation.helpUrl, count: 0, nodes: [], where, steps };
                    this.accessibility.set(key, finding);
                }

                finding.count++;

                if (finding.nodes.length < 5) {
                    finding.nodes.push({ target, html: (node.html ?? '').slice(0, 300), summary: (node.failureSummary ?? '').replace(/\s+/g, ' ').trim() });
                }
            }
        }

        for (const { property, color, element } of colorsOutsideTheme(roots)) {
            const target = placeOf(element);
            const seen = `${property}|${color}|${target}`;

            if (this.seenColors.has(seen)) continue;
            this.seenColors.add(seen);

            const key = `${property}|${color}`;
            const finding = this.theme.get(key);

            if (finding) {
                finding.count++;
            } else {
                const steps = stepsTo(element, path);
                this.theme.set(key, { property, color, count: 1, example: describe(element), target, where: whereOf(steps), steps });
            }
        }
    }

    /**
     * Opens each part of the scope one by one, checks it and goes into it: the controls with aria-expanded first, then
     * the tabs of each tab list, as going through tabs draws their panels anew. A part inside another part is left for
     * when the scope of that part is explored.
     */
    async explore(scope: Element, path: Step[], depth: number) {
        if (depth >= maxDepth) return;

        const { tabLists, toggles } = controlsIn(scope);
        // Opening one part can draw the others anew, so each is found again by its step before it is pressed.
        const steps = toggles.map(stepOf);
        // What is open from the start can close as something else opens, like a section of an accordion that keeps one
        // open, so it is opened again afterwards.
        const open = toggles.filter(toggle => toggle.getAttribute('aria-expanded') === 'true').map(stepOf);

        try {
            for (const step of steps) {
                if (this.isOver) return;

                const toggle = locate(step);
                if (toggle && isUsable(toggle)) await this.expand(toggle, path, depth);
            }
        } finally {
            for (const step of open) {
                const toggle = locate(step);
                if (toggle && toggle.getAttribute('aria-expanded') !== 'true' && location.pathname === this.path) await press(toggle, 300);
            }
        }

        for (const tabList of tabLists) {
            if (this.isOver) return;
            await this.showTabs(tabList, path, depth);
        }
    }

    /** Opens what a control expands, checks it and what is in it, and closes it again; a popup's content isn't gone into. */
    private async expand(toggle: HTMLElement, path: Step[], depth: number) {
        const step = stepOf(toggle);
        const reached = [...path, step];
        const isPopup = (toggle.getAttribute('aria-haspopup') ?? 'false') !== 'false';

        // Open from the start, so it was checked with the page; only what is in it is left.
        if (toggle.getAttribute('aria-expanded') === 'true') {
            const region = regionOf(toggle);
            if (region && isPopup === false) await this.explore(region, reached, depth + 1);
            return;
        }

        const roots = await press(toggle, 300);
        const opened = locate(step);

        if (opened?.getAttribute('aria-expanded') !== 'true' || location.pathname !== this.path) return;

        try {
            await this.check(reached, roots);

            const region = regionOf(opened);
            if (region && isPopup === false) await this.explore(region, reached, depth + 1);
        } finally {
            await collapse(step, isPopup);
        }
    }

    /** Shows each tab of a tab list, checks it and goes into its panel, then shows the tab that was shown before. */
    private async showTabs(tabList: Element, path: Step[], depth: number) {
        const tabs = Array.from(tabList.querySelectorAll<HTMLElement>('[role="tab"]')).filter(tab => tab.closest('[role="tablist"]') === tabList && isUsable(tab));
        const shown = tabs.find(tab => tab.getAttribute('aria-selected') === 'true');
        const shownStep = shown ? stepOf(shown) : null;
        const others = tabs.filter(tab => tab !== shown).map(stepOf);

        // The tab shown from the start was checked with the page; only what is in its panel is left.
        if (shown && shownStep) {
            const panel = panelOf(shown);
            if (panel) await this.explore(panel, [...path, shownStep], depth + 1);
        }

        try {
            for (const step of others) {
                if (this.isOver) return;

                // A tab that didn't show in time is left out, and the run reported as not complete, rather than checked as the one before it.
                if (await show(step) === false) {
                    this.isCut = true;
                    continue;
                }

                if (location.pathname !== this.path) return;

                await this.check([...path, step], null);

                const panel = panelOf(locate(step));
                if (panel) await this.explore(panel, [...path, step], depth + 1);
            }
        } finally {
            if (shownStep && others.length > 0 && location.pathname === this.path) {
                await show(shownStep);
            }
        }
    }

    /** The findings, most severe first and in the order their states were checked, the 30 most used colors, and whether every state was checked. */
    result() {
        // A stable sort, so the findings of one impact stay in the order the page was gone through.
        const accessibility = Array.from(this.accessibility.values()).sort((a, b) => impactOrder.indexOf(a.impact) - impactOrder.indexOf(b.impact));
        const theme = Array.from(this.theme.values()).sort((a, b) => b.count - a.count).slice(0, 30);

        return { accessibility, theme, checked: this.checked, isComplete: this.isCut === false };
    }
}

/** The tab lists and the controls with aria-expanded of a scope, leaving out those inside a part of it that opens. */
function controlsIn(scope: Element) {
    const regionIds = new Set(Array.from(document.querySelectorAll('[aria-controls]')).flatMap(element => element.getAttribute('aria-controls')!.split(/\s+/)));
    const isDirect = (element: Element) => {
        for (let current = element.parentElement; current && current !== scope; current = current.parentElement) {
            if (current.getAttribute('role') === 'tabpanel' || (current.id && regionIds.has(current.id))) return false;
        }

        return true;
    };

    const tabLists = Array.from(scope.querySelectorAll('[role="tablist"]')).filter(tabList => isDirect(tabList) && tabList.closest('.wb-layer, [data-wb-state], [inert]') === null);
    const toggles = Array.from(scope.querySelectorAll<HTMLElement>('[aria-expanded]')).filter(toggle => toggle.getAttribute('role') !== 'tab' && isDirect(toggle) && isUsable(toggle));

    return { tabLists, toggles };
}

/** Whether a control can be pressed: shown, enabled, not a link that leaves the page, and not in a forced state or the Workbench's overlay. */
function isUsable(control: Element) {
    if (control.closest('.wb-layer, [data-wb-state], [inert], a[href], [aria-disabled="true"]') || (control as HTMLButtonElement).disabled) return false;

    const rect = control.getBoundingClientRect();

    return rect.width > 0 && rect.height > 0 && getComputedStyle(control).visibility !== 'hidden';
}

/**
 * Closes what a step opened, then waits for the page to settle without its closing animation, as nothing is checked
 * then: a popup with Escape, the way its pattern closes it, and anything else with a second press; the other way when
 * the first one leaves it open.
 */
async function collapse(step: Step, isPopup: boolean) {
    const escape = (control: Element) => [control, document.activeElement ?? document.body].forEach(target => target.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', code: 'Escape', bubbles: true, cancelable: true })));
    const click = (control: HTMLElement) => control.click();

    for (const close of isPopup ? [escape, click] : [click, escape]) {
        const control = locate(step);
        if (control?.getAttribute('aria-expanded') !== 'true') return;

        const watch = new Watch();
        close(control);
        await watch.settle(200, { waitsForAnimations: false });
    }
}

/** Shows a tab and waits until it says it is shown and its panel has settled; returns whether it is shown. */
async function show(step: Step) {
    const tab = locate(step);
    const isShown = () => locate(step)?.getAttribute('aria-selected') === 'true';

    if (tab && isShown() === false) {
        const watch = new Watch();
        tab.click();
        await watch.settle(500, { until: isShown });
    }

    return isShown();
}

/** Presses a control the way a click does and waits until the page has settled; returns the parts that changed, or null for the whole page. */
async function press(control: HTMLElement, quiet: number) {
    const watch = new Watch();

    control.click();

    return await watch.settle(quiet);
}

/** The elements of the page that change while it is watched. */
class Watch {
    private readonly changed = new Set<Element>();
    private readonly observer = new MutationObserver(records => this.take(records));
    private lastChange = 0;

    constructor() {
        this.observer.observe(document.documentElement, { subtree: true, childList: true, attributes: true, characterData: true });
    }

    /** Notes the elements a change added or changed; what only lost children has nothing new to check. */
    private take(records: MutationRecord[]) {
        for (const record of records) {
            const added = Array.from(record.addedNodes).map(node => node instanceof Element ? node : record.target as Element);
            const elements = record.type === 'childList' ? added : [record.target instanceof Element ? record.target : record.target.parentElement];

            for (const element of record.type === 'childList' && added.length === 0 ? [record.target as Element] : elements) {
                if (!element || element.closest('.wb-layer')) continue;

                this.lastChange = performance.now();

                // A class or style on the page itself, like a scroll lock, or a removal from it doesn't say which part changed.
                if (element === document.body || element === document.documentElement) continue;

                this.changed.add(element);
            }
        }
    }

    /**
     * Waits until nothing changed for the quiet time, and for the animations that end, 8 seconds at most; a first change
     * gets a server round trip to arrive, and a change that is sure to come, like a tab being shown, is waited for until
     * it has. Returns the outermost elements that changed, or null when that is most of the page, so the whole page is
     * checked.
     */
    async settle(quiet: number, { until, waitsForAnimations = true }: { until?: () => boolean; waitsForAnimations?: boolean } = {}) {
        const start = performance.now();

        while (performance.now() - start < 8000) {
            await delay(50);

            const now = performance.now();

            if (until) {
                if (until() && (this.lastChange === 0 || now - this.lastChange > quiet)) break;
            } else if (this.lastChange === 0 ? now - start > Math.max(quiet, 1500) : now - this.lastChange > quiet) {
                break;
            }
        }

        this.take(this.observer.takeRecords());
        this.observer.disconnect();

        if (waitsForAnimations) await animationsEnded();

        if (this.changed.size === 0 || this.changed.size > 400) return null;

        const connected = Array.from(this.changed).filter(element => element.isConnected);
        const roots = connected.filter(element => connected.some(other => other !== element && other.contains(element)) === false);

        return roots.length === 0 || roots.length > 25 ? null : roots;
    }
}

const delay = (milliseconds: number) => new Promise(resolve => setTimeout(resolve, milliseconds));

/** Waits for the animations and transitions that end, 1.5 seconds at most; a spinner's, which never ends, isn't waited for. */
async function animationsEnded() {
    const ending = document.getAnimations().filter(animation => animation.playState === 'running' && Number.isFinite(animation.effect?.getComputedTiming().endTime ?? Infinity));

    if (ending.length > 0) await Promise.race([Promise.all(ending.map(animation => animation.finished.catch(() => undefined))), delay(1500)]);
}

/** The panel a tab shows. */
function panelOf(tab: Element | null) {
    if (!tab) return null;

    return regionOf(tab) ?? (tab.id ? document.querySelector(`[role="tabpanel"][aria-labelledby~="${CSS.escape(tab.id)}"]`) : null);
}

/** The part of the page a control opens, by its aria-controls. */
function regionOf(control: Element) {
    for (const id of (control.getAttribute('aria-controls') ?? '').split(/\s+/).filter(Boolean)) {
        const region = document.getElementById(id);
        if (region) return region;
    }

    return null;
}

/** The step of pressing a control. */
function stepOf(control: Element): Step {
    return { selector: placeOf(control), kind: control.getAttribute('role') === 'tab' ? 'tab' : 'expand', label: nameOf(control) };
}

/** Finds a step's control by its place, or by its name when the page has moved, as on another device. */
function locate(step: Step) {
    const isKind = (element: Element) => step.kind === 'tab' ? element.getAttribute('role') === 'tab' : element.hasAttribute('aria-expanded');
    const found = query(step.selector) as HTMLElement | null;

    if (found && isKind(found) && nameOf(found) === step.label) return found;

    const candidates = Array.from(document.querySelectorAll<HTMLElement>(step.kind === 'tab' ? '[role="tab"]' : '[aria-expanded]'));

    return candidates.find(candidate => nameOf(candidate) === step.label && isUsable(candidate)) ?? (found && isKind(found) ? found : null);
}

/**
 * The presses that bring an element into sight: the tabs whose panels and the controls whose opened parts it sits in.
 * A popup's content is drawn outside them, at the end of the page, so for an element outside the panel of a tab the run
 * went through, or in no part at all, the path the run reached it through is taken, as that is what opens it.
 */
function stepsTo(element: Element | null, path: Step[]) {
    const steps: Step[] = [];

    for (let current = element; current && current !== document.body; current = current.parentElement) {
        if (!current.id) continue;

        const id = CSS.escape(current.id);
        const control = current.getAttribute('role') === 'tabpanel'
            ? document.querySelector(`[role="tab"][aria-controls~="${id}"]`) ?? labelledBy(current)
            : document.querySelector(`[aria-expanded="true"][aria-controls~="${id}"]`);

        if (control) steps.unshift(stepOf(control));
    }

    const isOutside = element !== null && path.some(step => step.kind === 'tab' && panelOf(locate(step))?.contains(element) === false);

    return isOutside || steps.length === 0 ? path : steps;
}

/** The tab that labels a tab panel. */
function labelledBy(panel: Element) {
    const id = panel.getAttribute('aria-labelledby')?.split(/\s+/)[0];
    const tab = id ? document.getElementById(id) : null;

    return tab?.getAttribute('role') === 'tab' ? tab : null;
}

/** Where the presses of a path lead, by their controls' names, or null for the page as it opens. */
function whereOf(steps: Step[]) {
    return steps.length > 0 ? steps.map(step => step.label).join(' › ') : null;
}

/** A control's name, as a screen reader would read it, from the first element that labels it, like a section's title without its description; 40 characters at most. */
function nameOf(control: Element) {
    const byIds = (ids: string | null) => ids?.split(/\s+/).map(id => document.getElementById(id)?.textContent?.trim()).find(Boolean);
    const label = control.id ? document.querySelector(`label[for="${CSS.escape(control.id)}"]`)?.textContent : null;
    const text = control.getAttribute('aria-label')?.trim()
        || byIds(control.getAttribute('aria-labelledby'))
        || (control as HTMLElement).innerText?.trim()
        || label?.trim()
        || control.getAttribute('title')?.trim()
        || control.getAttribute('placeholder')?.trim()
        || control.tagName.toLowerCase();
    const name = text.replace(/\s+/g, ' ');

    return name.length > 40 ? `${name.slice(0, 39)}…` : name;
}

/** An element's place in the page, as a path of nth-child steps from the body, which stays the same when it is drawn anew. */
function placeOf(element: Element) {
    const parts: string[] = [];

    for (let current = element; current !== document.body && current.parentElement; current = current.parentElement) {
        parts.unshift(`${current.tagName.toLowerCase()}:nth-child(${Array.prototype.indexOf.call(current.parentElement.children, current) + 1})`);
    }

    return ['html', 'body', ...parts].join(' > ');
}

/** The element a selector finds, or null when it finds none or isn't valid. */
function query(selector: string) {
    try {
        return document.querySelector(selector);
    } catch {
        return null;
    }
}

/** Finds the text, background and border colors that match no theme color, in the roots or in the whole page. */
function colorsOutsideTheme(roots: Element[] | null) {
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

    const found: { property: string; color: string; element: Element }[] = [];
    // bit components style themselves, so their elements are left out.
    const isOwnedByComponent = (element: Element) => Array.from(element.classList).some(name => name.startsWith('bit-'));
    const elements = roots ? roots.flatMap(root => [root, ...Array.from(root.querySelectorAll('*'))]) : Array.from(document.body.querySelectorAll('*'));

    // Only visible elements outside media, native pickers and nested frames are checked.
    for (const element of elements) {
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

            found.push({ property, color, element });
        }
    }

    probe.remove();

    return found;
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

/**
 * Brings a finding's element into sight: presses what leads to the part of the page it was found in, the way the user
 * would, and scrolls to it. False when it isn't there.
 */
export async function reveal(selector: string, steps: Step[] = []) {
    for (const step of steps) {
        const control = locate(step);
        if (!control) break;

        if (step.kind === 'tab') {
            await show(step);
        } else if (control.getAttribute('aria-expanded') !== 'true') {
            await press(control, 300);
        }
    }

    const element = query(selector);
    element?.scrollIntoView({ block: 'center', behavior: 'instant' as ScrollBehavior });

    return element !== null;
}

/** Brings a finding's element into sight and flashes an outline around it; false when it isn't there. */
export async function highlight(selector: string, steps: Step[] = []) {
    if (await reveal(selector, steps) === false) return false;

    (query(selector) as HTMLElement | null)?.animate([{ outline: '3px solid var(--bit-clr-err)', outlineOffset: '2px' }, { outline: '3px solid transparent', outlineOffset: '2px' }], { duration: 1600, iterations: 2 });

    return true;
}
