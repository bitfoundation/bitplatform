// BbScanner browser bridge.
//
// This file is deliberately small. Everything that can be done in C# is done in C#: the only
// jobs left here are the ones the browser will not let managed code do at all, which are opening
// a camera, keeping a <video> element fed, and copying the pixels of the latest frame.
//
// A frame is read back into one array the session keeps for its whole life, and copied from
// there into a .NET owned buffer through a MemoryView: no JSON, no base64, and no allocation per
// frame, so continuous scanning leaves the JavaScript heap as it found it.

// ---- Contracts shared with the C# interop ----------------------------------------------------------------

// The zero-copy WebAssembly path hands over a .NET MemoryView (a [JSMarshalAs<JSType.MemoryView>] Span<byte>)
// rather than an array: it exposes its length and set, but not element access.
interface MemoryViewLike {
    readonly length: number;
    set(source: Uint8Array, targetOffset?: number): void;
}

// camelCase JSON of BbCameraDevice.
interface CameraDeviceDescription {
    deviceId: string;
    label: string;
}

// camelCase JSON of BbCameraInfo.
interface CameraDescription {
    deviceId: string;
    label: string;
    width: number;
    height: number;
    frameRate: number;
    facingMode: string;
    supportsTorch: boolean;
    torchOn: boolean;
    supportsZoom: boolean;
    zoomMin: number;
    zoomMax: number;
    zoomStep: number;
    zoom: number;
}

// The Image Capture extensions to the track settings and capabilities. The DOM typings do not carry them,
// and the browsers that do not support them simply leave the members out.
interface ZoomRange {
    min: number;
    max: number;
    step?: number;
}

interface ImageCaptureCapabilities {
    torch?: boolean;
    zoom?: ZoomRange;
}

interface ImageCaptureSettings {
    deviceId?: string;
    width?: number;
    height?: number;
    frameRate?: number;
    facingMode?: string;
    torch?: boolean;
    zoom?: number;
}

// requestVideoFrameCallback is missing from Firefox and older Safari, so it is looked up rather than assumed.
interface FrameCallbackVideo {
    requestVideoFrameCallback?(callback: () => void): number;
    cancelVideoFrameCallback?(handle: number): void;
}

type ScanCanvas = HTMLCanvasElement | OffscreenCanvas;

type ScanContext = CanvasRenderingContext2D | OffscreenCanvasRenderingContext2D;

// The GPU frame path: the video is drawn into a WebGL canvas and read back with readPixels, which, unlike the 2D
// canvas's getImageData, writes into an array the caller supplies.
interface GlReadback {
    canvas: ScanCanvas;
    gl: WebGLRenderingContext;
    texture: WebGLTexture;
    pixels: Uint8Array;
    width: number;
    height: number;
}

interface Session {
    id: string;
    generation: number;
    stream: MediaStream | null;
    track: MediaStreamTrack | null;
    video: HTMLVideoElement | null;
    readback: GlReadback | null;
    canvas: ScanCanvas | null;
    ctx: ScanContext | null;
    canvasWidth: number;
    canvasHeight: number;
    hasNewFrame: boolean;
    usesFrameCallback: boolean;
    trackEnded: boolean;
    frameCallbackHandle: number;
    onTrackEnded: (() => void) | null;
    onVideoResize: (() => void) | null;
    resizeObserver: ResizeObserver | null;
    viewWidth: number;
    viewHeight: number;
    videoSizeVersion: number;
    viewSizeVersion: number;
    // Still images waiting to be read, keyed by the request that prepared them: two selections in flight at once
    // must never hand each other's pixels over.
    prepared: Map<number, Uint8ClampedArray>;
}

const state = {
    sessions: new Map<string, Session>(),
};

/** grabFrame result: the camera track has ended (unplugged, revoked, or taken by another app). */
const FRAME_TRACK_ENDED = -2;

/** grabFrame result: the supplied buffer is too small for a frame. */
const FRAME_BUFFER_TOO_SMALL = -1;

