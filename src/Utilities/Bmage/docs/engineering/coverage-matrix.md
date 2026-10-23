# Bmage requirement coverage matrix

Engineering tracking document; see [gap-analysis.md](gap-analysis.md) for the reasoning. Paths are relative to
`src/Utilities/Bmage`. Test files are under `Tests/Bit.Bmage.Tests` unless they say `E2E`.

**Status values**: `missing`, `planned`, `in progress`, `implemented` (code exists, not yet proven by tests),
`verified` (acceptance criteria pass in the named tests), `deferred` (with rationale), `unsupported` (by design).

## A. Import and decoding

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| File picker, drag and drop, paste, bytes, streams, URLs, camera | verified | P1 | `Input/ImageLoader.cs`, `Input/ImageSource.cs`, `Editor/Components/BmageDropZone.razor` | `ScenarioTests`, E2E `EditorTests`, `DemoRegressionTests` (paste) | - |
| Format sniffing by magic bytes | verified | P0 | `ImageFormat.cs` | `CodecTests` | - |
| PNG, JPEG, BMP, GIF managed decode | verified | P1 | `Codecs/*` | `CodecTests`, `AdversarialPipelineTests` | GIF/APNG first frame only (by design) |
| WebP, AVIF decode via browser | verified | P1 | `Codecs/BrowserCodecs.cs`, `Scripts/bit-bmage*.ts` | E2E `JsModuleContractTests`, `CapabilityTests` | Depends on browser |
| SVG | unsupported | - | `FallbackImageDecoder` refuses | `CodecTests` | Untrusted SVG can run script / fetch; by design |
| EXIF orientation | verified | P1 | `Operations/Transforms/OrientationOperation.cs` | `MetadataTests`, `GeometryTests` | - |
| Metadata extraction (EXIF, GPS, ICC, XMP) | verified | P1 | `Metadata/*` | `MetadataTests`, `AdversarialMetadataTests` | - |
| Size limits before allocation | verified | P0 | `ImageLimits.cs`, `BitBmageOptions.MaxSourceBytes` | `AdversarialPipelineTests`, `MemoryBudgetTests` | - |
| Corrupt / truncated input errors | verified | P0 | decoders | `CodecTests`, `AdversarialPipelineTests` | - |
| CORS failure explained | implemented | P1 | `ImageLoader.LoadFromUrlAsync` | - | Message only |
| Multi-image import | verified | P1 | `ImageLoader.LoadManyAsync`, batch | `PipelineTests` | - |
| Import a file as a layer (editor) | verified | P2 | `BmageEditor.AddImageLayerAsync`, Layers tab | E2E `EditingToolTests.TheNewControlsAreNamedForAssistiveTechnology` (control present) | Pixel placement covered by `OverlayAttachmentTests`; no E2E upload of a layer image yet |
| Animated image editing | deferred | P3 | - | - | First frame decoded; animation editing is a separate problem |

## B. Geometry

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| Crop, aspect crop, smart crop | verified | P1 | `Operations/Transforms/CropOperation.cs`, `Analysis/SmartCrop.cs` | `GeometryTests`, `AnalysisTests` | - |
| Rotate 90°, flip | verified | P1 | `OrientationOperation` | `GeometryTests` | - |
| Arbitrary rotation | verified | P1 | `RotateOperation` | `GeometryTests`, `AdversarialGeometryTests` | - |
| Resize (six modes, filters) | verified | P1 | `ResizeOperation`, `Resampler` | `GeometryTests`, `ResamplingQualityTests` | - |
| Skew, affine, perspective, straighten quad | verified | P2/P3 | `WarpOperations.cs` | `GeometryTests`, `AdversarialGeometryTests` | - |
| Canvas extend / reduce with anchor and background | verified | P1 | `Operations/Transforms/CanvasOperations.cs` (`CanvasSizeOperation`), `ImagePipeline.CanvasSize/Pad` | `CanvasSizeTests`, E2E `EditingToolTests.TheCanvasSizeAddsRoomAroundTheImage` | - |
| Trim transparent / uniform borders | verified | P1 | `Analysis/ContentBounds.cs`, `BmageEditor.TrimAsync` | `ContentBoundsTests` | An analysis that yields a crop, not an operation: sizes must be known without pixels |
| Straighten (rotate + crop to inscribed rectangle) | verified | P1 | `StraightenOperation` | `StraightenTests` (opacity sweep over 121 angles x 4 sizes x 3 samplers), E2E `StraighteningFromTheTransformTabCropsTheCorners` | ±45° |
| Overlays stay on content through geometric edits | verified | P0 | `ImagePipeline.TryGetTransform`, `ImageDocument.WithPipeline(.., keepOverlaysAttached)`, `Annotation/Layer/Mask.Transform` | `OverlayAttachmentTests` (25 of 29 fail without it), E2E `AnArrowStaysOnWhatItMarksWhenTheImageIsTurned` | Relative, content and bitmap masks stay put by design; non-affine steps leave overlays in place |
| Numeric transform inputs in the editor | verified | P1 | Transform tab in `BmageEditorPanel.razor` | E2E `EditingToolTests` (straighten, canvas size) | - |

