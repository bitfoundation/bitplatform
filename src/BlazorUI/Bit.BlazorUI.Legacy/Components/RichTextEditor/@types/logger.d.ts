declare const levels: readonly ["error", "warn", "log", "info"];
declare type DebugLevel = (typeof levels)[number];
declare function namespace(ns: string): Record<DebugLevel, (...args: unknown[]) => void>;
declare namespace namespace {
    var level: (newLevel: false | "error" | "warn" | "log" | "info") => void;
}
