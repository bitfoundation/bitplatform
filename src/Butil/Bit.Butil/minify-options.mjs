// The one place the esbuild minify settings live. build.mjs applies them to every chunk of a Release
// build, and the benchmark suite's weigh-modules.mjs applies the same object to measure what ships -
// so a change here (the target, keepNames, charset) moves both the artifacts and the figures held
// against them, instead of one drifting from the other.
export const MINIFY_OPTIONS = Object.freeze({ minify: true, target: 'es2019', legalComments: 'none' });