## C. Colour and tone

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| Brightness, contrast, exposure, gamma, saturation, vibrance, hue, temperature, tint, shadows/highlights, opacity | verified | P1 | `Operations/Adjustments/Adjustments.cs` | `OperationTests`, `FastPathEquivalenceTests` | Temperature/tint are channel-scale approximations (documented) |
| Levels, curves, colour matrix | verified | P1 | `Adjustments.cs`, `CurveOperation.cs`, `ColorMatrix.cs` | `OperationTests` | - |
| Grayscale, sepia, invert, threshold, posterize | verified | P1 | `Adjustments.cs` | `OperationTests` | - |
| Auto levels, auto contrast, histograms, statistics | verified | P1 | `Analysis/ImageStatistics.cs` | `AnalysisTests` | - |
| HSL / per-colour adjustment | verified | P1 | `Operations/Adjustments/ColorAdjustments.cs` (`HslAdjustment`) | `ColorAdjustmentTests` | HSL model, not perceptual (documented) |
| Channel mixer, black-and-white mixer | verified | P1 | `ChannelMixerAdjustment` | `ColorAdjustmentTests` | - |
| Gradient map, duotone | verified | P1 | `GradientMapOperation` | `ColorAdjustmentTests` | - |
| White balance (neutral pick, automatic) | verified | P1 | `WhiteBalanceAdjustment`, `AutoWhiteBalanceOperation`, editor picker | `ColorAdjustmentTests` | Gains on encoded sRGB (documented approximation) |
| Whites / blacks | verified | P1 | `WhitesBlacksAdjustment` | `ColorAdjustmentTests` (monotonic at every extreme) | - |
| Colorize | verified | P2 | `ColorizeAdjustment` | `ColorAdjustmentTests` | - |
| Colour replacement / selective colour | verified | P2 | `HslAdjustment`; `FillLayer` with `FloodMask` and `BlendMode.Color` | `ColorAdjustmentTests`, demo `/retouch` | - |
| Histogram and clipping view in the editor | verified | P2 | Adjust tab | E2E `TheHistogramFollowsTheImageAndReportsClipping` | Every fourth pixel of every fourth row is counted |
| ICC colour management | deferred | P3 | - | - | Profiles read and preserved, pixels treated as sRGB |

## D. Filters and effects

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| Gaussian blur, unsharp-mask sharpen, pixelate, noise/grain, median, vignette | verified | P1 | `Operations/Filters/Filters.cs` | `OperationTests`, `AdversarialFilterTests`, `ParallelProcessingTests` | - |
| Convolution, emboss, edge detect presets | verified | P2 | `ConvolutionFilter` | `OperationTests` | - |
| Motion blur | verified | P2 | `Operations/Filters/DirectionalFilters.cs` | `MotionBlurTests` (geometry, energy, edges, threads, continuity) | Cost grows with distance; capped at 1000 px (120 in the editor) |
| High-pass | verified | P2 | `HighPassFilter` | `HighPassTests` | - |
| Masked / selective application | verified | P1 | `Masks/MaskedOperation.cs` | `MaskTests` | - |
| Halftone | deferred | P3 | - | - | Niche; a custom operation can do it |
| Drop shadow, glow, outline as layer effects | deferred | P3 | - | - | Expressible with masks, `MorphedMask.Border`, blur and fill layers |

## E. Layers, compositing, masks

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| Image, fill, adjustment layers | verified | P2 | `Layers/Layer.cs` | `LayerTests` | - |
| Paint (brush) layer | verified | P2 | `Layers/PaintLayer.cs` | `PaintLayerTests`, E2E `TheBrushPaintsOntoANewPaintLayer`, `TheEraserRemovesPaintButNeverTheImage` | Re-rasterised on each composite of the layer |
| Blend modes (W3C), opacity, visibility, lock | verified | P2 | `Drawing/Compositor.Blending.cs` | `LayerTests`, E2E `ALayersOpacityAndBlendModeAreSetFromThePanel` | - |
| Layer masks | verified | P2 | `Layer.Mask` | `LayerTests`, `MaskTests` | - |
| Reorder, remove, duplicate | verified | P2 | `ImageDocument.MoveLayer/RemoveLayer/DuplicateLayer` | `LayerTests` | Duplicate covered through the editor E2E only by presence |
| Editor layer controls (select, opacity, blend, order, lock, duplicate, add) | verified | P2 | Layers tab | E2E `ALayersOpacityAndBlendModeAreSetFromThePanel` | - |
| Groups, clipping masks | deferred | P3 | - | - | Needs a group compositing model and a format change |

