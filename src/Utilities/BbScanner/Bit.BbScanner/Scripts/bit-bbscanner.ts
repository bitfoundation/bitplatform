// BbScanner browser bridge.
//
// This file is deliberately small. Everything that can be done in C# is done in C#: the only
// jobs left here are the ones the browser will not let managed code do at all, which are opening
// a camera, keeping a <video> element fed, and copying the pixels of the latest frame.
//
// The same module serves every Blazor hosting model, through two doors:
//
// - On WebAssembly, .NET runs in this page and calls the module through [JSImport]. A frame is read back into one
//   array the session keeps for its whole life, and copied from there into a .NET owned buffer through a MemoryView:
//   no JSON, no base64, and no allocation per frame, so continuous scanning leaves the JavaScript heap as it found it.
// - Under Blazor Server and Blazor Hybrid, .NET runs in another process - across a network, or behind a WebView's
//   message channel - and calls the module through IJSRuntime. Pixels are cut down before they travel there - the
//   luminance only, a byte a pixel, and only of the region the decoder will search - and are read back in slices
//   small enough for one Blazor Server message each (readData).

// ---- Contracts shared with the C# interop ----------------------------------------------------------------

// The zero-copy WebAssembly path hands over a .NET MemoryView (a [JSMarshalAs<JSType.MemoryView>] Span<byte>)
// rather than an array: it exposes its length and set, but not element access.
interface MemoryViewLike {
    readonly length: number;
    set(source: Uint8Array, targetOffset?: number): void;
    slice(start?: number, end?: number): Uint8Array;
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
    // Whether the shader writes luminance, four pixels to an RGBA texel, so that the frame comes back as the
    // grayscale plane the decoder works on - a quarter of the bytes, and no conversion left for managed code - or the
    // RGBA frame itself.
    luminance: boolean;
    // The width actually drawn and read back: a quarter of the frame's when it carries luminance.
    targetWidth: number;
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
    // Whether the page was hidden when the track ended: a phone ends a background page's camera, which is an
    // interruption to recover from rather than a camera that went away.
    endedWhileHidden: boolean;
    frameCallbackHandle: number;
    onTrackEnded: (() => void) | null;
    onVideoResize: (() => void) | null;
    onDeviceChange: (() => void) | null;
    resizeObserver: ResizeObserver | null;
    viewWidth: number;
    viewHeight: number;
    videoSizeVersion: number;
    viewSizeVersion: number;
    devicesVersion: number;
    // Still images waiting to be read, keyed by the request that prepared them: two selections in flight at once
    // must never hand each other's pixels over.
    prepared: Map<number, Uint8Array | Uint8ClampedArray>;
    // Encoded images the application handed over, waiting to be decoded, keyed the same way: whole on WebAssembly, in
    // parts where they cross from another process.
    staged: Map<number, Blob>;
    stagedParts: Map<number, Uint8Array[]>;
    // The element files can be dropped onto, and the listeners that make it a drop target and open the file picker.
    dropTarget: HTMLElement | null;
    dropListeners: DropListeners | null;
    // The searched region of the latest frame, as luminance, for a .NET in another process to read through readData.
    // Reused from frame to frame like the readback.
    frameData: Uint8Array | null;
    // The watchdog of a camera opened by a .NET in another process: when that process stops asking for frames - a
    // circuit that dropped, a server that went away - the camera is closed rather than left running for nobody.
    watchdogMs: number;
    watchdogTimer: number;
    lastContact: number;
    // Set when the watchdog closed the camera, so that a .NET that comes back reports it as an interruption and opens
    // the camera again, instead of a fault.
    released: boolean;
    // A native camera's preview, which the page shows through itself where the video element is (Bit.BbScanner.Maui).
    nativePreview: NativePreview | null;
}

// What a DotNetObjectReference arrives as through IJSRuntime.
interface DotNetReceiver {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
}

interface NativePreview {
    video: HTMLElement;
    root: HTMLElement;
    receiver: DotNetReceiver;
    frame: number;
    last: string;
    backdrop: string;
    // The ancestors made transparent, with their own inline background as it was, to put back.
    cleared: ClearedBackground[];
}

interface ClearedBackground {
    element: HTMLElement;
    color: string;
    colorPriority: string;
    image: string;
    imagePriority: string;
}

interface DropListeners {
    dragEnter: (event: DragEvent) => void;
    dragOver: (event: DragEvent) => void;
    dragLeave: (event: DragEvent) => void;
    drop: (event: DragEvent) => void;
    pick: (event: MouseEvent) => void;
}

const state = {
    sessions: new Map<string, Session>(),
};

/** grabFrame result: the camera track has ended (unplugged, revoked, or taken by another app). */
const FRAME_TRACK_ENDED = -2;

/** grabFrame result: the supplied buffer is too small for a frame. */
const FRAME_BUFFER_TOO_SMALL = -1;

/** prepareImage result: the file is larger than the application allows. */
const IMAGE_TOO_LARGE = -3;

/** prepareImage result: the file does not declare an image type. */
const IMAGE_NOT_AN_IMAGE = -4;

/** prepareImage result: the image declares more pixels than it is safe to decode. */
const IMAGE_TOO_MANY_PIXELS = -5;

/**
 * The most pixels a selected image may declare. A phone's largest photograph is around 200 megapixels; an image
 * beyond this is either a panorama nobody aims at a barcode or a file crafted to exhaust memory, and the decoded
 * bitmap alone would be more than a gigabyte. Checked against the file's header, before it is decoded.
 */
const MAX_IMAGE_PIXELS = 268_435_456;

/** poll flags, above the 24 bits of change versions; the values of BbScannerBridge's Poll constants. */
const POLL_HIDDEN = 1 << 24;
const POLL_ENDED = 1 << 25;
const POLL_INTERRUPTED = 1 << 26;
const POLL_FRAME = 1 << 27;

/** capabilities flags; the values of BbScannerBridge's Capability constants. */
const CAPABILITY_CAMERA = 1;
const CAPABILITY_SECURE_CONTEXT = 2;