function getSession(id: string): Session {
    let session = state.sessions.get(id);
    if (!session) {
        session = {
            id,
            generation: 0,
            stream: null,
            track: null,
            video: null,
            readback: null,
            canvas: null,
            ctx: null,
            canvasWidth: 0,
            canvasHeight: 0,
            hasNewFrame: false,
            usesFrameCallback: false,
            trackEnded: false,
            frameCallbackHandle: 0,
            onTrackEnded: null,
            onVideoResize: null,
            resizeObserver: null,
            viewWidth: 0,
            viewHeight: 0,
            videoSizeVersion: 0,
            viewSizeVersion: 0,
            prepared: new Map<number, Uint8ClampedArray>(),
        };
        state.sessions.set(id, session);
    }

    return session;
}

function stopTracks(stream: MediaStream | null): void {
    if (!stream) {
        return;
    }

    for (const track of stream.getTracks()) {
        try {
            track.stop();
        } catch {
            // A track that is already ended throws in some browsers; there is nothing to release.
        }
    }
}

function releaseStream(session: Session): void {
    // Any start() still in flight for this session must not attach its stream when it resolves.
    session.generation++;

    const video = session.video as (HTMLVideoElement & FrameCallbackVideo) | null;
    if (session.frameCallbackHandle && video && video.cancelVideoFrameCallback) {
        video.cancelVideoFrameCallback(session.frameCallbackHandle);
    }

    session.frameCallbackHandle = 0;

    if (session.track && session.onTrackEnded) {
        session.track.removeEventListener('ended', session.onTrackEnded);
    }

    session.onTrackEnded = null;
    if (session.resizeObserver) {
        session.resizeObserver.disconnect();
        session.resizeObserver = null;
    }

    stopTracks(session.stream);

    if (session.video) {
        session.video.srcObject = null;
    }

    if (session.video && session.onVideoResize) {
        session.video.removeEventListener('resize', session.onVideoResize);
    }

    session.onVideoResize = null;
    session.stream = null;
    session.track = null;
    session.hasNewFrame = false;
    session.usesFrameCallback = false;
    session.trackEnded = false;
}

function createCanvas(width: number, height: number): ScanCanvas {
    // An OffscreenCanvas keeps the pixels off the compositor entirely. Where it is missing the
    // detached <canvas> behaves the same, it just costs a little more memory.
    return typeof OffscreenCanvas === 'function'
        ? new OffscreenCanvas(width, height)
        : Object.assign(document.createElement('canvas'), { width, height });
}

function getReadbackContext(canvas: ScanCanvas): ScanContext | null {
    // willReadFrequently keeps the backing store in CPU memory, which is what makes
    // getImageData cheap instead of forcing a GPU readback on every frame.
    return canvas.getContext('2d', { willReadFrequently: true, alpha: false }) as ScanContext | null;
}

// The texture coordinate runs bottom to top while readPixels returns the bottom row first, so the two flips cancel
// and the array comes out top row first, the way the decoder reads it, without a flip pass on either side.
const READBACK_VERTEX_SHADER =
    'attribute vec2 p;varying vec2 t;void main(){t=p*0.5+0.5;gl_Position=vec4(p,0.0,1.0);}';

// mediump cannot address every texel of a wide frame on some mobile GPUs, so highp is used wherever it exists.
const READBACK_FRAGMENT_SHADER =
    '#ifdef GL_FRAGMENT_PRECISION_HIGH\nprecision highp float;\n#else\nprecision mediump float;\n#endif\n'
    + 'uniform sampler2D s;varying vec2 t;void main(){gl_FragColor=texture2D(s,t);}';

function compileShader(gl: WebGLRenderingContext, type: number, source: string): WebGLShader | null {
    const shader = gl.createShader(type);
    if (!shader) {
        return null;
    }

    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    return gl.getShaderParameter(shader, gl.COMPILE_STATUS) ? shader : null;
}

function loseContext(gl: WebGLRenderingContext): void {
    // Browsers cap the number of live WebGL contexts per page, so one that is finished with is given back now
    // rather than whenever the collector notices it.
    const extension = gl.getExtension('WEBGL_lose_context');
    if (extension) {
        extension.loseContext();
    }
}

