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
    prepared: Map<number, Uint8ClampedArray>;
    // Encoded images the application handed over, waiting to be decoded, keyed the same way.
    staged: Map<number, Blob>;
    // The element files can be dropped onto, and the listeners that make it a drop target.
    dropTarget: HTMLElement | null;
    dropListeners: DropListeners | null;
}

interface DropListeners {
    dragEnter: (event: DragEvent) => void;
    dragOver: (event: DragEvent) => void;
    dragLeave: (event: DragEvent) => void;
    drop: (event: DragEvent) => void;
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

/** prepareImage result: the image decodes to more pixels than it is safe to hold. */
const IMAGE_TOO_MANY_PIXELS = -5;

/**
 * The most pixels a selected image may decode to. A phone's largest photograph is around 200 megapixels; an image
 * beyond this is either a panorama nobody aims at a barcode or a file crafted to exhaust memory, and the decoded
 * bitmap alone would be more than a gigabyte.
 */
const MAX_IMAGE_PIXELS = 268_435_456;

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
            prepared: new Map<number, Uint8ClampedArray>(),
            staged: new Map<number, Blob>(),
            dropTarget: null,
            dropListeners: null,
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
 * Returns a token that changes whenever the camera renegotiates its resolution, the video
 * element is resized or the set of cameras changes, a byte for each. The managed side polls it
 * once per frame and recomputes only what actually changed, which is far cheaper than pushing an
 * event across the boundary.
 */
export function pollChanges(sessionId: string): number {
    const session = state.sessions.get(sessionId);
    if (!session) {
        return 0;
    }

    return (session.devicesVersion << 16) | (session.videoSizeVersion << 8) | session.viewSizeVersion;
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

    if (!session.hasNewFrame) {
        return 0;
    }

    const width = session.canvasWidth;
    const height = session.canvasHeight;
    if (!width || !height || !session.video.videoWidth) {
        return 0;
    }

    // The buffer is always sized for RGBA, which the 2D fallback needs, so either kind of frame fits.
    if (buffer.length < width * height * 4) {
        return FRAME_BUFFER_TOO_SMALL;
    }

    if (session.readback && !readWithGl(session.readback, session.video)) {
        // The context was lost or the driver refused the upload. The 2D canvas allocates a frame's worth on every
        // read, but it always works, and a scanner that keeps reading beats one that stops.
        releaseReadback(session);
        ensure2dCanvas(session);
    }

    let written: number;
    if (session.readback) {
        buffer.set(session.readback.pixels);
        written = session.readback.pixels.length;
    } else if (session.ctx) {
        session.ctx.drawImage(session.video, 0, 0, width, height);
        const data = session.ctx.getImageData(0, 0, width, height).data;
        buffer.set(new Uint8Array(data.buffer, data.byteOffset, data.byteLength));
        written = data.byteLength;
    } else {
        return 0;
    }

    if (session.usesFrameCallback) {
        session.hasNewFrame = false;
    }

    return written;
}

function isEnded(session: Session): boolean {
    return session.trackEnded || (!!session.track && session.track.readyState === 'ended');
}

/**
 * Reports whether the camera that ended was lost to the page being hidden - a phone switching to another app or
 * locking its screen ends the page's camera - rather than unplugged, revoked or taken by another application. The
 * page still being hidden counts: an ended track is not always announced by an event.
 */
export function wasLostWhileHidden(sessionId: string): boolean {
    const session = state.sessions.get(sessionId);
    return !!session && (session.endedWhileHidden || document.visibilityState === 'hidden');
}

/** Reports whether the page is hidden: in a background tab, a minimised window, or behind a locked screen. */
export function isPageHidden(): boolean {
    return document.visibilityState === 'hidden';
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
    detachDropTarget(session);
    session.prepared.forEach(wipe);
    session.prepared.clear();
    session.staged.clear();
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

        // A small file can still declare an enormous image. Browsers bound what they decode, but not tightly
        // enough to keep a phone's tab alive through the copy a scan makes of it.
        if (width * height > MAX_IMAGE_PIXELS) {
            return IMAGE_TOO_MANY_PIXELS;
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
 * Makes an element a drop target for image files. A file dropped onto it is handed to the scanner's file input as if
 * it had been picked, so it goes through exactly the same checks and the same decoding. While a file is dragged over
 * it the element carries data-drop="true", so a style can show that it will be taken.
 */
export function attachDropTarget(sessionId: string, targetElementId: string, inputElementId: string): void {
    const session = getSession(sessionId);
    detachDropTarget(session);

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
    };

    target.addEventListener('dragenter', listeners.dragEnter);
    target.addEventListener('dragover', listeners.dragOver);
    target.addEventListener('dragleave', listeners.dragLeave);
    target.addEventListener('drop', listeners.drop);
    session.dropTarget = target;
    session.dropListeners = listeners;
}

function detachDropTarget(session: Session): void {
    const target = session.dropTarget;
    const listeners = session.dropListeners;
    if (target && listeners) {
        target.removeEventListener('dragenter', listeners.dragEnter);
        target.removeEventListener('dragover', listeners.dragOver);
        target.removeEventListener('dragleave', listeners.dragLeave);
        target.removeEventListener('drop', listeners.drop);
        target.removeAttribute('data-drop');
    }

    session.dropTarget = null;
    session.dropListeners = null;
}

/** Reports whether the page is in a context where the camera API is reachable at all. */
export function isCameraSupported(): boolean {
    return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
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