/** readData's key for the latest frame's region; any other key is the request a prepared image is held under. */
const FRAME_DATA = -1;

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
            endedWhileHidden: false,
            frameCallbackHandle: 0,
            onTrackEnded: null,
            onVideoResize: null,
            onDeviceChange: null,
            resizeObserver: null,
            viewWidth: 0,
            viewHeight: 0,
            videoSizeVersion: 0,
            viewSizeVersion: 0,
            devicesVersion: 0,
            prepared: new Map<number, Uint8Array | Uint8ClampedArray>(),
            staged: new Map<number, Blob>(),
            stagedParts: new Map<number, Uint8Array[]>(),
            dropTarget: null,
            dropListeners: null,
            frameData: null,
            watchdogMs: 0,
            watchdogTimer: 0,
            lastContact: 0,
            released: false,
            nativePreview: null,
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

    if (session.watchdogTimer) {
        window.clearInterval(session.watchdogTimer);
    }

    session.watchdogTimer = 0;
    session.released = false;

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
    if (session.onDeviceChange && navigator.mediaDevices && typeof navigator.mediaDevices.removeEventListener === 'function') {
        navigator.mediaDevices.removeEventListener('devicechange', session.onDeviceChange);
    }

    session.onDeviceChange = null;
    session.stream = null;
    session.track = null;
    session.hasNewFrame = false;
    session.usesFrameCallback = false;
    session.trackEnded = false;
    session.endedWhileHidden = false;

    // The readback array and the canvas still hold the last frame the camera saw. With no camera open there is
    // nothing to keep them for: the managed side configures a fresh pair after every start, and until then the
    // picture of whatever the camera last pointed at is not kept in the page. Dropping the WebGL context also hands
    // one back to the browser, which allows only a handful per page.
    releaseReadback(session);
    wipeCanvas(session.canvas);
    session.canvas = null;
    session.ctx = null;
    wipe(session.frameData);
    session.frameData = null;
}

/**
 * Closes the camera of a session whose .NET has stopped asking for frames. Every poll and every grab counts as a sign
 * of life, and a running scanner polls on every tick, paused or not, so only a .NET that has gone away - or one held up
 * far longer than any frame should take - goes quiet for this long.
 */
function startWatchdog(session: Session, watchdogMs: number): void {
    session.watchdogMs = watchdogMs;
    session.lastContact = performance.now();
    if (watchdogMs <= 0) {
        return;
    }

    const generation = session.generation;
    session.watchdogTimer = window.setInterval(() => {
        if (session.generation !== generation || !session.stream) {
            return;
        }

        if (performance.now() - session.lastContact > session.watchdogMs) {
            releaseStream(session);
            session.released = true;
        }
    }, Math.max(250, Math.floor(watchdogMs / 4)));
}

/** Overwrites a copy of camera or image pixels before it is let go: freed memory keeps its contents until it is reused. */
function wipe(data: Uint8Array | Uint8ClampedArray | null | undefined): void {
    data?.fill(0);
}

