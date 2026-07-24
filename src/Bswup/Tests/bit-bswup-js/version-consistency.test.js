import { describe, it, expect } from 'vitest';
import { readBundle } from './harness.js';

// The version literal is hand-written in each script (there is no build-time templating), so a
// version bump that misses a file would silently ship mismatched bundles. This pins all four to
// one value so that mistake fails CI instead. The regex tolerates both the tsc output
// (`self['bit-bswup.sw version'] = 'v-10-5-0'`) and the minified Release form
// (`self["bit-bswup.sw version"]="v-10-5-0"`).
describe('bundle version consistency', () => {
    it('all four bundles declare the same version', () => {
        const versions = ['bit-bswup.js', 'bit-bswup.progress.js', 'bit-bswup.sw.js', 'bit-bswup.sw-cleanup.js']
            .map(name => {
                const match = readBundle(name).match(/version["']\]\s*=\s*["']([^"']+)["']/);
                expect(match, `${name} declares no version`).toBeTruthy();
                return match[1];
            });

        expect(new Set(versions).size).toBe(1);
    });
});
