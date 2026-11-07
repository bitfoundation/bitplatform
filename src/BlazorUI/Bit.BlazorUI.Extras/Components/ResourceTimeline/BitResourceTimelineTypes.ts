namespace BitBlazorUI {
    // The contract between the rendering-agnostic engine of the BitResourceTimeline (BitResourceTimelineEngine.ts) and
    // the renderers that paint the scene it builds every frame (canvas, SVG and HTML). Types alone: the file compiles to
    // nothing, so it is no piece of the bundle and no dependency of any script.

    /** What a renderer may ask of the engine after the frame it was handed has been painted. */
    export interface ResourceTimelineRendererHost {
        /** Schedules another frame (an icon image finished loading after the frame that laid it out). */
        requestRender(): void;
        /** Paints the current scene in this turn (a canvas backing-store resize just cleared the bitmap). */
        paintNow(): void;
        /** The cached image element of an icon source, created and loading on the first request. */
        getImage(src: string): HTMLImageElement;
    }

    /** A renderer: it owns its surface element inside the wrapper and paints one scene per frame onto it. */
    export interface ResourceTimelineRenderer {
        /** The root element of the renderer; the engine binds the pointer and wheel events to it and drives its cursor. */
        surface: HTMLElement | SVGSVGElement;
        /** The viewport size changed: the surface is sized to match. */
        resize(cssWidth: number, cssHeight: number): void;
        /** Paints one frame from the scene the engine built. */
        render(scene: any): void;
        /** Removes the surface and releases what the renderer holds. */
        dispose(): void;
    }

    /** The constructor of a renderer, as the registry the engine is created with holds it. */
    export interface ResourceTimelineRendererType {
        new (wrapper: HTMLElement, host: ResourceTimelineRendererHost): ResourceTimelineRenderer;
    }

    /** The renderers the engine can switch between, by the lower-cased name the renderer option carries. */
    export interface ResourceTimelineRendererRegistry {
        [name: string]: ResourceTimelineRendererType;
    }

    /** What the engine reads of the theme in force on the component's root, in place of its built-in defaults. */
    export interface ResourceTimelineTheme {
        /** Resolved colors, keyed like the colors option (contentBg, axisBg, bar, ...). A key left out keeps the default. */
        colors: { [key: string]: string };
        /** The font family the default fonts of the axes and the labels are drawn in, or null to keep the default one. */
        fontFamily: string | null;
    }
}