function createReadback(width: number, height: number): GlReadback | null {
    const canvas = createCanvas(width, height);
    let gl: WebGLRenderingContext | null = null;
    try {
        gl = canvas.getContext('webgl', {
            alpha: false,
            antialias: false,
            depth: false,
            stencil: false,
            premultipliedAlpha: false,
            preserveDrawingBuffer: false,
        }) as WebGLRenderingContext | null;
    } catch {
        gl = null;
    }

    if (!gl) {
        return null;
    }

    const vertex = compileShader(gl, gl.VERTEX_SHADER, READBACK_VERTEX_SHADER);
    const fragment = compileShader(gl, gl.FRAGMENT_SHADER, READBACK_FRAGMENT_SHADER);
    const program = gl.createProgram();
    const quad = gl.createBuffer();
    const texture = gl.createTexture();
    if (!vertex || !fragment || !program || !quad || !texture) {
        loseContext(gl);
        return null;
    }

    gl.attachShader(program, vertex);
    gl.attachShader(program, fragment);
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
        loseContext(gl);
        return null;
    }

    gl.useProgram(program);

    gl.bindBuffer(gl.ARRAY_BUFFER, quad);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
    const position = gl.getAttribLocation(program, 'p');
    gl.enableVertexAttribArray(position);
    gl.vertexAttribPointer(position, 2, gl.FLOAT, false, 0, 0);

    // A camera frame is rarely a power of two on either side, which WebGL 1 only samples with edge clamping and
    // without mipmaps.
    gl.bindTexture(gl.TEXTURE_2D, texture);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
    gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, false);
    gl.viewport(0, 0, width, height);

    return { canvas, gl, texture, pixels: new Uint8Array(width * height * 4), width, height };
}

function releaseReadback(session: Session): void {
    if (session.readback) {
        loseContext(session.readback.gl);
        session.readback = null;
    }
}

/** Draws the current video frame through WebGL into the readback's own array. Returns false if the GPU path failed. */
function readWithGl(readback: GlReadback, video: HTMLVideoElement): boolean {
    const gl = readback.gl;
    if (gl.isContextLost()) {
        return false;
    }

    try {
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, video);
        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
        gl.readPixels(0, 0, readback.width, readback.height, gl.RGBA, gl.UNSIGNED_BYTE, readback.pixels);
    } catch {
        return false;
    }

    return gl.getError() === gl.NO_ERROR;
}

function ensureCanvas(session: Session, width: number, height: number): void {
    if ((session.readback || session.canvas) && session.canvasWidth === width && session.canvasHeight === height) {
        return;
    }

    releaseReadback(session);
    session.canvas = null;
    session.ctx = null;
    session.canvasWidth = width;
    session.canvasHeight = height;

    // WebGL is preferred because it reads a frame back without allocating; the 2D canvas is the fallback where
    // WebGL is missing, blocked, or (as on some headless hosts) has no GPU behind it.
    session.readback = createReadback(width, height);
    if (!session.readback) {
        ensure2dCanvas(session);
    }
}

function ensure2dCanvas(session: Session): void {
    if (session.ctx) {
        return;
    }

    const width = session.canvasWidth;
    const height = session.canvasHeight;
    session.canvas = createCanvas(width, height);

    if (session.canvas.width !== width) {
        session.canvas.width = width;
    }

    if (session.canvas.height !== height) {
        session.canvas.height = height;
    }

    session.ctx = getReadbackContext(session.canvas);
}

function scheduleFrameCallback(session: Session): void {
    const video = session.video as (HTMLVideoElement & FrameCallbackVideo) | null;
    if (!video) {
        return;
    }

    if (typeof video.requestVideoFrameCallback !== 'function') {
        // Firefox and older Safari have no way to tell a new frame from a repeat, so every grab
        // is treated as new and the managed side's timer alone paces the scan. The flag must
        // therefore stay set: clearing it after a grab, as the callback path does, would stop
        // the scanner dead after its very first frame.
        session.usesFrameCallback = false;
        session.hasNewFrame = true;
        return;
    }

    session.usesFrameCallback = true;
    const generation = session.generation;
    const step = (): void => {
        if (session.generation !== generation) {
            return;
        }

        session.hasNewFrame = true;
        session.frameCallbackHandle = video.requestVideoFrameCallback!(step);
    };

    session.frameCallbackHandle = video.requestVideoFrameCallback(step);
}

