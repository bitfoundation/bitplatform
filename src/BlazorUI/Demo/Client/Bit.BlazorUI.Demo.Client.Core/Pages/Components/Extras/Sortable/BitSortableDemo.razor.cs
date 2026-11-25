using System.Globalization;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Sortable;

public partial class BitSortableDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Animation",
            Type = "int",
            DefaultValue = "150",
            Description = "The duration in milliseconds of the slide animation of the items moving to their new positions, mirroring the SortableJS animation option. It follows the reduced-motion preference the way every animation of the library does (see ForceAnimation). Set to 0 to turn the animations off.",
        },
        new()
        {
            Name = "CanDrop",
            Type = "Func<BitSortableEventArgs<TItem>, bool>?",
            DefaultValue = "null",
            Description = "Decides whether the dragged item may be placed at a position in this list, as the item reaches it: return false to refuse. Asked with the proposed move - NewIndex is the index the item would have here, FromId the list it comes from - for a reorder within this list and for an item coming in from another list alike, before anything is shown. A refused position keeps the item where it was, and the pointer shows that it cannot be dropped there. In swap mode it is asked about the dragged item alone, with the index of the item it would trade places with; a swap across lists also asks the list the dragged item is in about the item that would come back to it. A multi-drag asks once more on the drop, with the index the item takes once the rest of the selection gathers next to it: a refusal there puts every selected item back and ends the drag as cancelled. One that throws refuses the position it was asked about, and the exception is passed on once that step of the drag is done: thrown to the caller of MoveAsync or MoveToAsync, and raised as this list's own during a drag the user makes, for the nearest ErrorBoundary. Keep it fast and free of side effects: it runs while the pointer moves. Mirrors the SortableJS onMove option.",
            LinkType = LinkType.Link,
            Href = "#event-args",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Extra content rendered after the items, inside the list.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitSortableClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the sortable list.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Clone",
            Type = "Func<TItem, TItem>?",
            DefaultValue = "null",
            Description = "The factory used to copy an item when Pull is Clone. Without one an item that is ICloneable is cloned, and any other one is shared, which is safe for values and strings.",
        },
        new()
        {
            Name = "ConfirmDrop",
            Type = "Func<BitSortableEventArgs<TItem>, Task<bool>>?",
            DefaultValue = "null",
            Description = "Asked once an item has been dropped into this list - from this list or another one - whether to keep the move, for a check that has to wait (a server call, a confirmation dialog). The drop shows at once; the lists stay as dropped while the task runs, and no other drag starts until it completes. Return false to put every list the drag changed back as it was: ItemsChanged is raised for them (the page may have rendered them as dropped meanwhile), OnEnd reports Canceled, and no OnUpdate, OnAdd, OnRemove or OnSort is raised. Return true to keep it: the events follow as usual. Do not change the lists while it runs: a refusal puts back what they held before the drag. One that throws refuses the drop, and the exception is passed on once the drag is reported, as one CanDrop throws is.",
            LinkType = LinkType.Link,
            Href = "#event-args",
        },
        new()
        {
            Name = "Delay",
            Type = "int",
            DefaultValue = "0",
            Description = "How long, in milliseconds, a mouse or pen press on an item must be held before it can be dragged; a pointer that moves further than DelayTolerance first is a click or a selection instead. The SortableJS delay option.",
        },
        new()
        {
            Name = "DelayTolerance",
            Type = "int",
            DefaultValue = "5",
            Description = "How far, in pixels, the pointer may move while Delay or TouchDelay runs before the press is taken for a scroll or a click instead of a drag. The SortableJS touchStartThreshold option.",
        },
        new()
        {
            Name = "Direction",
            Type = "BitSortableDirection",
            DefaultValue = "BitSortableDirection.Auto",
            Description = "The axis to sort along. Auto detects it from the layout; set it explicitly for grids. Mirrors the SortableJS direction option.",
            LinkType = LinkType.Link,
            Href = "#direction-enum",
        },
        new()
        {
            Name = "EmptyTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template of what the list shows while it has no items. It takes precedence over EmptyText.",
        },
        new()
        {
            Name = "EmptyText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text the list shows while it has no items. When null, the NoItemsFound text of the strings in force is used (BitStrings.NoItemsFound).",
        },
        new()
        {
            Name = "Filter",
            Type = "Func<TItem, bool>?",
            DefaultValue = "null",
            Description = "A predicate returning true for the items that must not be dragged. The other items can still be dragged past a filtered item, which moves it along by one, so it does not pin an item to its index.",
        },
        new()
        {
            Name = "ForceFallback",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the mouse drags like a finger or a pen: with pointer events and a preview of the item that follows the pointer (BitSortableClassStyles.Drag), instead of the browser's own drag and its drag image - for a preview styled the app's way, or a web view whose native drag and drop does not work. A finger and a pen always drag this way. The SortableJS forceFallback option.",
        },
        new()
        {
            Name = "Group",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the group of lists that can exchange items: lists with the same group name take each other's items.",
        },
        new()
        {
            Name = "Handle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of a drag handle within each item. With a handle only the handle starts a drag, and the rest of the item keeps its normal pointer behavior (text selection, inputs, links).",
        },
        new()
        {
            Name = "InvertedSwapThreshold",
            Type = "double",
            DefaultValue = "0",
            Description = "The fraction (0-1) of an item used for the inverted swap zone. Defaults to SwapThreshold when left at 0. Mirrors the SortableJS invertedSwapThreshold option.",
        },
        new()
        {
            Name = "InvertSwap",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true the swap zones sit at the item's edges instead of its center, giving a \"sort between items\" feel. Mirrors the SortableJS invertSwap option.",
        },
        new()
        {
            Name = "IsolateItems",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders each item's template in a component of its own, rendered again only when the item, the template or the list's parameters change - not on every step of a drag, where only the item wrappers move. For long lists with costly templates. Leave it off when a template shows something that changes as other items move during a drag (an item's position, say): it is brought up to date only when the drag ends.",
        },
        new()
        {
            Name = "ItemClass",
            Type = "Func<TItem, string?>?",
            DefaultValue = "null",
            Description = "The CSS class of an item's wrapper, chosen per item, which follows the item from list to list.",
        },
        new()
        {
            Name = "ItemKey",
            Type = "Func<TItem, object>?",
            DefaultValue = "null",
            Description = "What identifies an item, e.g. card => card.Id: the value its element is keyed by (@key), by which the list finds it, tells it from the other items and keeps it selected. Give it when the page may replace the items with new instances of the same data - a list loaded again from a server - so that each item keeps its element, its focus and its selection; or when items compared by reference stand for records with an id. The key must be unique within the list and must not change while the item is in it, and a Clone factory has to give a copy a key of its own: a copy whose key is already in the target list is refused as a duplicate. A lambda written inline is fine: the list tells a new ItemKey by its code, not by the delegate instance a render creates. Without it an item is identified by itself, compared by its Equals.",
        },
        new()
        {
            Name = "ItemLabel",
            Type = "Func<TItem, string>?",
            DefaultValue = "null",
            Description = "How the announcements name an item, e.g. card => card.Title. Without it an item is named by its ToString().",
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>?",
            DefaultValue = "null",
            Description = "The list of items to display and reorder. Bind with @bind-Items. It must be a mutable list (e.g. List<T>), changed in place; a read-only one (such as an array) can only be dragged from in Clone mode.",
        },
        new()
        {
            Name = "ItemsChanged",
            Type = "EventCallback<IList<TItem>>",
            DefaultValue = "",
            Description = "The callback raised with the list each time a drag changed it (supports @bind-Items). The list is the one Items holds, changed in place.",
        },
        new()
        {
            Name = "ItemStyle",
            Type = "Func<TItem, string?>?",
            DefaultValue = "null",
            Description = "The inline style of an item's wrapper, chosen per item, which follows the item from list to list.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The template rendered for each item.",
        },
        new()
        {
            Name = "Keyboard",
            Type = "bool",
            DefaultValue = "true",
            Description = "Lets the items be picked up, moved and dropped from the keyboard: an item is focusable (the list is one tab stop, and the arrow keys move between its items), Space or Enter picks it up, the arrow keys move it - within the list, and into the next list of the group in that direction - Home and End move it to the ends, Space or Enter drops it and Escape puts it back. Every step is announced to screen readers.",
        },
        new()
        {
            Name = "MaxItems",
            Type = "int?",
            DefaultValue = "null",
            Description = "The most items this list holds: once it holds this many, it takes no item from another list (a multi-drag counts every item it carries). Reordering within the list, and swapping, are not affected. null sets no limit.",
        },
        new()
        {
            Name = "MinItems",
            Type = "int?",
            DefaultValue = "null",
            Description = "The fewest items this list keeps: an item is not moved out to another list when that would leave fewer (a multi-drag counts every item it carries away). Reordering within the list, cloning out of it and swapping are not affected, and neither is a change the page makes to the list itself. null sets no limit.",
        },
        new()
        {
            Name = "MultiDrag",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the MultiDrag plugin behavior: click items to select several, then drag any one of them to move the whole selection together. Mirrors the SortableJS multiDrag option.",
        },
        new()
        {
            Name = "OnAdd",
            Type = "EventCallback<BitSortableEventArgs<TItem>>",
            DefaultValue = "",
            Description = "The callback raised on the destination list when an item is dropped in from another list.",
            LinkType = LinkType.Link,
            Href = "#event-args",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitSortableEventArgs<TItem>>",
            DefaultValue = "",
            Description = "The callback raised while dragging, each time the dragged item moves to a new position in this list.",
            LinkType = LinkType.Link,
            Href = "#event-args",
        },
        new()
        {
            Name = "OnEnd",
            Type = "EventCallback<BitSortableEventArgs<TItem>>",
            DefaultValue = "",
            Description = "The callback raised on the source list when a drag ends, whatever its outcome - also when it ends before it was released, because the source list left the page or the browser never reported its end.",
            LinkType = LinkType.Link,
            Href = "#event-args",
        },
        new()
        {
            Name = "OnRemove",
            Type = "EventCallback<BitSortableEventArgs<TItem>>",
            DefaultValue = "",
            Description = "The callback raised on the source list when an item is moved out to another list.",
            LinkType = LinkType.Link,
            Href = "#event-args",
        },
        new()
        {
            Name = "OnSort",
            Type = "EventCallback<BitSortableEventArgs<TItem>>",
            DefaultValue = "",
            Description = "The callback raised on every list whose items changed (add, remove or update).",
            LinkType = LinkType.Link,
            Href = "#event-args",
        },
        new()
        {
            Name = "OnStart",
            Type = "EventCallback<BitSortableEventArgs<TItem>>",
            DefaultValue = "",
            Description = "The callback raised on this list when a drag starts in it.",
            LinkType = LinkType.Link,
            Href = "#event-args",
        },
        new()
        {
            Name = "OnUpdate",
            Type = "EventCallback<BitSortableEventArgs<TItem>>",
            DefaultValue = "",
            Description = "The callback raised when the order of this list changed within the list.",
            LinkType = LinkType.Link,
            Href = "#event-args",
        },
        new()
        {
            Name = "Pull",
            Type = "BitSortablePull",
            DefaultValue = "BitSortablePull.Move",
            Description = "Controls how items may be dragged out of this list, mirroring the SortableJS group.pull option.",
            LinkType = LinkType.Link,
            Href = "#pull-enum",
        },
        new()
        {
            Name = "Put",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether items from other lists in the group may be dropped here.",
        },
        new()
        {
            Name = "PutGroups",
            Type = "IReadOnlyCollection<string>?",
            DefaultValue = "null",
            Description = "Other groups whose items may be dropped into this list, besides its own Group: a trash or an archive list that takes the cards of several boards, say. It lets items in only - this list's own items still go only to the lists of its group, or to lists that name its group here - and Put, MaxItems, CanDrop and the item type apply as for any other drop. The SortableJS group.put option given a list of group names.",
        },
        new()
        {
            Name = "RevertOnSpill",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, a drag that starts in this list and is released outside every list that takes it puts everything back where it was: a live reorder is undone, and an item previewed in another list returns home. The SortableJS revertOnSpill option. With the default false the drag keeps what it did, as in SortableJS: an item previewed in another list lands there wherever it is released. A drag cancelled with Escape where the list can tell (a keyboard drag, or a touch or pen drag) is always put back.",
        },
        new()
        {
            Name = "Scroll",
            Type = "bool",
            DefaultValue = "true",
            Description = "Scrolls whatever the pointer is near the edge of - the list, a scrolling panel around it, the page - while a finger, a pen or a ForceFallback mouse drags an item, the innermost one that can still scroll that way first. A native mouse drag is scrolled by the browser itself. The SortableJS scroll option.",
        },
        new()
        {
            Name = "ScrollSensitivity",
            Type = "int",
            DefaultValue = "40",
            Description = "How close, in pixels, the pointer must come to an edge to scroll (see Scroll).",
        },
        new()
        {
            Name = "ScrollSpeed",
            Type = "int",
            DefaultValue = "16",
            Description = "The most pixels an edge scrolls per frame, reached at the very edge (see Scroll).",
        },
        new()
        {
            Name = "SelectedItems",
            Type = "IReadOnlyCollection<TItem>?",
            DefaultValue = "null",
            Description = "The items selected for a multi-drag (MultiDrag), to read or set from the page: bind it with @bind-SelectedItems. Items that are not in the list, or that cannot be dragged, are left out.",
        },
        new()
        {
            Name = "SelectedItemsChanged",
            Type = "EventCallback<IReadOnlyCollection<TItem>>",
            DefaultValue = "",
            Description = "The callback raised with the selection, in list order, when a click, Shift+click or Shift+Space changes it, or a drag spends or clears it (supports @bind-SelectedItems).",
        },
        new()
        {
            Name = "Sort",
            Type = "bool",
            DefaultValue = "true",
            Description = "Allows reordering within this list. Set to false to only allow dragging out.",
        },
        new()
        {
            Name = "Strings",
            Type = "BitSortableStrings?",
            DefaultValue = "null",
            Description = "The texts the list gives assistive technology - the keyboard instructions, how an item is described, and what is announced as items are picked up, moved, dropped or put back - to translate or reword them. When null, the English texts of a new BitSortableStrings are used.",
            LinkType = LinkType.Link,
            Href = "#strings",
        },
        new()
        {
            Name = "Styles",
            Type = "BitSortableClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the sortable list.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Swap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the Swap plugin behavior: instead of sorting, the dragged item trades places with the item it is dropped on. While dragging, the item under the pointer is highlighted (BitSortableClassStyles.SwapTarget). Mirrors the SortableJS swap option.",
        },
        new()
        {
            Name = "SwapThreshold",
            Type = "double",
            DefaultValue = "1",
            Description = "The fraction (0-1) of an item that acts as its swap zone, mirroring the SortableJS swapThreshold option. With the default 1 the whole item swaps; a smaller value (e.g. 0.65) leaves a dead zone near each edge, so the pointer has to move further into an item before the dragged one swaps past it.",
        },
        new()
        {
            Name = "TouchDelay",
            Type = "int",
            DefaultValue = "200",
            Description = "How long, in milliseconds, a finger must rest on an item before it drags it - a long press - so that a swipe over the list still scrolls the page. A finger that moves further than DelayTolerance first scrolls. Set 0 with a Handle whose CSS has touch-action: none for a handle that drags at once.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "MoveAsync",
            Type = "Task<bool> MoveAsync(TItem item, int newIndex)",
            Description = "Moves the item to newIndex within this list, as a drag would: the list must allow it (Sort, Disabled, CanDrop), the destination's ConfirmDrop is asked, the items slide into place, and the drag's events follow, with Input set to Api. In swap mode the item trades places with the one at newIndex. Filter locks an item against the user's drags only, and a multi-drag selection is not carried along. It is the non-drag way to rearrange a list that an accessible app offers next to dragging: \"move up\", \"move to top\" buttons or an item menu. Returns true when the item moved; false when it is not in this list, the index is out of range or the item is already there, a drag is in progress, or a rule refused the move.",
        },
        new()
        {
            Name = "MoveToAsync",
            Type = "Task<bool> MoveToAsync(TItem item, string listId, int newIndex)",
            Description = "Moves the item from this list into the list whose id (its Id, or its UniqueId) is listId, at newIndex, as a drag would: the lists must exchange items (Group or the destination's PutGroups, Pull, Put, Disabled, the destination's MaxItems, this list's MinItems, the item type), the destination's CanDrop and ConfirmDrop are asked, a Clone source puts a copy there, and the drag's events follow with Input set to Api. Into a swap-mode list the item trades places with the one at newIndex. Returns true when the item moved; false when there is no such list, the item is not in this list, the index is out of range, a drag is in progress, or a rule refused the move.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "event-args",
            Title = "BitSortableEventArgs<TItem>",
            Description = "The payload of the drag lifecycle callbacks (OnStart, OnEnd, OnAdd, OnRemove, OnUpdate, OnSort and OnChange) and of the CanDrop and ConfirmDrop questions. Mirrors the SortableJS event object.",
            Parameters =
            [
                new() { Name = "Item", Type = "TItem", Description = "The item that was dragged." },
                new() { Name = "Items", Type = "IReadOnlyList<TItem>", DefaultValue = "[]", Description = "All items moved by this drag, in list order. For a multi-drag this is the whole selection (including Item); otherwise it contains just Item. Mirrors evt.items of the SortableJS MultiDrag plugin." },
                new() { Name = "OldIndex", Type = "int", Description = "The item's index within its original list when the drag started." },
                new() { Name = "NewIndex", Type = "int", Description = "The item's index within the destination list." },
                new() { Name = "FromId", Type = "string", DefaultValue = "\"\"", Description = "The id of the source list: its Id, or its UniqueId when it is given none." },
                new() { Name = "ToId", Type = "string", DefaultValue = "\"\"", Description = "The id of the destination list: its Id, or its UniqueId when it is given none." },
                new() { Name = "FromGroup", Type = "string?", DefaultValue = "null", Description = "The group name of the source list, if any." },
                new() { Name = "ToGroup", Type = "string?", DefaultValue = "null", Description = "The group name of the destination list, if any." },
                new() { Name = "CrossedLists", Type = "bool", Description = "true when the item crossed from one list into another." },
                new() { Name = "Input", Type = "BitSortableInput", Description = "How the drag was made: with a mouse, a finger, a pen, the keyboard, or by code.", LinkType = LinkType.Link, Href = "#input-enum" },
                new() { Name = "IsClone", Type = "bool", Description = "true when the move puts a copy of the item into another list (the source's Pull is Clone) and the original stays. Item is then the copy in the events that follow the drop, and the original in the ones before it." },
                new() { Name = "Canceled", Type = "bool", Description = "Set on OnEnd: true when the drag ended without keeping any change, and every list it touched was put back as it was. It was cancelled (Escape); released outside every list of a source with RevertOnSpill; refused by the destination's ConfirmDrop (or one that threw); a multi-drag whose gathering on the drop the destination's CanDrop refused (or threw on); or a drag whose list left the page before it was dropped, into a list with a ConfirmDrop, which cannot be asked then." },
                new() { Name = "IsSwap", Type = "bool", Description = "true when the move is a swap: the dragged Item trades places with SwappedWith (a list in Swap mode). Set on the events of a committed swap and on the CanDrop questions about one." },
                new() { Name = "SwappedWith", Type = "TItem?", Description = "In a swap (IsSwap), the item Item trades places with: it takes the place Item leaves - index OldIndex of the source list, unless a keyboard drag carried the item into another list first - so that a handler can save both moves. default for any other move: check IsSwap rather than this, as for an item of a value type default is an item too." },
            ]
        },
        new()
        {
            Id = "strings",
            Title = "BitSortableStrings",
            Description = "All screen-reader-visible strings of a BitSortable: the keyboard instructions every item points to, how an item is described, and what is announced as an item is picked up, moved, dropped or put back. Assign a customized instance to the Strings parameter - or to a BitSortableParams cascaded by a BitParams - to translate or reword them; every property defaults to the English text. A list announces with the strings of the list a drag started in. The announcements are functions of a BitSortableAnnouncement rather than format strings, since what they say depends on more than the values they put in.",
            Parameters =
            [
                new() { Name = "Instructions", Type = "string", DefaultValue = "\"To pick up the item, press Space or Enter. ...\"", Description = "How to drag an item with the keyboard; every item refers to it (aria-describedby), so a screen reader reads it when an item is focused." },
                new() { Name = "SelectionInstructions", Type = "string", DefaultValue = "\"Shift and Space select or deselect the item; ...\"", Description = "Added to Instructions in a MultiDrag list: how to select several items from the keyboard." },
                new() { Name = "RoleDescription", Type = "string", DefaultValue = "\"sortable item\"", Description = "What an item is called in place of \"list item\" (aria-roledescription)." },
                new() { Name = "PickedUp", Type = "Func<BitSortableAnnouncement, string>", Description = "An item was picked up.", LinkType = LinkType.Link, Href = "#announcement" },
                new() { Name = "Moved", Type = "Func<BitSortableAnnouncement, string>", Description = "A picked-up item moved to a new position, or a move made by code moved an item.", LinkType = LinkType.Link, Href = "#announcement" },
                new() { Name = "SwapTarget", Type = "Func<BitSortableAnnouncement, string>", Description = "In swap mode, a picked-up item will trade places with Target when it is dropped.", LinkType = LinkType.Link, Href = "#announcement" },
                new() { Name = "Dropped", Type = "Func<BitSortableAnnouncement, string>", Description = "An item was dropped.", LinkType = LinkType.Link, Href = "#announcement" },
                new() { Name = "Canceled", Type = "Func<BitSortableAnnouncement, string>", Description = "A drag was cancelled, or its drop refused, and the item put back.", LinkType = LinkType.Link, Href = "#announcement" },
                new() { Name = "Refused", Type = "Func<BitSortableAnnouncement, string>", Description = "A position the item was moved toward is refused (a rule, a full list), or the item cannot be picked up.", LinkType = LinkType.Link, Href = "#announcement" },
                new() { Name = "Locked", Type = "Func<BitSortableAnnouncement, string>", Description = "An item that cannot be dragged (one Filter locks, or one of a disabled list) was asked to be picked up.", LinkType = LinkType.Link, Href = "#announcement" },
                new() { Name = "Selected", Type = "Func<BitSortableAnnouncement, string>", Description = "In a MultiDrag list, Shift+Space added an item to the selection.", LinkType = LinkType.Link, Href = "#announcement" },
                new() { Name = "Deselected", Type = "Func<BitSortableAnnouncement, string>", Description = "In a MultiDrag list, Shift+Space took an item out of the selection.", LinkType = LinkType.Link, Href = "#announcement" },
            ]
        },
        new()
        {
            Id = "announcement",
            Title = "BitSortableAnnouncement",
            Description = "What a BitSortableStrings text is about: the item, and where it is.",
            Parameters =
            [
                new() { Name = "Item", Type = "string", Description = "The item, as ItemLabel names it." },
                new() { Name = "Position", Type = "int", Description = "Its position now, counted from 1." },
                new() { Name = "Count", Type = "int", Description = "How many items the list it is in holds." },
                new() { Name = "List", Type = "string?", Description = "The list it is in, as its AriaLabel names it; null when it has none." },
                new() { Name = "OldPosition", Type = "int", Description = "Its position when it was picked up, counted from 1." },
                new() { Name = "FromList", Type = "string?", Description = "The list it was picked up from, as its AriaLabel names it." },
                new() { Name = "Returned", Type = "bool", Description = "Whether it is back where it was picked up: in the same list (not merely one with the same name, or none), at the same position." },
                new() { Name = "Target", Type = "string?", Description = "In swap mode, the item it will trade places with; otherwise null." },
                new() { Name = "Selected", Type = "int", Description = "For Selected and Deselected: how many items are selected now." },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitSortableClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitSortable." },
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every item wrapper of the BitSortable." },
                new() { Name = "Ghost", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the item being dragged, and for the drop preview shown in another list." },
                new() { Name = "Chosen", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the item picked up." },
                new() { Name = "SwapTarget", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the item highlighted as the swap target while dragging in swap mode." },
                new() { Name = "Selected", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the items of a MultiDrag selection." },
                new() { Name = "Filtered", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the items the Filter locks." },
                new() { Name = "Drag", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the preview of the item that follows a finger, a pen, or the mouse with ForceFallback, which is placed above the page." },
                new() { Name = "Empty", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for what an empty list shows." },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "direction-enum",
            Name = "BitSortableDirection",
            Description = "The axis a list sorts along, mirroring the direction option of SortableJS. Determines whether the swap zones span the width or the height of an item.",
            Items =
            [
                new() { Name = "Auto", Description = "Detect the axis automatically from the layout of the items.", Value = "0" },
                new() { Name = "Horizontal", Description = "Sort along the horizontal (x) axis.", Value = "1" },
                new() { Name = "Vertical", Description = "Sort along the vertical (y) axis.", Value = "2" },
            ]
        },
        new()
        {
            Id = "input-enum",
            Name = "BitSortableInput",
            Description = "How a drag of a list was made, as its events report it.",
            Items =
            [
                new() { Name = "Mouse", Description = "A mouse (or a trackpad).", Value = "0" },
                new() { Name = "Touch", Description = "A finger on a touch screen.", Value = "1" },
                new() { Name = "Pen", Description = "A pen or stylus.", Value = "2" },
                new() { Name = "Keyboard", Description = "The keyboard: the item was picked up, moved and dropped with keys.", Value = "3" },
                new() { Name = "Api", Description = "Code: a move made through the list's methods, such as MoveAsync.", Value = "4" },
            ]
        },
        new()
        {
            Id = "pull-enum",
            Name = "BitSortablePull",
            Description = "Controls whether, and how, items may be dragged out of a list, mirroring the group.pull option of SortableJS.",
            Items =
            [
                new() { Name = "Move", Description = "Items can be moved out of the list (the default).", Value = "0" },
                new() { Name = "None", Description = "Items cannot be dragged out of the list.", Value = "1" },
                new() { Name = "Clone", Description = "A copy of the item is placed in the target list; the original stays put.", Value = "2" },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Sortable-ghost-opacity",
            DefaultValue = "0.4",
            Description = "Opacity of the item being dragged, and of the drop preview shown in another list.",
        },
        new()
        {
            Name = "--bit-Sortable-chosen-background",
            DefaultValue = "--bit-clr-bg-pri-hover",
            Description = "Background of the item picked up.",
        },
        new()
        {
            Name = "--bit-Sortable-selected-background",
            DefaultValue = "--bit-clr-pri-light",
            Description = "Background of an item of a MultiDrag selection.",
        },
        new()
        {
            Name = "--bit-Sortable-selected-ring-color",
            DefaultValue = "--bit-clr-pri",
            Description = "The ring around an item of a MultiDrag selection.",
        },
        new()
        {
            Name = "--bit-Sortable-swap-background",
            DefaultValue = "--bit-clr-pri-light",
            Description = "Background of the item a dragged one would trade places with, in swap mode.",
        },
        new()
        {
            Name = "--bit-Sortable-swap-outline-color",
            DefaultValue = "--bit-clr-pri",
            Description = "The dashed outline of the item a dragged one would trade places with.",
        },
        new()
        {
            Name = "--bit-Sortable-filtered-opacity",
            DefaultValue = "1",
            Description = "Opacity of an item the Filter locks. Not dimmed by default: the item's content stays live, and a faded item would fall below the contrast its text needs.",
        },
        new()
        {
            Name = "--bit-Sortable-drag-opacity",
            DefaultValue = "0.92",
            Description = "Opacity of the preview that follows a finger, a pen or a ForceFallback mouse. The preview is placed above the page, outside the list, so only a value set on :root (or an ancestor of the body) reaches it.",
        },
        new()
        {
            Name = "--bit-Sortable-drag-shadow",
            DefaultValue = "--bit-shd-popup",
            Description = "Shadow of the preview that follows a finger, a pen or a ForceFallback mouse; set it on :root.",
        },
        new()
        {
            Name = "--bit-Sortable-count-background",
            DefaultValue = "--bit-clr-pri",
            Description = "Background of the count a multi-drag's picture carries; set it on :root, as the picture is placed above the page.",
        },
        new()
        {
            Name = "--bit-Sortable-count-color",
            DefaultValue = "--bit-clr-pri-text",
            Description = "Text of the count a multi-drag's picture carries.",
        },
        new()
        {
            Name = "--bit-Sortable-accepts-outline",
            DefaultValue = "none",
            Description = "Outline of a list that takes the drag in progress.",
        },
        new()
        {
            Name = "--bit-Sortable-over-outline",
            DefaultValue = "none",
            Description = "Outline of the list under the pointer.",
        },
        new()
        {
            Name = "--bit-Sortable-refused-outline",
            DefaultValue = "none",
            Description = "Outline of the list under the pointer that refused the position.",
        },
        new()
        {
            Name = "--bit-Sortable-target-offset",
            DefaultValue = "2px",
            Description = "The offset of the accepts, over and refused outlines.",
        },
        new()
        {
            Name = "--bit-Sortable-empty-padding",
            DefaultValue = "spacing(2)",
            Description = "Padding of what an empty list shows.",
        },
        new()
        {
            Name = "--bit-Sortable-empty-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Text of what an empty list shows.",
        },
    ];


    private IList<string> basicItems = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"];

    private double animation = 150;

    private IList<string> animationItems = ["Apples", "Bread", "Cheese", "Dates", "Eggs"];

    private IList<string> groupA = ["A1", "A2", "A3", "A4"];
    private IList<string> groupB = ["B1", "B2", "B3", "B4"];

    private IList<string> inbox = ["Invoice", "Newsletter", "Meeting notes"];
    private IList<string> mailArchive = [];
    private IList<string> spam = [];

    // A read-only list: it is shown and cloned from, but never sorted, pulled from or dropped into.
    private readonly FormField[] palette = [new("Text box"), new("Checkbox"), new("Drop-down"), new("Date")];

    private IList<FormField> form = [];

    // A class, not a record: two copies of the same field must not be equal, since a list holds an item once.
    public sealed class FormField(string name)
    {
        public string Name { get; } = name;
    }

    private IList<string> pickFrom = ["Pick 1", "Pick 2", "Pick 3"];
    private IList<string> pinned = ["Rent", "Groceries"];
    private IList<string> target = ["Gym", "Books"];

    private bool archiveDisabled = true;

    private IList<string> archive = ["2024 report", "2025 report"];
    private IList<string> desk = ["Draft", "Notes"];

    private IList<ChecklistStep> steps =
    [
        new() { Text = "Pack the tent" },
        new() { Text = "Check the weather", Done = true },
        new() { Text = "Fill the water bottles" },
        new() { Text = "Charge the torch" },
    ];

    public sealed class ChecklistStep
    {
        public required string Text { get; set; }
        public bool Done { get; set; }
    }

    private IList<LockableTask> tasks =
    [
        new() { Title = "Write the tests" },
        new() { Title = "Release notes", Locked = true },
        new() { Title = "Deploy" },
        new() { Title = "Announce" },
    ];

    public sealed class LockableTask
    {
        public required string Title { get; init; }
        public bool Locked { get; set; }
    }

    private double swapThreshold = 0.65;
    private bool invertSwap;
    private double invertedSwapThreshold = 0.65;

    private IList<string> thresholdItems = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"];

    // The zones are drawn by the page's stylesheet, from these two custom properties.
    private string ZoneStyle => string.Create(CultureInfo.InvariantCulture,
        $"--zone:{swapThreshold:0.00};--inverted-zone:{invertedSwapThreshold:0.00}");

    private IList<int> numbers = Enumerable.Range(1, 12).ToList();

    private IList<string> tags = ["blazor", "dotnet", "drag-and-drop", "sortable", "css", "a11y"];

    private IList<TreeNode> tree =
    [
        new("Item 1.1", [new("Item 2.1"), new("Item 2.2", [new("Item 3.1"), new("Item 3.2")]), new("Item 2.3")]),
        new("Item 1.2"),
        new("Item 1.3", [new("Item 2.4"), new("Item 2.5")]),
        new("Item 1.4"),
    ];

    // "Item 1.1 [Item 2.1, Item 2.2 [Item 3.1, Item 3.2]], Item 1.2": every item, its children in brackets.
    private static string Describe(IList<TreeNode> nodes)
        => string.Join(", ", nodes.Select(n => n.Children.Count == 0 ? n.Name : $"{n.Name} [{Describe(n.Children)}]"));

    public sealed class TreeNode(string name, List<TreeNode>? children = null)
    {
        public string Name { get; } = name;
        public IList<TreeNode> Children { get; } = children ?? [];
    }

    private IList<string> todo = ["Write the docs", "Plan the release", "Tidy the backlog"];
    private IList<string> bugs = ["Crash on save", "Typo in the footer"];
    private IList<string> trash = [];

    private IList<string> pipeline = ["Build", "Test", "Deploy"];
    private IList<string> shelf = ["Lint", "Benchmark"];

    private bool approve = true;
    private string serverStatus = "idle";

    private IList<string> boardTodo = ["Write the spec", "Draw the icons", "Plan the launch"];
    private IList<string> boardDoing = ["Build the API", "Review the PR"];
    private IList<string> boardDone = ["Set up CI"];

    // Done takes the cards of In progress, and its own.
    private bool CanDropInDone(BitSortableEventArgs<string> e) => e.FromId is "board-doing" or "board-done";

    // Stands in for a call to a server that may refuse the move.
    private async Task<bool> ConfirmDoneAsync(BitSortableEventArgs<string> e)
    {
        serverStatus = $"asking about {e.Item}...";
        StateHasChanged();
        await Task.Delay(500);
        serverStatus = approve ? $"saved {e.Item}" : $"refused {e.Item}";
        StateHasChanged();
        return approve;
    }

    private BitSortable<string> backlogList = default!;
    private BitSortable<string> sprintList = default!;

    private IList<string> backlog = ["Sign-up page", "Password reset", "Audit log", "Dark mode"];
    private IList<string> sprint = ["Search", "Export to CSV"];

    private string lastSwap = "nothing yet";

    private IList<string> swapItems = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"];

    // SwappedWith names the item the dragged one traded places with: both moves, for a handler that saves them.
    private void HandleSwap(BitSortableEventArgs<string> e)
    {
        lastSwap = e.IsSwap ? $"{e.Item} and {e.SwappedWith}" : $"{e.Item} moved";
    }

    private IList<string> multiItems = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6"];
    private IReadOnlyCollection<string> selectedItems = [];

    private bool logChange;
    private readonly List<string> eventLog = [];

    private IList<string> eventsLeft = ["Item 1", "Item 2", "Item 3", "Item 4"];
    private IList<string> eventsRight = ["Item A", "Item B"];

    // "[OnAdd @events-right] Item 4: events-left#3 -> events-right#0 (mouse)": the event, the list that raised it, the
    // item, where it came from and where it went, how it was dragged and the flags that are set.
    private void Log(string name, string list, BitSortableEventArgs<string> e)
    {
        if (name == "OnChange" && logChange is false) return;

        var flags = string.Join(", ", new[] { e.Input.ToString().ToLowerInvariant(), e.IsClone ? "clone" : null, e.Canceled ? "canceled" : null }.OfType<string>());
        eventLog.Insert(0, $"[{name} @{list}] {e.Item}: {e.FromId}#{e.OldIndex} -> {e.ToId}#{e.NewIndex} ({flags})");
        if (eventLog.Count > 30) eventLog.RemoveAt(eventLog.Count - 1);
    }

    private bool keyboard = true;
    private bool german;

    private IList<Ticket> ticketBacklog = [new(1, "Sign-up page"), new(2, "Password reset"), new(3, "Audit log")];
    private IList<Ticket> ticketSprint = [new(4, "Search"), new(5, "Export to CSV")];

    private sealed record Ticket(int Id, string Title);

    private static string In(string? list) => list is null ? "" : $" in {list}";

    private readonly BitSortableStrings germanStrings = new()
    {
        Instructions = "Leertaste oder Eingabetaste nimmt das Element auf. Danach verschieben es die Pfeiltasten, Pos1 und Ende an den " +
                       "Anfang oder das Ende der Liste, Leertaste oder Eingabetaste legt es ab, und Escape bringt es zurück.",
        SelectionInstructions = "Umschalt und Leertaste wählen das Element aus oder ab; ein ausgewähltes Element nimmt die ganze Auswahl mit.",
        RoleDescription = "sortierbares Element",
        PickedUp = a => $"{a.Item} aufgenommen. Position {a.Position} von {a.Count}{In(a.List)}.",
        Moved = a => $"{a.Item} auf Position {a.Position} von {a.Count}{In(a.List)} verschoben.",
        SwapTarget = a => a.Target is null ? $"{a.Item} bleibt, wo es ist." : $"{a.Item} tauscht den Platz mit {a.Target}.",
        Dropped = a => a.Returned
            ? $"{a.Item} abgelegt. Wieder auf Position {a.Position} von {a.Count}{In(a.List)}."
            : $"{a.Item} auf Position {a.Position} von {a.Count}{In(a.List)} abgelegt.",
        Canceled = a => $"Abgebrochen. {a.Item} ist wieder auf Position {a.Position} von {a.Count}{In(a.List)}.",
        Refused = a => $"{a.Item} kann nicht dorthin verschoben werden.",
        Locked = a => $"{a.Item} kann nicht verschoben werden.",
        Selected = a => $"{a.Item} ausgewählt. {a.Selected} ausgewählt.",
        Deselected = a => $"{a.Item} abgewählt. {a.Selected} ausgewählt.",
    };

    private double touchDelay = 200;
    private double delay;
    private double delayTolerance = 5;
    private bool forceFallback;

    private IList<string> groceries = ["Kale", "Lemons", "Milk", "Nuts", "Oats"];

    private bool isolateItems;
    private bool scroll = true;
    private double scrollSensitivity = 40;
    private double scrollSpeed = 16;

    private IList<LargeRow> rows = Enumerable.Range(1, 300)
        .Select(i => new LargeRow($"Row {i}", i % 3 == 0 ? "blocked" : i % 2 == 0 ? "in review" : "open"))
        .ToList();

    private sealed record LargeRow(string Name, string Note);

    private static readonly Func<TaskRow, object> byId = row => row.Id;

    private bool keyById = true;
    private int loads = 1;

    private IList<TaskRow> taskRows = [new(1, "Write the spec"), new(2, "Review the design"), new(3, "Build the API"), new(4, "Ship it")];

    // What a refresh from the server does: the same rows, in the order last saved, as new objects.
    private void ReloadRows()
    {
        taskRows = taskRows.Select(row => new TaskRow(row.Id, row.Title)).ToList();
        loads++;
    }

    // A row as a server sends it: a class, so two loads of the same row are two different objects.
    public sealed class TaskRow(int id, string title)
    {
        public int Id { get; } = id;
        public string Title { get; } = title;
    }

    private readonly BitSortableParams[] sortableParams =
    [
        new() { Animation = 600, EmptyText = "Nothing here - drop an item." }
    ];

    private IList<string> paramsFirst = ["One", "Two", "Three"];
    private IList<string> paramsSecond = [];

    private const string cssVariablesStyle = "--bit-Sortable-ghost-opacity:0.15;" +
                                             "--bit-Sortable-chosen-background:var(--bit-clr-bg-sec);" +
                                             "--bit-Sortable-accepts-outline:2px dashed var(--bit-clr-pri);" +
                                             "--bit-Sortable-over-outline:2px solid var(--bit-clr-pri);";

    private IList<string> styledItems = ["Styled 1", "Styled 2", "Styled 3"];
    private IList<string> classedItems = ["Classy 1", "Classy 2", "Classy 3"];
    private IList<string> itemStyled = ["Ordinary", "Important", "Routine"];
    private IList<string> fruits = ["Apple", "Banana", "Cherry"];
    private IList<string> basket = ["Fig", "Grape"];

    private IList<string> rtlSteps = ["گام ۱", "گام ۲", "گام ۳", "گام ۴", "گام ۵"];
}
