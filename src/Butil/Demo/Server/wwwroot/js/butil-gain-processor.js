// The processor half of the WebAudio page's AudioWorklet sample.
//
// It has to be JavaScript, and it has to be a file: an AudioWorklet processor runs on the audio
// thread, which cannot call into .NET and must never block. What crosses back to C# is what this
// file chooses to send - here, a peak level a few times a second, over the node's message port.
//
// The two ways .NET drives it are both visible below: `gain` is a declared AudioParam, so
// AudioNodeHandle.SetParam changes it sample-accurately, and `port.onmessage` takes the commands
// AudioWorkletNodeHandle.PostMessage sends.

class ButilGainProcessor extends AudioWorkletProcessor {
    static get parameterDescriptors() {
        return [{ name: 'gain', defaultValue: 1, minValue: 0, maxValue: 4, automationRate: 'a-rate' }];
    }

    constructor(options) {
        super();
        // How often to report, in seconds. Reporting every render quantum would be 375 messages a
        // second for no benefit - the UI cannot use them.
        this._interval = options?.processorOptions?.reportIntervalSeconds ?? 0.1;
        this._muted = false;
        this._peak = 0;
        this._elapsed = 0;

        this.port.onmessage = e => {
            const command = typeof e.data === 'string' ? e.data : '';
            if (command === 'mute') this._muted = true;
            else if (command === 'unmute') this._muted = false;
        };
    }

    process(inputs, outputs, parameters) {
        const input = inputs[0];
        const output = outputs[0];
        const gain = parameters.gain;

        for (let channel = 0; channel < output.length; channel++) {
            const source = input[channel];
            const target = output[channel];
            if (!source) {
                target.fill(0);
                continue;
            }

            for (let i = 0; i < target.length; i++) {
                // An a-rate parameter arrives as one value per sample; a k-rate one, or a parameter
                // that happens not to be changing, arrives as a single value for the whole block.
                const value = gain.length > 1 ? gain[i] : gain[0];
                const sample = this._muted ? 0 : source[i] * value;
                target[i] = sample;
                const magnitude = sample < 0 ? -sample : sample;
                if (magnitude > this._peak) this._peak = magnitude;
            }
        }

        const quantum = output[0] ? output[0].length : 128;
        this._elapsed += quantum / sampleRate;
        if (this._elapsed >= this._interval) {
            this.port.postMessage(JSON.stringify({ peak: this._peak, muted: this._muted }));
            this._elapsed = 0;
            this._peak = 0;
        }

        // Keeping the processor alive even with no input: the node stays in the graph until .NET
        // disposes it, rather than being collected the first time the source falls silent.
        return true;
    }
}

registerProcessor('butil-gain-processor', ButilGainProcessor);
