import { describe, it, expect } from 'vitest';
import { readBundle } from './harness.js';

// The version literal is hand-written in each script (there is no build-time templating), so a
// version bump that misses a file would silently ship mismatched bundles. This pins all four to
// one value so that mistake fails CI instead. The regex tolerates both the tsc output
// (`self['bit-bswup.sw version'] = 'v-10-5-0'`) and the minified Release form
// (`self["bit-bswup.sw version"]="v-10-5-0"`).
describe('bundle version consistency', () => {
    it('all four bundles declare the same version', () => {
        const bundles = ['bit-bswup.js', 'bit-bswup.progress.js', 'bit-bswup.sw.js', 'bit-bswup.sw-cleanup.js'];
        const versions = bundles.map(name => {
            const match = readBundle(name).match(/version["']\]\s*=\s*["']([^"']+)["']/);
            expect(match, `${name} declares no version`).toBeTruthy();
            return match[1];
        });

        // Assert on the per-bundle mapping rather than `new Set(versions).size`: a size
        // mismatch reports "expected 2 to be 1", which says nothing about WHICH bundle was
        // missed on a version bump - the one fact needed to fix it. Comparing every entry to
        // the first prints the full name -> version list on failure.
        const expected = Object.fromEntries(bundles.map(name => [name, versions[0]]));
        const actual = Object.fromEntries(bundles.map((name, i) => [name, versions[i]]));
        expect(actual, 'bundles declare mismatched versions').toEqual(expected);
    });
});
