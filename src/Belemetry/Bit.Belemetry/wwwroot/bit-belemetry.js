// Scripts/features.ts
var Features = {
  None: 0,
  Errors: 1,
  PageViews: 2,
  Interactions: 4,
  Performance: 8,
  Dependencies: 16,
  Sessions: 32,
  UserTracking: 64,
  Logs: 128,
  Traces: 256,
  Metrics: 512,
  Persistence: 1024,
  Events: 2048,
  Diagnostics: 1 | 128 | 256 | 16,
  Analytics: 2 | 4 | 32 | 64 | 2048,
  Monitoring: 8 | 512,
  All: 4095
};

// Scripts/config.ts
function createDefaults() {
  return {
    service: { name: "unknown_service:webjs", version: null, namespace: null, instanceId: null, environment: null },
    resource: {},
    sdkVersion: "0.0.0",
    transport: "direct",
    exporters: [],
    features: Features.All,
    consent: { default: Features.None, persist: true, honorGlobalPrivacyControl: true, pendingLimit: 500 },
    pageViews: { trackHashChanges: false },
    interactions: { events: ["click"], captureText: false, maxTextLength: 64 },
    performance: { webVitals: true, documentLoad: true, resources: false, longTasks: false },
    dependencies: { fetch: true, xhr: true, propagateTraceContextTo: ["same-origin"], ignoreUrls: ["/_framework/", "/_blazor", "/_content/"] },
    errors: { ignoreMessages: ["ResizeObserver loop"] },
    logs: { captureConsole: false, consoleLevels: ["error", "warn"] },
    privacy: {
      redactQueryParameters: [
        "token",
        "code",
        "password",
        "pwd",
        "secret",
        "key",
        "apikey",
        "api_key",
        "auth",
        "authorization",
        "session",
        "sid",
        "sig",
        "signature",
        "email",
        "access_token",
        "id_token",
        "refresh_token"
      ],
      stripQueryString: false,
      stripFragment: true,
      scrubPatterns: true,
      collectUserAgent: true,
      blockedAttributes: []
    },
    session: { inactivityTimeoutMinutes: 30, maxDurationMinutes: 1440 },
    sampling: { sessionRatio: 1 },
    batch: { maxQueueSize: 1024, maxBatchSize: 100, flushIntervalMs: 5e3 },
    persistence: { enabled: true, maxBytes: 1048576, maxAgeHours: 48, retryIntervalMs: 6e4 },
    debug: false
  };
}
function resolveConfig(input) {
  const target = createDefaults();
  const source = input || {};
  for (const key of Object.keys(source)) {
    const value = source[key];
    if (value == null) continue;
    const current = target[key];
    if (isPlainObject(current) && isPlainObject(value)) {
      for (const prop of Object.keys(value)) {
        if (value[prop] != null) current[prop] = value[prop];
      }
    } else {
      target[key] = value;
    }
  }
  return target;
}
function isPlainObject(value) {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

// Scripts/storage.ts
var StorageKeys = {
  consent: "bit-belemetry-consent",
  session: "bit-belemetry-session",
  pseudoId: "bit-belemetry-pseudo-id",
  userId: "bit-belemetry-user-id",
  queue: "bit-belemetry-queue"
};
function store(kind) {
  try {
    return kind === "local" ? localStorage : sessionStorage;
  } catch {
    return null;
  }
}
function readItem(kind, key) {
  try {
    const s = store(kind);
    return s ? s.getItem(key) : null;
  } catch {
    return null;
  }
}
function writeItem(kind, key, value) {
  try {
    const s = store(kind);
    if (!s) return false;
    s.setItem(key, value);
    return true;
  } catch {
    return false;
  }
}
function removeItem(kind, key) {
  try {
    const s = store(kind);
    if (s) s.removeItem(key);
  } catch {
  }
}

// Scripts/consent.ts
var ConsentManager = class {
  /**
   * @param available The configured features (`config.features`). The grant never exceeds them: a grant of a
   * feature the configuration switched off is not recorded, and a default or restored decision is narrowed to them.
   */
  constructor(config, available = Features.All) {
    this.config = config;
    this.available = available;
    this.decision = null;
    this.pending = [];
    this.listeners = [];
  }
  /** Reads the persisted choice (if `persist`). */
  load() {
    if (!this.config.persist) return;
    const raw = readItem("local", StorageKeys.consent);
    if (!raw) return;
    try {
      const stored = JSON.parse(raw);
      if (stored && stored.v === 1 && typeof stored.features === "number") {
        this.decision = stored.features & Features.All & this.available;
      }
    } catch {
    }
  }
  get decided() {
    return this.decision !== null;
  }
  /** The features denied by the browser's Global Privacy Control signal: the analytics group, never granted. */
  gpcMask() {
    const nav = typeof navigator !== "undefined" ? navigator : null;
    return this.config.honorGlobalPrivacyControl && nav && nav.globalPrivacyControl === true ? Features.Analytics : 0;
  }
  /** The granted features: always within the configured ones, so exactly what is collected. */
  granted() {
    const value = this.decision !== null ? this.decision : this.config.default;
    return value & Features.All & this.available & ~this.gpcMask();
  }
  /** The features worth collecting now: everything grantable before a decision (to hold it), the grant after. */
  collectable() {
    return this.decision !== null ? this.granted() : Features.All & this.available & ~this.gpcMask();
  }
  isGranted(feature) {
    return (this.granted() & feature) !== 0;
  }
  /** Holds a record until the decision. The oldest record is dropped when `pendingLimit` is exceeded. */
  hold(record) {
    if (this.config.pendingLimit <= 0) return;
    this.pending.push(record);
    if (this.pending.length > this.config.pendingLimit) this.pending.shift();
  }
  get pendingCount() {
    return this.pending.length;
  }
  /** Records the decision, persists it, clears the storage of revoked features and notifies the listeners. */
  set(features) {
    const previous = this.granted();
    this.decision = (features | 0) & Features.All & this.available;
    if (this.config.persist) {
      writeItem("local", StorageKeys.consent, JSON.stringify({ v: 1, features: this.decision, time: Date.now() }));
    }
    this.changed(previous);
  }
  /** Applies new consent settings (a repeated `start`). Before a decision a new default changes the grant. */
  update(config) {
    const previous = this.granted();
    this.config = config;
    this.changed(previous);
  }
  onChange(listener) {
    this.listeners.push(listener);
  }
  changed(previous) {
    const next = this.granted();
    purgeStorage(previous & ~next);
    const released = this.pending.filter((r) => (next & r.feature) !== 0);
    this.pending = this.decided ? [] : this.pending.filter((r) => (next & r.feature) === 0);
    for (const listener of this.listeners) {
      try {
        listener(previous, next, released);
      } catch {
      }
    }
  }
};
function purgeStorage(revoked) {
  if (revoked & Features.Persistence) removeItem("local", StorageKeys.queue);
  if (revoked & Features.Sessions) removeItem("session", StorageKeys.session);
  if (revoked & Features.UserTracking) {
    removeItem("local", StorageKeys.pseudoId);
    removeItem("session", StorageKeys.userId);
  }
}

// Scripts/log.ts
var originalDebug = console.debug;
var originalLog = console.log;
var enabled = false;
function setDebug(on) {
  enabled = on;
}
function debug(...args) {
  if (!enabled) return;
  try {
    originalDebug.apply(console, ["[bit-belemetry]", ...args]);
  } catch {
  }
}
function print(...args) {
  try {
    originalLog.apply(console, args);
  } catch {
  }
}

// Scripts/ids.ts
function randomHex(byteCount) {
  const bytes = new Uint8Array(byteCount);
  try {
    crypto.getRandomValues(bytes);
  } catch {
    for (let i = 0; i < byteCount; i++) bytes[i] = Math.floor(Math.random() * 256);
  }
  let hex = "";
  for (let i = 0; i < byteCount; i++) hex += (bytes[i] < 16 ? "0" : "") + bytes[i].toString(16);
  return hex;
}
function nonZero(byteCount) {
  let id = randomHex(byteCount);
  while (/^0+$/.test(id)) id = randomHex(byteCount);
  return id;
}
function newTraceId() {
  return nonZero(16);
}
function newSpanId() {
  return nonZero(8);
}
function traceparent(traceId, spanId) {
  return "00-" + traceId + "-" + spanId + "-01";
}

// Scripts/model.ts
function isSpan(record) {
  return "startTime" in record;
}
var SpanKind = { Internal: 1, Server: 2, Client: 3, Producer: 4, Consumer: 5 };
var StatusCode = { Unset: 0, Ok: 1, Error: 2 };
var Severity = { Trace: 1, Debug: 5, Info: 9, Warn: 13, Error: 17, Fatal: 21 };
function severityText(severityNumber) {
  return severityNumber >= 21 ? "FATAL" : severityNumber >= 17 ? "ERROR" : severityNumber >= 13 ? "WARN" : severityNumber >= 9 ? "INFO" : severityNumber >= 5 ? "DEBUG" : "TRACE";
}

// Scripts/transport.ts
var MAX_ATTEMPTS = 3;
var KEEPALIVE_LIMIT = 60 * 1024;
var MAX_RETRY_AFTER_MS = 6e4;
var suppressed = 0;
function isSuppressed() {
  return suppressed > 0;
}
function parseRetryAfter(value) {
  if (!value) return null;
  const seconds = Number(value);
  if (!isNaN(seconds)) return Math.max(0, seconds * 1e3);
  const date = Date.parse(value);
  return isNaN(date) ? null : Math.max(0, date - Date.now());
}
function backoff(attempt) {
  return 1e3 * Math.pow(2, attempt - 1) * (0.8 + Math.random() * 0.4);
}
function toBase64(bytes) {
  let binary = "";
  for (let i = 0; i < bytes.length; i += 32768) {
    binary += String.fromCharCode.apply(null, bytes.subarray(i, i + 32768));
  }
  return btoa(binary);
}
function fromBase64(text) {
  const binary = atob(text);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return bytes;
}
async function pipe(bytes, transform) {
  const stream = new Blob([bytes]).stream().pipeThrough(transform);
  return new Uint8Array(await new Response(stream).arrayBuffer());
}
async function gzip(bytes) {
  if (typeof CompressionStream === "undefined") return null;
  try {
    return await pipe(bytes, new CompressionStream("gzip"));
  } catch {
    return null;
  }
}
async function gunzip(bytes) {
  if (typeof DecompressionStream === "undefined") return null;
  try {
    return await pipe(bytes, new DecompressionStream("gzip"));
  } catch {
    return null;
  }
}
function post(request, unload) {
  const headers = {};
  const extra = request.headers || {};
  for (const key of Object.keys(extra)) headers[key] = extra[key];
  headers["Content-Type"] = request.contentType;
  if (request.contentEncoding) headers["Content-Encoding"] = request.contentEncoding;
  suppressed++;
  try {
    return fetch(request.url, {
      method: "POST",
      headers,
      body: request.body,
      // keepalive lets the request outlive the page, but browsers cap in-flight keepalive bodies at 64 KB.
      keepalive: unload || request.body.length <= KEEPALIVE_LIMIT,
      credentials: "omit"
    });
  } finally {
    suppressed--;
  }
}
function loadPersisted() {
  const raw = readItem("local", StorageKeys.queue);
  if (!raw) return [];
  try {
    const list = JSON.parse(raw);
    return Array.isArray(list) ? list.filter((e) => e && typeof e.url === "string" && typeof e.body === "string") : [];
  } catch {
    return [];
  }
}
function savePersisted(list, config, time = Date.now()) {
  const minTime = time - config.maxAgeHours * 36e5;
  const kept = list.filter((e) => e.time >= minTime);
  let json = JSON.stringify(kept);
  while (kept.length && json.length > config.maxBytes) {
    kept.shift();
    json = JSON.stringify(kept);
  }
  while (kept.length && !writeItem("local", StorageKeys.queue, json)) {
    kept.shift();
    json = JSON.stringify(kept);
  }
  if (!kept.length) removeItem("local", StorageKeys.queue);
  return kept;
}
var Transport = class {
  constructor(options) {
    this.options = options;
    this.inflight = [];
    this.resending = false;
  }
  /** Sends a request. During unload there is one keepalive attempt and no waiting. Never rejects. */
  send(request, unload = false) {
    const promise = this.deliver(request, unload).catch((e) => debug("send failed", e)).then(() => {
      const index = this.inflight.indexOf(promise);
      if (index >= 0) this.inflight.splice(index, 1);
    });
    this.inflight.push(promise);
    return promise;
  }
  /** Resolves when every request in flight (including retries) has settled. */
  idle() {
    return Promise.all(this.inflight.slice()).then(() => void 0);
  }
  async deliver(request, unload) {
    let current = request;
    for (let attempt = 1; ; attempt++) {
      let response = null;
      try {
        response = await post(current, unload);
      } catch (e) {
        debug("network error", current.url, e);
      }
      let delay = null;
      if (response) {
        const status = response.status;
        if (status === 206 && current.exporter === "azureMonitor") {
          let next = null;
          try {
            const text = await response.text();
            const plain = current.raw || (current.contentEncoding === "gzip" ? await gunzip(current.body) : null);
            next = rebuildPartial(current, text, plain);
          } catch {
          }
          if (!next) return;
          current = next;
        } else if (status >= 200 && status < 300) {
          return;
        } else if (!isRetryable(status)) {
          debug("dropped request, status", status, current.url);
          return;
        }
        delay = parseRetryAfter(response.headers.get("Retry-After"));
      }
      const offline = !response && typeof navigator !== "undefined" && navigator.onLine === false;
      if (unload || offline || attempt >= MAX_ATTEMPTS || delay !== null && delay > MAX_RETRY_AFTER_MS) {
        await this.persist(current);
        return;
      }
      const wait = delay !== null ? delay : backoff(attempt);
      await (this.options.sleep ? this.options.sleep(wait) : new Promise((resolve) => setTimeout(resolve, wait)));
    }
  }
  /** Stores a request for a later resend, if persistence is configured and consented; otherwise drops it. */
  async persist(request) {
    if (!this.options.persistence.enabled || !this.options.canPersist()) {
      debug("dropped request (no persistence)", request.url);
      return;
    }
    let body = request.raw || null;
    if (!body) body = !request.contentEncoding ? request.body : request.contentEncoding === "gzip" ? await gunzip(request.body) : null;
    if (!body) return;
    const list = loadPersisted();
    list.push({
      id: newSpanId(),
      exporter: request.exporter,
      url: request.url,
      contentType: request.contentType,
      body: toBase64(body),
      time: request.time || Date.now()
    });
    savePersisted(list, this.options.persistence);
  }
  /** Resends the persisted requests (on start and when the browser comes back online). */
  resendPersisted() {
    if (this.resending || !this.options.persistence.enabled || !this.options.canPersist()) return Promise.resolve();
    const list = savePersisted(loadPersisted(), this.options.persistence);
    if (!list.length) return Promise.resolve();
    removeItem("local", StorageKeys.queue);
    this.resending = true;
    const headersFor = this.options.headersFor;
    return Promise.all(list.map((entry) => {
      let body;
      try {
        body = fromBase64(entry.body);
      } catch {
        return void 0;
      }
      return this.send({
        exporter: entry.exporter,
        url: entry.url,
        contentType: entry.contentType,
        headers: headersFor ? headersFor(entry.exporter, entry.url) : null,
        body,
        time: entry.time
      });
    })).then(() => {
      this.resending = false;
    });
  }
};

// Scripts/exporters/exporter.ts
var Signals = {
  Traces: 1,
  Logs: 2,
  Metrics: 4,
  All: 7
};
function signalsOf(config) {
  return config.signals == null ? Signals.All : config.signals;
}

// Scripts/exporters/protobuf.ts
var WireType = { Varint: 0, Fixed64: 1, LengthDelimited: 2, Fixed32: 5 };
var TWO_32 = 4294967296;
var utf8 = null;
function encodeUtf8(text) {
  return (utf8 || (utf8 = new TextEncoder())).encode(text);
}
function splitInt64(value) {
  const negative = value < 0;
  const abs = Math.abs(value);
  let lo = abs % TWO_32 >>> 0;
  let hi = Math.floor(abs / TWO_32) >>> 0;
  if (negative) {
    lo = ~lo + 1 >>> 0;
    hi = ~hi + (lo === 0 ? 1 : 0) >>> 0;
  }
  return [lo, hi];
}
function msToNanos(ms) {
  const micros = Math.max(0, Math.round(ms * 1e3));
  const microsLo = micros % TWO_32;
  const microsHi = Math.floor(micros / TWO_32);
  const lo = microsLo * 1e3;
  return [lo % TWO_32 >>> 0, microsHi * 1e3 + Math.floor(lo / TWO_32) >>> 0];
}
var ProtobufWriter = class _ProtobufWriter {
  constructor() {
    this.buffer = new Uint8Array(256);
    this.view = new DataView(this.buffer.buffer);
    this.pos = 0;
  }
  ensure(extra) {
    if (this.pos + extra <= this.buffer.length) return;
    let size = this.buffer.length * 2;
    while (size < this.pos + extra) size *= 2;
    const next = new Uint8Array(size);
    next.set(this.buffer.subarray(0, this.pos));
    this.buffer = next;
    this.view = new DataView(next.buffer);
  }
  get length() {
    return this.pos;
  }
  finish() {
    return this.buffer.slice(0, this.pos);
  }
  tag(field, wireType) {
    return this.uint32((field << 3 | wireType) >>> 0);
  }
  /** A varint of an unsigned 32-bit value. */
  uint32(value) {
    this.ensure(5);
    value >>>= 0;
    while (value > 127) {
      this.buffer[this.pos++] = value & 127 | 128;
      value >>>= 7;
    }
    this.buffer[this.pos++] = value;
    return this;
  }
  /** A varint of a 64-bit value given as uint32 halves. */
  varint64(lo, hi) {
    this.ensure(10);
    lo >>>= 0;
    hi >>>= 0;
    while (hi > 0 || lo > 127) {
      this.buffer[this.pos++] = lo & 127 | 128;
      lo = (lo >>> 7 | hi << 25) >>> 0;
      hi >>>= 7;
    }
    this.buffer[this.pos++] = lo;
    return this;
  }
  /** A varint of a safe integer as int64 (negative values take 10 bytes, as protobuf requires). */
  int64(value) {
    const [lo, hi] = splitInt64(value);
    return this.varint64(lo, hi);
  }
  fixed32(value) {
    this.ensure(4);
    this.view.setUint32(this.pos, value >>> 0, true);
    this.pos += 4;
    return this;
  }
  fixed64(lo, hi) {
    this.ensure(8);
    this.view.setUint32(this.pos, lo >>> 0, true);
    this.view.setUint32(this.pos + 4, hi >>> 0, true);
    this.pos += 8;
    return this;
  }
  double(value) {
    this.ensure(8);
    this.view.setFloat64(this.pos, value, true);
    this.pos += 8;
    return this;
  }
  raw(bytes) {
    this.ensure(bytes.length);
    this.buffer.set(bytes, this.pos);
    this.pos += bytes.length;
    return this;
  }
  // Field helpers (tag + value).
  uint32Field(field, value) {
    return this.tag(field, WireType.Varint).uint32(value);
  }
  int64Field(field, value) {
    return this.tag(field, WireType.Varint).int64(value);
  }
  boolField(field, value) {
    return this.tag(field, WireType.Varint).uint32(value ? 1 : 0);
  }
  fixed32Field(field, value) {
    return this.tag(field, WireType.Fixed32).fixed32(value);
  }
  fixed64Field(field, lo, hi) {
    return this.tag(field, WireType.Fixed64).fixed64(lo, hi);
  }
  doubleField(field, value) {
    return this.tag(field, WireType.Fixed64).double(value);
  }
  bytesField(field, bytes) {
    return this.tag(field, WireType.LengthDelimited).uint32(bytes.length).raw(bytes);
  }
  stringField(field, value) {
    return this.bytesField(field, encodeUtf8(value));
  }
  /** A nested message: written into a child writer, then copied with its length prefix. */
  messageField(field, write) {
    const child = new _ProtobufWriter();
    write(child);
    return this.bytesField(field, child.finish());
  }
  packedVarint(field, values) {
    const child = new _ProtobufWriter();
    for (const value of values) child.int64(value);
    return this.bytesField(field, child.finish());
  }
  packedFixed64(field, values) {
    const child = new _ProtobufWriter();
    for (const [lo, hi] of values) child.fixed64(lo, hi);
    return this.bytesField(field, child.finish());
  }
  packedDouble(field, values) {
    const child = new _ProtobufWriter();
    for (const value of values) child.double(value);
    return this.bytesField(field, child.finish());
  }
};
function hexToBytes(hex) {
  const bytes = new Uint8Array(hex.length >> 1);
  for (let i = 0; i < bytes.length; i++) bytes[i] = parseInt(hex.substr(i * 2, 2), 16);
  return bytes;
}

// Scripts/exporters/azure.ts
var DEFAULT_INGESTION_ENDPOINT = "https://dc.services.visualstudio.com";
var RETRYABLE_STATUS = [408, 429, 439, 500, 502, 503, 504];
function isRetryable(status) {
  return RETRYABLE_STATUS.indexOf(status) >= 0;
}
function parseConnectionString(connectionString) {
  const map = {};
  for (const part of String(connectionString || "").split(";")) {
    const index = part.indexOf("=");
    if (index > 0) map[part.slice(0, index).trim().toLowerCase()] = part.slice(index + 1).trim();
  }
  const iKey = map["instrumentationkey"];
  if (!iKey) return null;
  let endpoint = map["ingestionendpoint"];
  if (!endpoint && map["endpointsuffix"]) endpoint = "https://" + (map["location"] ? map["location"] + "." : "") + "dc." + map["endpointsuffix"];
  return { iKey, endpoint: (endpoint || DEFAULT_INGESTION_ENDPOINT).replace(/\/+$/, "") };
}
function pad(value, length) {
  let text = String(value);
  while (text.length < length) text = "0" + text;
  return text;
}
function formatDuration(ms) {
  const ticks = Math.max(0, Math.round(ms * 1e4));
  const totalSeconds = Math.floor(ticks / 1e7);
  const fraction = ticks % 1e7;
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor(totalSeconds % 86400 / 3600);
  const minutes = Math.floor(totalSeconds % 3600 / 60);
  const seconds = totalSeconds % 60;
  return (days ? days + "." : "") + pad(hours, 2) + ":" + pad(minutes, 2) + ":" + pad(seconds, 2) + "." + pad(fraction, 7);
}
function severityLevel(severityNumber) {
  return severityNumber >= 21 ? 4 : severityNumber >= 17 ? 3 : severityNumber >= 13 ? 2 : severityNumber >= 9 ? 1 : 0;
}
var TAG_ATTRIBUTES = ["session.id", "enduser.pseudo.id", "user.id"];
function stringify(value) {
  return typeof value === "string" ? value : Array.isArray(value) ? JSON.stringify(value) : String(value);
}
function properties(attributes, consumed) {
  let result = null;
  for (const key of Object.keys(attributes)) {
    if (consumed.indexOf(key) >= 0 || TAG_ATTRIBUTES.indexOf(key) >= 0) continue;
    const value = attributes[key];
    if (value == null) continue;
    (result || (result = {}))[key.slice(0, 150)] = stringify(value).slice(0, 8192);
  }
  return result;
}
function str(attributes, key) {
  const value = attributes[key];
  return value == null ? void 0 : stringify(value);
}
function baseTags(resource, ctx) {
  const tags = {};
  const name = str(resource, "service.name");
  const ns = str(resource, "service.namespace");
  if (name) tags["ai.cloud.role"] = ns ? "[" + ns + "]/" + name : name;
  const instance = str(resource, "service.instance.id");
  if (instance) tags["ai.cloud.roleInstance"] = instance;
  const version = str(resource, "service.version");
  if (version) tags["ai.application.ver"] = version;
  const userAgent = str(resource, "user_agent.original");
  if (userAgent) tags["ai.user.agent"] = userAgent;
  tags["ai.device.type"] = "Browser";
  if (ctx.maskIp) tags["ai.location.ip"] = "0.0.0.0";
  tags["ai.internal.sdkVersion"] = "bitblmjs:" + ctx.sdkVersion;
  return tags;
}
function envelope(type, time, ctx, tags, baseData, props) {
  if (props) baseData.properties = props;
  return {
    ver: 1,
    name: "Microsoft.ApplicationInsights." + type,
    time: new Date(time).toISOString(),
    sampleRate: 100,
    iKey: ctx.iKey,
    tags,
    data: { baseType: type + "Data", baseData }
  };
}
function recordTags(base, attributes, traceId, parentId, operationName) {
  const tags = { ...base };
  if (traceId) tags["ai.operation.id"] = traceId;
  if (parentId) tags["ai.operation.parentId"] = parentId;
  if (operationName) tags["ai.operation.name"] = operationName;
  const session = str(attributes, "session.id");
  if (session) tags["ai.session.id"] = session;
  const pseudo = str(attributes, "enduser.pseudo.id");
  if (pseudo) tags["ai.user.id"] = pseudo;
  const user = str(attributes, "user.id");
  if (user) tags["ai.user.authUserId"] = user;
  return tags;
}
function dependencyTarget(a, url) {
  const host = str(a, "server.address");
  const port = str(a, "server.port");
  if (host) return port && port !== "80" && port !== "443" ? host + ":" + port : host;
  if (!url) return void 0;
  try {
    return new URL(url).host || void 0;
  } catch {
    return void 0;
  }
}
function spanToEnvelope(span, resource, ctx) {
  const a = span.attributes;
  const success = !span.status || span.status.code !== StatusCode.Error;
  const duration = formatDuration(span.endTime - span.startTime);
  const statusCode = str(a, "http.response.status_code");
  if (span.kind === SpanKind.Server || span.kind === SpanKind.Consumer) {
    const method = str(a, "http.request.method");
    const route = str(a, "http.route");
    const name = method && route ? method + " " + route : span.name;
    const tags2 = recordTags(baseTags(resource, ctx), a, span.traceId, span.parentSpanId, name);
    const consumed2 = ["http.response.status_code", "url.full"];
    const data2 = { ver: 2, id: span.spanId, name, duration, responseCode: statusCode || (success ? "200" : "500"), success };
    const url2 = str(a, "url.full");
    if (url2) data2.url = url2;
    return envelope("Request", span.startTime, ctx, tags2, data2, properties(a, consumed2));
  }
  const tags = recordTags(baseTags(resource, ctx), a, span.traceId, span.parentSpanId);
  const consumed = ["http.response.status_code", "url.full", "server.address", "server.port"];
  const url = str(a, "url.full");
  const isHttp = a["http.request.method"] != null || url !== void 0;
  const code = Number(a["http.response.status_code"]);
  const data = {
    ver: 2,
    id: span.spanId,
    name: span.name,
    resultCode: statusCode || "",
    data: url || span.name,
    duration,
    success: success && (!code || code < 400),
    type: isHttp ? "Ajax" : "InProc"
  };
  const target = dependencyTarget(a, url);
  if (target) data.target = target;
  return envelope("RemoteDependency", span.startTime, ctx, tags, data, properties(a, consumed));
}
function logToEnvelope(log, resource, ctx) {
  const a = log.attributes;
  const base = baseTags(resource, ctx);
  if (a["exception.type"] != null || a["exception.message"] != null) {
    const stack = str(a, "exception.stacktrace");
    const detail = { id: 1, outerId: 0, typeName: str(a, "exception.type") || "Error", message: str(a, "exception.message") || log.body || "", hasFullStack: !!stack };
    if (stack) detail.stack = stack;
    const tags2 = recordTags(base, a, log.traceId, log.spanId);
    return envelope(
      "Exception",
      log.time,
      ctx,
      tags2,
      { ver: 2, exceptions: [detail], severityLevel: severityLevel(log.severityNumber) },
      properties(a, Object.keys(a).filter((key) => key.indexOf("exception.") === 0))
    );
  }
  if (log.eventName === "browser.page_view") {
    const url = str(a, "url.full");
    const name = str(a, "page.title") || str(a, "url.path") || url || log.body;
    const tags2 = recordTags(base, a, log.traceId, log.spanId, name);
    const data = { ver: 2, name, id: newSpanId(), duration: "00:00:00" };
    if (url) data.url = url;
    return envelope("PageView", log.time, ctx, tags2, data, properties(a, ["url.full", "page.title"]));
  }
  const tags = recordTags(base, a, log.traceId, log.spanId);
  if (log.eventName === "browser.web_vital") {
    const value = a["web_vital.value"];
    const metric = { name: str(a, "web_vital.name") || log.body, kind: 0, value: typeof value === "number" ? value : Number(value) || 0, count: 1 };
    return envelope("Metric", log.time, ctx, tags, { ver: 2, metrics: [metric] }, properties(a, ["web_vital.name", "web_vital.value"]));
  }
  if (log.eventName) {
    return envelope("Event", log.time, ctx, tags, { ver: 2, name: log.eventName }, properties(a, []));
  }
  return envelope("Message", log.time, ctx, tags, { ver: 2, message: (log.body || "").slice(0, 32768), severityLevel: severityLevel(log.severityNumber) }, properties(a, []));
}
function toEnvelopes(batch, ctx) {
  const envelopes = [];
  for (const span of batch.spans) envelopes.push(spanToEnvelope(span, batch.resource, ctx));
  for (const log of batch.logs) envelopes.push(logToEnvelope(log, batch.resource, ctx));
  return envelopes;
}
function rebuildPartial(request, responseText, plainBody) {
  const result = JSON.parse(responseText);
  const errors = result && result.errors;
  if (!Array.isArray(errors)) return null;
  const items = JSON.parse(new TextDecoder().decode(plainBody || request.raw || request.body));
  if (!Array.isArray(items)) return null;
  const retry = [];
  for (const error of errors) {
    if (error && isRetryable(error.statusCode) && items[error.index] !== void 0) retry.push(items[error.index]);
  }
  if (!retry.length) return null;
  return { ...request, body: encodeUtf8(JSON.stringify(retry)), raw: null, contentEncoding: null };
}
function createAzureExporter(config, transport, sdkVersion) {
  const target = parseConnectionString(config.connectionString);
  if (!target) {
    debug("azureMonitor: invalid connection string");
    return { urls: [], export: () => Promise.resolve() };
  }
  const url = target.endpoint + "/v2.1/track";
  const ctx = { iKey: target.iKey, maskIp: config.maskIp !== false, sdkVersion };
  const compress = config.compression === "gzip";
  const signals = signalsOf(config);
  return {
    urls: [target.endpoint],
    async export(batch, unload) {
      const filtered = {
        resource: batch.resource,
        spans: signals & Signals.Traces ? batch.spans : [],
        logs: signals & Signals.Logs ? batch.logs : []
      };
      const envelopes = toEnvelopes(filtered, ctx);
      if (!envelopes.length) return;
      const body = encodeUtf8(JSON.stringify(envelopes));
      const zipped = compress && !unload ? await gzip(body) : null;
      transport.send({
        exporter: "azureMonitor",
        url,
        contentType: "application/json",
        contentEncoding: zipped ? "gzip" : null,
        body: zipped || body,
        raw: zipped ? body : null
      }, unload);
    }
  };
}

// Scripts/exporters/console.ts
function describe(record) {
  if (isSpan(record)) return "span " + record.name + " (" + Math.round(record.endTime - record.startTime) + " ms)";
  return record.severityText + " " + (record.eventName ? record.eventName + " " : "") + record.body;
}
function createConsoleExporter() {
  return {
    urls: [],
    export(batch) {
      for (const record of [...batch.spans, ...batch.logs]) print("[bit-belemetry] " + describe(record), record.attributes);
      return Promise.resolve();
    }
  };
}

// Scripts/exporters/otlp-common.ts
function groupByScope(records) {
  const groups = {};
  const order = [];
  for (const record of records) {
    const scope = record.scope || "bit.belemetry";
    if (!groups[scope]) {
      groups[scope] = [];
      order.push(scope);
    }
    groups[scope].push(record);
  }
  return order.map((scope) => [scope, groups[scope]]);
}
function isInt(value) {
  return Number.isSafeInteger(value) && !Object.is(value, -0);
}
function msToNanosString(ms) {
  const micros = Math.max(0, Math.round(ms * 1e3));
  return micros === 0 ? "0" : micros + "000";
}

// Scripts/exporters/otlp-json.ts
function toAnyValue(value) {
  if (typeof value === "string") return { stringValue: value };
  if (typeof value === "boolean") return { boolValue: value };
  if (typeof value === "number") {
    if (isInt(value)) return { intValue: String(value) };
    return { doubleValue: isFinite(value) ? value : String(value) };
  }
  if (Array.isArray(value)) return { arrayValue: { values: value.map(toAnyValue) } };
  return {};
}
function toKeyValues(attributes) {
  const list = [];
  for (const key of Object.keys(attributes || {})) {
    const value = attributes[key];
    if (value != null) list.push({ key, value: toAnyValue(value) });
  }
  return list;
}
function scopeJson(name, version) {
  return version ? { name, version } : { name };
}
function spanJson(span) {
  const json = {
    traceId: span.traceId,
    spanId: span.spanId
  };
  if (span.parentSpanId) json.parentSpanId = span.parentSpanId;
  json.flags = 1;
  json.name = span.name;
  json.kind = span.kind;
  json.startTimeUnixNano = msToNanosString(span.startTime);
  json.endTimeUnixNano = msToNanosString(span.endTime);
  json.attributes = toKeyValues(span.attributes);
  if (span.events && span.events.length) {
    json.events = span.events.map((e) => ({ timeUnixNano: msToNanosString(e.time), name: e.name, attributes: toKeyValues(e.attributes) }));
  }
  const status = {};
  if (span.status && span.status.message) status.message = span.status.message;
  if (span.status && span.status.code) status.code = span.status.code;
  json.status = status;
  return json;
}
function logJson(log) {
  const json = {
    timeUnixNano: msToNanosString(log.time),
    observedTimeUnixNano: msToNanosString(log.time),
    severityNumber: log.severityNumber,
    severityText: log.severityText,
    body: toAnyValue(log.body),
    attributes: toKeyValues(log.attributes)
  };
  if (log.traceId) {
    json.flags = 1;
    json.traceId = log.traceId;
  }
  if (log.spanId) json.spanId = log.spanId;
  if (log.eventName) json.eventName = log.eventName;
  return json;
}
function tracesToJson(resource, spans, scopeVersion) {
  return {
    resourceSpans: [{
      resource: { attributes: toKeyValues(resource) },
      scopeSpans: groupByScope(spans).map(([scope, records]) => ({ scope: scopeJson(scope, scopeVersion), spans: records.map(spanJson) }))
    }]
  };
}
function logsToJson(resource, logs, scopeVersion) {
  return {
    resourceLogs: [{
      resource: { attributes: toKeyValues(resource) },
      scopeLogs: groupByScope(logs).map(([scope, records]) => ({ scope: scopeJson(scope, scopeVersion), logRecords: records.map(logJson) }))
    }]
  };
}

// Scripts/exporters/otlp-proto.ts
function writeAnyValue(w, value) {
  if (typeof value === "string") w.stringField(1, value);
  else if (typeof value === "boolean") w.boolField(2, value);
  else if (typeof value === "number") {
    if (isInt(value)) w.int64Field(3, value);
    else w.doubleField(4, value);
  } else if (Array.isArray(value)) {
    w.messageField(5, (a) => {
      for (const item of value) a.messageField(1, (v) => writeAnyValue(v, item));
    });
  }
}
function writeAttributes(w, field, attributes) {
  for (const key of Object.keys(attributes)) {
    const value = attributes[key];
    if (value == null) continue;
    w.messageField(field, (kv) => {
      kv.stringField(1, key);
      kv.messageField(2, (v) => writeAnyValue(v, value));
    });
  }
}
function writeScope(w, name, version) {
  w.messageField(1, (s) => {
    s.stringField(1, name);
    if (version) s.stringField(2, version);
  });
}
function writeTime(w, field, ms) {
  const [lo, hi] = msToNanos(ms);
  w.fixed64Field(field, lo, hi);
}
function writeSpan(w, span) {
  w.bytesField(1, hexToBytes(span.traceId));
  w.bytesField(2, hexToBytes(span.spanId));
  if (span.parentSpanId) w.bytesField(4, hexToBytes(span.parentSpanId));
  w.stringField(5, span.name);
  w.uint32Field(6, span.kind);
  writeTime(w, 7, span.startTime);
  writeTime(w, 8, span.endTime);
  writeAttributes(w, 9, span.attributes);
  for (const event of span.events || []) {
    w.messageField(11, (e) => {
      writeTime(e, 1, event.time);
      e.stringField(2, event.name);
      writeAttributes(e, 3, event.attributes || {});
    });
  }
  const status = span.status;
  if (status && (status.code || status.message)) {
    w.messageField(15, (s) => {
      if (status.message) s.stringField(2, status.message);
      if (status.code) s.uint32Field(3, status.code);
    });
  }
  w.fixed32Field(16, 1);
}
function writeLog(w, log) {
  writeTime(w, 1, log.time);
  if (log.severityNumber) w.uint32Field(2, log.severityNumber);
  if (log.severityText) w.stringField(3, log.severityText);
  if (log.body != null) w.messageField(5, (v) => writeAnyValue(v, log.body));
  writeAttributes(w, 6, log.attributes);
  if (log.traceId) w.fixed32Field(8, 1);
  if (log.traceId) w.bytesField(9, hexToBytes(log.traceId));
  if (log.spanId) w.bytesField(10, hexToBytes(log.spanId));
  writeTime(w, 11, log.time);
  if (log.eventName) w.stringField(12, log.eventName);
}
function encodeTraces(resource, spans, scopeVersion) {
  const w = new ProtobufWriter();
  w.messageField(1, (rs) => {
    rs.messageField(1, (r) => writeAttributes(r, 1, resource));
    for (const [scope, records] of groupByScope(spans)) {
      rs.messageField(2, (ss) => {
        writeScope(ss, scope, scopeVersion);
        for (const span of records) ss.messageField(2, (s) => writeSpan(s, span));
      });
    }
  });
  return w.finish();
}
function encodeLogs(resource, logs, scopeVersion) {
  const w = new ProtobufWriter();
  w.messageField(1, (rl) => {
    rl.messageField(1, (r) => writeAttributes(r, 1, resource));
    for (const [scope, records] of groupByScope(logs)) {
      rl.messageField(2, (sl) => {
        writeScope(sl, scope, scopeVersion);
        for (const log of records) sl.messageField(2, (l) => writeLog(l, log));
      });
    }
  });
  return w.finish();
}

// Scripts/exporters/otlp.ts
var DEFAULT_OTLP_ENDPOINT = "http://localhost:4318";
function otlpUrls(config) {
  const base = (config.endpoint || DEFAULT_OTLP_ENDPOINT).replace(/\/+$/, "");
  return { base, traces: config.tracesEndpoint || base + "/v1/traces", logs: config.logsEndpoint || base + "/v1/logs" };
}
function createOtlpExporter(config, transport, sdkVersion) {
  const urls = otlpUrls(config);
  const json = config.protocol === "http/json";
  const contentType = json ? "application/json" : "application/x-protobuf";
  const compress = config.compression === "gzip";
  const signals = signalsOf(config);
  const send = async (url, body, unload) => {
    let payload = body;
    let encoding = null;
    if (compress && !unload) {
      const zipped = await gzip(body);
      if (zipped) {
        payload = zipped;
        encoding = "gzip";
      }
    }
    transport.send({ exporter: "otlp", url, contentType, contentEncoding: encoding, headers: config.headers, body: payload, raw: encoding ? body : null }, unload);
  };
  return {
    urls: [urls.base, urls.traces, urls.logs],
    headers: config.headers,
    export(batch, unload) {
      const sends = [];
      if (batch.spans.length && signals & Signals.Traces) {
        const body = json ? encodeUtf8(JSON.stringify(tracesToJson(batch.resource, batch.spans, sdkVersion))) : encodeTraces(batch.resource, batch.spans, sdkVersion);
        sends.push(send(urls.traces, body, unload));
      }
      if (batch.logs.length && signals & Signals.Logs) {
        const body = json ? encodeUtf8(JSON.stringify(logsToJson(batch.resource, batch.logs, sdkVersion))) : encodeLogs(batch.resource, batch.logs, sdkVersion);
        sends.push(send(urls.logs, body, unload));
      }
      return Promise.all(sends).then(() => void 0);
    }
  };
}

// Scripts/exporters/relay.ts
var DotNetCallbacks = {
  relay: "Belemetry.Relay",
  contextChanged: "Belemetry.ContextChanged",
  drainForUnload: "Belemetry.DrainForUnload"
};
function relayBatch(ref, batch) {
  try {
    return ref.invokeMethodAsync(DotNetCallbacks.relay, batch).then(() => void 0, (e) => debug("relay failed", e));
  } catch (e) {
    debug("relay failed", e);
    return Promise.resolve();
  }
}

// Scripts/clock.ts
function timeOrigin() {
  const p = performance;
  return p.timeOrigin || p.timing && p.timing.navigationStart || Date.now() - p.now();
}
function now() {
  return timeOrigin() + performance.now();
}
function toEpoch(highResTime) {
  return timeOrigin() + highResTime;
}

// Scripts/records.ts
function createLog(scope, feature, severityNumber, body, eventName, attributes = {}, time = now()) {
  return {
    time,
    severityNumber,
    severityText: severityText(severityNumber),
    body,
    eventName,
    attributes,
    traceId: null,
    spanId: null,
    scope,
    feature
  };
}
function createSpan(scope, feature, name, kind, startTime, endTime, attributes = {}, traceId = newTraceId(), spanId = newSpanId()) {
  return {
    traceId,
    spanId,
    parentSpanId: null,
    name,
    kind,
    startTime,
    endTime,
    attributes,
    status: { code: StatusCode.Unset },
    events: [],
    scope,
    feature
  };
}
function createException(scope, type, message, stack, attributes) {
  const attrs = { ...attributes || {} };
  attrs["exception.type"] = type || "Error";
  attrs["exception.message"] = message || "";
  if (stack) attrs["exception.stacktrace"] = stack;
  return createLog(scope, Features.Errors, Severity.Error, message || type || "Error", "exception", attrs);
}
function sanitizeAttributes(input) {
  const output = {};
  if (!input || typeof input !== "object") return output;
  for (const key of Object.keys(input)) {
    const value = input[key];
    const type = typeof value;
    if (type === "string" || type === "boolean" || type === "number" && isFinite(value)) {
      output[key] = value;
    } else if (Array.isArray(value)) {
      const first2 = typeof value[0];
      if (value.every((v) => typeof v === first2 && (first2 === "string" || first2 === "boolean" || first2 === "number"))) output[key] = value;
    }
  }
  return output;
}

// Scripts/instrumentations/console.ts
var CONSOLE_SCOPE = "bit.belemetry.console";
var SEVERITY = { error: Severity.Error, warn: Severity.Warn, info: Severity.Info, log: Severity.Info, debug: Severity.Debug, trace: Severity.Trace };
var busy = false;
function format(value) {
  if (typeof value === "string") return value;
  if (value instanceof Error) return value.name + ": " + value.message;
  try {
    const json = JSON.stringify(value);
    return json === void 0 ? String(value) : json.slice(0, 1024);
  } catch {
    return String(value);
  }
}
function installConsole(ctx) {
  const target = console;
  const restore = [];
  for (const level of ctx.config.logs.consoleLevels || []) {
    const original = target[level];
    if (typeof original !== "function" || !(level in SEVERITY)) continue;
    const wrapper = function(...args) {
      const result = original.apply(this, args);
      if (!busy) {
        busy = true;
        try {
          const attributes = {};
          const error = args.find((a) => a instanceof Error);
          if (error) {
            attributes["exception.type"] = error.name;
            attributes["exception.message"] = error.message;
            if (error.stack) attributes["exception.stacktrace"] = error.stack;
          }
          const feature = level === "error" ? Features.Errors : Features.Logs;
          ctx.emit(createLog(CONSOLE_SCOPE, feature, SEVERITY[level], args.map(format).join(" ").slice(0, 8192), null, attributes));
        } catch {
        } finally {
          busy = false;
        }
      }
      return result;
    };
    target[level] = wrapper;
    restore.push(() => {
      if (target[level] === wrapper) target[level] = original;
    });
  }
  return () => {
    for (const undo of restore) undo();
  };
}

// Scripts/instrumentations/context.ts
var noop = () => {
};
function guard(fn) {
  return (...args) => {
    try {
      fn(...args);
    } catch {
    }
  };
}
function compilePatterns(sources) {
  const list = [];
  for (const source of sources || []) {
    try {
      list.push(new RegExp(source));
    } catch {
    }
  }
  return list;
}
function matchesAny(patterns, text) {
  for (const pattern of patterns) {
    if (pattern.test(text)) return true;
  }
  return false;
}
function absoluteUrl(url) {
  try {
    return new URL(url, location.href).href;
  } catch {
    return url;
  }
}

// Scripts/instrumentations/document-load.ts
var DOCUMENT_LOAD_SCOPE = "bit.belemetry.document_load";
var EVENTS = [
  "fetchStart",
  "domainLookupStart",
  "domainLookupEnd",
  "connectStart",
  "secureConnectionStart",
  "connectEnd",
  "requestStart",
  "responseStart",
  "responseEnd",
  "domInteractive",
  "domContentLoadedEventStart",
  "domContentLoadedEventEnd",
  "domComplete",
  "loadEventStart",
  "loadEventEnd"
];
var emitted = false;
function resetDocumentLoadState() {
  emitted = false;
}
function emit(ctx) {
  if (emitted) return;
  const nav = performance.getEntriesByType("navigation")[0];
  if (!nav) return;
  emitted = true;
  const span = createSpan(
    DOCUMENT_LOAD_SCOPE,
    Features.Performance,
    "documentLoad",
    SpanKind.Internal,
    toEpoch(nav.startTime),
    toEpoch(nav.loadEventEnd || nav.duration || performance.now())
  );
  span.attributes["url.full"] = location.href;
  if (nav.encodedBodySize) span.attributes["http.response.body.size"] = nav.encodedBodySize;
  if (nav.nextHopProtocol) span.attributes["network.protocol.name"] = nav.nextHopProtocol;
  for (const name of EVENTS) {
    const value = nav[name];
    if (typeof value === "number" && value > 0) span.events.push({ name, time: toEpoch(value), attributes: {} });
  }
  ctx.emit(span);
}
function installDocumentLoad(ctx) {
  if (emitted) return noop;
  let timer = null;
  const run = guard(() => {
    timer = setTimeout(guard(() => emit(ctx)), 0);
  });
  if (document.readyState === "complete") run();
  else addEventListener("load", run);
  return () => {
    removeEventListener("load", run);
    if (timer) clearTimeout(timer);
  };
}

// Scripts/instrumentations/errors.ts
var ERRORS_SCOPE = "bit.belemetry.errors";
function describeReason(reason) {
  if (reason && typeof reason === "object" && ("message" in reason || "stack" in reason)) {
    const e = reason;
    return { type: e.name || "Error", message: String(e.message || ""), stack: e.stack };
  }
  let message;
  try {
    message = typeof reason === "string" ? reason : JSON.stringify(reason);
  } catch {
    message = String(reason);
  }
  return { type: "UnhandledRejection", message: message || String(reason) };
}
function installErrors(ctx) {
  const ignore = compilePatterns(ctx.config.errors.ignoreMessages);
  const report = (type, message, stack) => {
    if (matchesAny(ignore, message)) return;
    ctx.emit(createException(ERRORS_SCOPE, type, message, stack));
  };
  const onError = guard((event) => {
    const error = event.error;
    const message = event.message || error && error.message || "";
    if (!message && !error) return;
    const stack = error && error.stack || (event.filename ? "at " + event.filename + ":" + event.lineno + ":" + event.colno : void 0);
    report(error && error.name || "Error", String(message), stack);
  });
  const onRejection = guard((event) => {
    const reason = describeReason(event.reason);
    report(reason.type, reason.message, reason.stack);
  });
  addEventListener("error", onError);
  addEventListener("unhandledrejection", onRejection);
  return () => {
    removeEventListener("error", onError);
    removeEventListener("unhandledrejection", onRejection);
  };
}

// Scripts/instrumentations/http.ts
var DEPENDENCIES_SCOPE = "bit.belemetry.http";
function createPropagationMatcher(targets) {
  const list = targets || [];
  const sameOrigin = list.indexOf("same-origin") >= 0;
  const all = list.indexOf("*") >= 0;
  const patterns = compilePatterns(list.filter((t) => t !== "same-origin" && t !== "*"));
  return (url) => {
    if (all) return true;
    if (sameOrigin) {
      try {
        if (new URL(url, location.href).origin === location.origin) return true;
      } catch {
      }
    }
    return matchesAny(patterns, url);
  };
}
function createHttpTracer(ctx) {
  const ignore = compilePatterns(ctx.config.dependencies.ignoreUrls);
  const propagate = createPropagationMatcher(ctx.config.dependencies.propagateTraceContextTo);
  return {
    shouldTrace: (url, hasTraceparent) => !hasTraceparent && !isSuppressed() && !ctx.isExporterUrl(url) && !matchesAny(ignore, url),
    shouldPropagate: propagate,
    start(method, url) {
      const span = createSpan(DEPENDENCIES_SCOPE, Features.Dependencies, method, SpanKind.Client, now(), 0);
      span.attributes["http.request.method"] = method;
      span.attributes["url.full"] = url;
      try {
        const parsed = new URL(url);
        span.attributes["server.address"] = parsed.hostname;
        span.attributes["server.port"] = Number(parsed.port) || (parsed.protocol === "https:" ? 443 : 80);
      } catch {
      }
      return span;
    },
    end(span, status, error) {
      span.endTime = now();
      if (status > 0) span.attributes["http.response.status_code"] = status;
      if (error !== void 0) {
        const e = error;
        span.attributes["error.type"] = e && e.name || "Error";
        span.status = { code: StatusCode.Error, message: e && e.message || String(error) };
      } else if (status >= 400) {
        span.attributes["error.type"] = String(status);
        span.status = { code: StatusCode.Error };
      }
      ctx.emit(span);
    }
  };
}

// Scripts/instrumentations/fetch.ts
function installFetch(ctx) {
  const g = globalThis;
  const original = g.fetch;
  if (typeof original !== "function") return noop;
  const tracer = createHttpTracer(ctx);
  let active = true;
  const wrapped = function(input, init2) {
    let span = null;
    try {
      if (active) {
        const request = typeof Request !== "undefined" && input instanceof Request ? input : null;
        const url = absoluteUrl(request ? request.url : String(input));
        const headers = new Headers(init2 && init2.headers || (request ? request.headers : void 0));
        if (tracer.shouldTrace(url, headers.has("traceparent"))) {
          const method = String(init2 && init2.method || request && request.method || "GET").toUpperCase();
          span = tracer.start(method, url);
          if (tracer.shouldPropagate(url)) {
            headers.set("traceparent", traceparent(span.traceId, span.spanId));
            init2 = { ...init2 || {}, headers };
          }
        }
      }
    } catch {
      span = null;
    }
    const result = original.call(globalThis, input, init2);
    if (!span) return result;
    const s = span;
    return result.then((response) => {
      try {
        tracer.end(s, response.status);
      } catch {
      }
      return response;
    }, (error) => {
      try {
        tracer.end(s, 0, error);
      } catch {
      }
      throw error;
    });
  };
  g.fetch = wrapped;
  return () => {
    active = false;
    if (g.fetch === wrapped) g.fetch = original;
  };
}

// Scripts/instrumentations/interactions.ts
var INTERACTION_SCOPE = "bit.belemetry.interactions";
var INTERACTIVE = "a,button,[role=button],input[type=submit],input[type=button],[data-bit-belemetry-name]";
function resolveTarget(target) {
  let node = target;
  if (node && node.nodeType !== 1) node = node.parentElement;
  const element = node;
  if (!element || typeof element.closest !== "function") return null;
  if (element.closest("[data-bit-belemetry-ignore]")) return null;
  return element.closest(INTERACTIVE);
}
function xpath(element) {
  const parts = [];
  let node = element;
  for (let depth = 0; node && node.nodeType === 1 && depth < 32; depth++) {
    if (node.id) {
      parts.unshift('//*[@id="' + node.id + '"]');
      return parts.join("/");
    }
    let index = 1;
    for (let sibling = node.previousElementSibling; sibling; sibling = sibling.previousElementSibling) {
      if (sibling.nodeName === node.nodeName) index++;
    }
    parts.unshift(node.nodeName.toLowerCase() + "[" + index + "]");
    node = node.parentElement;
  }
  return "/" + parts.join("/");
}
function describeTarget(element, eventType, config) {
  const attributes = {
    "event.type": eventType,
    "target.element": element.nodeName.toLowerCase()
  };
  if (element.id) attributes["target.id"] = element.id;
  const name = element.getAttribute("data-bit-belemetry-name") || element.getAttribute("aria-label") || element.getAttribute("name");
  if (name) attributes["target.name"] = name;
  if (config.captureText && element.nodeName !== "INPUT") {
    const text = (element.textContent || "").replace(/\s+/g, " ").trim();
    if (text) attributes["target.text"] = text.slice(0, Math.max(0, config.maxTextLength));
  }
  attributes["target.xpath"] = xpath(element);
  return attributes;
}
function installInteractions(ctx) {
  const config = ctx.config.interactions;
  const events = config.events && config.events.length ? config.events : ["click"];
  const handler = guard((event) => {
    const path = typeof event.composedPath === "function" ? event.composedPath() : [];
    const element = resolveTarget(path.length ? path[0] : event.target);
    if (!element) return;
    const attributes = describeTarget(element, event.type, config);
    const label = attributes["target.name"] || attributes["target.id"] || attributes["target.element"];
    ctx.emit(createLog(INTERACTION_SCOPE, Features.Interactions, Severity.Info, event.type + " " + label, "browser.user_interaction", attributes));
  });
  for (const type of events) document.addEventListener(type, handler, true);
  return () => {
    for (const type of events) document.removeEventListener(type, handler, true);
  };
}

// Scripts/instrumentations/long-tasks.ts
var LONG_TASKS_SCOPE = "bit.belemetry.long_tasks";
function installLongTasks(ctx) {
  if (typeof PerformanceObserver === "undefined") return noop;
  try {
    const supported = PerformanceObserver.supportedEntryTypes;
    if (supported && supported.indexOf("longtask") < 0) return noop;
    const observer = new PerformanceObserver(guard((list) => {
      for (const entry of list.getEntries()) {
        const start2 = toEpoch(entry.startTime);
        ctx.emit(createSpan(LONG_TASKS_SCOPE, Features.Performance, "longtask", SpanKind.Internal, start2, start2 + entry.duration));
      }
    }));
    observer.observe({ type: "longtask", buffered: true });
    return () => observer.disconnect();
  } catch {
    return noop;
  }
}

// Scripts/instrumentations/page-views.ts
var PAGE_VIEW_SCOPE = "bit.belemetry.page_views";
var DEDUPE_MS = 100;
var state = { key: "", url: "", time: -Infinity, initialSent: false };
function resetPageViewState() {
  state.key = "";
  state.url = "";
  state.time = -Infinity;
  state.initialSent = false;
}
function pageKey(url, trackHash) {
  return trackHash ? url : url.split("#")[0];
}
function trackPageView(ctx, type, options = {}) {
  const url = absoluteUrl(options.url || location.href);
  const key = pageKey(url, ctx.config.pageViews.trackHashChanges);
  const time = now();
  if (key === state.key && (options.onlyIfChanged || time - state.time < DEDUPE_MS)) return false;
  const referrer = state.url || document.referrer;
  state.key = key;
  state.url = url;
  state.time = time;
  if (type === "initial") state.initialSent = true;
  let path = url;
  try {
    path = new URL(url).pathname;
  } catch {
  }
  const title = document.title || "";
  const attributes = { ...options.attributes || {} };
  attributes["url.full"] = url;
  attributes["url.path"] = path;
  attributes["page.title"] = title;
  if (referrer) attributes["page.referrer"] = referrer;
  attributes["browser.navigation.type"] = type;
  ctx.emit(createLog(PAGE_VIEW_SCOPE, Features.PageViews, Severity.Info, options.name || title || path, "browser.page_view", attributes, time));
  return true;
}
function installPageViews(ctx) {
  if (!state.initialSent) trackPageView(ctx, "initial");
  let active = true;
  const h = history;
  const originalPush = h.pushState;
  const originalReplace = h.replaceState;
  const later = (type, onlyIfChanged) => setTimeout(guard(() => {
    if (active) trackPageView(ctx, type, { onlyIfChanged });
  }), 0);
  const pushState = function(...args) {
    const result = originalPush.apply(this, args);
    try {
      if (active) later("push", false);
    } catch {
    }
    return result;
  };
  const replaceState = function(...args) {
    const result = originalReplace.apply(this, args);
    try {
      if (active) later("replace", true);
    } catch {
    }
    return result;
  };
  h.pushState = pushState;
  h.replaceState = replaceState;
  const onPop = guard(() => trackPageView(ctx, "pop", { onlyIfChanged: true }));
  const onHash = guard(() => trackPageView(ctx, "hash", { onlyIfChanged: true }));
  const onEnhancedLoad = guard(() => trackPageView(ctx, "push", { onlyIfChanged: true }));
  addEventListener("popstate", onPop);
  if (ctx.config.pageViews.trackHashChanges) addEventListener("hashchange", onHash);
  let blazor = null;
  const hookBlazor = guard(() => {
    const candidate = window.Blazor;
    if (active && !blazor && candidate && typeof candidate.addEventListener === "function") {
      blazor = candidate;
      candidate.addEventListener("enhancedload", onEnhancedLoad);
    }
  });
  hookBlazor();
  if (!blazor) addEventListener("load", hookBlazor);
  return () => {
    active = false;
    if (h.pushState === pushState) h.pushState = originalPush;
    if (h.replaceState === replaceState) h.replaceState = originalReplace;
    removeEventListener("popstate", onPop);
    removeEventListener("hashchange", onHash);
    removeEventListener("load", hookBlazor);
    const b = blazor;
    if (b && typeof b.removeEventListener === "function") b.removeEventListener("enhancedload", onEnhancedLoad);
  };
}

// Scripts/instrumentations/resources.ts
var RESOURCES_SCOPE = "bit.belemetry.resources";
var SKIPPED_INITIATORS = ["fetch", "xmlhttprequest", "beacon"];
function installResources(ctx) {
  if (typeof PerformanceObserver === "undefined") return noop;
  const onEntries = (entries) => {
    for (const entry of entries) {
      if (SKIPPED_INITIATORS.indexOf(entry.initiatorType) >= 0 || ctx.isExporterUrl(entry.name)) continue;
      const span = createSpan(RESOURCES_SCOPE, Features.Performance, "resourceFetch", SpanKind.Internal, toEpoch(entry.startTime), toEpoch(entry.responseEnd || entry.startTime + entry.duration));
      span.attributes["url.full"] = entry.name;
      if (entry.encodedBodySize) span.attributes["http.response.body.size"] = entry.encodedBodySize;
      if (entry.nextHopProtocol) span.attributes["network.protocol.name"] = entry.nextHopProtocol;
      ctx.emit(span);
    }
  };
  try {
    const observer = new PerformanceObserver(guard((list) => onEntries(list.getEntries())));
    observer.observe({ type: "resource", buffered: true });
    return () => observer.disconnect();
  } catch {
    return noop;
  }
}

// Scripts/lifecycle.ts
var first = [];
var last = [];
var installed = false;
function fire() {
  for (const callback of first.concat(last)) {
    try {
      callback();
    } catch {
    }
  }
}
function onVisibilityChange() {
  if (document.visibilityState === "hidden") fire();
}
function onHidden(callback, phase = "first") {
  if (!installed && typeof document !== "undefined") {
    installed = true;
    document.addEventListener("visibilitychange", onVisibilityChange, true);
    addEventListener("pagehide", fire, true);
  }
  const list = phase === "first" ? first : last;
  list.push(callback);
  return () => {
    const index = list.indexOf(callback);
    if (index >= 0) list.splice(index, 1);
  };
}

// Scripts/instrumentations/web-vitals.ts
var WEB_VITALS_SCOPE = "bit.belemetry.web_vitals";
var THRESHOLDS = {
  LCP: [2500, 4e3],
  INP: [200, 500],
  CLS: [0.1, 0.25],
  FCP: [1800, 3e3],
  TTFB: [800, 1800]
};
function rate(name, value) {
  const [good, poor] = THRESHOLDS[name];
  return value > poor ? "poor" : value > good ? "needs-improvement" : "good";
}
var ClsAccumulator = class {
  constructor() {
    this.value = 0;
    this.windowValue = 0;
    this.first = 0;
    this.last = 0;
  }
  add(startTime, shift) {
    if (this.windowValue && startTime - this.last < 1e3 && startTime - this.first < 5e3) {
      this.windowValue += shift;
    } else {
      this.windowValue = shift;
      this.first = startTime;
    }
    this.last = startTime;
    if (this.windowValue > this.value) this.value = this.windowValue;
  }
};
var InpAccumulator = class {
  constructor() {
    this.count = 0;
    this.longest = [];
  }
  add(interactionId, duration) {
    if (!interactionId) return;
    const existing = this.longest.find((i) => i.id === interactionId);
    if (existing) {
      existing.latency = Math.max(existing.latency, duration);
    } else {
      this.count++;
      const min = this.longest[this.longest.length - 1];
      if (this.longest.length < 10 || duration > min.latency) this.longest.push({ id: interactionId, latency: duration });
    }
    this.longest.sort((a, b) => b.latency - a.latency);
    this.longest.splice(10);
  }
  value(interactionCount) {
    if (!this.longest.length) return null;
    const count = interactionCount || this.count;
    return this.longest[Math.min(this.longest.length - 1, Math.floor(count / 50))].latency;
  }
};
function navigationEntry() {
  try {
    const entry = performance.getEntriesByType("navigation")[0];
    return entry ? entry : null;
  } catch {
    return null;
  }
}
function observe(type, callback, options = {}) {
  try {
    if (typeof PerformanceObserver === "undefined") return null;
    const supported = PerformanceObserver.supportedEntryTypes;
    if (supported && supported.indexOf(type) < 0) return null;
    const observer = new PerformanceObserver(guard((list) => callback(list.getEntries())));
    observer.observe({ type, buffered: true, ...options });
    return observer;
  } catch {
    return null;
  }
}
var reported = {};
var ids = {};
function resetWebVitalsState() {
  reported = {};
  ids = {};
}
function installWebVitals(ctx) {
  const nav = navigationEntry();
  const activationStart = nav && nav.activationStart || 0;
  const prerendering = document.prerendering;
  const navigationType = prerendering || activationStart > 0 ? "prerender" : nav ? String(nav.type).replace(/_/g, "-") : "navigate";
  let firstHidden = document.visibilityState === "hidden" ? 0 : Infinity;
  const observers = [];
  const report = (name, value) => {
    const previous = reported[name];
    if (previous === value) return;
    reported[name] = value;
    const id = ids[name] || (ids[name] = "v1-" + Date.now() + "-" + Math.floor(Math.random() * 9e12 + 1e12));
    ctx.emit(createLog(WEB_VITALS_SCOPE, Features.Performance, Severity.Info, name, "browser.web_vital", {
      "web_vital.name": name,
      "web_vital.value": value,
      "web_vital.delta": value - (previous || 0),
      "web_vital.rating": rate(name, value),
      "web_vital.id": id,
      "web_vital.navigation_type": navigationType
    }));
  };
  const track = (observer) => {
    if (observer) observers.push(observer);
    return observer;
  };
  if (nav && nav.responseStart > 0) report("TTFB", Math.max(0, nav.responseStart - activationStart));
  const fcp = track(observe("paint", (entries) => {
    for (const entry of entries) {
      if (entry.name === "first-contentful-paint" && entry.startTime < firstHidden) {
        report("FCP", Math.max(0, entry.startTime - activationStart));
        if (fcp) fcp.disconnect();
      }
    }
  }));
  let lcp = null;
  let lcpDone = false;
  const onLcp = (entries) => {
    for (const entry of entries) if (entry.startTime < firstHidden) lcp = Math.max(0, entry.startTime - activationStart);
  };
  const lcpObserver = track(observe("largest-contentful-paint", onLcp));
  const finalizeLcp = guard(() => {
    if (lcpDone) return;
    lcpDone = true;
    if (lcpObserver) {
      onLcp(lcpObserver.takeRecords());
      lcpObserver.disconnect();
    }
    if (lcp !== null) report("LCP", lcp);
    for (const type of ["keydown", "click"]) removeEventListener(type, finalizeLcp, true);
  });
  for (const type of ["keydown", "click"]) addEventListener(type, finalizeLcp, true);
  const cls = new ClsAccumulator();
  let sawShift = false;
  const onShift = (entries) => {
    for (const entry of entries) {
      if (!entry.hadRecentInput) {
        sawShift = true;
        cls.add(entry.startTime, entry.value);
      }
    }
  };
  const clsObserver = track(observe("layout-shift", onShift));
  const inp = new InpAccumulator();
  const onEvents = (entries) => {
    for (const entry of entries) inp.add(entry.interactionId || 0, entry.duration);
  };
  const inpObserver = track(observe("event", onEvents, { durationThreshold: 40 }));
  track(observe("first-input", onEvents));
  const onHide = () => {
    if (firstHidden === Infinity) firstHidden = performance.now();
    finalizeLcp();
    if (clsObserver) onShift(clsObserver.takeRecords());
    if (clsObserver && (sawShift || reported.FCP !== void 0)) report("CLS", cls.value);
    if (inpObserver) onEvents(inpObserver.takeRecords());
    const count = performance.interactionCount;
    const value = inp.value(count);
    if (value !== null) report("INP", value);
  };
  const removeHidden = onHidden(onHide, "first");
  return () => {
    removeHidden();
    for (const observer of observers) observer.disconnect();
    for (const type of ["keydown", "click"]) removeEventListener(type, finalizeLcp, true);
  };
}

// Scripts/instrumentations/xhr.ts
function installXhr(ctx) {
  const Xhr = globalThis.XMLHttpRequest;
  if (typeof Xhr !== "function") return noop;
  const proto = Xhr.prototype;
  const originalOpen = proto.open;
  const originalSend = proto.send;
  const originalSetRequestHeader = proto.setRequestHeader;
  const tracer = createHttpTracer(ctx);
  const info = /* @__PURE__ */ new WeakMap();
  let active = true;
  const open = function(...args) {
    try {
      if (active) info.set(this, { method: String(args[0]).toUpperCase(), url: absoluteUrl(String(args[1])), traced: false });
    } catch {
    }
    return originalOpen.apply(this, args);
  };
  const setRequestHeader = function(name, value) {
    try {
      const data = info.get(this);
      if (data && String(name).toLowerCase() === "traceparent") data.traced = true;
    } catch {
    }
    return originalSetRequestHeader.call(this, name, value);
  };
  const send = function(body) {
    try {
      const data = info.get(this);
      if (active && data && tracer.shouldTrace(data.url, data.traced)) {
        const span = tracer.start(data.method, data.url);
        if (tracer.shouldPropagate(data.url)) originalSetRequestHeader.call(this, "traceparent", traceparent(span.traceId, span.spanId));
        const xhr = this;
        const onEnd = () => {
          try {
            xhr.removeEventListener("loadend", onEnd);
            if (xhr.status > 0) tracer.end(span, xhr.status);
            else tracer.end(span, 0, { name: "NetworkError", message: "XMLHttpRequest failed" });
          } catch {
          }
        };
        xhr.addEventListener("loadend", onEnd);
      }
    } catch {
    }
    return originalSend.call(this, body);
  };
  proto.open = open;
  proto.setRequestHeader = setRequestHeader;
  proto.send = send;
  return () => {
    active = false;
    if (proto.open === open) proto.open = originalOpen;
    if (proto.setRequestHeader === setRequestHeader) proto.setRequestHeader = originalSetRequestHeader;
    if (proto.send === send) proto.send = originalSend;
  };
}

// Scripts/privacy.ts
var URL_ATTRIBUTES = ["url.full", "page.referrer", "url.original", "http.url"];
var ID_ATTRIBUTES = ["session.id", "enduser.pseudo.id", "user.id"];
var BEARER = /\bBearer\s+[A-Za-z0-9\-._~+/]+=*/gi;
var JWT = /\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]*/g;
var EMAIL = /[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/g;
var CARD = /\b\d(?:[ -]?\d){12,18}\b/g;
var IPV4 = new RegExp("(?<!Version=)\\b(?:(?:25[0-5]|2[0-4]\\d|1?\\d?\\d)\\.){3}(?:25[0-5]|2[0-4]\\d|1?\\d?\\d)\\b", "g");
function luhn(digits) {
  let sum = 0;
  let double = false;
  for (let i = digits.length - 1; i >= 0; i--) {
    let d = digits.charCodeAt(i) - 48;
    if (d < 0 || d > 9) return false;
    if (double) {
      d *= 2;
      if (d > 9) d -= 9;
    }
    sum += d;
    double = !double;
  }
  return sum % 10 === 0;
}
function scrub(text) {
  if (!text) return text;
  return text.replace(BEARER, "Bearer [token]").replace(JWT, "[jwt]").replace(EMAIL, "[email]").replace(CARD, (match) => {
    const digits = match.replace(/[ -]/g, "");
    return digits.length >= 13 && digits.length <= 19 && luhn(digits) ? "[card]" : match;
  }).replace(IPV4, "[ip]");
}
function redactUrl(url, privacy) {
  if (!url) return url;
  let parsed;
  try {
    parsed = new URL(url, typeof location !== "undefined" ? location.href : void 0);
  } catch {
    return url;
  }
  if (parsed.username || parsed.password) {
    parsed.username = "";
    parsed.password = "";
  }
  const href = parsed.href;
  const search = parsed.search;
  const hash = parsed.hash;
  const base = href.slice(0, href.length - search.length - hash.length);
  const names = {};
  for (const name of privacy.redactQueryParameters || []) names[String(name).toLowerCase()] = true;
  const redact = (part) => part.replace(/([?&#])([^=&#]*)=([^&#]*)/g, (match, sep, key) => {
    let name = key;
    try {
      name = decodeURIComponent(key.replace(/\+/g, " "));
    } catch {
    }
    return names[name.toLowerCase()] ? sep + key + "=REDACTED" : match;
  });
  return base + (privacy.stripQueryString ? "" : redact(search)) + (privacy.stripFragment ? "" : redact(hash));
}
var HOST_ATTRIBUTES = ["server.address"];
function scrubUrl(url) {
  const schemeEnd = url.indexOf("://");
  if (schemeEnd < 0) return scrub(url);
  const authorityStart = schemeEnd + 3;
  let pathStart = url.length;
  for (const c of ["/", "?", "#"]) {
    const index = url.indexOf(c, authorityStart);
    if (index >= 0 && index < pathStart) pathStart = index;
  }
  let authority = url.slice(authorityStart, pathStart);
  const at = authority.lastIndexOf("@");
  if (at >= 0) authority = authority.slice(at + 1);
  return url.slice(0, authorityStart) + authority + scrub(url.slice(pathStart));
}
function clean(attributes, privacy) {
  const blocked = privacy.blockedAttributes || [];
  for (const key of Object.keys(attributes)) {
    if (ID_ATTRIBUTES.indexOf(key) >= 0) continue;
    if (blocked.indexOf(key) >= 0) {
      delete attributes[key];
      continue;
    }
    const value = attributes[key];
    if (typeof value === "string") {
      if (HOST_ATTRIBUTES.indexOf(key) >= 0) continue;
      const isUrl = URL_ATTRIBUTES.indexOf(key) >= 0;
      let next = isUrl ? redactUrl(value, privacy) : value;
      if (privacy.scrubPatterns) next = isUrl ? scrubUrl(next) : scrub(next);
      attributes[key] = next;
    } else if (privacy.scrubPatterns && Array.isArray(value) && typeof value[0] === "string") {
      attributes[key] = value.map(scrub);
    }
  }
}
function applyPrivacy(record, privacy) {
  clean(record.attributes, privacy);
  if (isSpan(record)) {
    if (privacy.scrubPatterns && record.status.message) record.status.message = scrub(record.status.message);
    for (const event of record.events) clean(event.attributes, privacy);
  } else if (privacy.scrubPatterns) {
    record.body = scrub(record.body);
  }
}

// Scripts/session.ts
var SessionManager = class {
  constructor(config) {
    this.config = config;
    this.state = null;
    this.persistent = false;
    this.lastSaved = 0;
    this.tracking = false;
    this.pseudo = null;
    this.user = null;
    /** Called when the session id or the user ids change. */
    this.onChange = null;
  }
  /** Turns sessionStorage persistence on or off. Turning it on at start adopts a stored, still valid session. */
  setPersistent(on, time = now()) {
    if (on === this.persistent) return;
    this.persistent = on;
    if (!on) {
      removeItem("session", StorageKeys.session);
      return;
    }
    if (!this.state) {
      const raw = readItem("session", StorageKeys.session);
      try {
        const stored = raw ? JSON.parse(raw) : null;
        if (stored && typeof stored.id === "string" && /^[0-9a-f]{32}$/.test(stored.id) && !this.expired(stored, time)) {
          this.state = { id: stored.id, start: +stored.start, last: +stored.last };
        }
      } catch {
      }
    }
    this.save();
  }
  /** The current session id, rotating it when it expired. Every call counts as activity. */
  id(time = now()) {
    const state3 = this.state;
    if (!state3 || this.expired(state3, time)) {
      this.state = { id: newTraceId(), start: time, last: time };
      this.save();
      if (state3) this.notify();
      return this.state.id;
    }
    if (time > state3.last) state3.last = time;
    if (time - this.lastSaved > 1e3) this.save();
    return state3.id;
  }
  /** The current session id without counting as activity (creates one if there is none yet). */
  peek() {
    return this.state ? this.state.id : this.id();
  }
  expired(state3, time) {
    return time - state3.last > this.config.inactivityTimeoutMinutes * 6e4 || time - state3.start > this.config.maxDurationMinutes * 6e4;
  }
  save() {
    if (!this.persistent || !this.state) return;
    this.lastSaved = this.state.last;
    writeItem("session", StorageKeys.session, JSON.stringify(this.state));
  }
  get userTracking() {
    return this.tracking;
  }
  /** Turns user tracking on (loading or creating the pseudo id) or off (forgetting both ids). */
  setUserTracking(on) {
    if (on === this.tracking) return;
    this.tracking = on;
    if (on) {
      let pseudo = readItem("local", StorageKeys.pseudoId);
      if (!pseudo || !/^[0-9a-f]{32}$/.test(pseudo)) {
        pseudo = newTraceId();
        writeItem("local", StorageKeys.pseudoId, pseudo);
      }
      this.pseudo = pseudo;
      if (this.user === null) this.user = readItem("session", StorageKeys.userId);
    } else {
      this.pseudo = null;
      this.user = null;
      removeItem("local", StorageKeys.pseudoId);
      removeItem("session", StorageKeys.userId);
    }
    this.notify();
  }
  get pseudoId() {
    return this.pseudo;
  }
  get userId() {
    return this.user;
  }
  /** Sets the authenticated user id. It is only kept with user tracking; returns whether it was kept. */
  setUser(id) {
    const value = this.tracking && id ? String(id) : null;
    const changed = value !== this.user;
    this.user = value;
    if (value) writeItem("session", StorageKeys.userId, value);
    else removeItem("session", StorageKeys.userId);
    if (changed) this.notify();
    return value !== null;
  }
  notify() {
    try {
      if (this.onChange) this.onChange();
    } catch {
    }
  }
};
function isSessionSampled(sessionId, ratio) {
  if (!(ratio < 1)) return true;
  if (!(ratio > 0)) return false;
  const bucket = parseInt(sessionId.slice(0, 8), 16);
  return !isNaN(bucket) && bucket / 4294967296 < ratio;
}

// Scripts/pipeline.ts
var Pipeline = class {
  constructor(config, consent, session, resource) {
    this.config = config;
    this.consent = consent;
    this.session = session;
    this.resource = resource;
    this.queue = [];
    this.timer = null;
    this.soon = false;
    /** Null holds the queue (relay transport before .NET attached). */
    this.sink = null;
  }
  get size() {
    return this.queue.length;
  }
  /** Entry point of every record. Never throws. */
  emit(record) {
    try {
      if (!(this.config.features & record.feature)) return;
      const sessionId = this.session.id();
      if (!isSessionSampled(sessionId, this.config.sampling.sessionRatio)) return;
      if (!(this.consent.collectable() & record.feature)) return;
      applyPrivacy(record, this.config.privacy);
      record.attributes["session.id"] = sessionId;
      if (this.consent.isGranted(record.feature)) this.enqueue(record);
      else this.consent.hold(record);
    } catch (e) {
      debug("emit failed", e);
    }
  }
  /** Queues an allowed record (also used for the records released by a consent decision). */
  enqueue(record) {
    if (this.session.userTracking) {
      const pseudo = this.session.pseudoId;
      const user = this.session.userId;
      if (pseudo) record.attributes["enduser.pseudo.id"] = pseudo;
      if (user) record.attributes["user.id"] = user;
    }
    this.queue.push(record);
    const batch = this.config.batch;
    if (this.queue.length > batch.maxQueueSize) this.queue.splice(0, this.queue.length - batch.maxQueueSize);
    if (this.queue.length >= batch.maxBatchSize) {
      if (!this.soon) {
        this.soon = true;
        setTimeout(() => {
          this.soon = false;
          this.flush();
        }, 0);
      }
    } else if (!this.timer) {
      this.timer = setTimeout(() => {
        this.timer = null;
        this.flush();
      }, batch.flushIntervalMs);
    }
  }
  /** Drops the queued records of revoked features (and the user ids when user tracking is revoked). */
  purge(revoked) {
    if (!revoked) return;
    this.queue = this.queue.filter((r) => (r.feature & revoked) === 0);
    if (revoked & Features.UserTracking) {
      for (const record of this.queue) {
        delete record.attributes["enduser.pseudo.id"];
        delete record.attributes["user.id"];
      }
    }
  }
  /** Hands every queued record to the sink in batches of `maxBatchSize`. */
  flush(unload = false) {
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = null;
    }
    const sink = this.sink;
    if (!sink || !this.queue.length) return Promise.resolve();
    const sends = [];
    const size = Math.max(1, this.config.batch.maxBatchSize);
    while (this.queue.length) {
      const batch = { resource: this.resource, spans: [], logs: [] };
      for (const record of this.queue.splice(0, size)) {
        if (isSpan(record)) batch.spans.push(record);
        else batch.logs.push(record);
      }
      try {
        sends.push(Promise.resolve(sink(batch, unload)).catch((e) => debug("export failed", e)));
      } catch (e) {
        debug("export failed", e);
      }
    }
    return Promise.all(sends).then(() => void 0);
  }
};

// Scripts/resource.ts
var pageInstanceId;
function buildResource(config) {
  const attributes = {};
  const set = (key, value) => {
    if (value != null && value !== "") attributes[key] = value;
  };
  const service = config.service;
  set("service.name", service.name);
  set("service.version", service.version);
  set("service.namespace", service.namespace);
  set("service.instance.id", service.instanceId || (pageInstanceId != null ? pageInstanceId : pageInstanceId = newTraceId()));
  set("deployment.environment.name", service.environment);
  set("telemetry.sdk.name", "bit-belemetry");
  set("telemetry.sdk.language", "webjs");
  set("telemetry.sdk.version", config.sdkVersion);
  if (typeof navigator !== "undefined") {
    set("browser.language", navigator.language);
    const uaData = navigator.userAgentData;
    if (uaData) {
      if (uaData.brands) attributes["browser.brands"] = uaData.brands.map((b) => b.brand + " " + b.version);
      if (typeof uaData.mobile === "boolean") attributes["browser.mobile"] = uaData.mobile;
      set("browser.platform", uaData.platform);
    }
    if (config.privacy.collectUserAgent) set("user_agent.original", navigator.userAgent);
  }
  const extra = config.resource || {};
  for (const key of Object.keys(extra)) set(key, extra[key]);
  return attributes;
}

// Scripts/bit-belemetry.ts
var INSTRUMENTATIONS = [
  ["errors", Features.Errors, () => true, installErrors],
  ["pageViews", Features.PageViews, () => true, installPageViews],
  ["interactions", Features.Interactions, () => true, installInteractions],
  ["webVitals", Features.Performance, (c) => c.performance.webVitals, installWebVitals],
  ["documentLoad", Features.Performance, (c) => c.performance.documentLoad, installDocumentLoad],
  ["resources", Features.Performance, (c) => c.performance.resources, installResources],
  ["longTasks", Features.Performance, (c) => c.performance.longTasks, installLongTasks],
  ["fetch", Features.Dependencies, (c) => c.dependencies.fetch, installFetch],
  ["xhr", Features.Dependencies, (c) => c.dependencies.xhr, installXhr],
  ["console", Features.Errors | Features.Logs, (c) => c.logs.captureConsole, installConsole]
];
var state2 = null;
var bridge = null;
var lastContext = "";
function effective(s) {
  return s.config.features & s.consent.granted();
}
function createExporters(config, transport) {
  const list = [];
  for (const exporter of config.exporters || []) {
    try {
      if (exporter.type === "otlp") list.push(createOtlpExporter(exporter, transport, config.sdkVersion));
      else if (exporter.type === "azureMonitor") list.push(createAzureExporter(exporter, transport, config.sdkVersion));
      else if (exporter.type === "console") list.push(createConsoleExporter());
    } catch (e) {
      debug("invalid exporter", exporter, e);
    }
  }
  return list;
}
function exporterFor(s, url) {
  const absolute = absoluteUrl(url);
  for (const exporter of s.exporters) {
    for (const prefix of exporter.urls) {
      if (prefix && absolute.indexOf(absoluteUrl(prefix)) === 0) return exporter;
    }
  }
  return void 0;
}
function applyGrant(s) {
  const features = effective(s);
  s.session.setPersistent((features & Features.Sessions) !== 0);
  s.session.setUserTracking((features & Features.UserTracking) !== 0);
}
function reconcile(s) {
  const mask = s.config.features & s.consent.collectable();
  for (const [key, features, enabled2, install] of INSTRUMENTATIONS) {
    const want = (mask & features) !== 0 && enabled2(s.config);
    const current = s.installed[key];
    try {
      if (want && !current) {
        s.installed[key] = install(s.ctx);
        debug("installed", key);
      } else if (!want && current) {
        delete s.installed[key];
        current();
        debug("uninstalled", key);
      }
    } catch (e) {
      debug("instrumentation failed", key, e);
    }
  }
}
function updateSink(s) {
  const relay = s.config.transport === "relay" || bridge !== null && bridge.relay;
  if (relay) {
    s.pipeline.sink = bridge ? ((batch) => relayBatch(bridge.ref, batch)) : null;
  } else {
    s.pipeline.sink = (batch, unload) => Promise.all(s.exporters.map((exporter) => {
      try {
        return exporter.export(batch, unload).catch((e) => debug("export failed", e));
      } catch (e) {
        debug("export failed", e);
        return void 0;
      }
    })).then(() => void 0);
  }
}
function syncContext() {
  if (!bridge) return;
  try {
    const context = getContext();
    const json = JSON.stringify(context);
    if (json === lastContext) return;
    lastContext = json;
    bridge.ref.invokeMethodAsync(DotNetCallbacks.contextChanged, context).catch((e) => debug("context sync failed", e));
  } catch (e) {
    debug("context sync failed", e);
  }
}
function onConsentChanged(s, previous, next, released) {
  s.pipeline.purge(previous & ~next);
  applyGrant(s);
  for (const record of released) {
    if (s.config.features & record.feature) s.pipeline.enqueue(record);
  }
  reconcile(s);
  if (next & ~previous & Features.Persistence) s.transport.resendPersisted();
  debug("consent", next, "decided", s.consent.decided);
  syncContext();
}
function onPageHidden(s) {
  s.pipeline.flush(true);
  if (bridge && bridge.unloadDrain) {
    try {
      const requests = bridge.ref.invokeMethod(DotNetCallbacks.drainForUnload);
      sendRequestsCore(requests, true);
    } catch (e) {
      debug("unload drain unavailable", e);
    }
  }
}
function init(input) {
  const config = resolveConfig(input);
  setDebug(!!config.debug);
  const consent = new ConsentManager(config.consent, config.features);
  consent.load();
  const session = new SessionManager(config.session);
  let s;
  const transport = new Transport({
    persistence: config.persistence,
    canPersist: () => (effective(s) & Features.Persistence) !== 0,
    headersFor: (_exporter, url) => {
      const exporter = exporterFor(s, url);
      return exporter ? exporter.headers : null;
    }
  });
  const exporters = createExporters(config, transport);
  const pipeline = new Pipeline(config, consent, session, buildResource(config));
  s = {
    config,
    consent,
    session,
    pipeline,
    transport,
    exporters,
    ctx: {
      config,
      emit: (record) => pipeline.emit(record),
      isExporterUrl: (url) => exporterFor(s, url) !== void 0
    },
    installed: {},
    disposers: []
  };
  return s;
}
function start(config) {
  try {
    if (state2) {
      const s2 = state2;
      const next = resolveConfig(config);
      s2.config.transport = next.transport;
      updateSink(s2);
      s2.config.consent = next.consent;
      s2.consent.update(next.consent);
      return;
    }
    const s = state2 = init(config);
    applyGrant(s);
    updateSink(s);
    s.consent.onChange((previous, next, released) => onConsentChanged(s, previous, next, released));
    s.session.onChange = syncContext;
    s.disposers.push(onHidden(() => onPageHidden(s), "last"));
    const onOnline = () => s.transport.resendPersisted();
    addEventListener("online", onOnline);
    s.disposers.push(() => removeEventListener("online", onOnline));
    const retryMs = s.config.persistence.retryIntervalMs;
    if (s.config.persistence.enabled && retryMs > 0) {
      const timer = setInterval(onOnline, Math.max(1e3, retryMs));
      s.disposers.push(() => clearInterval(timer));
    }
    reconcile(s);
    s.transport.resendPersisted();
    syncContext();
    debug("started", s.config);
  } catch (e) {
    debug("start failed", e);
  }
}
function setConsent(features) {
  try {
    if (state2) state2.consent.set(features);
  } catch (e) {
    debug("setConsent failed", e);
  }
}
function getConsent() {
  return state2 ? state2.consent.granted() : 0;
}
function isConsentDecided() {
  return state2 ? state2.consent.decided : false;
}
function setUser(id) {
  try {
    if (state2) state2.session.setUser(id);
  } catch (e) {
    debug("setUser failed", e);
  }
}
function trackEvent(name, attributes) {
  try {
    if (state2 && name) state2.pipeline.emit(createLog("bit.belemetry.events", Features.Events, Severity.Info, String(name), String(name), sanitizeAttributes(attributes)));
  } catch (e) {
    debug("trackEvent failed", e);
  }
}
function trackPageView2(name, url, attributes) {
  try {
    if (state2) trackPageView(state2.ctx, "manual", { name, url, attributes: sanitizeAttributes(attributes) });
  } catch (e) {
    debug("trackPageView failed", e);
  }
}
function trackException(type, message, stack, attributes) {
  try {
    if (state2) state2.pipeline.emit(createException(ERRORS_SCOPE, String(type || "Error"), String(message || ""), stack, sanitizeAttributes(attributes)));
  } catch (e) {
    debug("trackException failed", e);
  }
}
function flush() {
  try {
    return state2 ? state2.pipeline.flush(false) : Promise.resolve();
  } catch (e) {
    debug("flush failed", e);
    return Promise.resolve();
  }
}
function getContext() {
  if (!state2) return { consent: 0, decided: false, sessionId: null, pseudoId: null, userId: null };
  const s = state2;
  return { consent: s.consent.granted(), decided: s.consent.decided, sessionId: s.session.peek(), pseudoId: s.session.pseudoId, userId: s.session.userId };
}
function attachDotNet(dotNetRef, options) {
  try {
    bridge = dotNetRef ? { ref: dotNetRef, relay: !!(options && options.relay), unloadDrain: !!(options && options.unloadDrain) } : null;
    lastContext = "";
    if (state2) {
      updateSink(state2);
      syncContext();
      state2.pipeline.flush();
    }
  } catch (e) {
    debug("attachDotNet failed", e);
  }
}
function sendRequestsCore(requests, unload) {
  if (!Array.isArray(requests) || !requests.length) return Promise.resolve();
  const transport = state2 ? state2.transport : new Transport({ persistence: resolveConfig().persistence, canPersist: () => false });
  const sends = [];
  for (const request of requests) {
    try {
      sends.push(transport.send({
        exporter: request.exporter,
        url: request.url,
        contentType: request.contentType,
        contentEncoding: request.contentEncoding || null,
        headers: request.headers || null,
        body: fromBase64(request.body || "")
      }, unload));
    } catch (e) {
      debug("invalid request", e);
    }
  }
  return Promise.all(sends).then(() => void 0);
}
function sendRequests(requests) {
  try {
    return sendRequestsCore(requests, false);
  } catch (e) {
    debug("sendRequests failed", e);
    return Promise.resolve();
  }
}
function _reset() {
  if (state2) {
    for (const key of Object.keys(state2.installed)) {
      try {
        state2.installed[key]();
      } catch {
      }
    }
    for (const dispose of state2.disposers) dispose();
    state2.pipeline.sink = null;
  }
  state2 = null;
  bridge = null;
  lastContext = "";
  resetPageViewState();
  resetDocumentLoadState();
  resetWebVitalsState();
}
function _getState() {
  return state2;
}
var api = { start, setConsent, getConsent, isConsentDecided, setUser, trackEvent, trackPageView: trackPageView2, trackException, flush, getContext, attachDotNet, sendRequests, Features };
if (typeof window !== "undefined") {
  window.BitBelemetry = api;
  try {
    const element = document.getElementById("bit-belemetry-config");
    if (element) {
      let config = null;
      try {
        config = JSON.parse(element.textContent || "");
      } catch {
        config = null;
      }
      if (config) start(config);
    }
  } catch {
  }
}
export {
  Features,
  _getState,
  _reset,
  attachDotNet,
  flush,
  getConsent,
  getContext,
  isConsentDecided,
  sendRequests,
  setConsent,
  setUser,
  start,
  trackEvent,
  trackException,
  trackPageView2 as trackPageView
};
