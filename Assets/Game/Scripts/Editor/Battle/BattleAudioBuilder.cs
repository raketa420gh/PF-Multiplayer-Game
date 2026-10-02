using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Synthesizes placeholder combat sounds as 16-bit mono WAV assets.
    internal static class BattleAudioBuilder
    {
        public const string SwingPath = BattleEditorUtility.AudioFolder + "/Swing.wav";
        public const string HitPath = BattleEditorUtility.AudioFolder + "/Hit.wav";
        public const string BlockPath = BattleEditorUtility.AudioFolder + "/Block.wav";
        public const string ShotPath = BattleEditorUtility.AudioFolder + "/Shot.wav";

        private const int SampleRate = 44100;

        [MenuItem("Tools/Game/Battle/Build Audio")]
        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.AudioFolder);

            Write(SwingPath, 0.28f, Swing);
            Write(HitPath, 0.22f, Hit);
            Write(BlockPath, 0.4f, Block);
            Write(ShotPath, 0.25f, Shot);

            AssetDatabase.Refresh();
        }

        private static float Swing(float time, float duration, float noise, ref float filter)
        {
            float phase = time / duration;
            float envelope = Mathf.Pow(Mathf.Sin(phase * Mathf.PI), 2f);
            filter = Mathf.Lerp(filter, noise, Mathf.Lerp(0.04f, 0.25f, envelope));

            return filter * envelope * 1.6f;
        }

        private static float Hit(float time, float duration, float noise, ref float filter)
        {
            float thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(110f, 50f, time / duration) * time) * Mathf.Exp(-time * 22f);
            filter = Mathf.Lerp(filter, noise, 0.12f);

            return thump * 0.9f + filter * Mathf.Exp(-time * 45f) * 0.9f;
        }

        private static float Block(float time, float duration, float noise, ref float filter)
        {
            float ring = Mathf.Sin(2f * Mathf.PI * 830f * time) * 0.35f +
                         Mathf.Sin(2f * Mathf.PI * 1370f * time) * 0.3f +
                         Mathf.Sin(2f * Mathf.PI * 2230f * time) * 0.2f +
                         Mathf.Sin(2f * Mathf.PI * 3190f * time) * 0.12f;

            return ring * Mathf.Exp(-time * 11f) + noise * Mathf.Exp(-time * 90f) * 0.5f;
        }

        private static float Shot(float time, float duration, float noise, ref float filter)
        {
            float twang = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(240f, 150f, time / duration) * time) * Mathf.Exp(-time * 18f);
            filter = Mathf.Lerp(filter, noise, 0.2f);

            return twang * 0.7f + filter * Mathf.Exp(-time * 25f) * 0.5f;
        }

        private delegate float Synth(float time, float duration, float noise, ref float filter);

        private static void Write(string path, float duration, Synth synth)
        {
            int count = Mathf.CeilToInt(duration * SampleRate);
            System.Random random = new System.Random(path.GetHashCode());
            float filter = 0f;

            using FileStream stream = new FileStream(path, FileMode.Create);
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
