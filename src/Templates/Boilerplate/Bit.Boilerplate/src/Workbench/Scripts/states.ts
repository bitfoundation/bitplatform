type RuleParent = CSSStyleSheet | CSSGroupingRule;

const marker = 'data-wb-pseudo';
const pseudoClass = /^:(focus-visible|focus-within|focus|hover|active)(?![\w-])/;
const functional = /^:(not|is|where|has|host|host-context|nth-[a-z-]+|lang|dir)\(/;
const hoverOnly = /^\(\s*(any-)?hover\s*:\s*hover\s*\)$/;
const focusable = 'input:not([type=hidden]), textarea, select, button, a[href], [tabindex], [role=button], [role=tab], [role=switch], [role=checkbox], [role=radio], [role=combobox], [role=slider], [role=option]';
const rewritten = new WeakSet<CSSStyleSheet>();

function splitSelectors(text: string) {
    const parts: string[] = [];
    let depth = 0;
    let start = 0;

    for (let index = 0; index < text.length; index++) {
        const character = text[index];

        if (character === '(' || character === '[') depth++;
        else if (character === ')' || character === ']') depth--;
        else if (character === ',' && depth === 0) {
            parts.push(text.slice(start, index));
            start = index + 1;
        }
    }

    parts.push(text.slice(start));

    return parts.map(part => part.trim()).filter(part => part.length > 0);
}

function rewriteSelector(selector: string) {
    const negated: boolean[] = [];
    let result = '';
    let changed = false;

    for (let index = 0; index < selector.length;) {
        const rest = selector.slice(index);
        const call = functional.exec(rest);

        if (call) {
            negated.push(call[1] === 'not' || negated.includes(true));
            result += call[0];
            index += call[0].length;
            continue;
        }

        const character = selector[index];

        if (character === '[') {
            const end = selector.indexOf(']', index);
            const stop = end < 0 ? selector.length : end + 1;
            result += selector.slice(index, stop);
            index = stop;
            continue;
        }

        if (rest.startsWith('::')) {
            result += '::';
            index += 2;
            continue;
        }

        const state = character === ':' ? pseudoClass.exec(rest) : null;

        if (state && negated.includes(true) === false) {
            result += `[${marker}~="${state[1]}"]`;
            index += state[0].length;
            changed = true;
            continue;
        }

        if (character === '(') negated.push(negated.includes(true));
        if (character === ')') negated.pop();

        result += character;
        index++;
    }

    return changed ? result : null;
}

function rewriteRule(rule: CSSStyleRule) {
    const selectors = splitSelectors(rule.selectorText).map(rewriteSelector).filter((selector): selector is string => selector !== null);

    return selectors.length > 0 ? `${selectors.join(',')}{${rule.style.cssText}}` : null;
}

function insert(parent: RuleParent, text: string, index: number) {
    try {
        parent.insertRule(text, index);
        return true;
    } catch {
        return false;
    }
}

function rewriteRules(parent: RuleParent) {
    for (let index = 0; index < parent.cssRules.length; index++) {
        const rule = parent.cssRules[index];

        if (rule instanceof CSSStyleRule) {
            const text = rewriteRule(rule);

            if (text !== null && insert(parent, text, index + 1)) index++;

            continue;
        }

        if (rule instanceof CSSMediaRule && hoverOnly.test(rule.media.mediaText.trim())) {
            const texts = Array.from(rule.cssRules)
                .filter((inner): inner is CSSStyleRule => inner instanceof CSSStyleRule)
                .map(rewriteRule)
                .filter((text): text is string => text !== null);

            texts.forEach(text => {
                if (insert(parent, text, index + 1)) index++;
            });

            continue;
        }

        if ('cssRules' in rule && 'insertRule' in rule) {
            rewriteRules(rule as CSSGroupingRule);
        }
    }
}

function rewriteSheets() {
    Array.from(document.styleSheets).forEach(sheet => {
        if (rewritten.has(sheet)) return;

        rewritten.add(sheet);

        try {
            rewriteRules(sheet);
        } catch {
            return;
        }
    });
}

function addStates(element: Element, states: Iterable<string>) {
    const current = (element.getAttribute(marker) ?? '').split(' ').filter(state => state.length > 0);

    element.setAttribute(marker, Array.from(new Set([...current, ...states])).join(' '));
}

export function forceStates() {
    rewriteSheets();

    document.querySelectorAll<HTMLElement>('[data-wb-state]').forEach(holder => {
        const states = (holder.dataset.wbState ?? '').split(' ').filter(state => state.length > 0);
        const target = holder.querySelectorAll<HTMLElement>(holder.dataset.wbTarget ?? focusable)[Math.max(0, Number(holder.dataset.wbIndex ?? 1) - 1)];

        if (target === undefined || target.hasAttribute(marker)) return;

        const own = new Set<string>();
        const around = new Set<string>();

        if (states.includes('hover') || states.includes('active')) {
            own.add('hover');
            around.add('hover');
        }

        if (states.includes('active')) {
            own.add('active');
            around.add('active');
        }

        if (states.includes('focus')) {
            ['focus', 'focus-visible', 'focus-within'].forEach(state => own.add(state));
            around.add('focus-within');
        }

        addStates(target, own);

        for (let element = target.parentElement; element !== null && element !== holder.parentElement; element = element.parentElement) {
            addStates(element, around);
        }

        if (states.includes('focus')) {
            target.dispatchEvent(new FocusEvent('focus'));
            target.dispatchEvent(new FocusEvent('focusin', { bubbles: true }));
        }
    });
}