function describeTrack(track: MediaStreamTrack, video: HTMLVideoElement): CameraDescription {
    const settings: ImageCaptureSettings = typeof track.getSettings === 'function' ? track.getSettings() as ImageCaptureSettings : {};
    const capabilities: ImageCaptureCapabilities = typeof track.getCapabilities === 'function' ? track.getCapabilities() as ImageCaptureCapabilities : {};

    // The <video> element reports the frames as they are actually delivered, which is what the
    // canvas copies; the track settings can lag or be swapped on rotated mobile cameras.
    const width = video.videoWidth || settings.width || 0;
    const height = video.videoHeight || settings.height || 0;

    return {
        deviceId: settings.deviceId ?? '',
        label: track.label ?? '',
        width,
        height,
        frameRate: settings.frameRate ?? 0,
        facingMode: settings.facingMode ?? '',
        supportsTorch: Object.prototype.hasOwnProperty.call(capabilities, 'torch'),
        torchOn: settings.torch === true,
        supportsZoom: Object.prototype.hasOwnProperty.call(capabilities, 'zoom'),
        zoomMin: capabilities.zoom ? capabilities.zoom.min : 0,
        zoomMax: capabilities.zoom ? capabilities.zoom.max : 0,
        zoomStep: capabilities.zoom && capabilities.zoom.step ? capabilities.zoom.step : 0,
        zoom: settings.zoom ?? 0,
    };
}

function waitForVideoSize(video: HTMLVideoElement, timeoutMs: number): Promise<void> {
    if (video.videoWidth) {
        return Promise.resolve();
    }

    // Safari reports a zero sized video until metadata has actually arrived.
    return new Promise<void>(resolve => {
        let timer = 0;
        const done = (): void => {
            window.clearTimeout(timer);
            video.removeEventListener('loadedmetadata', done);
            video.removeEventListener('resize', done);
            resolve();
        };

        video.addEventListener('loadedmetadata', done);
        video.addEventListener('resize', done);
        timer = window.setTimeout(done, timeoutMs);
    });
}

/**
 * Returns the available video inputs as JSON. Labels are only populated once the user has
 * granted camera permission at least once, which is a browser privacy rule, not a bug.
 */
export async function listCameras(): Promise<string> {
    if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices) {
        return '[]';
    }

    const devices = await navigator.mediaDevices.enumerateDevices();
    const cameras: CameraDeviceDescription[] = [];
    const seen = new Set<string>();
    let index = 0;
    for (const device of devices) {
        if (device.kind !== 'videoinput') {
            continue;
        }

        index++;

        // Before permission is granted every device reports an empty id; one placeholder entry
        // is enough to say "a camera exists".
        if (seen.has(device.deviceId)) {
            continue;
        }

        seen.add(device.deviceId);
        cameras.push({
            deviceId: device.deviceId,
            label: device.label || `Camera ${index}`,
        });
    }

    return JSON.stringify(cameras);
}

/**
 * Opens a camera and binds it to the <video> element with the given id.
 * Returns a JSON description of the negotiated track.
 *
 * A second start on the same session, or a stop or dispose, supersedes a start still waiting
 * for the permission prompt: when the older one resolves its stream is closed again and it
 * rejects with AbortError, so a camera can never be left running by a request nobody wants.
 */
