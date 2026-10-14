import type { ResolvedConfig } from './config';
import type { Attributes } from './model';

interface UserAgentData {
    brands?: { brand: string; version: string }[];
    mobile?: boolean;
    platform?: string;
}

/** Builds the resource attributes shared by every record of this page. */
export function buildResource(config: ResolvedConfig): Attributes {
    const attributes: Attributes = {};
    const set = (key: string, value: unknown) => {
        if (value != null && value !== '') attributes[key] = value as string;
    };
    const service = config.service;
    set('service.name', service.name);
    set('service.version', service.version);
    set('service.namespace', service.namespace);
    set('service.instance.id', service.instanceId);
    set('deployment.environment.name', service.environment);
    set('telemetry.sdk.name', 'bit-belemetry');
    set('telemetry.sdk.language', 'webjs');
    set('telemetry.sdk.version', config.sdkVersion);
    if (typeof navigator !== 'undefined') {
        set('browser.language', navigator.language);
        const uaData = (navigator as Navigator & { userAgentData?: UserAgentData }).userAgentData;
        if (uaData) {
            if (uaData.brands) attributes['browser.brands'] = uaData.brands.map(b => b.brand + ' ' + b.version);
            if (typeof uaData.mobile === 'boolean') attributes['browser.mobile'] = uaData.mobile;
            set('browser.platform', uaData.platform);
        }
        if (config.privacy.collectUserAgent) set('user_agent.original', navigator.userAgent);
    }
    const extra = config.resource || {};
    for (const key of Object.keys(extra)) set(key, extra[key]);
    return attributes;
}
