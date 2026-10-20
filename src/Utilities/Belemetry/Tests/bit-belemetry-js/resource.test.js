import { describe, expect, it } from 'vitest';
import { resolveConfig } from '../../Bit.Belemetry/Scripts/config.ts';
import { buildResource } from '../../Bit.Belemetry/Scripts/resource.ts';

describe('resource', () => {
    it('gives the page its own service.instance.id when the server sends none, stable for the page', () => {
        const first = buildResource(resolveConfig({ service: { name: 'shop' } }));
        const second = buildResource(resolveConfig({ service: { name: 'shop' } }));

        expect(first['service.instance.id']).toMatch(/^[0-9a-f]{32}$/);
        expect(second['service.instance.id']).toBe(first['service.instance.id']);
    });

    it('keeps an explicit instance id', () => {
        const resource = buildResource(resolveConfig({ service: { name: 'shop', instanceId: 'inst-1' } }));

        expect(resource['service.instance.id']).toBe('inst-1');
    });
});
