var BitButil = BitButil || {};

(function (butil: any) {
    butil.speech = {
        isSupported() { return 'speechSynthesis' in window; },
        getVoices() {
            const synth = window.speechSynthesis;
            if (!synth) return [];
            return synth.getVoices().map(v => ({
                name: v.name,
                lang: v.lang,
                voiceUri: v.voiceURI,
                default: v.default,
                localService: v.localService
            }));
        },
        speak(utterance: any) {
            const synth = window.speechSynthesis;
            if (!synth) return;
            const u = new SpeechSynthesisUtterance(utterance.text ?? '');
            if (utterance.lang) u.lang = utterance.lang;
            if (typeof utterance.rate === 'number') u.rate = utterance.rate;
            if (typeof utterance.pitch === 'number') u.pitch = utterance.pitch;
            if (typeof utterance.volume === 'number') u.volume = utterance.volume;
            if (utterance.voiceName) {
                const match = synth.getVoices().find(v => v.name === utterance.voiceName);
                if (match) u.voice = match;
            }
            synth.speak(u);
        },
        cancel() { window.speechSynthesis?.cancel(); },
        pause() { window.speechSynthesis?.pause(); },
        resume() { window.speechSynthesis?.resume(); },
        isSpeaking() { return !!window.speechSynthesis?.speaking; },
        isPending() { return !!window.speechSynthesis?.pending; }
    };
}(BitButil));
