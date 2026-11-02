import { describe, it, expect, afterEach } from 'vitest';
import { commitStylesChecked } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

// An element whose inline style is a plain map, serialised into its style attribute.
function element(style) {
    const props = new Map(Object.entries(style));
    return {
        props,
        getAttribute: () => [...props].map(([k, v]) => `${k}: ${v};`).join(' '),
        style: { setProperty: (k, v) => props.set(k, v) },
    };
}

// An animation on it whose commitStyles writes `commits` - or nothing, as WebKit's sometimes does.
function animation(el, keyframes, progress, commits) {
    return {
        effect: {
            target: el,
            getKeyframes: () => keyframes,
            getComputedTiming: () => ({ progress }),
        },
        commitStyles: () => { for (const [k, v] of Object.entries(commits)) el.props.set(k, v); },
    };
}

const fade = [
    { offset: null, computedOffset: 0, easing: 'linear', composite: 'auto', opacity: '0', transform: 'translate(0px,12px)' },
    { offset: null, computedOffset: 1, easing: 'linear', composite: 'auto', opacity: '1', transform: 'none' },
];

describe('commitStylesChecked', () => {
    afterEach(() => { delete globalThis.getComputedStyle; });

    it('leaves a commit that wrote the values alone', () => {
        const el = element({ opacity: '0' });
        commitStylesChecked(animation(el, fade, 1, { opacity: '1', transform: 'translate(0px)' }));
        expect(el.props.get('opacity')).toBe('1');
        expect(el.props.get('transform')).toBe('translate(0px)');
    });

    it('writes the keyframe a finished animation ended on when the commit wrote nothing', () => {
        const el = element({ transform: 'translate(0px,12px)', opacity: '0', display: 'inline-block' });
        commitStylesChecked(animation(el, fade, 1, {}));
        expect(el.props.get('opacity')).toBe('1');
        expect(el.props.get('transform')).toBe('none');
        expect(el.props.get('display')).toBe('inline-block');
    });

    it('ends on the first keyframe when the animation played back to it', () => {
        const el = element({ opacity: '0.5' });
        commitStylesChecked(animation(el, fade, 0, {}));
        expect(el.props.get('opacity')).toBe('0');
    });

    it('writes what an animation stopped part-way shows', () => {
        const el = element({ opacity: '0' });
        globalThis.getComputedStyle = () => ({ getPropertyValue: p => ({ opacity: '0.42', transform: 'matrix(1, 0, 0, 1, 0, 7)' })[p] ?? '' });
        commitStylesChecked(animation(el, fade, 0.42, {}));
        expect(el.props.get('opacity')).toBe('0.42');
        expect(el.props.get('transform')).toBe('matrix(1, 0, 0, 1, 0, 7)');
    });

    it('names properties as CSS does', () => {
        const el = element({});
        const keyframes = [
            { computedOffset: 0, backgroundColor: 'rgb(0, 0, 0)', '--glow': '0' },
            { computedOffset: 1, backgroundColor: 'rgb(255, 0, 0)', '--glow': '1' },
        ];
        commitStylesChecked(animation(el, keyframes, 1, {}));
        expect(el.props.get('background-color')).toBe('rgb(255, 0, 0)');
        expect(el.props.get('--glow')).toBe('1');
    });

    it('throws as commitStyles does', () => {
        const el = element({});
        const anim = animation(el, fade, 1, {});
        anim.commitStyles = () => { throw new Error('InvalidStateError'); };
        expect(() => commitStylesChecked(anim)).toThrow('InvalidStateError');
    });
});
