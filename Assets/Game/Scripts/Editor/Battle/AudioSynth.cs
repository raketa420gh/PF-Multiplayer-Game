using System;
using System.IO;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Small offline synthesizer for the generated sounds: noise and tone sources, resonant filters, struck and plucked
    /// bodies, a room reverb and a WAV writer. Sounds are float buffers at SampleRate, built up layer by layer.
    internal static class AudioSynth
    {
        public const int SampleRate = 44100;

        private enum Filter
        {
            LowPass,
            HighPass,
            BandPass
        }

        public static float[] Silence(float seconds)
        {
            return new float[Mathf.CeilToInt(seconds * SampleRate)];
        }

        public static float[] Noise(float seconds, int seed)
        {
            float[] buffer = Silence(seconds);
            System.Random random = new System.Random(seed);

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (float)(random.NextDouble() * 2.0 - 1.0);

            return buffer;
        }

        /// Noise with its energy in the lows, for wind, rumble and anything heavy.
        public static float[] Brown(float seconds, int seed)
        {
            float[] buffer = Noise(seconds, seed);
            float value = 0f;

            for (int i = 0; i < buffer.Length; i++)
            {
                value = (value + buffer[i] * 0.02f) * 0.998f;
                buffer[i] = value * 3.5f;
            }

            // The integrator wanders; without this the sound would sit off-centre and thump when it starts.
            return buffer.HighPass(25f);
        }

        /// Oscillator with its pitch and loudness given over time; richness adds harmonics on top of the sine.
        public static float[] Tone(float seconds, Func<float, float> frequency, Func<float, float> amplitude, float richness = 0f)
        {
            float[] buffer = Silence(seconds);
            double phase = 0.0;

            for (int i = 0; i < buffer.Length; i++)
            {
                float time = i / (float)SampleRate;
                phase += frequency(time) * 2.0 * Math.PI / SampleRate;
                float sample = (float)Math.Sin(phase);

                if (richness > 0f)
                    sample += richness * ((float)Math.Sin(phase * 2.0) * 0.5f + (float)Math.Sin(phase * 3.0) * 0.33f + (float)Math.Sin(phase * 4.0) * 0.25f);

                buffer[i] = sample * amplitude(time);
            }

            return buffer;
        }

        /// Sawtooth: the raw buzz of a throat or a wing, to be shaped by filters.
        public static float[] Buzz(float seconds, Func<float, float> frequency)
        {
            float[] buffer = Silence(seconds);
            float phase = 0f;

            for (int i = 0; i < buffer.Length; i++)
            {
                phase = Mathf.Repeat(phase + frequency(i / (float)SampleRate) / SampleRate, 1f);
                buffer[i] = phase * 2f - 1f;
            }

            return buffer;
        }

        /// A voice: the source run through parallel formant resonators (frequency, q, gain).
        public static float[] Formants(this float[] source, params (float frequency, float q, float gain)[] formants)
        {
            float[] buffer = new float[source.Length];

            foreach ((float frequency, float q, float gain) in formants)
                buffer.Add(((float[])source.Clone()).BandPass(frequency, q), gain);

            return buffer;
        }

        /// Something hit once: a click of noise ringing the given modes.
        public static float[] Strike(float seconds, int seed, params (float frequency, float decay, float gain)[] modes)
        {
            return Ring(seconds, modes, Noise(0.004f, seed).Decay(700f, 0.0003f));
        }

        /// A struck body: every mode (frequency, decay per second, gain) is a resonator rung by the excitation.
        public static float[] Ring(float seconds, (float frequency, float decay, float gain)[] modes, float[] excitation)
        {
            float[] buffer = Silence(seconds);

            foreach ((float frequency, float decay, float gain) in modes)
            {
                float radius = Mathf.Exp(-decay / SampleRate);
                float a1 = 2f * radius * Mathf.Cos(2f * Mathf.PI * frequency / SampleRate);
                float a2 = -radius * radius;
                float y1 = 0f;
                float y2 = 0f;

                for (int i = 0; i < buffer.Length; i++)
                {
                    float y = a1 * y1 + a2 * y2 + (i < excitation.Length ? excitation[i] : 0f);
                    y2 = y1;
                    y1 = y;
                    buffer[i] += y * gain;
                }
            }

            return buffer.Normalize(1f);
        }

        /// Karplus-Strong string: a burst of noise circulating in a delay line that loses its highs on every pass.
        public static float[] Pluck(float frequency, float seconds, float sustain, float brightness, int seed)
        {
            float[] buffer = Silence(seconds);
            int length = Mathf.Max(2, Mathf.RoundToInt(SampleRate / frequency));
            float[] line = new float[length];
            System.Random random = new System.Random(seed);
            float previous = 0f;

            float mean = 0f;

            for (int i = 0; i < length; i++)
            {
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                previous = Mathf.Lerp(previous, white, brightness);
                line[i] = previous;
                mean += previous / length;
            }

            // An offset in the burst would circulate as a slowly dying hum below the note.
            for (int i = 0; i < length; i++)
                line[i] -= mean;

            float loss = Mathf.Pow(0.001f, 1f / (sustain * frequency));

            for (int i = 0; i < buffer.Length; i++)
            {
                int index = i % length;
                buffer[i] = line[index];
                line[index] = (line[index] + line[(index + 1) % length]) * 0.5f * loss;
            }

            return buffer;
        }

        public static float[] Shape(this float[] buffer, Func<float, float> envelope)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] *= envelope(i / (float)SampleRate);

            return buffer;
        }

        /// Exponential decay after a short linear attack.
        public static float[] Decay(this float[] buffer, float rate, float attack = 0.002f)
        {
            return buffer.Shape(time => time < attack ? time / attack : Mathf.Exp(-(time - attack) * rate));
        }

        public static float[] LowPass(this float[] buffer, float cutoff, float q = 0.707f)
        {
            return Biquad(buffer, Filter.LowPass, _ => cutoff, q);
        }

        public static float[] HighPass(this float[] buffer, float cutoff, float q = 0.707f)
        {
            return Biquad(buffer, Filter.HighPass, _ => cutoff, q);
        }

        public static float[] BandPass(this float[] buffer, float frequency, float q)
        {
            return Biquad(buffer, Filter.BandPass, _ => frequency, q);
        }

        public static float[] BandPass(this float[] buffer, Func<float, float> frequency, float q)
        {
            return Biquad(buffer, Filter.BandPass, frequency, q);
        }

        public static float[] LowPass(this float[] buffer, Func<float, float> cutoff, float q = 0.707f)
        {
            return Biquad(buffer, Filter.LowPass, cutoff, q);
        }

        /// Mixes the source into the target from the given moment; whatever runs past the end is dropped, or wrapped around for loops.
        public static float[] Add(this float[] target, float[] source, float gain = 1f, float at = 0f, bool isWrapped = false)
        {
            int offset = Mathf.RoundToInt(at * SampleRate);

            for (int i = 0; i < source.Length; i++)
            {
                int index = offset + i;

                if (isWrapped)
                    index = (index % target.Length + target.Length) % target.Length;
                else if (index < 0 || index >= target.Length)
                    continue;

                target[index] += source[i] * gain;
            }

            return target;
        }

        public static float[] Drive(this float[] buffer, float amount)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (float)Math.Tanh(buffer[i] * amount);

            return buffer;
        }

        public static float[] Normalize(this float[] buffer, float peak = 0.85f)
        {
            float max = 1e-6f;

            foreach (float sample in buffer)
                max = Mathf.Max(max, Mathf.Abs(sample));

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] *= peak / max;

            return buffer;
        }

        public static float[] Fade(this float[] buffer, float fadeIn, float fadeOut)
        {
            float duration = buffer.Length / (float)SampleRate;

            return buffer.Shape(time => Mathf.Clamp01(fadeIn <= 0f ? 1f : time / fadeIn) * Mathf.Clamp01(fadeOut <= 0f ? 1f : (duration - time) / fadeOut));
        }

        /// Makes a seamless loop: the tail past the loop length is cross-faded over the head.
        public static float[] Loop(this float[] buffer, float seconds)
        {
            float[] loop = Silence(seconds);
            int overlap = buffer.Length - loop.Length;

            for (int i = 0; i < loop.Length; i++)
            {
                if (i >= overlap)
                {
                    loop[i] = buffer[i];

                    continue;
                }

                float alpha = i / (float)overlap;
                loop[i] = buffer[i] * Mathf.Sqrt(alpha) + buffer[loop.Length + i] * Mathf.Sqrt(1f - alpha);
            }

            return loop;
        }

        /// Stone room: four damped comb filters into two all-passes; the buffer grows by the tail.
        public static float[] Reverb(this float[] buffer, float decay, float mix, float damping = 0.35f, int spread = 0)
        {
            int[] combs = { 1557 + spread, 1617 - spread, 1491 + spread * 2, 1422 - spread };
            int[] allPasses = { 225 + spread, 556 - spread };
            float[] dry = buffer;
            float[] wet = new float[buffer.Length + Mathf.CeilToInt(decay * SampleRate)];

            foreach (int delay in combs)
            {
                float feedback = Mathf.Pow(10f, -3f * delay / (decay * SampleRate));
                float[] line = new float[delay];
                float filtered = 0f;

                for (int i = 0; i < wet.Length; i++)
                {
                    int index = i % delay;
                    float output = line[index];
                    filtered = Mathf.Lerp(output, filtered, damping);
                    line[index] = (i < dry.Length ? dry[i] : 0f) + filtered * feedback;
                    wet[i] += output * 0.25f;
                }
            }

            foreach (int delay in allPasses)
            {
                float[] line = new float[delay];

                for (int i = 0; i < wet.Length; i++)
                {
                    int index = i % delay;
                    float delayed = line[index];
                    float input = wet[i];
                    line[index] = input + delayed * 0.5f;
                    wet[i] = delayed - input * 0.5f;
                }
            }

            for (int i = 0; i < wet.Length; i++)
                wet[i] = wet[i] * mix + (i < dry.Length ? dry[i] * (1f - mix * 0.5f) : 0f);

            return wet;
        }

        public static void Write(string path, float[] left, float[] right = null, bool isLoop = false)
        {
            short channels = (short)(right == null ? 1 : 2);
            int count = left.Length;
            float release = 0.004f * SampleRate;

            using FileStream stream = new FileStream(path, FileMode.Create);
            using BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + count * 2 * channels);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2 * channels);
            writer.Write((short)(2 * channels));
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(count * 2 * channels);

            for (int i = 0; i < count; i++)
            {
                // One-shots end on silence; loops are written as they are (their ends already meet).
                float fade = isLoop ? 1f : Mathf.Clamp01((count - i) / release);
                writer.Write((short)(Mathf.Clamp(left[i] * fade, -1f, 1f) * short.MaxValue));

                if (right != null)
                    writer.Write((short)(Mathf.Clamp(right[i] * fade, -1f, 1f) * short.MaxValue));
            }
        }

        private static float[] Biquad(float[] buffer, Filter filter, Func<float, float> frequency, float q)
        {
            const int block = 32;
            float b0 = 0f, b1 = 0f, b2 = 0f, a1 = 0f, a2 = 0f;
            float x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (i % block == 0)
                {
                    float omega = 2f * Mathf.PI * Mathf.Clamp(frequency(i / (float)SampleRate), 20f, SampleRate * 0.45f) / SampleRate;
                    float cos = Mathf.Cos(omega);
                    float alpha = Mathf.Sin(omega) / (2f * q);
                    float a0 = 1f + alpha;

                    switch (filter)
                    {
                        case Filter.LowPass:
                            b0 = b2 = (1f - cos) * 0.5f / a0;
                            b1 = (1f - cos) / a0;
                            break;
                        case Filter.HighPass:
                            b0 = b2 = (1f + cos) * 0.5f / a0;
                            b1 = -(1f + cos) / a0;
                            break;
                        default:
                            b0 = alpha / a0;
                            b1 = 0f;
                            b2 = -alpha / a0;
                            break;
                    }

                    a1 = -2f * cos / a0;
                    a2 = (1f - alpha) / a0;
                }

                float x = buffer[i];
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1;
                x1 = x;
                y2 = y1;
                y1 = y;
                buffer[i] = y;
            }

            return buffer;
        }
    }
}
