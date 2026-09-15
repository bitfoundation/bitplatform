var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Wiring nodes together and scheduling their AudioParams. Split from the factories because a
    // graph is built once and then driven: the two are used at different times and neither needs
    // the other's code.
    butil.webAudioParams = {
        connect,
        connectToDestination,
        disconnect,
        setParam,
        rampParam,
        cancelScheduledParam,
        setProperty,
        start,
        stopNode
    };

    function connect(fromId: string, toId: string) {
        const from = butil.webAudioNodes.nodeOf(fromId);
        const to = butil.webAudioNodes.nodeOf(toId);
        if (!from || !to) return false;
        try { from.connect(to); return true; } catch { return false; }
    }

    // Everything Butil-managed goes through the master gain rather than straight to the context's
    // destination, so one call can duck or mute the whole app.
    function connectToDestination(id: string) {
        const node = butil.webAudioNodes.nodeOf(id);
        const master = butil.webAudio.master();
        if (!node || !master) return false;
        try { node.connect(master); return true; } catch { return false; }
    }

    function disconnect(id: string) {
        const node = butil.webAudioNodes.nodeOf(id);
        if (!node) return false;
        try { node.disconnect(); return true; } catch { return false; }
    }

    function param(id: string, name: string) {
        const node = butil.webAudioNodes.nodeOf(id);
        if (!node) return null;
        // A worklet's declared parameters live in an AudioParamMap rather than on the node itself,
        // so they are only reachable through parameters.get() - the node's own properties are
        // still looked at first, since a built-in node keeps its params there.
        let value = node[name];
        if (!value && node.parameters?.get) {
            try { value = node.parameters.get(name); } catch { /* not a param of this worklet */ }
        }
        // An AudioParam is distinguished from a plain number property by having a setValueAtTime.
        return value && typeof value.setValueAtTime === 'function' ? value : null;
    }

    function setParam(id: string, name: string, value: number, atTime: number) {
        const target = param(id, name);
        const ctx = butil.webAudio.currentContext();
        if (!target || !ctx) return false;
        try {
            if (atTime > 0) target.setValueAtTime(value, ctx.currentTime + atTime);
            else target.value = value;
            return true;
        } catch {
            return false;
        }
    }

    function rampParam(id: string, name: string, value: number, seconds: number, exponential: boolean) {
        const target = param(id, name);
        const ctx = butil.webAudio.currentContext();
        if (!target || !ctx) return false;
        try {
            // Ramps start from the value at the last scheduled point, so anchoring "now" first is
            // what stops a ramp from starting at whatever was scheduled before it.
            target.cancelScheduledValues(ctx.currentTime);
            target.setValueAtTime(target.value, ctx.currentTime);
            // An exponential ramp cannot cross or reach zero, so a zero target is nudged to a value
            // below hearing rather than throwing.
            if (exponential) target.exponentialRampToValueAtTime(value === 0 ? 0.0001 : value, ctx.currentTime + seconds);
            else target.linearRampToValueAtTime(value, ctx.currentTime + seconds);
            return true;
        } catch {
            return false;
        }
    }

    function cancelScheduledParam(id: string, name: string) {
        const target = param(id, name);
        const ctx = butil.webAudio.currentContext();
        if (!target || !ctx) return false;
        try { target.cancelScheduledValues(ctx.currentTime); return true; } catch { return false; }
    }

    function setProperty(id: string, name: string, value: any) {
        const node = butil.webAudioNodes.nodeOf(id);
        if (!node) return false;
        try { node[name] = value; return true; } catch { return false; }
    }

    function start(id: string, when: number, offset: number, duration: number) {
        const node = butil.webAudioNodes.nodeOf(id);
        const ctx = butil.webAudio.currentContext();
        if (!node?.start || !ctx) return false;
        const at = ctx.currentTime + (when > 0 ? when : 0);
        try {
            if (duration > 0) node.start(at, offset > 0 ? offset : 0, duration);
            else if (offset > 0) node.start(at, offset);
            else node.start(at);
            return true;
        } catch {
            // A scheduled source can only be started once, ever.
            return false;
        }
    }

    function stopNode(id: string, when: number) {
        const node = butil.webAudioNodes.nodeOf(id);
        const ctx = butil.webAudio.currentContext();
        if (!node?.stop || !ctx) return false;
        try { node.stop(ctx.currentTime + (when > 0 ? when : 0)); return true; }
        catch { return false; }
    }
}(BitButil));