export async function start(sessionId: string, videoElementId: string, deviceId: string | null, requestedWidth: number,
                            requestedHeight: number, facingMode: string | null): Promise<string> {
    const session = getSession(sessionId);
    releaseStream(session);
    const generation = session.generation;

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        throw new Error('NotSupportedError: this browser exposes no camera API.');
    }

    const video = document.getElementById(videoElementId) as HTMLVideoElement | null;
    if (!video) {
        // Not NotFoundError: that is getUserMedia's name for a missing camera, and this is a page problem.
        throw new Error('InvalidStateError: the scanner video element is not in the document.');
    }

    const videoConstraints: MediaTrackConstraints = {};
    if (deviceId) {
        videoConstraints.deviceId = { exact: deviceId };
    } else if (facingMode) {
        videoConstraints.facingMode = { ideal: facingMode };
    }

    if (requestedWidth > 0) {
        videoConstraints.width = { ideal: requestedWidth };
    }

    if (requestedHeight > 0) {
        videoConstraints.height = { ideal: requestedHeight };
    }

    const stream = await navigator.mediaDevices.getUserMedia({ audio: false, video: videoConstraints });

    const superseded = (): boolean => session.generation !== generation || state.sessions.get(sessionId) !== session;
    if (superseded()) {
        stopTracks(stream);
        throw new Error('AbortError: the camera request was superseded.');
    }

    session.stream = stream;
    session.video = video;
    video.setAttribute('playsinline', '');
    video.setAttribute('muted', '');
    video.muted = true;
    video.srcObject = stream;

    try {
        await video.play();
        await waitForVideoSize(video, 2000);
    } catch (error) {
        if (!superseded()) {
            releaseStream(session);
        }

        throw error;
    }

    if (superseded()) {
        // releaseStream already closed the stream; make sure of it in case a newer start
        // replaced the session object outright.
        stopTracks(stream);
        throw new Error('AbortError: the camera request was superseded.');
    }

    const track = stream.getVideoTracks()[0];
    session.track = track;
    session.trackEnded = false;
    session.onTrackEnded = () => {
        session.trackEnded = true;
    };
    track.addEventListener('ended', session.onTrackEnded);

    session.onVideoResize = () => {
        session.videoSizeVersion = (session.videoSizeVersion + 1) & 0xFF;
    };

    video.addEventListener('resize', session.onVideoResize);

    scheduleFrameCallback(session);
    observeViewSize(session, video);

    return JSON.stringify(describeTrack(track, video));
}

function observeViewSize(session: Session, video: HTMLVideoElement): void {
    session.viewWidth = video.clientWidth;
    session.viewHeight = video.clientHeight;
    if (typeof ResizeObserver !== 'function') {
        return;
    }

    session.resizeObserver = new ResizeObserver(() => {
        session.viewWidth = video.clientWidth;
        session.viewHeight = video.clientHeight;
        session.viewSizeVersion = (session.viewSizeVersion + 1) & 0xFF;
    });
    session.resizeObserver.observe(video);
}

/**
 * Returns a token that changes whenever the camera renegotiates its resolution or the video
 * element is resized. The managed side polls it once per frame and recomputes only what
 * actually changed, which is far cheaper than pushing an event across the boundary.
 */
export function pollChanges(sessionId: string): number {
    const session = state.sessions.get(sessionId);
    if (!session) {
        return 0;
    }

    return (session.videoSizeVersion << 8) | session.viewSizeVersion;
}

/**
 * Returns the rendered size of the video element packed as (width << 16) | height, so the
 * managed side can map frame coordinates onto what is actually visible when the video is
 * cropped to fill its box.
 */
export function getViewSize(sessionId: string): number {
    const session = state.sessions.get(sessionId);
    if (!session || !session.video) {
        return 0;
    }

    const width = Math.min(0x7FFF, Math.max(0, Math.round(session.viewWidth || session.video.clientWidth || 0)));
    const height = Math.min(0xFFFF, Math.max(0, Math.round(session.viewHeight || session.video.clientHeight || 0)));
    return (width << 16) | height;
}

/** Sets the resolution frames are sampled at. Lower is faster and is chosen by the managed side. */
export function configure(sessionId: string, width: number, height: number): void {
    const session = getSession(sessionId);
    ensureCanvas(session, width, height);
}

/**
 * Copies the most recent camera frame into a .NET buffer as RGBA.
 * Returns the number of bytes written, 0 when there is no new frame, -1 when the buffer is
 * too small, or -2 when the camera track has ended.
 */
