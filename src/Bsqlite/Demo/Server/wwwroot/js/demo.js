window.bsqliteDemo = {
  download(filename, bytes) {
    const blob = new Blob([bytes], { type: "application/vnd.sqlite3" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
  },

  async copy(text) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      // Clipboard API needs a secure context and permission; fall back to a hidden textarea.
      const area = document.createElement("textarea");
      area.value = text;
      area.setAttribute("readonly", "");
      area.style.position = "fixed";
      area.style.opacity = "0";
      document.body.appendChild(area);
      area.select();
      const ok = document.execCommand("copy");
      document.body.removeChild(area);
      return ok;
    }
  },

  /**
   * The boot script in index.html owns the resolution rules so that the first paint is already
   * correct; this object only reads and writes the preference, then asks it to re-apply.
   */
  theme: {
    /** "light" | "dark" | "system" - what the reader chose, not what is painted. */
    get() {
      return window.__bsqliteTheme.preference();
    },

    /** Stores the preference and returns what is actually painted now: "light" or "dark". */
    set(mode) {
      const key = window.__bsqliteTheme.key;
      try {
        if (mode === "light" || mode === "dark") {
          localStorage.setItem(key, mode);
        } else {
          localStorage.removeItem(key);
        }
      } catch {
        // Storage blocked: the choice still applies to this page, it just will not survive a reload.
      }
      return window.__bsqliteTheme.apply();
    },

    /** What the page is painting right now: "light" or "dark". */
    resolved() {
      return document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
    },
  },

  /** Keeps the page behind the mobile navigation drawer from scrolling with it. */
  lockScroll(locked) {
    document.body.style.overflow = locked ? "hidden" : "";
  },
};
