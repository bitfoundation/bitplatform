import type { ResolvedConfig } from './config';
import type { ConsentManager, ConsentMark } from './consent';
import { Features } from './features';
import { ErrorLimiter, limitRecord } from './limits';
import { debug } from './log';
import { isSpan, type Attributes, type RecordBatch, type TelemetryRecord } from './model';
import { applyPrivacy, isBlocked } from './privacy';
import { isSessionSampled, type SessionManager } from './session';
import { countDropped } from './stats';

/**
 * Where batches go: the exporters (direct) or .NET (relay). `mark` is the consent the batch left the queue under: a
 * request built from it is withdrawn once one of its features is revoked, during a compression or a retry.
 */
export type Sink = (batch: RecordBatch, unload: boolean, mark: ConsentMark) => Promise<void>;

/**
 * The app's own processing of a record (`addProcessor`), run as the record is handed to the exporters: it may change
 * the record in place, and return `false` to drop it. It must be fast and synchronous - during unload nothing else runs.
 */
export type Processor = (record: TelemetryRecord) => boolean | void;

/**
 * consent check -> error limits -> privacy -> value length limit -> enrichment -> bounded queue -> batches. Records of
 * features the user has not decided on yet are held by the consent manager; records the user (or the configuration)
 * does not allow are dropped.
 */
export class Pipeline {
    private queue: TelemetryRecord[] = [];
    private timer: ReturnType<typeof setTimeout> | null = null;
    private soon = false;
    private errors: ErrorLimiter;
    private failedProcessors: Processor[] = [];

    /** Null holds the queue (relay transport before .NET attached). */
    sink: Sink | null = null;

    /** The app's processors, in order (shared with the module, so they can be added before start). */
    processors: Processor[] = [];

    /** Attributes the app set for every record (`setAttributes`); a record's own attributes win. */
    attributes: Attributes = {};

    constructor(public config: ResolvedConfig, private consent: ConsentManager, private session: SessionManager, public resource: Attributes) {
        this.errors = new ErrorLimiter(config.errors);
    }

    get size(): number {
        return this.queue.length;
    }

    /** Entry point of every record. Never throws. */
    emit(record: TelemetryRecord): void {
        try {
            if (!(this.config.features & record.feature)) return;
            const sessionId = this.session.id();
            if (!isSessionSampled(sessionId, this.config.sampling.sessionRatio)) {
                countDropped('sampled_out');
                return;
            }
            if (!(this.consent.collectable() & record.feature)) {
                countDropped('not_consented');
                return;
            }
            // An error thrown in a loop would fill the queue and evict everything else.
            if (record.feature === Features.Errors && !isSpan(record) && !this.errors.allow(record)) {
                countDropped('error_limit');
                return;
            }
            // Before privacy: the app's context (a tenant, a feature flag) is scrubbed like everything else.
            for (const key of Object.keys(this.attributes)) {
                if (!(key in record.attributes)) record.attributes[key] = this.attributes[key];
            }
            applyPrivacy(record, this.config.privacy);
            // After privacy, so a cut never hides an e-mail address from the scrubber.
            limitRecord(record, this.config.maxValueLength);
            this.setId(record, 'session.id', sessionId);
            if (this.consent.isGranted(record.feature)) this.enqueue(record);
            else this.consent.hold(record);
        } catch (e) {
            debug('emit failed', e);
        }
    }

    /** Queues an allowed record (also used for the records released by a consent decision). */
    enqueue(record: TelemetryRecord): void {
        if (this.session.userTracking) {
            const pseudo = this.session.pseudoId;
            const user = this.session.userId;
            if (pseudo) this.setId(record, 'enduser.pseudo.id', pseudo);
            if (user) this.setId(record, 'user.id', user);
        }
        this.queue.push(record);
        const batch = this.config.batch;
        if (this.queue.length > batch.maxQueueSize) countDropped('queue_full', this.queue.splice(0, this.queue.length - batch.maxQueueSize).length);
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

    /** Adds an id after privacy, which leaves the ids alone - unless the operator blocked it. */
    private setId(record: TelemetryRecord, key: string, value: string): void {
        if (!isBlocked(key, this.config.privacy)) record.attributes[key] = value;
    }

    /** Drops the queued records of revoked features (and the user ids when user tracking is revoked). */
    purge(revoked: number): void {
        if (!revoked) return;
        const before = this.queue.length;
        this.queue = this.queue.filter(r => (r.feature & revoked) === 0);
        countDropped('consent_revoked', before - this.queue.length);
        if (revoked & Features.UserTracking) {
            for (const record of this.queue) {
                delete record.attributes['enduser.pseudo.id'];
                delete record.attributes['user.id'];
            }
        }
    }

    /** Hands every queued record to the sink in batches of `maxBatchSize`. */
    flush(unload = false): Promise<void> {
        if (this.timer) {
            clearTimeout(this.timer);
            this.timer = null;
        }
        const sink = this.sink;
        if (!sink || !this.queue.length) return Promise.resolve();
        const sends: Promise<void>[] = [];
        const size = Math.max(1, this.config.batch.maxBatchSize);
        while (this.queue.length) {
            const batch: RecordBatch = { resource: this.resource, spans: [], logs: [] };
            // With UserTracking while the ids are added: a revoke of it withdraws the batch too.
            let features = this.session.userTracking ? Features.UserTracking : 0;
            for (const record of this.queue.splice(0, size)) {
                if (!this.process(record)) continue;
                features |= record.feature;
                if (isSpan(record)) batch.spans.push(record);
                else batch.logs.push(record);
            }
            if (!batch.spans.length && !batch.logs.length) continue;
            try {
                // As it leaves the queue, which a revoke purges: a revoke from here on is one the batch escaped.
                sends.push(Promise.resolve(sink(batch, unload, this.consent.mark(features))).catch(e => debug('export failed', e)));
            } catch (e) {
                debug('export failed', e);
            }
        }
        return Promise.all(sends).then(() => undefined);
    }

    /**
     * Runs the app's processors on a record; false drops it. Run here, at export, rather than when the record is
     * emitted, so a processor added right after the script started still sees the initial page view.
     */
    private process(record: TelemetryRecord): boolean {
        for (const processor of this.processors) {
            try {
                if (processor(record) === false) {
                    countDropped('filtered');
                    return false;
                }
            } catch (e) {
                // Skipped for this record, as in .NET; said once per processor.
                if (this.failedProcessors.indexOf(processor) < 0) {
                    this.failedProcessors.push(processor);
                    debug('processor failed', e);
                }
            }
        }
        return true;
    }
}