export function grabFrame(sessionId: string, buffer: MemoryViewLike): number {
    const session = state.sessions.get(sessionId);
    if (!session || !session.video || (!session.readback && !session.ctx)) {
        return 0;
    }

    if (isEnded(session)) {
        return FRAME_TRACK_ENDED;
    }

    if (!session.hasNewFrame) {
        return 0;
    }

    const width = session.canvasWidth;
    const height = session.canvasHeight;
    if (!width || !height || !session.video.videoWidth) {
        return 0;
    }

    const required = width * height * 4;
    if (buffer.length < required) {
        return FRAME_BUFFER_TOO_SMALL;
    }

    if (session.readback && !readWithGl(session.readback, session.video)) {
        // The context was lost or the driver refused the upload. The 2D canvas allocates a frame's worth on every
        // read, but it always works, and a scanner that keeps reading beats one that stops.
        releaseReadback(session);
        ensure2dCanvas(session);
    }

    if (session.readback) {
        buffer.set(session.readback.pixels);
    } else if (session.ctx) {
        session.ctx.drawImage(session.video, 0, 0, width, height);
        const data = session.ctx.getImageData(0, 0, width, height).data;
        buffer.set(new Uint8Array(data.buffer, data.byteOffset, data.byteLength));
    } else {
        return 0;
    }

    if (session.usesFrameCallback) {
        session.hasNewFrame = false;
    }

    return required;
}

function isEnded(session: Session): boolean {
    return session.trackEnded || (!!session.track && session.track.readyState === 'ended');
}

/**
 * Reports whether the camera track has ended. A paused scanner grabs no frames, so it asks this instead to notice
 * a camera that was unplugged or revoked while it was paused.
 */
export function isTrackEnded(sessionId: string): boolean {
    const session = state.sessions.get(sessionId);
    return !!session && isEnded(session);
}

/** Turns the torch on or off. Returns whether the request was accepted. */
export async function setTorch(sessionId: string, on: boolean): Promise<boolean> {
    const session = state.sessions.get(sessionId);
    if (!session || !session.track || typeof session.track.applyConstraints !== 'function') {
        return false;
    }

    try {
        await session.track.applyConstraints({ advanced: [{ torch: !!on } as MediaTrackConstraintSet] });
        return true;
    } catch {
        return false;
    }
}

/** Sets the optical or digital zoom. Returns whether the request was accepted. */
export async function setZoom(sessionId: string, value: number): Promise<boolean> {
    const session = state.sessions.get(sessionId);
    if (!session || !session.track || typeof session.track.applyConstraints !== 'function') {
        return false;
    }

    try {
        await session.track.applyConstraints({ advanced: [{ zoom: value } as MediaTrackConstraintSet] });
        return true;
    } catch {
        return false;
    }
}

/** Returns the current track description as JSON, or an empty string when nothing is running. */
export function describe(sessionId: string): string {
    const session = state.sessions.get(sessionId);
    if (!session || !session.track || !session.video) {
        return '';
    }

    return JSON.stringify(describeTrack(session.track, session.video));
}

/** Stops the camera and releases the stream. The session and its canvas survive for a restart. */
export function stop(sessionId: string): void {
    const session = state.sessions.get(sessionId);
    if (session) {
        releaseStream(session);
    }
}

/** Stops the camera and forgets the session entirely. */
export function dispose(sessionId: string): void {
    const session = state.sessions.get(sessionId);
    if (!session) {
        return;
    }

    releaseStream(session);
    releaseReadback(session);
    session.prepared.clear();
    session.canvas = null;
    session.ctx = null;
    session.video = null;
    state.sessions.delete(sessionId);
}

/**
 * Decodes the first file selected in the given <input type="file"> into a pixel buffer held on
 * the JS side under requestId, scaled so that neither edge exceeds maxDimension.
 * Returns the dimensions packed as (width << 16) | height, or 0 when there is nothing to read.
 */
