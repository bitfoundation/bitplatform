// Demo JS module of the Share & automate page: reads the BevTools page hub the way a tool of your own would.
export async function askHub() {
    const hub = window.__BIT_BEVTOOLS__;
    if (!hub) return [];
    const answers = [];
    for (const target of hub.targets()) {
        const answer = { label: target.label, platform: target.platform || "", renderMode: target.renderMode || "", valueAccess: "", capturing: false, error: null };
        try {
            // Every answer comes from the runtime's BevToolsProtocol, with the same redaction as the panel.
            const hello = await hub.request(target.id, "hello");
            answer.valueAccess = hello.valueAccess || "";
            answer.capturing = !!hello.capturing;
        } catch (e) {
            answer.error = String((e && e.message) || e);
        }
        answers.push(answer);
    }
    return answers;
}
