import { customProperties } from './frames';

type AxeResult = { id: string; impact: string | null; help: string; helpUrl: string; nodes: { target: string[] }[] };

let axeLoading: Promise<void> | null = null;

function loadAxe() {
    if ((window as any).axe) return Promise.resolve();

    axeLoading ??= new Promise<void>((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'scripts/axe.min.js';
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('The accessibility checker could not be loaded.'));
        document.head.appendChild(script);
    });

    return axeLoading;
}

export async function runChecks() {
    await loadAxe();

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

function checkTheme() {
    const themeColors = new Set<string>();
    const probe = document.createElement('span');
    probe.style.display = 'none';
    document.body.appendChild(probe);

    for (const name of customProperties(document, name => name.startsWith('--bit-clr-'))) {
        probe.style.color = `var(${name})`;
        themeColors.add(getComputedStyle(probe).color);
    }

    probe.remove();

    const findings = new Map<string, { property: string; color: string; count: number; example: string }>();
    const isOwnedByComponent = (element: Element) => Array.from(element.classList).some(name => name.startsWith('bit-'));

    for (const element of Array.from(document.body.querySelectorAll('*'))) {
        if (element.closest('.wb-layer, svg, canvas, iframe, img, video') || isOwnedByComponent(element)) continue;

        const rect = element.getBoundingClientRect();
        if (rect.width === 0 || rect.height === 0) continue;

        const style = getComputedStyle(element);
        if (style.visibility === 'hidden') continue;

        const parent = element.parentElement ? getComputedStyle(element.parentElement) : null;
        const hasText = Array.from(element.childNodes).some(node => node.nodeType === Node.TEXT_NODE && node.textContent?.trim());

        const checks: [string, string][] = [];
        if (hasText && style.color !== parent?.color) checks.push(['color', style.color]);
        if (style.backgroundColor !== 'rgba(0, 0, 0, 0)') checks.push(['background-color', style.backgroundColor]);
        if (parseFloat(style.borderTopWidth) > 0 && style.borderTopStyle !== 'none') checks.push(['border-color', style.borderTopColor]);

        for (const [property, color] of checks) {
            if (themeColors.has(color) || color === 'rgba(0, 0, 0, 0)') continue;

            const key = `${property}|${color}`;
            const finding = findings.get(key);

            if (finding) {
                finding.count++;
            } else {
                findings.set(key, { property, color, count: 1, example: describe(element) });
            }
        }
    }

    return Array.from(findings.values()).sort((a, b) => b.count - a.count).slice(0, 30);
}

function describe(element: Element) {
    const classes = Array.from(element.classList).filter(name => !name.startsWith('b-')).slice(0, 2);
    const text = (element as HTMLElement).innerText?.trim().replace(/\s+/g, ' ').slice(0, 40);

    return `${element.tagName.toLowerCase()}${classes.map(name => `.${name}`).join('')}${text ? ` “${text}”` : ''}`;
}

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
