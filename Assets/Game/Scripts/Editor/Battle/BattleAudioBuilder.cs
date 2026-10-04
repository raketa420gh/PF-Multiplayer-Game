using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Synthesizes the combat sounds as 16-bit mono WAV assets, a few takes of each so repeated blows do not sound alike.
    internal static class BattleAudioBuilder
    {
        public const string Swing = "Swing";
        public const string Hit = "Hit";
        public const string Block = "Block";
        public const string Clank = "Clank";
        public const string Shot = "Shot";

        private const int Takes = 3;

        [MenuItem("Tools/Game/Battle/Build Audio")]
        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.AudioFolder);

            for (int take = 0; take < Takes; take++)
            {
                Write(Swing, take, CreateSwing(take));
                Write(Hit, take, CreateHit(take));
                Write(Block, take, CreateBlock(take));
                Write(Clank, take, CreateClank(take));
                Write(Shot, take, CreateShot(take));
            }

            AssetDatabase.Refresh();
        }

        /// All takes of a sound: "Name", "Name2", "Name3"...
        public static AudioClip[] Load(string name, string folder = BattleEditorUtility.AudioFolder)
        {
            List<AudioClip> clips = new();

            for (int take = 0; ; take++)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Path(folder, name, take));

                if (clip == null)
                    break;

                clips.Add(clip);
            }

            return clips.ToArray();
        }

        public static string Path(string folder, string name, int take)
        {
            return $"{folder}/{name}{(take == 0 ? string.Empty : (take + 1).ToString())}.wav";
        }

        private static void Write(string name, int take, float[] sound)
        {
            AudioSynth.Write(Path(BattleEditorUtility.AudioFolder, name, take), sound);
        }

        /// Blade through the air: a band of noise whose pitch rises and falls with the speed of the swing, over a low body of moved air.
        private static float[] CreateSwing(int take)
        {
            float duration = 0.3f + take * 0.04f;
            float peak = 1500f + take * 350f;
            Func<float, float> speed = time => Mathf.Pow(Mathf.Sin(Mathf.Clamp01(time / duration) * Mathf.PI), 2f);

            float[] air = AudioSynth.Noise(duration, 11 + take).BandPass(time => 350f + peak * speed(time), 1.4f).Shape(time => Mathf.Pow(speed(time), 1.3f));
            float[] hiss = AudioSynth.Noise(duration, 21 + take).HighPass(3500f).Shape(time => Mathf.Pow(speed(time), 2.5f));
            float[] body = AudioSynth.Noise(duration, 31 + take).BandPass(time => 160f + 220f * speed(time), 1.2f).Shape(speed);

            return air.LowPass(4200f).Add(hiss, 0.07f).Add(body, 0.8f).Normalize(0.8f);
        }

        /// Steel into a body: a dull thud, the wet give of flesh and the slap on cloth and leather.
        private static float[] CreateHit(int take)
        {
            const float duration = 0.32f;
            float flutter = 48f + take * 9f;

            float[] thud = AudioSynth.Tone(duration, time => 55f + 110f * Mathf.Exp(-time * 30f), time => Mathf.Exp(-time * 22f));
            float[] body = AudioSynth.Noise(duration, 41 + take).LowPass(380f + take * 60f).Decay(30f);
            float[] squelch = AudioSynth.Noise(duration, 51 + take).BandPass(time => 700f + 1600f * Mathf.Exp(-time * 16f), 2.2f)
                .Shape(time => Mathf.Exp(-time * 20f) * (0.55f + 0.45f * Mathf.Sin(time * 2f * Mathf.PI * flutter)));
            float[] slap = AudioSynth.Noise(0.05f, 61 + take).HighPass(2200f).Decay(110f, 0.0005f);

            return thud.Add(body, 1.3f).Add(squelch, 0.55f, 0.008f).Add(slap, 0.4f).Drive(1.6f).Normalize();
        }

        /// Blade on blade: the modes of a struck steel bar ring out, each dying at its own rate, after a click and a short scrape.
        private static float[] CreateBlock(int take)
        {
            float[] ratios = { 1f, 1.47f, 2.756f, 3.9f, 5.404f, 6.8f, 8.933f };
            float pitch = 640f + take * 130f;
            System.Random random = new System.Random(71 + take);
            (float, float, float)[] modes = new (float, float, float)[ratios.Length];

            for (int i = 0; i < ratios.Length; i++)
            {
                float detune = 1f + ((float)random.NextDouble() - 0.5f) * 0.03f;
                modes[i] = (pitch * ratios[i] * detune, 7f + ratios[i] * 2.2f + (float)random.NextDouble() * 4f, 1f / (1f + i * 0.45f));
            }

            float[] strike = AudioSynth.Noise(0.006f, 81 + take).HighPass(900f).Decay(600f, 0.0003f);
            float[] ring = AudioSynth.Ring(0.75f, modes, strike);
            float[] click = AudioSynth.Noise(0.012f, 91 + take).HighPass(4000f).Decay(350f, 0.0002f);
            float[] scrape = AudioSynth.Noise(0.12f, 95 + take).BandPass(2800f + take * 500f, 5f)
                .Shape(time => Mathf.Sin(Mathf.Clamp01(time / 0.12f) * Mathf.PI) * (0.5f + 0.5f * Mathf.Sin(time * 900f)));

            return ring.Add(click, 0.5f).Add(scrape, 0.2f, 0.01f).Normalize();
        }

        /// Steel on stone: a short hard tink, the knock of the wall and chips falling.
        private static float[] CreateClank(int take)
        {
            float pitch = 2100f + take * 300f;
            float[] gates = AudioSynth.Noise(0.01f, 125 + take);

            float[] strike = AudioSynth.Noise(0.004f, 121 + take).HighPass(1500f).Decay(700f, 0.0003f);
            float[] tink = AudioSynth.Ring(0.32f, new[] { (pitch, 38f, 1f), (pitch * 1.6f, 55f, 0.6f), (pitch * 2.47f, 70f, 0.35f), (880f, 45f, 0.5f) }, strike);
            float[] knock = AudioSynth.Noise(0.2f, 122 + take).LowPass(700f).Decay(42f);
            float[] chips = AudioSynth.Noise(0.3f, 123 + take).HighPass(2500f)
                .Shape(time => time > 0.03f && gates[(int)(time * 220f) % gates.Length] > 0.6f ? Mathf.Exp(-time * 14f) : 0f);

            return tink.Add(knock, 1.1f).Add(chips.LowPass(7000f), 0.12f).Normalize();
        }

        /// Bow release: the string snaps back and hums, the limbs thump, the arrow leaves with a hiss.
        private static float[] CreateShot(int take)
        {
            const float duration = 0.4f;

            float[] twang = AudioSynth.Pluck(92f + take * 14f, duration, 0.18f, 0.35f, 101 + take).LowPass(1400f);
            float[] overtone = AudioSynth.Pluck(184f + take * 28f, duration, 0.1f, 0.5f, 105 + take);
            float[] thump = AudioSynth.Tone(0.15f, time => 70f + 70f * Mathf.Exp(-time * 40f), time => Mathf.Exp(-time * 38f));
            float[] hiss = AudioSynth.Noise(duration, 111 + take).BandPass(time => 900f + 2200f * Mathf.Exp(-time * 9f), 1.3f)
                .Shape(time => Mathf.Sqrt(Mathf.Clamp01(time / 0.03f)) * Mathf.Exp(-time * 13f));

            return AudioSynth.Silence(duration).Add(twang, 0.9f).Add(overtone, 0.35f).Add(thump, 0.7f).Add(hiss.LowPass(5000f), 0.3f, 0.01f).Normalize(0.8f);
        }
    }
}