export async function prepareImageFromInput(sessionId: string, inputElementId: string, requestId: number,
                                            maxDimension: number): Promise<number> {
    const input = document.getElementById(inputElementId) as HTMLInputElement | null;
    if (!input || !input.files || input.files.length === 0) {
        return 0;
    }

    const file = input.files[0];

    // Clear the selection so that choosing the same file again raises another change event.
    try {
        input.value = '';
    } catch {
        // Older browsers refuse to reset a file input; the next selection of a different file
        // still works.
    }

    return await prepareImageFromBlob(sessionId, requestId, file, maxDimension);
}

async function decodeBitmap(blob: Blob): Promise<ImageBitmap> {
    try {
        // Honour the EXIF orientation of photographs, which phones store rotated.
        return await createImageBitmap(blob, { imageOrientation: 'from-image' });
    } catch (error) {
        if (error instanceof TypeError || (error && (error as { name?: string }).name === 'TypeError')) {
            // The option is unsupported by this browser; fall back to the default orientation.
            return await createImageBitmap(blob);
        }

        throw error;
    }
}

/** Decodes an image blob the same way as prepareImageFromInput. */
export async function prepareImageFromBlob(sessionId: string, requestId: number, blob: Blob, maxDimension: number): Promise<number> {
    const session = getSession(sessionId);
    const bitmap = await decodeBitmap(blob);

    try {
        // A session disposed while the image was decoding wants nothing back, and storing into it would keep the
        // pixels alive with nobody left to read them.
        if (state.sessions.get(sessionId) !== session) {
            return 0;
        }

        let width = bitmap.width;
        let height = bitmap.height;
        if (!width || !height) {
            return 0;
        }

        const longest = Math.max(width, height);
        if (maxDimension > 0 && longest > maxDimension) {
            const scale = maxDimension / longest;
            width = Math.max(1, Math.round(width * scale));
            height = Math.max(1, Math.round(height * scale));
        }

        // Dimensions travel packed into one positive integer, so the width gets fifteen bits.
        if (width > 0x7FFF || height > 0xFFFF) {
            const scale = 0x7FFF / Math.max(width, height);
            width = Math.max(1, Math.floor(width * scale));
            height = Math.max(1, Math.floor(height * scale));
        }

        const ctx = getReadbackContext(createCanvas(width, height));
        if (!ctx) {
            return 0;
        }

        // The canvas has no alpha channel and starts out opaque black, so a transparent image - the usual output of
        // a code generator - would be composited over black and its dark modules lost against it. Paper is white.
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, width, height);
        ctx.drawImage(bitmap, 0, 0, width, height);
        session.prepared.set(requestId, ctx.getImageData(0, 0, width, height).data);

        return (width << 16) | height;
    } finally {
        bitmap.close();
    }
}

/** Copies the image prepared under requestId into a .NET buffer. Returns bytes written. */
export function readPrepared(sessionId: string, requestId: number, buffer: MemoryViewLike): number {
    const session = state.sessions.get(sessionId);
    const data = session ? session.prepared.get(requestId) : undefined;
    if (!session || !data) {
        return 0;
    }

    // Released either way: a full resolution image is megabytes, and holding one alive because
    // the caller mis-sized its buffer would be a leak for the life of the session.
    session.prepared.delete(requestId);

    if (buffer.length < data.byteLength) {
        return FRAME_BUFFER_TOO_SMALL;
    }

    buffer.set(new Uint8Array(data.buffer, data.byteOffset, data.byteLength));
    return data.byteLength;
}

/** Forgets an image prepared under requestId without reading it, for a request a newer one superseded. */
export function releasePrepared(sessionId: string, requestId: number): void {
    const session = state.sessions.get(sessionId);
    if (session) {
        session.prepared.delete(requestId);
    }
}

/** Reports whether the page is in a context where the camera API is reachable at all. */
export function isCameraSupported(): boolean {
    return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
}

/** Reports whether the page is served from a secure context, which the camera API requires. */
export function isSecureContext(): boolean {
    return !!window.isSecureContext;
}

/** Clicks an element by id, used to open the hidden file picker from managed code. */
export function click(elementId: string): void {
    const element = document.getElementById(elementId);
    if (element) {
        element.click();
    }
}
