/** A style sheet or a grouping rule, either of which can hold and insert rules. */
type RuleParent = CSSStyleSheet | CSSGroupingRule;

/** The attribute that stands in for a forced pseudo-class, listing the states it forces. */
const marker = 'data-wb-pseudo';
/** A pseudo-class that can be forced, at the start of the rest of a selector. */
const pseudoClass = /^:(focus-visible|focus-within|focus|hover|active)(?![\w-])/;
/** Functional pseudo-classes whose arguments are walked through; inside :not() the states are left as they are. */
const functional = /^:(not|is|where|has|host|host-context|nth-[a-z-]+|lang|dir)\(/;
/** A media query that holds only on devices that can hover. */
const hoverOnly = /^\(\s*(any-)?hover\s*:\s*hover\s*\)$/;
/** The elements a state holder targets when it names none. */
const focusable = 'input:not([type=hidden]), textarea, select, button, a[href], [tabindex], [role=button], [role=tab], [role=switch], [role=checkbox], [role=radio], [role=combobox], [role=slider], [role=option]';
/** Style sheets that already have their forced-state copies. */
const rewritten = new WeakSet<CSSStyleSheet>();

/** Splits a selector list at its top-level commas. */
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

/** Replaces :hover, :active and the focus pseudo-classes with the marker attribute, outside :not(); null when nothing changed. */
function rewriteSelector(selector: string) {
    // One entry per open parenthesis: whether that point of the selector is inside a :not().
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

/** A rule's stateful selectors, rewritten, with its declarations; null when none of them uses a state. */
function rewriteRule(rule: CSSStyleRule) {
    const selectors = splitSelectors(rule.selectorText).map(rewriteSelector).filter((selector): selector is string => selector !== null);

    return selectors.length > 0 ? { selectors, declarations: rule.style.cssText } : null;
}

/** Inserts a rewritten copy at the index as one rule, or as one rule per selector, and answers how many rules went in. */
function insertCopy(parent: RuleParent, copy: { selectors: string[]; declarations: string }, index: number) {
    if (insert(parent, `${copy.selectors.join(',')}{${copy.declarations}}`, index)) return 1;

    // The browser rejects a whole selector list for one selector it can't parse, so each selector is tried on its own.
    let count = 0;

    copy.selectors.forEach(selector => {
        if (insert(parent, `${selector}{${copy.declarations}}`, index + count)) count++;
    });

    return count;
}

/** Inserts a rule, and answers false when the browser rejects it. */
function insert(parent: RuleParent, text: string, index: number) {
    try {
        parent.insertRule(text, index);
        return true;
    } catch {
        return false;
    }
}

/** Adds a forced-state copy after each rule that uses a state, and lifts hover-only media rules so a touch view shows them too. */
function rewriteRules(parent: RuleParent) {
    for (let index = 0; index < parent.cssRules.length; index++) {
        const rule = parent.cssRules[index];

        if (rule instanceof CSSStyleRule) {
            const copy = rewriteRule(rule);

            // The copy goes right after the original, and the loop steps over it.
            if (copy !== null) index += insertCopy(parent, copy, index + 1);

            continue;
        }

        if (rule instanceof CSSMediaRule && hoverOnly.test(rule.media.mediaText.trim())) {
            Array.from(rule.cssRules)
                .filter((inner): inner is CSSStyleRule => inner instanceof CSSStyleRule)
                .map(rewriteRule)
                .forEach(copy => {
                    if (copy !== null) index += insertCopy(parent, copy, index + 1);
                });

            continue;
        }

        // Other grouping rules, such as @media and @supports, are walked into.
        if ('cssRules' in rule && 'insertRule' in rule) {
            rewriteRules(rule as CSSGroupingRule);
        }
    }
}

/** Rewrites each style sheet of the page once; a sheet that can't be read is skipped. */
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

/** Adds states to an element's marker attribute. */
function addStates(element: Element, states: Iterable<string>) {
    const current = (element.getAttribute(marker) ?? '').split(' ').filter(state => state.length > 0);

    element.setAttribute(marker, Array.from(new Set([...current, ...states])).join(' '));
}

/** Shows hover, pressed and focus on the elements [data-wb-state] holders point at, without a pointer or keyboard, so they show on touch and in screenshots. */
export function forceStates() {
    rewriteSheets();

    document.querySelectorAll<HTMLElement>('[data-wb-state]').forEach(holder => {
        const states = (holder.dataset.wbState ?? '').split(' ').filter(state => state.length > 0);
        // The holder names its target with data-wb-target and data-wb-index (from 1), or gets its first focusable element.
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

        // The ancestors up to the holder get hover and focus-within, as they would from a real pointer or focus.
        for (let element = target.parentElement; element !== null && element !== holder.parentElement; element = element.parentElement) {
            addStates(element, around);
        }

        // Components that follow focus through events get focus events as well.
        if (states.includes('focus')) {
            target.dispatchEvent(new FocusEvent('focus'));
            target.dispatchEvent(new FocusEvent('focusin', { bubbles: true }));
        }
    });
}