## F. Selection and background

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| Rectangle, ellipse, polygon, path, brush, gradient, tone/colour range, bitmap masks | verified | P2 | `Masks/MaskShapes.cs` | `MaskTests` | - |
| Add, subtract, intersect, invert, feather | verified | P2 | `CombinedMask`, `Mask.Inverted/Feather` | `MaskTests` | - |
| Transparency-based selection | verified | P2 | `BitmapMask` (`MaskChannel.Alpha`), `FloodMask` on transparency | `MaskTests`, `FloodMaskTests` | - |
| Magic wand (contiguous / global, tolerance, soft edge) | verified | P2 | `Masks/SelectionMasks.cs` (`FloodMask`) | `FloodMaskTests` | - |
| Expand, contract, border | verified | P2 | `MorphedMask` | `MorphedMaskTests` (exact distances, windows, nesting guard) | Up to 250 px |
| Uniform background removal | verified | P2 | `ImagePipeline.RemoveBackground` | `FloodMaskTests.RemovingAPlainBackground...`, demo `/retouch` | Plain backdrops only; a subject touching a corner in the backdrop's colour goes with it |
| Fill and magic eraser tools in the editor | verified | P2 | `BmageEditor` (Fill, MagicEraser) | E2E `TheFillToolFillsTheClickedRegionAsALayer` | Magic eraser not asserted in E2E |
| Selection tools in the editor (rectangle, ellipse, lasso, magic wand; Shift adds, Alt subtracts) with marching ants | verified | P2 | `BmageEditor.razor`, `Masks/MaskContours.cs`, `BmageEditorCanvas` outline | `MaskContourTests`, E2E `ARectangleSelectionIsOutlinedAndItsPixelsMadeTransparent`, `TheMagicWandSelectsARegionToCropTo`, `ASelectionCopiedToALayerBecomesAnImageLayer` | Lasso not driven in E2E; outline traced at most 640 px across |
| Selection actions: make transparent, fill, copy to layer, crop to, invert, select all, deselect | verified | P2 | `BmageEditor` selection API, toolbar group | the E2E tests above | - |
| Selection follows geometric edits and undo / redo | verified | P2 | `MoveSelectionWith`, `FollowSelection` | E2E `TheSelectionComesBackWithTheImageWhenATurnIsUndone` | Dropped across non-affine steps |
| AI segmentation | deferred | P3 | `BitmapMask` extension point | - | No model bundled (size, licensing, network) |

## G. Drawing, painting, retouching

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| Freehand (vector), lines, arrows, rectangles, ellipses, polygons | verified | P1 | `Annotations/AnnotationTypes.cs` | `DrawingTests`, `AdversarialAnnotationTests` | - |
| Raster brush with hardness, opacity, pressure | verified | P2 | `PaintStroke`, Brush tool | `PaintLayerTests`, E2E `TheBrushPaintsOntoANewPaintLayer` | Pen pressure not exercised in E2E (Playwright has no pen) |
| Eraser | verified | P2 | `PaintStroke.Erase`, Eraser tool | `PaintLayerTests`, E2E `TheEraserRemovesPaintButNeverTheImage` | - |
| Bucket fill | verified | P2 | `FillLayer` + `FloodMask`, Fill tool | E2E `TheFillToolFillsTheClickedRegionAsALayer` | - |
| Eyedropper | verified | P2 | `Analysis/ColorSampler.cs`, Eyedropper tool | `ColorSamplerTests`, E2E `TheEyedropperPicksTheColourUnderThePointer` | - |
| Clone / heal | deferred | P3 | - | - | Useful healing needs patch-based reconstruction |

## H. Text, shapes, annotations

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| Text with font, size, weight, style, alignment, line height, spacing, wrapping, stroke, background | verified | P1 | `TextAnnotation`, `Drawing/ImageCanvas.cs`, `Text/*` | `TextTests` | Managed font rasteriser has no complex shaping; browser rasteriser has |
| Callouts, steps, highlights, redaction, image stamps | verified | P1 | `AnnotationTypes.cs` | `AdversarialAnnotationTests`, E2E `EditorTests` | - |
| Select, move, z-order, lock, keyboard nudge | verified | P1 | `ImageDocument`, `BmageEditor.razor` | E2E `DemoRegressionTests` | - |
| Edit a selected annotation's text, size, rotation, opacity, order and lock from the panel | verified | P1 | Tools tab | E2E `PlacedTextIsEditedFromThePanel` | - |

