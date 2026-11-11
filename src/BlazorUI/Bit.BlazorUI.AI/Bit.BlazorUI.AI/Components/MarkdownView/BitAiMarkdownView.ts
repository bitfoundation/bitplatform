namespace BitBlazorUI {
    // The browser side of BitAiMarkdownView: following a footnote reference to its note, or a back-link to the place the
    // note was cited from, without leaving the page.
    export class AiMarkdownView {
        // The links are written as "#<id>", which the browser - and Blazor's router, which takes every link inside the
        // base address for its own - resolves against the <base href>, not against the page: followed as it is, the link
        // of an app whose base is "/" leads to the home page. So the view answers the click itself and brings the reader
        // to the element here.
        // The address bar is given the fragment, so the address copied from it leads back to the note, but in place of the
        // current entry rather than as a new one: Blazor numbers the history entries it pushes, and one pushed behind its
        // back would throw off the step a Back takes, which is what a NavigationLock asks the router to confirm. The
        // entry's own state is kept for the same reason. The back-link is the way back.
        // The scroll and the focus are the core's in-page scroll (BitLink's): smooth unless the reader asked for less
        // motion, and the focus moved along with it, so the next Tab and a screen reader carry on from the note too.
        public static follow(id: string) {
            if (!id) return;

            try {
                const url = new URL(location.href);
                url.hash = id;
                history.replaceState(history.state, '', url.href);
            } catch {
                // A sandboxed frame may refuse to touch its history; the reader is still taken to the note.
            }

            Utils.scrollElementIntoView(id, true);
        }
    }
}
