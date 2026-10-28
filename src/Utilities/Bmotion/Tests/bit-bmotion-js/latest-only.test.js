import { describe, it, expect } from 'vitest';
import { latestOnly, coalesce } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

// A send to .NET that the test settles by hand - a Blazor Server round trip, held open.
function controlledSend() {
    const sent = [];
    const settle = [];
    const send = value => {
        sent.push(value);
        return new Promise(resolve => settle.push(resolve));
    };
    return { sent, send, settleNext: async () => { settle.shift()(); await new Promise(r => setTimeout(r, 0)); } };
}

// Scroll positions stream to .NET: one call at a time, and while one is on its way the newer values replace each
// other - the stream never queues up behind a slow connection, and what arrives next is always the freshest.
describe('latestOnly', () => {
    it('sends the first value at once', () => {
        const { sent, send } = controlledSend();
        latestOnly(send)(1);
        expect(sent).toEqual([1]);
    });

    it('holds back values while one is on its way, then sends only the latest', async () => {
        const { sent, send, settleNext } = controlledSend();
        const push = latestOnly(send);
        push(1);
        push(2);
        push(3);
        expect(sent).toEqual([1]);
        await settleNext();
        expect(sent).toEqual([1, 3]);
        await settleNext();
        expect(sent).toEqual([1, 3]);
    });

    it('carries on after a send fails', async () => {
        let first = true;
        const sent = [];
        const push = latestOnly(value => {
            sent.push(value);
            if (first) { first = false; return Promise.reject(new Error('circuit gone')); }
            return Promise.resolve();
        });
        push(1);
        push(2);
        await new Promise(r => setTimeout(r, 0));
        expect(sent).toEqual([1, 2]);
    });
});

// Pan moves add up: folded while one is on its way, and flushed before the end of the gesture.
describe('coalesce', () => {
    const sum = (a, b) => ({ delta: a.delta + b.delta, at: b.at });

    it('folds the values that arrive while one is on its way', async () => {
        const { sent, send, settleNext } = controlledSend();
        const moves = coalesce(send, sum);
        moves.push({ delta: 1, at: 1 });
        moves.push({ delta: 2, at: 3 });
        moves.push({ delta: 4, at: 7 });
        await settleNext();
        expect(sent).toEqual([{ delta: 1, at: 1 }, { delta: 6, at: 7 }], 'no movement is lost, and the position is the latest');
    });

    it('flushes what is waiting at once, so it goes before what follows', () => {
        const { sent, send } = controlledSend();
        const moves = coalesce(send, sum);
        moves.push({ delta: 1, at: 1 });
        moves.push({ delta: 2, at: 3 });
        moves.flush();
        expect(sent).toEqual([{ delta: 1, at: 1 }, { delta: 2, at: 3 }]);
        moves.flush();
        expect(sent.length).toBe(2, 'nothing waiting, nothing sent');
    });
});
