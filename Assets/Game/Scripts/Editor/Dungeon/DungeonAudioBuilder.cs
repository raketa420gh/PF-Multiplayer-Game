using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Synthesized dungeon sounds and the tavern theme as 16-bit WAV assets. No external samples.
    internal static class DungeonAudioBuilder
    {
        public const string Folder = "Assets/Game/Audio/Dungeon";
        private const int SampleRate = 44100;

        public static string Path(string name) => $"{Folder}/{name}.wav";

        public static void Build()
        {
            Battle.BattleEditorUtility.EnsureFolder(Folder);

            Write("Footstep", 0.18f, Footstep);
            Write("FootstepB", 0.16f, FootstepB);
            Write("DoorCreak", 0.9f, DoorCreak);
            Write("ChestOpen", 0.7f, ChestOpen);
            Write("Drink", 0.9f, Drink);
            Write("Bandage", 0.8f, Bandage);
            Write("Portal", 2f, Portal);
            Write("PortalOpen", 1.4f, PortalOpen);
            Write("SwarmTick", 0.35f, SwarmTick);
            Write("LevelUp", 1.2f, LevelUp);
            Write("Click", 0.08f, Click);
            Write("Growl", 1.1f, Growl);
            Write("Rattle", 0.5f, Rattle);
            Write("Cast", 0.6f, Cast);
            Write("Lever", 0.5f, Lever);
            Write("Ambient", 12f, Ambient);
            Write("Menu", 32f, Menu);
            Write("Death", 1.6f, Death);
            Write("Extract", 2.2f, Extract);

            AssetDatabase.Refresh();
            Debug.Log($"[{nameof(DungeonAudioBuilder)}] Audio built in {Folder}");
        }

        private delegate float Synth(float time, float duration, float noise, ref float filter);

        private static float Envelope(float time, float attack, float decay)
        {
            return time < attack ? time / attack : Mathf.Exp(-(time - attack) * decay);
        }

        private static float Footstep(float time, float duration, float noise, ref float filter)
        {
            filter = Mathf.Lerp(filter, noise, 0.35f);

            return filter * Envelope(time, 0.004f, 28f) * 0.9f + Mathf.Sin(2f * Mathf.PI * 70f * time) * Mathf.Exp(-time * 40f) * 0.5f;
        }

        private static float FootstepB(float time, float duration, float noise, ref float filter)
        {
            filter = Mathf.Lerp(filter, noise, 0.5f);

            return filter * Envelope(time, 0.003f, 32f) * 0.8f + Mathf.Sin(2f * Mathf.PI * 90f * time) * Mathf.Exp(-time * 45f) * 0.4f;
        }

        private static float DoorCreak(float time, float duration, float noise, ref float filter)
        {
            float phase = time / duration;
            float frequency = 180f + Mathf.Sin(phase * 9f) * 40f + phase * 60f;
            float creak = Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.35f + Mathf.Sin(2f * Mathf.PI * frequency * 2.01f * time) * 0.2f;
            filter = Mathf.Lerp(filter, noise, 0.08f);

            return (creak + filter * 0.3f) * Mathf.Sin(phase * Mathf.PI) * 0.8f;
        }

        private static float ChestOpen(float time, float duration, float noise, ref float filter)
        {
            float knock = Mathf.Sin(2f * Mathf.PI * 120f * time) * Mathf.Exp(-time * 25f);
            filter = Mathf.Lerp(filter, noise, 0.1f);
            float creak = Mathf.Sin(2f * Mathf.PI * (240f + time * 90f) * time) * 0.25f * Mathf.Clamp01((time - 0.15f) * 4f) * Mathf.Exp(-(time - 0.15f) * 3f);

            return knock * 0.8f + filter * Mathf.Exp(-time * 18f) * 0.6f + creak;
        }

        private static float Drink(float time, float duration, float noise, ref float filter)
        {
            float gulp = Mathf.Repeat(time, 0.28f);
            float tone = Mathf.Sin(2f * Mathf.PI * (260f - gulp * 400f) * gulp) * Mathf.Exp(-gulp * 22f);
            filter = Mathf.Lerp(filter, noise, 0.15f);

            return tone * 0.6f + filter * Mathf.Exp(-gulp * 30f) * 0.25f;
        }

        private static float Bandage(float time, float duration, float noise, ref float filter)
        {
            filter = Mathf.Lerp(filter, noise, 0.06f);
            float rustle = Mathf.Abs(Mathf.Sin(time * 14f));

            return filter * rustle * 0.9f;
        }

        private static float Portal(float time, float duration, float noise, ref float filter)
        {
            float a = Mathf.Sin(2f * Mathf.PI * 110f * time) * 0.3f;
            float b = Mathf.Sin(2f * Mathf.PI * 165f * time + Mathf.Sin(time * 3f)) * 0.2f;
            float c = Mathf.Sin(2f * Mathf.PI * 220.5f * time) * 0.15f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * 880f * time) * 0.05f * (0.5f + 0.5f * Mathf.Sin(time * 7f));

            return (a + b + c + shimmer) * 0.9f;
        }

        private static float PortalOpen(float time, float duration, float noise, ref float filter)
        {
            float phase = time / duration;
            float frequency = Mathf.Lerp(90f, 520f, phase * phase);

            return (Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.5f + Mathf.Sin(2f * Mathf.PI * frequency * 1.5f * time) * 0.25f) * Mathf.Sin(phase * Mathf.PI);
        }

        private static float SwarmTick(float time, float duration, float noise, ref float filter)
        {
            filter = Mathf.Lerp(filter, noise, 0.02f);
            float buzz = Mathf.Sin(2f * Mathf.PI * 55f * time) * Mathf.Sin(2f * Mathf.PI * 310f * time);

            return (buzz * 0.6f + filter * 0.8f) * Envelope(time, 0.01f, 12f);
        }

        private static float LevelUp(float time, float duration, float noise, ref float filter)
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int index = Mathf.Min((int)(time / 0.22f), notes.Length - 1);
            float local = time - index * 0.22f;

            return Mathf.Sin(2f * Mathf.PI * notes[index] * time) * Mathf.Exp(-local * 6f) * 0.5f + Mathf.Sin(2f * Mathf.PI * notes[index] * 2f * time) * Mathf.Exp(-local * 9f) * 0.15f;
        }

        private static float Click(float time, float duration, float noise, ref float filter)
        {
            return Mathf.Sin(2f * Mathf.PI * 1800f * time) * Mathf.Exp(-time * 90f) * 0.5f + noise * Mathf.Exp(-time * 200f) * 0.3f;
        }

        private static float Growl(float time, float duration, float noise, ref float filter)
        {
            float phase = time / duration;
            float frequency = 70f + Mathf.Sin(phase * 5f) * 15f;
            filter = Mathf.Lerp(filter, noise, 0.25f);
            float voice = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * time)) * 0.25f + Mathf.Sin(2f * Mathf.PI * frequency * 3f * time) * 0.2f;

            return (voice + filter * 0.35f) * Mathf.Sin(phase * Mathf.PI) * 0.9f;
        }

        private static float Rattle(float time, float duration, float noise, ref float filter)
        {
            float tick = Mathf.Repeat(time * 23f, 1f) < 0.15f ? 1f : 0f;
            filter = Mathf.Lerp(filter, noise, 0.4f);

            return filter * tick * Mathf.Exp(-time * 3f) * 0.9f + Mathf.Sin(2f * Mathf.PI * 1400f * time) * tick * 0.15f;
        }

        private static float Cast(float time, float duration, float noise, ref float filter)
        {
            float phase = time / duration;
            float frequency = Mathf.Lerp(300f, 900f, phase);
            filter = Mathf.Lerp(filter, noise, 0.05f);

            return (Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.35f + filter * 0.4f) * Mathf.Sin(phase * Mathf.PI);
        }

        private static float Lever(float time, float duration, float noise, ref float filter)
        {
            float clank = Mathf.Sin(2f * Mathf.PI * 520f * time) * Mathf.Exp(-time * 14f) + Mathf.Sin(2f * Mathf.PI * 780f * time) * Mathf.Exp(-time * 20f) * 0.5f;
            filter = Mathf.Lerp(filter, noise, 0.2f);

            return clank * 0.5f + filter * Mathf.Exp(-time * 25f) * 0.4f;
        }

        private static float Ambient(float time, float duration, float noise, ref float filter)
        {
            filter = Mathf.Lerp(filter, noise, 0.012f);
            float drone = Mathf.Sin(2f * Mathf.PI * 48f * time) * 0.12f + Mathf.Sin(2f * Mathf.PI * 72f * time + Mathf.Sin(time * 0.3f)) * 0.08f;
            float wind = filter * (0.3f + 0.2f * Mathf.Sin(time * 0.37f));
            float drip = 0f;
            float dripTime = Mathf.Repeat(time + 1.3f, 3.7f);

            if (dripTime < 0.12f)
                drip = Mathf.Sin(2f * Mathf.PI * 2400f * dripTime) * Mathf.Exp(-dripTime * 60f) * 0.25f;

            float fade = Mathf.Min(1f, time * 2f, (duration - time) * 2f);

            return (drone + wind + drip) * fade * 0.6f;
        }

        private static readonly float[] s_scale = { 220f, 246.94f, 261.63f, 293.66f, 329.63f, 349.23f, 392f, 440f };
        private static readonly int[] s_melody = { 0, 2, 4, 7, 4, 2, 0, 3, 5, 7, 5, 3, 0, 2, 4, 2, 7, 5, 4, 2, 0, 0, 2, 4, 5, 4, 2, 1, 0, 1, 2, 0 };
        private static readonly int[] s_bass = { 0, 0, 3, 3, 5, 5, 4, 4 };

        private static float Pluck(float frequency, float local, float decay)
        {
            return (Mathf.Sin(2f * Mathf.PI * frequency * local) * 0.5f + Mathf.Sin(2f * Mathf.PI * frequency * 2f * local) * 0.25f +
                    Mathf.Sin(2f * Mathf.PI * frequency * 3f * local) * 0.1f) * Mathf.Exp(-local * decay);
        }

        private static float Menu(float time, float duration, float noise, ref float filter)
        {
            const float beat = 0.5f;
            int step = (int)(time / beat);
            float local = time - step * beat;
            float melody = Pluck(s_scale[s_melody[step % s_melody.Length]], local, 5f) * 0.35f;
            int bassStep = (int)(time / (beat * 4f));
            float bassLocal = time - bassStep * beat * 4f;
            float bass = Pluck(s_scale[s_bass[bassStep % s_bass.Length]] * 0.5f, bassLocal, 1.8f) * 0.3f;
            float pad = Mathf.Sin(2f * Mathf.PI * s_scale[s_bass[bassStep % s_bass.Length]] * time) * 0.06f;
            float fade = Mathf.Min(1f, time * 0.5f, (duration - time) * 0.5f);

            return (melody + bass + pad) * fade;
        }

        private static float Death(float time, float duration, float noise, ref float filter)
        {
            float phase = time / duration;
            float frequency = Mathf.Lerp(160f, 40f, phase);

            return (Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.5f + Mathf.Sin(2f * Mathf.PI * frequency * 1.5f * time) * 0.2f) * (1f - phase);
        }

        private static float Extract(float time, float duration, float noise, ref float filter)
        {
            float[] notes = { 392f, 493.88f, 587.33f, 783.99f };
            int index = Mathf.Min((int)(time / 0.4f), notes.Length - 1);
            float local = time - index * 0.4f;

            return Pluck(notes[index], local, 3f) * 0.5f + Mathf.Sin(2f * Mathf.PI * notes[0] * 0.5f * time) * 0.1f * (1f - time / duration);
        }

        private static void Write(string path, float duration, Synth synth)
        {
            string file = Path(path);
            int count = Mathf.CeilToInt(duration * SampleRate);
            System.Random random = new System.Random(path.GetHashCode());
            float filter = 0f;

            using FileStream stream = new FileStream(file, FileMode.Create);
            using BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + count * 2);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(count * 2);

            for (int i = 0; i < count; i++)
            {
                float time = i / (float)SampleRate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float fadeOut = Mathf.Clamp01((duration - time) * 60f);
                float sample = Mathf.Clamp(synth(time, duration, noise, ref filter) * fadeOut, -1f, 1f);
                writer.Write((short)(sample * short.MaxValue * 0.8f));
            }
        }
    }
}