/** Lets go of a canvas's picture: resizing a canvas discards its backing store. */
function wipeCanvas(canvas: ScanCanvas | null | undefined): void {
    if (canvas) {
        canvas.width = 0;
        canvas.height = 0;
    }
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
const READBACK_PRECISION = '#ifdef GL_FRAGMENT_PRECISION_HIGH\nprecision highp float;\n#else\nprecision mediump float;\n#endif\n';

const READBACK_FRAGMENT_SHADER = READBACK_PRECISION
    + 'uniform sampler2D s;varying vec2 t;void main(){gl_FragColor=texture2D(s,t);}';

// Four horizontally adjacent pixels' luminance to a texel, with the decoder's own BT.601 weights (77, 150 and 29 over
// 256), so the bytes read back are the grayscale plane itself. d is one frame pixel in texture coordinates: the texel
// at t covers frame pixels 4x to 4x + 3, whose centres are at t - 1.5d, t - 0.5d, t + 0.5d and t + 1.5d.
const LUMINANCE_FRAGMENT_SHADER = READBACK_PRECISION
    + 'uniform sampler2D s;uniform float d;varying vec2 t;const vec3 w=vec3(77.0,150.0,29.0)/256.0;'
    + 'float l(float o){return dot(texture2D(s,vec2(t.x+o*d,t.y)).rgb,w);}'
    + 'void main(){gl_FragColor=vec4(l(-1.5),l(-0.5),l(0.5),l(1.5));}';

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
    // Luminance needs four pixels to a texel, and every channel of it read back as written: an alpha channel, not
    // premultiplied into the others.
    const luminance = width % 4 === 0;
    const targetWidth = luminance ? width / 4 : width;
    const canvas = createCanvas(targetWidth, height);
    let gl: WebGLRenderingContext | null = null;
    try {
        gl = canvas.getContext('webgl', {
            alpha: luminance,
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
    const fragment = compileShader(gl, gl.FRAGMENT_SHADER, luminance ? LUMINANCE_FRAGMENT_SHADER : READBACK_FRAGMENT_SHADER);
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
    gl.viewport(0, 0, targetWidth, height);
    if (luminance) {
        gl.uniform1f(gl.getUniformLocation(program, 'd'), 1 / width);
    }

    // RGBA or packed luminance, the array is the size of what is read back: four bytes a texel either way.
    return {
        canvas,
        gl,
        texture,
        pixels: new Uint8Array(targetWidth * height * 4),
        width,
        height,
        luminance,
        targetWidth,
    };
}

function releaseReadback(session: Session): void {
    if (session.readback) {
        wipe(session.readback.pixels);
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
        gl.readPixels(0, 0, readback.targetWidth, readback.height, gl.RGBA, gl.UNSIGNED_BYTE, readback.pixels);
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
    wipeCanvas(session.canvas);
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
    for (const device of devices) {
        if (device.kind !== 'videoinput') {
            continue;
        }

        // Before permission is granted every device reports an empty id; one placeholder entry
        // is enough to say "a camera exists".
        if (seen.has(device.deviceId)) {
            continue;
        }

        // A camera without a label is named on the managed side, in the application's language.
        seen.add(device.deviceId);
        cameras.push({
            deviceId: device.deviceId,
            label: device.label,
        });
    }

    return JSON.stringify(cameras);
}

/**
 * Gives an error the DOMException name the managed side classifies it by, at the front of its message. A rejection
 * reaches .NET as its message alone through IJSRuntime, and getUserMedia's messages ("Permission denied") do not say
 * which error they are.
 */
function named(error: unknown): unknown {
    const name = (error as { name?: unknown } | null)?.name;
    const message = (error as { message?: unknown } | null)?.message;
    if (typeof name !== 'string' || !name || name === 'Error' || typeof message !== 'string' || message.startsWith(name)) {
        return error;
    }

    return new Error(`${name}: ${message}`);
}

/**
 * Opens a camera and binds it to the <video> element with the given id.
 * Returns a JSON description of the negotiated track.
 *
 * A second start on the same session, or a stop or dispose, supersedes a start still waiting
 * for the permission prompt: when the older one resolves its stream is closed again and it
 * rejects with AbortError, so a camera can never be left running by a request nobody wants.
 *
 * A watchdog above zero, which a .NET in another process asks for, closes the camera once that many milliseconds pass
 * without a poll or a grab.
 */
export async function start(sessionId: string, videoElementId: string, deviceId: string | null, requestedWidth: number,
                            requestedHeight: number, facingMode: string | null, watchdogMs: number): Promise<string> {
    try {
        return await open(sessionId, videoElementId, deviceId, requestedWidth, requestedHeight, facingMode, watchdogMs);
    } catch (error) {
        throw named(error);
    }
}

async function open(sessionId: string, videoElementId: string, deviceId: string | null, requestedWidth: number,
                    requestedHeight: number, facingMode: string | null, watchdogMs: number): Promise<string> {
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
        session.endedWhileHidden = document.visibilityState === 'hidden';
    };
    track.addEventListener('ended', session.onTrackEnded);

    session.onVideoResize = () => {
        session.videoSizeVersion = (session.videoSizeVersion + 1) & 0xFF;
    };

    video.addEventListener('resize', session.onVideoResize);

    // A camera plugged in or unplugged while scanning belongs in the camera picker without a restart.
    if (typeof navigator.mediaDevices.addEventListener === 'function') {
        session.onDeviceChange = () => {
            session.devicesVersion = (session.devicesVersion + 1) & 0xFF;
        };
        navigator.mediaDevices.addEventListener('devicechange', session.onDeviceChange);
    }

    scheduleFrameCallback(session);
    observeViewSize(session, video);
    startWatchdog(session, watchdogMs);

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
 * Returns everything the managed side checks on a tick of its frame loop, in one number, so that a .NET across a
 * network pays one round trip for it rather than one a question:
 *
 * - the low 24 bits, a token that changes whenever the camera renegotiates its resolution, the video element is
 *   resized or the set of cameras changes, a byte for each. The managed side recomputes only what actually changed,
 *   which is far cheaper than pushing an event across the boundary;
 * - POLL_HIDDEN, when the page is hidden: in a background tab, a minimised window, or behind a locked screen;
 * - POLL_ENDED, when the camera track has ended - unplugged, revoked, taken by another application, or closed by the
 *   watchdog. A paused scanner grabs no frames, so this is how it notices;
 * - POLL_INTERRUPTED, when the camera that ended was lost to the page being hidden - a phone switching to another app
 *   or locking its screen ends the page's camera - or to the watchdog, rather than gone for good. The page still being
 *   hidden counts: an ended track is not always announced by an event;
 * - POLL_FRAME, when the camera has delivered a frame not grabbed yet.
 *
 * It is also the sign of life the watchdog waits for.
 */
export function poll(sessionId: string): number {
    const hidden = document.visibilityState === 'hidden';
    let flags = hidden ? POLL_HIDDEN : 0;
    const session = state.sessions.get(sessionId);
    if (!session) {
        return flags;
    }

    session.lastContact = performance.now();
    if (session.released || isEnded(session)) {
        flags |= POLL_ENDED;
    }

    if (session.released || session.endedWhileHidden || hidden) {
        flags |= POLL_INTERRUPTED;
    }

    if (hasFrame(session)) {
        flags |= POLL_FRAME;
    }

    return flags | (session.devicesVersion << 16) | (session.videoSizeVersion << 8) | session.viewSizeVersion;
}

function hasFrame(session: Session): boolean {
    return session.hasNewFrame && !!session.video && !!session.video.videoWidth && !!session.canvasWidth
        && !!session.canvasHeight && !!(session.readback || session.ctx) && !isEnded(session);
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
 * Copies the most recent camera frame into a .NET buffer: as the grayscale plane, a byte a pixel, when the GPU path is
 * in use, and as RGBA when the 2D canvas is. Returns the number of bytes written, which says which of the two it is, 0
 * when there is no new frame, -1 when the buffer is too small, or -2 when the camera track has ended.
 */
export function grabFrame(sessionId: string, buffer: MemoryViewLike): number {
    const session = state.sessions.get(sessionId);
    if (!session || !session.video || (!session.readback && !session.ctx)) {
        return 0;
    }

    if (isEnded(session)) {
        return FRAME_TRACK_ENDED;
    }

    if (!hasFrame(session)) {
        return 0;
    }

    // The buffer is always sized for RGBA, which the 2D fallback needs, so either kind of frame fits.
    if (buffer.length < session.canvasWidth * session.canvasHeight * 4) {
        return FRAME_BUFFER_TOO_SMALL;
    }

    const data = readFrame(session);
    if (!data) {
        return 0;
    }

    buffer.set(data);
    return data.length;
}

/**
 * Grabs the most recent camera frame for a .NET in another process, as the luminance of the region it will search, a
 * byte a pixel and row after row: the frame is cut down here, before it crosses a network or a WebView's message
 * channel, rather than sent whole to be cut down there. The region is in the pixels of the configured frame size.
 * Returns the number of bytes the region holds, which .NET then reads through readData, or 0 when there is no new
 * frame.
 */
export function grabFrameData(sessionId: string, x: number, y: number, width: number, height: number): number {
    const session = state.sessions.get(sessionId);
    if (!session) {
        return 0;
    }

    session.lastContact = performance.now();
    if (!hasFrame(session)) {
        return 0;
    }

    const frameWidth = session.canvasWidth;
    const frameHeight = session.canvasHeight;
    x = Math.max(0, Math.min(frameWidth - 1, Math.floor(x)));
    y = Math.max(0, Math.min(frameHeight - 1, Math.floor(y)));
    width = Math.max(1, Math.min(frameWidth - x, Math.floor(width)));
    height = Math.max(1, Math.min(frameHeight - y, Math.floor(height)));

    const data = readFrame(session);
    if (!data) {
        return 0;
    }

    const size = width * height;
    let region = session.frameData;
    if (!region || region.length !== size) {
        wipe(region);
        region = session.frameData = new Uint8Array(size);
    }

    if (data.length === frameWidth * frameHeight) {
        for (let row = 0; row < height; row++) {
            const start = (y + row) * frameWidth + x;
            region.set(data.subarray(start, start + width), row * width);
        }
    } else {
        for (let row = 0; row < height; row++) {
            let source = ((y + row) * frameWidth + x) * 4;
            let target = row * width;
            for (let column = 0; column < width; column++, source += 4) {
                region[target++] = luminance(data, source);
            }
        }
    }

    return region.length;
}

/**
 * Reads the current video frame at the configured size: the grayscale plane, a byte a pixel, when the GPU path is in
 * use, and RGBA when the 2D canvas is. The array is the session's own, valid until the next read. Marks the frame as
 * grabbed.
 */
function readFrame(session: Session): Uint8Array | null {
    const video = session.video!;
    if (session.readback && !readWithGl(session.readback, video)) {
        // The context was lost or the driver refused the upload. The 2D canvas allocates a frame's worth on every
        // read, but it always works, and a scanner that keeps reading beats one that stops.
        releaseReadback(session);
        ensure2dCanvas(session);
    }

    let data: Uint8Array;
    if (session.readback) {
        data = session.readback.pixels;
    } else if (session.ctx) {
        session.ctx.drawImage(video, 0, 0, session.canvasWidth, session.canvasHeight);
        const pixels = session.ctx.getImageData(0, 0, session.canvasWidth, session.canvasHeight).data;
        data = new Uint8Array(pixels.buffer, pixels.byteOffset, pixels.byteLength);
    } else {
        return null;
    }

    if (session.usesFrameCallback) {
        session.hasNewFrame = false;
    }

    return data;
}

/** The luminance of the RGBA pixel at offset, with the decoder's own fixed point BT.601 weights. */
function luminance(rgba: Uint8Array | Uint8ClampedArray, offset: number): number {
    return (rgba[offset] * 77 + rgba[offset + 1] * 150 + rgba[offset + 2] * 29) >> 8;
}

function isEnded(session: Session): boolean {
    return session.trackEnded || (!!session.track && session.track.readyState === 'ended');
}

/** Kinds of feedback; the values of BbScanFeedback. */
const FEEDBACK_VIBRATE = 1;
const FEEDBACK_BEEP = 2;

let feedbackAudio: AudioContext | null = null;

/**
 * Signals a scan. Both kinds are best effort: vibration is missing from Safari, and a page may only make a sound once
 * the user has interacted with it, so either can do nothing at all without that being an error.
 */
export function feedback(kinds: number): void {
    if (kinds & FEEDBACK_VIBRATE && typeof navigator.vibrate === 'function') {
        try {
            navigator.vibrate(80);
        } catch {
            // A browser may refuse outside a user gesture; there is nothing to report.
        }
    }

    if (kinds & FEEDBACK_BEEP) {
        beep();
    }
}

function beep(): void {
    const AudioContextType = window.AudioContext
        ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!AudioContextType) {
        return;
    }

    try {
        // One context for the page, suspended between beeps so that it does not keep the audio hardware awake.
        const audio = feedbackAudio ??= new AudioContextType();
        if (audio.state !== 'running') {
            // Resuming needs the page to have had a user gesture. A beep that cannot sound now is dropped rather than
            // queued on a context that may never run, where its nodes would pile up, or played late, after the scan.
            const asked = performance.now();
            void audio.resume().then(() => {
                if (performance.now() - asked < 150) {
                    playBeep(audio);
                } else if (beepsPlaying === 0 && feedbackAudio === audio && audio.state === 'running') {
                    // Resumed too late to belong to the scan - typically at the user's first tap, long after it - so
                    // the context it woke goes back to sleep rather than keeping the audio hardware awake.
                    void audio.suspend().catch(() => undefined);
                }
            }).catch(() => undefined);
            return;
        }

        playBeep(audio);
    } catch {
        // No audio output, or the browser refused the context; the scan was still reported.
    }
}

let beepsPlaying = 0;

function playBeep(audio: AudioContext): void {
    try {
        const start = audio.currentTime;
        const oscillator = audio.createOscillator();
        const gain = audio.createGain();
        oscillator.type = 'sine';
        oscillator.frequency.value = 1760;

        // A short swell and fade rather than a square edge, which clicks.
        gain.gain.setValueAtTime(0.0001, start);
        gain.gain.exponentialRampToValueAtTime(0.25, start + 0.01);
        gain.gain.exponentialRampToValueAtTime(0.0001, start + 0.12);
        oscillator.connect(gain);
        gain.connect(audio.destination);
        oscillator.start(start);
        oscillator.stop(start + 0.13);
        beepsPlaying++;
        oscillator.onended = () => {
            oscillator.disconnect();
            gain.disconnect();

            // Two scans a frame apart overlap their beeps: only the last one to finish may suspend the context.
            if (--beepsPlaying === 0 && feedbackAudio === audio && audio.state === 'running') {
                void audio.suspend().catch(() => undefined);
            }
        };
    } catch {
        // The browser refused the nodes; the scan was still reported.
    }
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
    detachRoot(session);
    detachPreview(session);
    session.prepared.forEach(wipe);
    session.prepared.clear();
    session.staged.clear();
    session.stagedParts.clear();
    wipeCanvas(session.canvas);
    session.canvas = null;
    session.ctx = null;
    session.video = null;
    state.sessions.delete(sessionId);
}

/**
 * Decodes the first file selected in the given <input type="file"> into a pixel buffer held on
 * the JS side under requestId, scaled so that neither edge exceeds maxDimension.
 * Returns the dimensions packed as (width << 16) | height, 0 when there is nothing to read, or one of the negative
 * IMAGE_* codes when the file is refused before or after decoding.
 */
export async function prepareImageFromInput(sessionId: string, inputElementId: string, requestId: number,
                                            maxDimension: number, maxBytes: number): Promise<number> {
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

    return await prepareImageFromBlob(sessionId, requestId, file, maxDimension, maxBytes);
}

/**
 * Checks what a file claims to be before anything decodes it. The picker's accept attribute is a hint the user can
 * override, and a dropped file was never filtered at all, so both are checked here. A file with no type at all is
 * let through: some platforms report none for formats they do not know by name, such as HEIC on Windows, and the
 * decoder is the authority on whether it is an image.
 */
function refuseBlob(blob: Blob, maxBytes: number): number {
    if (maxBytes > 0 && blob.size > maxBytes) {
        return IMAGE_TOO_LARGE;
    }

    if (blob.type && !blob.type.startsWith('image/')) {
        return IMAGE_NOT_AN_IMAGE;
    }

    return 0;
}

/** How much of a file declaredPixels reads: enough to get past a JPEG's EXIF, thumbnail and colour profile. */
const IMAGE_HEADER_BYTES = 512 * 1024;

/**
 * The number of pixels an image declares in its header, read without decoding it, or 0 for a format this does not
 * know - PNG, JPEG, GIF, BMP, WebP, and AVIF and HEIC through their ispe box, are known. The size has to be known
 * before the image is decoded: decoding allocates the whole bitmap, so a small file that declares an enormous image
 * would have exhausted the tab's memory before any check made afterwards could refuse it.
 */
async function declaredPixels(blob: Blob): Promise<number> {
    const bytes = new Uint8Array(await blob.slice(0, IMAGE_HEADER_BYTES).arrayBuffer());
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    const length = bytes.length;
    const ascii = (offset: number, text: string): boolean => {
        if (offset + text.length > length) {
            return false;
        }

        for (let i = 0; i < text.length; i++) {
            if (bytes[offset + i] !== text.charCodeAt(i)) {
                return false;
            }
        }

        return true;
    };

    // PNG: the IHDR chunk always comes first.
    if (length >= 24 && bytes[0] === 0x89 && ascii(1, 'PNG') && ascii(12, 'IHDR')) {
        return view.getUint32(16) * view.getUint32(20);
    }

    // GIF: the logical screen, which every frame is drawn into.
    if (length >= 10 && (ascii(0, 'GIF87a') || ascii(0, 'GIF89a'))) {
        return view.getUint16(6, true) * view.getUint16(8, true);
    }

    // BMP: a twelve byte core header or any of the larger info headers; a top-down bitmap's height is negative.
    if (length >= 26 && ascii(0, 'BM')) {
        return view.getUint32(14, true) === 12
            ? view.getUint16(18, true) * view.getUint16(20, true)
            : Math.abs(view.getInt32(18, true)) * Math.abs(view.getInt32(22, true));
    }

    // WebP: lossy, lossless or extended, each with its own way of writing the size.
    if (length >= 30 && ascii(0, 'RIFF') && ascii(8, 'WEBP')) {
        if (ascii(12, 'VP8 ')) {
            return (view.getUint16(26, true) & 0x3FFF) * (view.getUint16(28, true) & 0x3FFF);
        }

        if (ascii(12, 'VP8L')) {
            const bits = view.getUint32(21, true);
            return ((bits & 0x3FFF) + 1) * (((bits >>> 14) & 0x3FFF) + 1);
        }

        if (ascii(12, 'VP8X')) {
            const width = (bytes[24] | (bytes[25] << 8) | (bytes[26] << 16)) + 1;
            const height = (bytes[27] | (bytes[28] << 8) | (bytes[29] << 16)) + 1;
            return width * height;
        }

        return 0;
    }

    // JPEG: the segments are walked to the start of frame, which follows the application segments.
    if (length >= 4 && bytes[0] === 0xFF && bytes[1] === 0xD8) {
        let offset = 2;
        while (offset + 9 <= length) {
            if (bytes[offset] !== 0xFF) {
                return 0;
            }

            const marker = bytes[offset + 1];
            if (marker === 0xFF) {
                offset++;
                continue;
            }

            // Every start of frame marker but the three that are not one: DHT, JPG and DAC.
            if (marker >= 0xC0 && marker <= 0xCF && marker !== 0xC4 && marker !== 0xC8 && marker !== 0xCC) {
                return view.getUint16(offset + 5) * view.getUint16(offset + 7);
            }

            // Markers without a length: the restart markers and the start and end of image.
            if ((marker >= 0xD0 && marker <= 0xD9) || marker === 0x01) {
                offset += 2;
                continue;
            }

            offset += 2 + view.getUint16(offset + 2);
        }

        return 0;
    }

    // AVIF and HEIC: an ISO base media file whose image spatial extents box gives the size of each image it holds.
    if (length >= 12 && ascii(4, 'ftyp')) {
        let largest = 0;
        for (let offset = 8; offset + 16 <= length; offset++) {
            if (bytes[offset] === 0x69 && ascii(offset, 'ispe')) {
                largest = Math.max(largest, view.getUint32(offset + 8) * view.getUint32(offset + 12));
            }
        }

        return largest;
    }

    return 0;
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
export async function prepareImageFromBlob(sessionId: string, requestId: number, blob: Blob, maxDimension: number,
                                           maxBytes: number): Promise<number> {
    const refused = refuseBlob(blob, maxBytes);
    if (refused) {
        return refused;
    }

    const session = getSession(sessionId);

    // A small file can still declare an enormous image. Browsers bound what they decode, but not tightly enough to
    // keep a phone's tab alive through it, so the size the file declares is checked before anything is decoded. What
    // is decoded is then copied out at maxDimension, whatever its size.
    if (await declaredPixels(blob) > MAX_IMAGE_PIXELS) {
        return IMAGE_TOO_MANY_PIXELS;
    }

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
        wipeCanvas(ctx.canvas as ScanCanvas);

        return (width << 16) | height;
    } finally {
        bitmap.close();
    }
}

/**
 * Readies the image prepared under requestId for a .NET in another process, as its luminance, a byte a pixel: a
 * quarter of what the RGBA would cost to send. The RGBA copy is wiped at once; the luminance is held, for readData to
 * read, until releasePrepared wipes it. Returns the number of bytes, or 0 when nothing is prepared.
 */
export function readPreparedData(sessionId: string, requestId: number): number {
    const session = state.sessions.get(sessionId);
    const data = session ? session.prepared.get(requestId) : undefined;
    if (!session || !data) {
        return 0;
    }

    if (data instanceof Uint8ClampedArray) {
        const gray = new Uint8Array(data.length >> 2);
        for (let i = 0, offset = 0; i < gray.length; i++, offset += 4) {
            gray[i] = luminance(data, offset);
        }

        wipe(data);
        session.prepared.set(requestId, gray);
        return gray.length;
    }

    return data.length;
}

/**
 * Returns a slice of what grabFrameData (key FRAME_DATA) or readPreparedData (key: the request) readied, for a .NET in
 * another process. The slices are what make the trip: a Blazor Server circuit refuses a single message over 32 KB from
 * the page, so .NET asks for slices under it, several at once, each an ordinary call that can time out and be asked
 * again - where a stream, once lost to a dropped connection, cannot.
 */
export function readData(sessionId: string, key: number, offset: number, count: number): Uint8Array {
    const session = state.sessions.get(sessionId);
    const data = !session ? undefined : key === FRAME_DATA ? session.frameData : session.prepared.get(key);
    if (!session || !data || offset < 0 || offset >= data.length) {
        return new Uint8Array(0);
    }

    if (key === FRAME_DATA) {
        session.lastContact = performance.now();
    }

    // A copy rather than a view: what is sent must be exactly the slice, whatever the encoder makes of a view's offset.
    return new Uint8Array(data.subarray(offset, Math.min(data.length, offset + count)));
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
        wipe(data);
        return FRAME_BUFFER_TOO_SMALL;
    }

    buffer.set(new Uint8Array(data.buffer, data.byteOffset, data.byteLength));
    wipe(data);
    return data.byteLength;
}

/** Forgets an image prepared under requestId without reading it, for a request a newer one superseded. */
export function releasePrepared(sessionId: string, requestId: number): void {
    const session = state.sessions.get(sessionId);
    if (session) {
        wipe(session.prepared.get(requestId));
        session.prepared.delete(requestId);
        session.staged.delete(requestId);
        session.stagedParts.delete(requestId);
    }
}

/**
 * Holds an encoded image the application handed over - the bytes of a PNG or JPEG file - under requestId, for
 * prepareStagedImage to decode. The view onto managed memory is only valid for the duration of this call, so the bytes
 * are copied out of it here, synchronously.
 */
export function stageImage(sessionId: string, requestId: number, bytes: MemoryViewLike, type: string | null): void {
    const session = getSession(sessionId);
    session.staged.set(requestId, new Blob([bytes.slice() as BlobPart], type ? { type } : undefined));
}

/**
 * Decodes an image stageImage holds under requestId into a pixel buffer, with the same checks and the same scaling as a
 * file picked from the input. Returns the dimensions packed as (width << 16) | height, 0 when nothing is staged, or one
 * of the negative IMAGE_* codes.
 */
export async function prepareStagedImage(sessionId: string, requestId: number, maxDimension: number,
                                         maxBytes: number): Promise<number> {
    const session = state.sessions.get(sessionId);
    const blob = session ? session.staged.get(requestId) : undefined;
    if (!session || !blob) {
        return 0;
    }

    session.staged.delete(requestId);
    return await prepareImageFromBlob(sessionId, requestId, blob, maxDimension, maxBytes);
}

/**
 * Holds one part of an encoded image a .NET in another process sends - the bytes of a PNG or JPEG file, in parts so
 * that no one message is large - under requestId and its index, for prepareStagedParts. Parts may arrive in any order.
 */
export function stageImagePart(sessionId: string, requestId: number, index: number, part: Uint8Array): void {
    const session = getSession(sessionId);
    let parts = session.stagedParts.get(requestId);
    if (!parts) {
        parts = [];
        session.stagedParts.set(requestId, parts);
    }

    parts[index] = part;
}

/**
 * Decodes the parts stageImagePart holds under requestId, all count of them, as one image, the way prepareStagedImage
 * decodes one staged on WebAssembly. Returns the packed dimensions, 0 when parts are missing, or a negative IMAGE_* code.
 */
export async function prepareStagedParts(sessionId: string, requestId: number, count: number, type: string | null,
                                         maxDimension: number, maxBytes: number): Promise<number> {
    const session = state.sessions.get(sessionId);
    const parts = session ? session.stagedParts.get(requestId) : undefined;
    if (session) {
        session.stagedParts.delete(requestId);
    }

    if (!session || !parts || parts.length !== count) {
        return 0;
    }

    for (let i = 0; i < count; i++) {
        if (!parts[i]) {
            return 0;
        }
    }

    const blob = new Blob(parts as BlobPart[], type ? { type } : undefined);
    return await prepareImageFromBlob(sessionId, requestId, blob, maxDimension, maxBytes);
}

/**
 * Copies the camera's current frame into a pixel buffer held under requestId, at the camera's own resolution scaled so
 * that neither edge exceeds maxDimension. The live frame loop reads frames scaled down for speed; this is the one frame
 * a dense or distant symbol needs every pixel of. Returns the packed dimensions, or 0 when no camera is running.
 */
export function prepareVideoFrame(sessionId: string, requestId: number, maxDimension: number): number {
    const session = state.sessions.get(sessionId);
    const video = session ? session.video : null;
    if (!session || !video || !session.stream || !video.videoWidth || !video.videoHeight || isEnded(session)) {
        return 0;
    }

    let width = video.videoWidth;
    let height = video.videoHeight;
    const longest = Math.max(width, height);
    if (maxDimension > 0 && longest > maxDimension) {
        const scale = maxDimension / longest;
        width = Math.max(1, Math.round(width * scale));
        height = Math.max(1, Math.round(height * scale));
    }

    // Dimensions travel packed into one positive integer.
    if (width > 0x7FFF || height > 0xFFFF) {
        return 0;
    }

    const ctx = getReadbackContext(createCanvas(width, height));
    if (!ctx) {
        return 0;
    }

    ctx.drawImage(video, 0, 0, width, height);
    session.prepared.set(requestId, ctx.getImageData(0, 0, width, height).data);
    wipeCanvas(ctx.canvas as ScanCanvas);
    return (width << 16) | height;
}

/**
 * Wires the scanner's root element up to its file input, in the page itself:
 *
 * - The root becomes a drop target for image files. A file dropped onto it is handed to the file input as if it had
 *   been picked, so it goes through exactly the same checks and the same decoding. While a file is dragged over it
 *   the element carries data-drop="true", so a style can show that it will be taken.
 * - A click on an element inside it whose data-bit-bbscanner-pick names the file input opens the file picker, here,
 *   in the click's own event. A browser only opens a picker for a script while the user's click is fresh, and a click
 *   that went to a server and came back - under Blazor Server, a network round trip later - may no longer be.
 */
export function attachRoot(sessionId: string, targetElementId: string, inputElementId: string): void {
    const session = getSession(sessionId);
    detachRoot(session);

    const target = document.getElementById(targetElementId);
    if (!target) {
        return;
    }

    const input = (): HTMLInputElement | null => document.getElementById(inputElementId) as HTMLInputElement | null;
    const carriesFiles = (event: DragEvent): boolean =>
        !!event.dataTransfer && Array.prototype.indexOf.call(event.dataTransfer.types, 'Files') >= 0;

    // dragenter and dragleave fire for every child the pointer crosses, so the element counts them rather than
    // trusting any single dragleave to mean the pointer has gone.
    let depth = 0;
    const listeners: DropListeners = {
        dragEnter: event => {
            if (carriesFiles(event)) {
                depth++;
            }
        },
        dragOver: event => {
            // The input only exists while file scanning is enabled; without it a drop is not taken.
            if (!carriesFiles(event) || !input()) {
                return;
            }

            event.preventDefault();
            event.dataTransfer!.dropEffect = 'copy';
            if (depth === 0) {
                depth = 1;
            }

            target.setAttribute('data-drop', 'true');
        },
        dragLeave: () => {
            depth = Math.max(0, depth - 1);
            if (depth === 0) {
                target.removeAttribute('data-drop');
            }
        },
        drop: event => {
            depth = 0;
            target.removeAttribute('data-drop');
            const element = input();
            if (!carriesFiles(event) || !element || !event.dataTransfer || event.dataTransfer.files.length === 0) {
                return;
            }

            event.preventDefault();

            // A FileList cannot be constructed, but the one a drop carries can be assigned; the input's change event
            // then runs the same path as a file chosen from the dialog.
            element.files = event.dataTransfer.files;
            element.dispatchEvent(new Event('change', { bubbles: true }));
        },
        pick: event => {
            // Matched by the input it names, so that the button of a scanner nested in this one opens its own picker.
            const trigger = (event.target as Element | null)?.closest?.('[data-bit-bbscanner-pick]');
            if (!trigger || trigger.getAttribute('data-bit-bbscanner-pick') !== inputElementId) {
                return;
            }

            input()?.click();
        },
    };

    target.addEventListener('dragenter', listeners.dragEnter);
    target.addEventListener('dragover', listeners.dragOver);
    target.addEventListener('dragleave', listeners.dragLeave);
    target.addEventListener('drop', listeners.drop);
    target.addEventListener('click', listeners.pick);
    session.dropTarget = target;
    session.dropListeners = listeners;
}

function detachRoot(session: Session): void {
    const target = session.dropTarget;
    const listeners = session.dropListeners;
    if (target && listeners) {
        target.removeEventListener('dragenter', listeners.dragEnter);
        target.removeEventListener('dragover', listeners.dragOver);
        target.removeEventListener('dragleave', listeners.dragLeave);
        target.removeEventListener('drop', listeners.drop);
        target.removeEventListener('click', listeners.pick);
        target.removeAttribute('data-drop');
    }

    session.dropTarget = null;
    session.dropListeners = null;
}

/**
 * Reports what the page offers the scanner: CAPABILITY_CAMERA when the camera API is reachable at all, and
 * CAPABILITY_SECURE_CONTEXT when the page is served from a secure context, which the camera API requires.
 */
export function capabilities(): number {
    return (navigator.mediaDevices && typeof navigator.mediaDevices.getUserMedia === 'function' ? CAPABILITY_CAMERA : 0)
        | (window.isSecureContext ? CAPABILITY_SECURE_CONTEXT : 0);
}

/**
 * Reports what the browser would do with a request for the camera - 'granted', 'denied' or 'prompt' - without making
 * one, or '' where it will not say: the Permissions API is missing, or does not know the camera permission by name.
 */
export async function queryCameraPermission(): Promise<string> {
    if (!navigator.permissions || typeof navigator.permissions.query !== 'function') {
        return '';
    }

    try {
        const status = await navigator.permissions.query({ name: 'camera' as PermissionName });
        return status.state;
    } catch {
        return '';
    }
}

/** Whether a computed colour paints anything: not transparent, nor of zero alpha. */
function isColour(color: string): boolean {
    return !!color && !/^(transparent|rgba\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*0\s*\))$/.test(color);
}

/** Whether a computed background paints anything. */
function paints(style: CSSStyleDeclaration): boolean {
    return style.backgroundImage !== 'none' || isColour(style.backgroundColor);
}

/**
 * Shows a native camera's preview through the page - Bit.BbScanner.Maui draws it behind the WebView - where the
 * scanner's video element is, and reports where that is to receiver's Place whenever it moves, resizes, shows or hides:
 * on every animation frame, the page posting only what changed.
 *
 * Nothing the page paints may cover the preview, so the scanner and every element above it in the tree are made
 * transparent while it shows, and put back afterwards. The page's own background colour - the outermost one painted -
 * is reported along with the placement, for what is behind the WebView to be painted the same colour, so the page
 * around the scanner looks as it did.
 */
export function attachNativePreview(sessionId: string, videoElementId: string, receiver: DotNetReceiver): void {
    const session = getSession(sessionId);
    const video = document.getElementById(videoElementId);

    // A camera switched to another: the page is already showing a preview through itself, and stays as it is rather
    // than flash its backgrounds back for the moment between the two.
    if (video && session.nativePreview?.video === video) {
        session.nativePreview.receiver = receiver;
        session.nativePreview.last = '';
        return;
    }

    detachPreview(session);
    if (!video) {
        return;
    }

    const root = (video.closest('.bit-bbscanner') as HTMLElement | null) ?? video.parentElement ?? video;
    root.setAttribute('data-native-preview', 'true');

    const cleared: ClearedBackground[] = [];
    let backdrop: string | null = null;
    for (let element = root.parentElement; element; element = element.parentElement) {
        const style = getComputedStyle(element);
        if (!paints(style)) {
            continue;
        }

        if (isColour(style.backgroundColor)) {
            backdrop = style.backgroundColor;
        }

        cleared.push({
            element,
            color: element.style.getPropertyValue('background-color'),
            colorPriority: element.style.getPropertyPriority('background-color'),
            image: element.style.getPropertyValue('background-image'),
            imagePriority: element.style.getPropertyPriority('background-image'),
        });
        element.style.setProperty('background-color', 'transparent', 'important');
        element.style.setProperty('background-image', 'none', 'important');
    }

    // A page that paints no background of its own shows the WebView's canvas, which is white.
    const preview: NativePreview = { video, root, receiver, frame: 0, last: '', backdrop: backdrop ?? 'rgb(255, 255, 255)', cleared };
    session.nativePreview = preview;

    const step = (): void => {
        if (session.nativePreview !== preview) {
            return;
        }

        const rect = video.getBoundingClientRect();
        const clip = clipOf(video);
        const radius = parseFloat(getComputedStyle(root).borderTopLeftRadius) || 0;
        const visible = video.isConnected && rect.width > 0 && rect.height > 0 && clip[2] > 0 && clip[3] > 0;
        const placement = [rect.left, rect.top, rect.width, rect.height, radius, visible, preview.backdrop, ...clip];
        const key = placement.join('|');
        if (key !== preview.last) {
            preview.last = key;
            void preview.receiver.invokeMethodAsync('Place', ...placement).catch(() => undefined);
        }

        preview.frame = requestAnimationFrame(step);
    };

    step();
}

/**
 * The part of the viewport an element shows in: the viewport, cut down by every element above it that clips what
 * overflows it. A native preview drawn behind the page is clipped to it, so a scanner partly scrolled out of a panel
 * shows only inside the panel.
 */
function clipOf(element: HTMLElement): [number, number, number, number] {
    let left = 0;
    let top = 0;
    let right = window.innerWidth;
    let bottom = window.innerHeight;
    for (let ancestor = element.parentElement; ancestor && ancestor !== document.documentElement; ancestor = ancestor.parentElement) {
        const style = getComputedStyle(ancestor);
        if (style.overflowX === 'visible' && style.overflowY === 'visible') {
            continue;
        }

        const rect = ancestor.getBoundingClientRect();
        left = Math.max(left, rect.left);
        top = Math.max(top, rect.top);
        right = Math.min(right, rect.right);
        bottom = Math.min(bottom, rect.bottom);
    }

    return [left, top, Math.max(0, right - left), Math.max(0, bottom - top)];
}

/** Takes a native camera's preview out of the page, putting back what attachNativePreview made transparent. */
export function detachNativePreview(sessionId: string): void {
    const session = state.sessions.get(sessionId);
    if (session) {
        detachPreview(session);
    }
}

function detachPreview(session: Session): void {
    const preview = session.nativePreview;
    if (!preview) {
        return;
    }

    session.nativePreview = null;
    cancelAnimationFrame(preview.frame);
    preview.root.removeAttribute('data-native-preview');
    for (const cleared of preview.cleared) {
        cleared.element.style.setProperty('background-color', cleared.color, cleared.colorPriority);
        cleared.element.style.setProperty('background-image', cleared.image, cleared.imagePriority);
    }

    // The default placement: the page has taken the preview out.
    void preview.receiver.invokeMethodAsync('Place', 0, 0, 0, 0, 0, false, null, 0, 0, 0, 0).catch(() => undefined);
}

/** Clicks an element by id, used to open the hidden file picker from managed code. */
export function click(elementId: string): void {
    const element = document.getElementById(elementId);
    if (element) {
        element.click();
    }
}