## I. History

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| Undo, redo, labels, go-to, reset, depth | verified | P1 | `Editing/EditHistory.cs` | `EditorServicesTests`, `EditSession*Tests` | - |
| Transient drags as one step | verified | P1 | `PushTransient/CommitTransient` | `EditSessionDraftTests` | - |
| Atomic multi-step edits | verified | P1 | `ImageEditSession.Apply(Func)` | `EditorServicesTests` | - |
| History memory estimate counts layers and strokes | verified | P2 | `HistoryEntry.EstimatedSize` | `PaintLayerTests.PaintingIsAnUndoableDocumentEdit...` | Shared images not counted (not duplicated); images added or copied to layers are kept until another image is opened |
| History waits while the editor is busy; measured actions stop if the image changed meanwhile | implemented | P1 | `BmageEditor.Undo/Redo/Reset/GoToHistoryAsync`, `IsStill` | - | No test interleaves an undo with a running trim |
| Before / after compare | verified | P1 | `ImageEditSession.GetSourcePreviewAsync`, Compare button, `\` key | E2E `CompareShowsTheOriginalUntilItIsTurnedOff` | - |
| History timeline serialisation | deferred | P3 | - | - | Documents and projects serialise; the timeline does not |

## J. Export and interoperability

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| PNG, JPEG, BMP managed; WebP, AVIF browser | verified | P1 | `Codecs/*`, `Output/ImageExporter.cs` | `CodecTests`, E2E `ProcessingTests` | - |
| Quality, size budget, alpha flattening | verified | P1 | `ImageExportOptions`, `ImageExporter` | `ExportSearchTests` | - |
| Metadata policies | verified | P1 | `Metadata/Containers.cs` | `MetadataTests` | - |
| Blob, bytes, stream, download, clipboard | verified | P1 | `ImageExporter` | E2E `EditorTests` | - |
| JSON document (external image references) | verified | P2 | `Serialization/*` | `SerializationTests` | - |
| Self-contained project file | verified | P2 | `Serialization/BmageProject.cs`, editor Save/Open project | `ProjectTests` (round trip, dedup, ownership, hostile archives, zip bomb, pixel budget, unused images, malformed manifests, damaged entries, leak-free failure), E2E `AProjectSavedAndReopenedIsStillEditable` | Images stored as PNG: large for big photos |

## K. Performance and memory

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| Proxy and draft previews, staged caches | verified | P0 | `ImageEditSession` | `EditSessionCacheTests`, `EditSessionProxyTests`, `EditSessionDraftTests` | - |
| Pipeline optimiser (exact rewrites) | verified | P0 | `Pipeline/PipelineOptimizer.cs` | `PipelineTests`, `FastPathEquivalenceTests` | - |
| Parallel rows with identical pixels | verified | P1 | `Operations/ParallelRows.cs` | `ParallelProcessingTests`, `ColorAdjustmentTests`, `MotionBlurTests` | - |
| Worker codecs in the browser | verified | P1 | `Scripts/bit-bmage-worker.ts` | E2E `JsModuleContractTests` (Chromium, Firefox) | Playwright's Windows WebKit has no `OffscreenCanvas`: the page-thread fallback is verified there instead |
| Releases wait for full renders already running | verified | P0 | `ImageEditSession.RenderFullAsync`, `WhenIdle` | `ReviewRegressionTests.DisposingDuringAnExportKeepsTheOriginalUntilTheExportIsDone` (fails without it) | Interactive Server: a replaced preview can be disposed while the circuit thread reads it (one wrong frame) |
| Memory estimates and budgets | verified | P1 | `ImagePipeline.EstimateMemory`, `Memory/BudgetedPixelAllocator.cs` | `MemoryBudgetTests` | New operations use the generic estimate |
| Benchmarks of the new tools | verified | P1 | `Tests/Bit.Bmage.Benchmarks` (`EditingToolBenchmarks`) | `docs/performance.md#editing-tools` | Desktop numbers; WebAssembly not re-measured |

## L. Browser compatibility

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| Capability detection by probing | verified | P0 | `Capabilities/*` | E2E `CapabilityTests` | - |
| Chromium E2E | verified | P0 | `Tests/Bit.Bmage.Tests.E2E` | see log | - |
| Firefox, WebKit E2E | verified | P0 | `BMAGE_E2E_BROWSER` | see log | Playwright's engine builds on Windows, not Safari or iOS |

## M-Q. API, editor, accessibility, security, extensibility

| Feature | Status | Pri | Implementation | Evidence | Limitations / remaining |
| --- | --- | --- | --- | --- | --- |
| Headless + editor APIs, DI, cancellation, progress, typed errors | verified | P0 | throughout | engine and E2E suites | - |
| Replaceable editor chrome, localisation, RTL, theming | verified | P1 | `Editor/*` | E2E `EditorTests` | New strings are localisable records like the old |
| Keyboard access, roles, roving focus, labelled controls | verified | P1 | `Editor/Components/*` | E2E `EditorLifecycleTests`, `EditingToolTests.TheNewControlsAreNamedForAssistiveTechnology` | Slider names include their value ("Straighten 0"), a pre-existing pattern |
| Local-only processing, metadata stripping default, SVG refused | verified | P0 | throughout | `MetadataTests`, `CodecTests` | Projects remove the original's metadata by default too |
| Untrusted project archives | verified | P0 | `BmageProject.LoadAsync` | `ProjectTests` (`TheImagesTogetherAreBoundedBeforeTheyAreDecoded`, `TheFilesAreCountedFromTheDirectoryNotFromWhatTheArchiveDeclares`, `AMalformedProjectIsReportedAsOneWhateverIsWrongWithIt`) | ZIP64 refused |
| URL host allow-list | deferred | P2 | - | - | Fetches run in the user's browser under CORS; an app can validate URLs before calling |
| Extension points (operations, masks, layers, annotations, codecs, tools) | verified | P1 | see `docs/extending.md` | `SerializationTests` (custom codecs) | `Transform` overridable for custom overlays |

## Verification log

| Date | Batch | What ran | Result |
| --- | --- | --- | --- |
| 2026-10-22 | baseline | `Bit.Bmage.Tests` net10.0 | 969 / 969 passed |
| 2026-10-22 | baseline | E2E, Chromium, Debug demo | 71 / 71 passed |
| 2026-10-22 | baseline | `Bit.Bmage.Tests.Mcp` | 218 / 219: a pre-existing failure once two guides passed the 15,000 character read limit; fixed in the test (219 / 219) |
| 2026-10-22 | 1-6 engine | `Bit.Bmage.Tests` net10.0 | 1120 / 1120 passed |
| 2026-10-22 | 1-6 engine | all three target frameworks build | 0 warnings, 0 errors |
| 2026-10-22 | editor | E2E, Chromium, existing tests after the editor changes | 71 / 71 passed |
| 2026-10-22 | editor | E2E, Chromium, `EditingToolTests` | 11 / 11 passed after one fix (a layer click toggled the selection off) |
| 2026-10-22 | 8 | `ProjectTests` | 9 / 9 passed |
| 2026-10-22 | 7-11 | E2E full, Chromium | 88 / 88 passed |
| 2026-10-23 | 7-11 | E2E full, Firefox (Playwright Gecko) | 88 passed, 1 inconclusive (synthetic paste), 0 failed |
| 2026-10-23 | 7-11 | E2E full, WebKit (Playwright, Windows) | 84 / 88: four failures, all from `OffscreenCanvas` being absent (a test helper used it; two worker tests assumed workers). Fixed in the tests |
| 2026-10-23 | review | independent review of the editor, selection and project batch | 5 confirmed defects, 3 plausible; fixed but for the Interactive Server preview race (see gap analysis 10) |
| 2026-10-23 | review fixes | `Bit.Bmage.Tests` net10.0 | 1140 / 1140 passed; the new export test fails with the fix reverted |
| 2026-10-23 | review fixes | `Bit.Bmage.Tests.Mcp` | 219 / 219 after a code review rule's false positive on the project recipe was fixed |
| 2026-10-23 | review fixes | `Bit.Bmage` net8.0, net9.0, net10.0 build | 0 warnings, 0 errors |
| 2026-10-23 | review fixes | E2E full, Chromium | 90 / 90 passed (7 min 49 s) |
| 2026-10-23 | review fixes | E2E full, WebKit | 88 passed, 2 inconclusive (worker path without `OffscreenCanvas`), 0 failed |
| 2026-10-23 | review fixes | E2E full, Firefox | 89 passed, 1 inconclusive (synthetic paste), 0 failed |
| 2026-10-23 | final | E2E `EditingToolTests`, Chromium, after the last editor change | 17 / 17 passed |
