var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface SessionEntry {
        session: any;
        referenceSpace: any;
        pose: any;                       // the most recent viewer pose, as plain JSON-able data
        frameHandle: number | null;
        gl: any;
        dotNetRef: any;
        poseMethod: string;
        poseIntervalMs: number;
        lastPoseSent: number;
    }

    const _sessions: { [id: string]: SessionEntry } = {};

    butil.webXr = {
        isSupported() { return !!(navigator as any).xr; },
        async isSessionSupported(mode: string) {
            const xr = (navigator as any).xr;
            if (!xr?.isSessionSupported) return false;
            try { return !!(await xr.isSessionSupported(mode)); }
            catch { return false; }  // an unknown mode rejects rather than answering false
        },
        requestSession,
        attachCanvas,
        viewerPose,
        inputSources,
        end,
        disposeAll
    };

    async function requestSession(id: string, mode: string, options: any, dotNetRef: any, endMethod: string, inputMethod: string, poseMethod: string) {
        const xr = (navigator as any).xr;
        if (!xr?.requestSession) return null;

        end(id);
        let session: any;
        try {
            session = await xr.requestSession(mode, butil.utils.pick(options, ['requiredFeatures', 'optionalFeatures']));
        } catch {
            // No device, a required feature the runtime doesn't have, or no user gesture behind it.
            return null;
        }

        // The reference space decides what the poses are relative to, and not every runtime offers
        // every type - so the requested one is tried first and the safe ones are tried after it.
        const candidates = [options?.referenceSpaceType, 'local-floor', 'local', 'viewer'].filter(Boolean);
        let referenceSpace: any = null;
        let referenceSpaceType = '';
        for (const type of candidates) {
            try {
                referenceSpace = await session.requestReferenceSpace(type);
                referenceSpaceType = type;
                break;
            } catch { /* try the next one */ }
        }
        if (!referenceSpace) {
            try { session.end(); } catch { /* already ended */ }
            return null;
        }

        const entry: SessionEntry = {
            session, referenceSpace, pose: null, frameHandle: null, gl: null,
            dotNetRef, poseMethod,
            poseIntervalMs: options?.poseIntervalMs > 0 ? options.poseIntervalMs : 0,
            lastPoseSent: 0
        };
        _sessions[id] = entry;

        session.addEventListener('end', () => {
            delete _sessions[id];
            butil.utils.dispatch(dotNetRef, endMethod, id);
        }, { once: true });

        for (const type of ['select', 'selectstart', 'selectend', 'squeeze', 'squeezestart', 'squeezeend']) {
            session.addEventListener(type, (e: any) => butil.utils.dispatch(dotNetRef, inputMethod, id, type,
                e?.inputSource?.handedness ?? 'none', e?.inputSource?.targetRayMode ?? ''));
        }

        // A pose only exists inside an XR frame callback, so the loop runs whether or not anyone is
        // watching: it is what keeps the most recent pose available to a .NET call that arrives
        // between frames, and it is what a session needs anyway to stay running.
        const onFrame = (time: number, frame: any) => {
            const current = _sessions[id];
            if (!current) return;
            current.frameHandle = session.requestAnimationFrame(onFrame);
            const pose = frame.getViewerPose(current.referenceSpace);
            current.pose = pose ? describePose(pose) : null;

            if (!current.poseIntervalMs || !current.pose) return;
            if (time - current.lastPoseSent < current.poseIntervalMs) return;
            current.lastPoseSent = time;
            butil.utils.dispatch(current.dotNetRef, current.poseMethod, id, current.pose);
        };
        entry.frameHandle = session.requestAnimationFrame(onFrame);

        return { mode, referenceSpaceType };
    }

    function describePose(pose: any) {
        return {
            transform: describeTransform(pose.transform),
            emulatedPosition: !!pose.emulatedPosition,
            views: (pose.views ?? []).map((view: any) => ({
                eye: view.eye ?? 'none',
                transform: describeTransform(view.transform),
                // A plain array rather than the Float32Array the runtime hands out, which does not
                // survive JSON serialization as numbers.
                projectionMatrix: view.projectionMatrix ? Array.from(view.projectionMatrix as Float32Array) : []
            }))
        };
    }

    function describeTransform(transform: any) {
        const p = transform?.position ?? {};
        const o = transform?.orientation ?? {};
        return {
            x: p.x ?? 0, y: p.y ?? 0, z: p.z ?? 0,
            orientationX: o.x ?? 0, orientationY: o.y ?? 0, orientationZ: o.z ?? 0, orientationW: o.w ?? 1
        };
    }

    // An immersive session displays nothing until it has a base layer, and the WebGL context it is
    // built on has to have been created XR-compatible - which cannot be done after the fact for a
    // context that already exists.
    function attachCanvas(id: string, canvas: any) {
        const entry = _sessions[id];
        const Layer: any = (window as any).XRWebGLLayer;
        if (!entry || !canvas || !Layer) return false;
        try {
            const gl = canvas.getContext('webgl2', { xrCompatible: true }) ?? canvas.getContext('webgl', { xrCompatible: true });
            if (!gl) return false;
            entry.gl = gl;
            entry.session.updateRenderState({ baseLayer: new Layer(entry.session, gl) });
            return true;
        } catch {
            return false;
        }
    }

    function viewerPose(id: string) {
        return _sessions[id]?.pose ?? null;
    }

    function inputSources(id: string) {
        const entry = _sessions[id];
        if (!entry) return [];
        return Array.from(entry.session.inputSources ?? []).map((source: any) => ({
            handedness: source.handedness ?? 'none',
            targetRayMode: source.targetRayMode ?? '',
            profiles: Array.from(source.profiles ?? []),
            hasGamepad: !!source.gamepad,
            hasGripSpace: !!source.gripSpace
        }));
    }

    function end(id: string) {
        const entry = _sessions[id];
        if (!entry) return;
        delete _sessions[id];
        if (entry.frameHandle !== null) {
            try { entry.session.cancelAnimationFrame(entry.frameHandle); } catch { /* session already ended */ }
        }
        try { entry.session.end(); } catch { /* already ended */ }
    }

    function disposeAll() {
        for (const id of Object.keys(_sessions)) end(id);
    }
}(BitButil));
