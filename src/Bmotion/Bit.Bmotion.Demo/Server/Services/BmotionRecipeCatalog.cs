using Bit.Bmotion.Demo.Server.Dtos;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// The patterns people actually ask an animation library for, written out in full.
/// <para>
/// The API reference answers "what does this parameter do"; a recipe answers "what do I write for
/// the thing I want", which is the question that arrives. Each one is short, complete and correct
/// against the current API, and carries the caveat that is not visible in the code - the missing
/// <c>@key</c>, the <c>position: relative</c> the container needs, the render mode it will not
/// survive. Those caveats are where hand-written animation code usually goes wrong, and they are
/// invisible to anyone reading only the parameter table.
/// </para>
/// </summary>
public static class BmotionRecipeCatalog
{
    /// <summary>Every recipe, code included.</summary>
    public static readonly BmotionRecipeDto[] All =
    [
        new()
        {
            Id = "fade-in-on-mount",
            Title = "Fade and slide in on mount",
            Intent = "Make an element appear smoothly when the page or component first renders.",
            Keywords = "fade in enter mount appear entrance intro initial animate slide up first render",
            SeeAlso = "/basic",
            Code = """
                <Bmotion Initial="Bm.To(opacity: 0, y: 20)"
                         Animate="Bm.To(opacity: 1, y: 0)"
                         Transition="Bm.Spring(bounce: 0.2, duration: 0.5)">
                    <div class="card">Content</div>
                </Bmotion>
                """,
            Notes = "Initial only runs when the element mounts. To replay it, change the element's @key so " +
                    "Blazor treats it as a new element. Both properties animated here are compositor-eligible, " +
                    "so this works on Blazor Server as well."
        },
        new()
        {
            Id = "staggered-list",
            Title = "Staggered list entrance",
            Intent = "Make a list of items appear one after another rather than all at once.",
            Keywords = "stagger list cascade sequence one by one children delay items entrance variants",
            SeeAlso = "/variants",
            Code = """
                <Bmotion Variants="_container" InitialState="hidden" State="visible"
                         Transition="Bm.Tween(0.3, staggerChildren: 0.06, delayChildren: 0.1)">
                    <ul class="list">
                        @foreach (var item in _items)
                        {
                            <Bmotion Variants="_item" @key="item.Id">
                                <li>@item.Title</li>
                            </Bmotion>
                        }
                    </ul>
                </Bmotion>

                @code {
                    private readonly BmVariants _container = new()
                    {
                        ["hidden"] = Bm.To(opacity: 0),
                        ["visible"] = Bm.To(opacity: 1),
                    };

                    private readonly BmVariants _item = new()
                    {
                        ["hidden"] = Bm.To(opacity: 0, y: 16),
                        ["visible"] = Bm.To(opacity: 1, y: 0),
                    };
                }
                """,
            Notes = "The stagger lives on the CONTAINER's transition, not on the items - each child inherits " +
                    "the state name and gets its slot in the cascade. Do not also put a Transition on the items " +
                    "unless you mean to override the timing (a per-variant transition is the tidier way). " +
                    "Bm.Stagger(..., from: BmStaggerFrom.Center) via childStagger makes the cascade radiate " +
                    "instead of running first-to-last."
        },
        new()
        {
            Id = "exit-animation",
            Title = "Animate an element out before it is removed",
            Intent = "Fade or slide something away when it disappears, instead of it vanishing instantly.",
            Keywords = "exit leave remove unmount disappear hide conditional close dismiss animate presence",
            SeeAlso = "/presence",
            Code = """
                <BmotionAnimatePresence IsPresent="_show">
                    <Bmotion Initial="Bm.To(opacity: 0, scale: 0.95)"
                             Animate="Bm.To(opacity: 1, scale: 1)"
                             Exit="Bm.To(opacity: 0, scale: 0.95)"
                             Transition="Bm.Tween(0.2)">
                        <div class="panel">Content</div>
                    </Bmotion>
                </BmotionAnimatePresence>

                @code {
                    private bool _show = true;
                }
                """,
            Notes = "Exit does nothing without a presence component around it: Blazor removes the element from " +
                    "the DOM the moment the condition flips, and there is nothing left to animate. Never wrap " +
                    "the content in @if - that is exactly what BmotionAnimatePresence replaces. Use " +
                    "BmotionPresenceGroup for a list, and BmotionPresenceSwitch when one item replaces another."
        },
        new()
        {
            Id = "modal-dialog",
            Title = "Modal with a backdrop",
            Intent = "Open and close a dialog with the backdrop fading and the panel scaling in.",
            Keywords = "modal dialog popup overlay backdrop scrim open close panel drawer sheet",
            SeeAlso = "/presence",
            Code = """
                <BmotionAnimatePresence IsPresent="_open" Mode="BmPresenceMode.Wait">
                    <div>
                        <Bmotion Initial="Bm.To(opacity: 0)" Animate="Bm.To(opacity: 1)"
                                 Exit="Bm.To(opacity: 0)" Transition="Bm.Tween(0.15)">
                            <div class="backdrop" @onclick="Close"></div>
                        </Bmotion>

                        <Bmotion Initial="Bm.To(opacity: 0, scale: 0.94, y: 12)"
                                 Animate="Bm.To(opacity: 1, scale: 1, y: 0)"
                                 Exit="Bm.To(opacity: 0, scale: 0.98, y: 8)"
                                 Transition="Bm.Spring(bounce: 0.15, duration: 0.35)">
                            <div class="dialog" role="dialog" aria-modal="true">
                                <h2>Title</h2>
                                <button @onclick="Close">Close</button>
                            </div>
                        </Bmotion>
                    </div>
                </BmotionAnimatePresence>

                @code {
                    private bool _open;

                    private void Close() => _open = false;
                }
                """,
            Notes = "The exit is deliberately faster and shallower than the enter: a dialog that leaves as " +
                    "slowly as it arrives feels unresponsive. Keep the accessibility attributes - animation " +
                    "does not replace role=\"dialog\", aria-modal or focus management."
        },
        new()
        {
            Id = "hover-and-press",
            Title = "Hover and press feedback",
            Intent = "Make a button or card respond to the pointer.",
            Keywords = "hover press tap click button card interactive feedback scale lift whilehover whiletap",
            SeeAlso = "/gestures",
            Code = """
                <Bmotion WhileHover="Bm.To(scale: 1.04, y: -2)"
                         WhileTap="Bm.To(scale: 0.97)"
                         Transition="Bm.Spring(stiffness: 400, damping: 25)">
                    <button class="card" @onclick="Open">Open</button>
                </Bmotion>
                """,
            Notes = "Gesture overlays revert on their own when the gesture ends - do not write the resting " +
                    "state into Animate as well. A stiff, well-damped spring is right here: pointer feedback " +
                    "has to arrive within a frame or two, so bounce reads as lag. Tap is keyboard-accessible " +
                    "out of the box on a focusable element."
        },
        new()
        {
            Id = "reveal-on-scroll",
            Title = "Reveal an element when it scrolls into view",
            Intent = "Animate a section in the first time the reader scrolls to it.",
            Keywords = "scroll reveal in view viewport intersection appear on scroll lazy entrance once",
            SeeAlso = "/scroll",
            Code = """
                <Bmotion Initial="Bm.To(opacity: 0, y: 32)"
                         WhileInView="Bm.To(opacity: 1, y: 0)"
                         Once="true"
                         Viewport='new BmViewport { Amount = "0.3" }'
                         Transition="Bm.Tween(0.5, BmEase.Out)">
                    <section class="feature">Content</section>
                </Bmotion>
                """,
            Notes = "Once=\"true\" is almost always what is wanted: without it the section re-animates every " +
                    "time it re-enters the viewport, which is distracting on the way back up. Viewport.Amount " +
                    "is how much of the element must be visible before it counts as in view."
        },
        new()
        {
            Id = "scroll-progress-bar",
            Title = "Reading-progress bar",
            Intent = "Show how far down the page the reader has scrolled.",
            Keywords = "scroll progress bar indicator reading position parallax timeline scrolltimeline page",
            SeeAlso = "/scroll",
            Code = """
                <Bmotion Timeline="BmScrollTimeline.Page()" Animate="Bm.To(scaleX: [0, 1])">
                    <div class="progress-bar" style="transform-origin: 0 50%;" />
                </Bmotion>
                """,
            Notes = "This runs entirely in the browser: the keyframes are pre-sampled once and handed to a " +
                    "native scroll timeline, so there is no scroll handler, no per-frame interop and nothing " +
                    "to dispose. Transition does not apply while a Timeline is attached - scroll position IS " +
                    "the progress. Only transform components and opacity can be scroll-driven."
        },
        new()
        {
            Id = "layout-animation",
            Title = "Animate an element between two layouts",
            Intent = "Make an element glide to its new position or size when the layout changes, without animating any property by hand.",
            Keywords = "layout flip position size move resize expand collapse accordion reflow rearrange",
            SeeAlso = "/layout",
            Code = """
                <Bmotion Layout="BmLayout.Size"
                         LayoutAnchor="BmLayoutAnchor.Center"
                         LayoutDependency="_isOpen"
                         Transition="Bm.Spring(bounce: 0.15, duration: 0.4)">
                    <div class="panel">
                        <h3 @onclick="Toggle">Section</h3>
                        @if (_isOpen)
                        {
                            <p>Body</p>
                        }
                    </div>
                </Bmotion>

                @code {
                    private bool _isOpen;

                    private void Toggle() => _isOpen = !_isOpen;
                }
                """,
            Notes = "Nothing here animates height - the element is simply re-rendered into its new size and " +
                    "Bmotion measures the difference (FLIP). Use BmLayout.Position when scaling would distort " +
                    "text. Add LayoutScroll=\"true\" inside a scrolling container, or the container's own scroll " +
                    "between the two measurements reads as movement and the element jumps. Layout animations " +
                    "need the frame loop, so they snap on Blazor Server."
        },
        new()
        {
            Id = "shared-element",
            Title = "Shared-element transition between two views",
            Intent = "Make a thumbnail appear to grow into the full view it opens.",
            Keywords = "shared element magic move hero transition thumbnail expand detail layoutid morph",
            SeeAlso = "/layout",
            Code = """
                @if (_selected is null)
                {
                    <div class="grid">
                        @foreach (var photo in _photos)
                        {
                            <Bmotion LayoutId='@($"photo-{photo.Id}")' @key="photo.Id">
                                <img src="@photo.Url" @onclick="() => _selected = photo" />
                            </Bmotion>
                        }
                    </div>
                }
                else
                {
                    <Bmotion LayoutId='@($"photo-{_selected.Id}")'>
                        <img class="full" src="@_selected.Url" @onclick="() => _selected = null" />
                    </Bmotion>
                }
                """,
            Notes = "The two elements are matched by LayoutId, so it has to be identical on both sides and " +
                    "unique on the page. Exactly one element with a given LayoutId may be mounted at a time - " +
                    "the transition is between the one leaving and the one arriving."
        },
        new()
        {
            Id = "drag-with-constraints",
            Title = "Drag an element within bounds",
            Intent = "Let the user drag something, with edges it cannot be thrown past.",
            Keywords = "drag draggable pointer move constraints bounds elastic momentum snap back slider",
            SeeAlso = "/drag",
            Code = """
                <Bmotion Drag="BmDrag.Both"
                         DragConstraints="new BmDragConstraints { Left = -120, Right = 120, Top = -80, Bottom = 80 }"
                         DragElastic="BmDragElastic.Uniform(0.2)"
                         DragMomentum="true"
                         WhileDrag="Bm.To(scale: 1.05)">
                    <div class="handle" />
                </Bmotion>
                """,
            Notes = "Drag needs the synchronous per-frame loop, so it is WebAssembly-only: on Blazor Server " +
                    "the element does not move at all. Gate it on BmotionCapabilities.SupportsFrameLoop and " +
                    "give the server path a non-dragging equivalent. DragElastic controls how far past the " +
                    "constraint the element can be pulled before it resists."
        },
        new()
        {
            Id = "loading-spinner",
            Title = "Continuous loading animation",
            Intent = "Spin or pulse something forever while work is in progress.",
            Keywords = "spinner loading loop infinite repeat forever rotate pulse skeleton busy indicator",
            SeeAlso = "/keyframes",
            Code = """
                <Bmotion Animate="Bm.To(rotate: 360)"
                         Transition="Bm.Tween(1, BmEase.Linear, repeat: BmRepeat.Forever)">
                    <div class="spinner" />
                </Bmotion>

                @* a pulse instead: keyframes, mirrored so it breathes rather than snapping back *@
                <Bmotion Animate="Bm.To(scale: [1, 1.15, 1], opacity: [0.6, 1, 0.6])"
                         Transition="Bm.Tween(1.4, repeat: BmRepeat.Forever)">
                    <div class="dot" />
                </Bmotion>
                """,
            Notes = "BmEase.Linear is the one case where linear is correct: a spinner that eases has a visible " +
                    "stutter every revolution. Keyframe arrays need the per-frame loop, so the pulse above " +
                    "snaps on Blazor Server while the plain rotation still plays."
        },
        new()
        {
            Id = "split-text-headline",
            Title = "Animate a headline word by word",
            Intent = "Reveal a heading one character, word or line at a time.",
            Keywords = "split text headline letters characters words lines typography reveal per letter title",
            SeeAlso = "/split-text",
            Code = """
                <BmotionSplitText Text="Motion, written in C#"
                                  By="BmSplitBy.Words"
                                  Initial="Bm.To(opacity: 0, y: 24)"
                                  Animate="Bm.To(opacity: 1, y: 0)"
                                  Stagger="Bm.Stagger(0.05)"
                                  Transition="Bm.Spring(bounce: 0.25, duration: 0.6)">
                </BmotionSplitText>
                """,
            Notes = "The text is split in C# and each unit rendered as its own element, so there is no DOM " +
                    "surgery and no flash of unsplit text. Split by Character for short headings only - a long " +
                    "sentence becomes a great many animated elements, and screen readers read the whole string " +
                    "either way."
        },
        new()
        {
            Id = "programmatic-animation",
            Title = "Animate from C# code",
            Intent = "Trigger an animation from an event handler rather than from component state.",
            Keywords = "programmatic imperative code behind service animate async selector elementreference trigger manual",
            SeeAlso = "/programmatic",
            Code = """
                @inject BmotionAnimateService Motion

                <Bmotion @ref="_box">
                    <div class="box" />
                </Bmotion>

                <button @onclick="PulseAsync">Pulse</button>

                @code {
                    private Bmotion _box = default!;

                    private async Task PulseAsync()
                    {
                        // Through the component reference - the element it wraps.
                        await _box.AnimateAsync(Bm.To(scale: 1.2), Bm.Spring(bounce: 0.5));
                        await _box.AnimateAsync(Bm.To(scale: 1), Bm.Spring(bounce: 0.2));

                        // Or by selector, across many elements at once, with a stagger.
                        await Motion.AnimateAsync(".card", Bm.To(y: 0, opacity: 1),
                                                  Bm.Tween(0.4), Bm.Stagger(0.06));
                    }
                }
                """,
            Notes = "Prefer declarative Animate for anything that follows component state - it survives " +
                    "re-renders, and an imperative animation does not. Reach for the service when the " +
                    "animation is genuinely an event (a pulse on save, a shake on error) or when it has to " +
                    "span elements the component does not own."
        },
        new()
        {
            Id = "respect-reduced-motion",
            Title = "Respect the reduced-motion preference",
            Intent = "Stop animations from harming users who have asked their operating system for less motion.",
            Keywords = "accessibility reduced motion prefers-reduced-motion a11y vestibular disable animation policy",
            SeeAlso = "/accessibility",
            Code = """
                // Program.cs - set the policy once, for the whole app.
                builder.Services.AddBitBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.User);
                """,
            Notes = "The default is IgnoreUnlessConfigured, which is back-compatible rather than correct: it " +
                    "consults the OS preference only inside a <BmotionConfig>. User respects it everywhere and " +
                    "is what a new app should set. Reduced motion does not switch animation off - transforms, " +
                    "layout and dimension changes snap while opacity and colour still animate, so the state " +
                    "change stays legible. Do not hand-roll this with a media query check of your own."
        },
    ];

    /// <summary>The recipes without their code or notes, for the listing.</summary>
    public static BmotionRecipeDto[] Summaries =>
        [.. All.Select(recipe => recipe with { Code = null, Notes = null })];

    /// <summary>One recipe by id, matched loosely so "modal" finds "modal-dialog".</summary>
    public static BmotionRecipeDto? Find(string? id)
    {
        var key = (id ?? string.Empty).Trim();

        if (key.Length == 0) return null;

        return All.FirstOrDefault(recipe => string.Equals(recipe.Id, key, StringComparison.OrdinalIgnoreCase))
            ?? All.FirstOrDefault(recipe => recipe.Id.Contains(key, StringComparison.OrdinalIgnoreCase))
            ?? All.FirstOrDefault(recipe => recipe.Title.Contains(key, StringComparison.OrdinalIgnoreCase));
    }
}
