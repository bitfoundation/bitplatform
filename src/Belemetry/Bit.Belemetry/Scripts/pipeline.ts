import type { ResolvedConfig } from './config';
import type { ConsentManager } from './consent';
import { Features } from './features';
import { debug } from './log';
import { isSpan, type Attributes, type RecordBatch, type TelemetryRecord } from './model';
import { applyPrivacy } from './privacy';
import { isSessionSampled, type SessionManager } from './session';

/** Where batches go: the exporters (direct) or .NET (relay). */
export type Sink = (batch: RecordBatch, unload: boolean) => Promise<void>;

/**
 * consent check -> privacy -> enrichment -> bounded queue -> batches. Records of features the user has not decided
 * on yet are held by the consent manager; records the user (or the configuration) does not allow are dropped.
 */
export class Pipeline {
    private queue: TelemetryRecord[] = [];
    private timer: ReturnType<typeof setTimeout> | null = null;
    private soon = false;

    /** Null holds the queue (relay transport before .NET attached). */
    sink: Sink | null = null;

    constructor(public config: ResolvedConfig, private consent: ConsentManager, private session: SessionManager, public resource: Attributes) { }

    get size(): number {
        return this.queue.length;
    }

    /** Entry point of every record. Never throws. */
    emit(record: TelemetryRecord): void {
        try {
            if (!(this.config.features & record.feature)) return;
            const sessionId = this.session.id();
            if (!isSessionSampled(sessionId, this.config.sampling.sessionRatio)) return;
            if (!(this.consent.collectable() & record.feature)) return;
            applyPrivacy(record, this.config.privacy);
            record.attributes['session.id'] = sessionId;
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
            if (pseudo) record.attributes['enduser.pseudo.id'] = pseudo;
            if (user) record.attributes['user.id'] = user;
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
    purge(revoked: number): void {
        if (!revoked) return;
        this.queue = this.queue.filter(r => (r.feature & revoked) === 0);
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
            for (const record of this.queue.splice(0, size)) {
                if (isSpan(record)) batch.spans.push(record);
                else batch.logs.push(record);
            }
            try {
                sends.push(Promise.resolve(sink(batch, unload)).catch(e => debug('export failed', e)));
            } catch (e) {
                debug('export failed', e);
            }
        }
        return Promise.all(sends).then(() => undefined);
    }
}
