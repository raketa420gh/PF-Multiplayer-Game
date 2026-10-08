using System;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Synthesized dungeon sounds, the cave ambience and the tavern theme as 16-bit WAV assets. No external samples:
    /// every sound is layered from struck, plucked, bowed and blown models of AudioSynth.
    internal static class DungeonAudioBuilder
    {
        public const string Folder = "Assets/Game/Audio/Dungeon";

        private const float Beat = 0.5f;
        private const float Bar = Beat * 4f;

        private static readonly string[] s_melody =
        {
            "D5 - F5 E5 D5 - A4 -", "D5 E5 F5 G5 A5 - F5 -", "G5 - E5 D5 C5 - E5 -", "G5 E5 C5 E5 D5 - - .",
            "D5 - F5 E5 D5 - A4 -", "C5 D5 E5 C5 A4 - C5 -", "E5 - G5 E5 D5 C5 B4 C5", "D5 - - - A4 - D5 .",
            "A5 - A5 G5 F5 - C5 -", "G5 - G5 F5 E5 - C5 -", "F5 E5 D5 E5 F5 - A5 -", "E5 - C5 - A4 - - .",
            "A5 - C6 A5 F5 - A5 -", "G5 - E5 G5 C6 - G5 -", "A5 G5 E5 C5 E5 - D5 C5", "D5 - - - - - . ."
        };

        /// Chord of every bar: bass root and fifth, then the triad for the strums.
        private static readonly string[] s_chords =
        {
            "D2 A2 D4 F4 A4", "D2 A2 D4 F4 A4", "C2 G2 C4 E4 G4", "C2 G2 C4 E4 G4", "D2 A2 D4 F4 A4", "A1 E2 C4 E4 A4", "C2 G2 C4 E4 G4", "D2 A2 D4 F4 A4",
            "F2 C3 C4 F4 A4", "C2 G2 C4 E4 G4", "D2 A2 D4 F4 A4", "A1 E2 C4 E4 A4", "F2 C3 C4 F4 A4", "C2 G2 C4 E4 G4", "A1 E2 C4 E4 A4", "D2 A2 D4 F4 A4"
        };

        private static readonly string[] s_flute = { "C5", "C5", "D5", "E5", "F5", "E5", "E5", "D5" };

        public static string Path(string name) => BattleAudioBuilder.Path(Folder, name, 0);

        public static AudioClip[] Load(string name) => BattleAudioBuilder.Load(name, Folder);

        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(Folder);

            for (int take = 0; take < 3; take++)
            {
                Write("Footstep", take, CreateFootstep(take, 0));
                Write("FootstepB", take, CreateFootstep(take, 1));
                Write("Rattle", take, CreateRattle(take));
                Write("Scream", take, CreateScream(take));
            }

            for (int take = 0; take < 2; take++)
            {
                Write("DoorCreak", take, CreateDoorCreak(take));
                Write("Growl", take, CreateGrowl(take));
            }

            Write("ChestOpen", 0, CreateChestOpen());
            Write("Drink", 0, CreateDrink());
            Write("Bandage", 0, CreateBandage());
            Write("PortalOpen", 0, CreatePortalOpen());
            Write("SwarmTick", 0, CreateSwarmTick());
            Write("LevelUp", 0, CreateLevelUp());
            Write("Click", 0, CreateClick());
            Write("Cast", 0, CreateCast());
            Write("Lever", 0, CreateLever());
            Write("Death", 0, CreateDeath());
            Write("Extract", 0, CreateExtract());
            Write("Screech", 0, CreateScreech());
            AudioSynth.Write(Path("Portal"), CreatePortal(), null, true);
            AudioSynth.Write(Path("Ambient"), CreateAmbient(0), CreateAmbient(1), true);
            AudioSynth.Write(Path("Menu"), CreateMenu(0), CreateMenu(1), true);

            AssetDatabase.Refresh();
            Debug.Log($"[{nameof(DungeonAudioBuilder)}] Audio built in {Folder}");
        }

        private static void Write(string name, int take, float[] sound)
        {
            AudioSynth.Write(BattleAudioBuilder.Path(Folder, name, take), sound);
        }

        private static float Note(string name)
        {
            int semitone = "C D EF G A B".IndexOf(name[0]);
            int midi = 12 * (name[^1] - '0' + 1) + semitone;

            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        /// Sparse crackle: the noise is let through in random grains.
        private static Func<float, float> Grains(int seed, float rate, float density)
        {
            float[] gates = AudioSynth.Noise(0.02f, seed);

            return time => gates[(int)(time * rate) % gates.Length] > 1f - density * 2f ? 1f : 0f;
        }

        /// Dry hinge or strained wood: friction grabs and slips at a wandering rate, each slip knocking the wood's own modes.
        private static float[] CreateCreak(float seconds, Func<float, float> rate, int seed)
        {
            float[] slips = AudioSynth.Silence(seconds);
            System.Random random = new System.Random(seed);
            float phase = 0f;

            for (int i = 0; i < slips.Length; i++)
            {
                phase += rate(i / (float)AudioSynth.SampleRate) * (0.7f + (float)random.NextDouble() * 0.6f) / AudioSynth.SampleRate;

                if (phase < 1f)
                    continue;

                phase -= 1f;
                slips[i] = 0.4f + (float)random.NextDouble() * 0.6f;
            }

            return AudioSynth.Ring(seconds, new[] { (310f, 45f, 1f), (545f, 60f, 0.8f), (870f, 75f, 0.6f), (1380f, 90f, 0.45f), (2150f, 120f, 0.25f) }, slips);
        }

        /// Boot on flagstones: the heel lands, the sole rolls and scuffs, grit crunches under it.
        private static float[] CreateFootstep(int take, int foot)
        {
            int seed = 200 + take * 10 + foot * 5;
            Func<float, float> grains = Grains(seed + 3, 260f, 0.22f);

            float[] heel = AudioSynth.Noise(0.12f, seed).LowPass(230f + take * 35f).Decay(55f);
            float[] weight = AudioSynth.Tone(0.12f, time => 58f + 32f * Mathf.Exp(-time * 50f), time => Mathf.Exp(-time * 60f));
            float[] scuff = AudioSynth.Noise(0.15f, seed + 1).BandPass(950f + take * 220f + foot * 170f, 1.1f).LowPass(3800f)
                .Shape(time => Mathf.Clamp01(time / 0.006f) * Mathf.Exp(-time * 42f));
            float[] grit = AudioSynth.Noise(0.17f, seed + 2).HighPass(4200f).Shape(time => grains(time) * Mathf.Exp(-time * 28f));

            return AudioSynth.Silence(0.22f).Add(heel, 1.2f).Add(weight, 0.8f).Add(scuff, 0.5f, 0.022f + take * 0.006f).Add(grit.LowPass(8000f), 0.05f, 0.03f).Normalize(0.8f);
        }

        private static float[] CreateDoorCreak(int take)
        {
            const float swing = 0.8f;

            float[] creak = CreateCreak(swing, time => 36f + take * 14f + 72f * Mathf.Pow(Mathf.Sin(time / swing * Mathf.PI), 2f), 300 + take)
                .Shape(time => Mathf.Sin(time / swing * Mathf.PI));
            float[] drag = AudioSynth.Brown(swing, 302 + take).LowPass(260f).Shape(time => Mathf.Sin(time / swing * Mathf.PI));
            float[] stop = AudioSynth.Strike(0.2f, 304 + take, (120f, 30f, 1f), (245f, 42f, 0.7f), (410f, 60f, 0.4f)).LowPass(900f);

            return AudioSynth.Silence(1f).Add(creak).Add(drag, 0.35f).Add(stop, 0.6f, 0.78f).Normalize(0.8f);
        }

        /// Latch, the lid coming off the box, the hinges, and the lid thrown back against its stop.
        private static float[] CreateChestOpen()
        {
            float[] latch = AudioSynth.Strike(0.15f, 310, (1900f, 60f, 1f), (3100f, 80f, 0.6f), (4700f, 110f, 0.4f));
            float[] lid = AudioSynth.Strike(0.3f, 311, (150f, 28f, 1f), (275f, 36f, 0.7f), (430f, 48f, 0.5f), (820f, 70f, 0.3f));
            float[] hinge = CreateCreak(0.4f, time => 55f + 60f * time / 0.4f, 312).Shape(time => Mathf.Sin(time / 0.4f * Mathf.PI));
            float[] stop = AudioSynth.Strike(0.2f, 313, (190f, 40f, 1f), (360f, 55f, 0.6f), (760f, 80f, 0.3f));

            return AudioSynth.Silence(0.8f).Add(latch, 0.45f).Add(lid, 0.9f, 0.04f).Add(hinge, 0.5f, 0.11f).Add(stop, 0.55f, 0.56f).Normalize(0.8f);
        }

        /// Three swallows: a bubble of air rising in pitch over the low glug of the throat.
        private static float[] CreateDrink()
        {
            float[] sound = AudioSynth.Silence(0.95f);

            for (int i = 0; i < 3; i++)
            {
                float pitch = 250f + i * 35f;
                float[] bubble = AudioSynth.Tone(0.14f, time => pitch + 520f * Mathf.Pow(time / 0.14f, 2f), time => Mathf.Pow(Mathf.Sin(time / 0.14f * Mathf.PI), 2f));
                float[] glug = AudioSynth.Tone(0.1f, time => 115f - 250f * time, time => Mathf.Exp(-time * 30f));
                float[] throat = AudioSynth.Noise(0.12f, 320 + i).LowPass(700f).Shape(time => Mathf.Sin(time / 0.12f * Mathf.PI));
                float at = 0.05f + i * 0.3f;
                sound.Add(bubble, 0.8f, at + 0.03f).Add(glug, 0.6f, at).Add(throat, 0.35f, at);
            }

            return sound.Normalize(0.75f);
        }

        /// Linen torn off and wound around: a rip, then soft rustling passes.
        private static float[] CreateBandage()
        {
            Func<float, float> fibres = Grains(331, 600f, 0.3f);

            float[] rip = AudioSynth.Noise(0.16f, 330).HighPass(2600f).Shape(time => fibres(time) * Mathf.Sin(time / 0.16f * Mathf.PI));
            float[] rustle = AudioSynth.Noise(0.85f, 332).BandPass(time => 2800f + 1500f * Mathf.Sin(time * 13f), 0.8f)
                .Shape(time => Mathf.Pow(Mathf.Abs(Mathf.Sin(time * Mathf.PI * 2.4f)), 3f) * (0.6f + 0.4f * fibres(time * 0.2f)));

            return rustle.Add(rip, 0.7f, 0.02f).LowPass(6500f).Normalize(0.7f);
        }

        /// Hum of an open portal; every partial fits the two-second loop a whole number of times.
        private static float[] CreatePortal()
        {
            const float loop = 2f;
            (float frequency, float gain, float wobble)[] partials = { (55f, 0.5f, 0.5f), (82.5f, 0.35f, 1f), (110.5f, 0.3f, 1.5f), (165f, 0.2f, 0.5f), (331f, 0.06f, 2f) };
            float[] sound = AudioSynth.Silence(loop);

            foreach ((float frequency, float gain, float wobble) in partials)
                sound.Add(AudioSynth.Tone(loop, _ => frequency, time => 0.7f + 0.3f * Mathf.Sin(time * 2f * Mathf.PI * wobble)), gain);

            float[] shimmer = AudioSynth.Noise(loop + 0.5f, 340).BandPass(4200f, 3f).Loop(loop).Shape(time => 0.6f + 0.4f * Mathf.Sin(time * 2f * Mathf.PI * 3f));

            return sound.Add(shimmer, 0.25f).Normalize(0.7f);
        }

        private static float[] CreatePortalOpen()
        {
            const float rise = 1.4f;

            float[] wind = AudioSynth.Noise(rise, 350).BandPass(time => 200f * Mathf.Pow(15f, time / rise), 2.5f).Shape(time => Mathf.Pow(time / rise, 1.5f));
            float[] tone = AudioSynth.Tone(rise, time => 80f * Mathf.Pow(8f, time / rise), time => time / rise, 0.5f);
            float[] bloom = AudioSynth.Strike(0.9f, 351, (660f, 5f, 1f), (1584f, 7f, 0.5f), (2574f, 9f, 0.3f));

            return AudioSynth.Silence(2.1f).Add(wind, 0.8f).Add(tone, 0.35f).Add(bloom, 0.6f, 1.2f).Reverb(0.9f, 0.35f).Normalize(0.8f);
        }

        /// The swarm closing in: a thick insect drone under the flutter of wings.
        private static float[] CreateSwarmTick()
        {
            const float duration = 0.4f;

            float[] drone = AudioSynth.Buzz(duration, time => 150f + 25f * Mathf.Sin(time * 90f)).BandPass(900f, 0.8f)
                .Shape(time => 0.5f + 0.5f * Mathf.Sin(time * 2f * Mathf.PI * 38f));
            float[] wings = AudioSynth.Noise(duration, 360).BandPass(2600f, 2f).Shape(time => Mathf.Abs(Mathf.Sin(time * 2f * Mathf.PI * 70f)));

            return drone.Add(wings, 0.7f).Shape(time => Mathf.Clamp01(time / 0.02f) * Mathf.Exp(-time * 9f)).Normalize(0.75f);
        }

        private static float[] CreateBell(float frequency, float seconds, int seed)
        {
            return AudioSynth.Ring(seconds, new[]
            {
                (frequency, 3.5f, 1f), (frequency * 2f, 5f, 0.45f), (frequency * 3.01f, 7f, 0.25f), (frequency * 4.2f, 10f, 0.15f), (frequency * 5.43f, 14f, 0.08f)
            }, AudioSynth.Noise(0.003f, seed).LowPass(3000f));
        }

        private static float[] CreateLevelUp()
        {
            string[] notes = { "C5", "E5", "G5", "C6" };
            float[] sound = AudioSynth.Silence(1.5f);

            for (int i = 0; i < notes.Length; i++)
                sound.Add(CreateBell(Note(notes[i]), 1f, 370 + i), 0.7f, i * 0.16f);

            return sound.Reverb(1f, 0.3f).Normalize(0.75f);
        }

        private static float[] CreateClick()
        {
            float[] tick = AudioSynth.Strike(0.07f, 380, (1250f, 160f, 1f), (2300f, 220f, 0.6f), (3900f, 300f, 0.3f));

            return tick.Add(AudioSynth.Noise(0.004f, 381).HighPass(3000f), 0.25f).Normalize(0.6f);
        }

        /// A throat: a low buzzing source with a rough flutter, shaped by the formants of a wide open maw.
        private static float[] CreateGrowl(int take)
        {
            const float duration = 1.1f;
            float pitch = 62f + take * 14f;

            float[] throat = AudioSynth.Buzz(duration, time => pitch + 16f * Mathf.Sin(time / duration * Mathf.PI) + 3f * Mathf.Sin(time * 43f))
                .Shape(time => 0.65f + 0.35f * Mathf.Sin(time * 2f * Mathf.PI * (27f + take * 4f)));
            float[] voice = throat.Formants((420f, 3f, 1f), (1050f, 4f, 0.6f), (2400f, 5f, 0.25f)).Add(((float[])throat.Clone()).LowPass(180f), 0.8f);
            float[] breath = AudioSynth.Noise(duration, 390 + take).BandPass(1400f, 0.7f);

            return voice.Normalize(1f).Add(breath, 0.12f).Shape(time => Mathf.Pow(Mathf.Sin(time / duration * Mathf.PI), 0.7f)).Drive(2.2f).Normalize(0.6f);
        }

        /// Dry bones knocking together: a run of small hard clacks, each bone with a pitch of its own.
        private static float[] CreateRattle(int take)
        {
            float[] sound = AudioSynth.Silence(0.55f);
            System.Random random = new System.Random(400 + take);
            float at = 0.01f;

            for (int i = 0; i < 7 + take; i++)
            {
                float pitch = 700f + (float)random.NextDouble() * 1200f;
                sound.Add(AudioSynth.Strike(0.08f, 401 + take * 20 + i, (pitch, 110f, 1f), (pitch * 2.3f, 150f, 0.5f), (pitch * 3.7f, 190f, 0.3f)),
                    0.5f + (float)random.NextDouble() * 0.5f, at);
                at += 0.03f + (float)random.NextDouble() * 0.07f;
            }

            return sound.Normalize(0.75f);
        }

        /// Gathering a spell: rising wind, a glassy chord that trembles, sparks.
        private static float[] CreateCast()
        {
            const float duration = 0.7f;
            float[] partials = { 880f, 1318f, 1778f, 2637f };
            System.Random random = new System.Random(410);

            float[] sound = AudioSynth.Noise(duration, 411).BandPass(time => 500f * Mathf.Pow(9f, time / duration), 3f).Shape(time => Mathf.Sin(time / duration * Mathf.PI));

            for (int i = 0; i < partials.Length; i++)
            {
                float frequency = partials[i];
                float tremble = 6f + i;
                sound.Add(AudioSynth.Tone(duration, time => frequency * (1f + 0.15f * time),
                    time => Mathf.Sin(time / duration * Mathf.PI) * (0.6f + 0.4f * Mathf.Sin(time * 2f * Mathf.PI * tremble))), 0.16f);
            }

            for (int i = 0; i < 6; i++)
            {
                float pitch = 3000f + (float)random.NextDouble() * 3000f;
                sound.Add(AudioSynth.Strike(0.1f, 412 + i, (pitch, 60f, 1f)), 0.12f, 0.1f + (float)random.NextDouble() * 0.5f);
            }

            return sound.Reverb(0.7f, 0.3f).Normalize(0.75f);
        }

        /// Iron lever thrown over: the clank, the ratchet catching, stone grinding somewhere behind the wall.
        private static float[] CreateLever()
        {
            float[] sound = AudioSynth.Silence(0.6f).Add(AudioSynth.Strike(0.5f, 420, (410f, 16f, 1f), (1130f, 24f, 0.7f), (1890f, 30f, 0.5f), (3100f, 45f, 0.3f)));

            for (int i = 0; i < 5; i++)
                sound.Add(AudioSynth.Strike(0.04f, 421 + i, (2300f, 200f, 1f), (3600f, 260f, 0.5f)), 0.35f, 0.08f + i * 0.055f);

            float[] grind = AudioSynth.Brown(0.35f, 427).LowPass(320f).Shape(time => Mathf.Sin(time / 0.35f * Mathf.PI));

            return sound.Add(grind, 0.8f, 0.05f).Normalize(0.8f);
        }

        /// A last groan, the body hitting the floor, gear clattering after it.
        private static float[] CreateDeath()
        {
            const float groan = 0.7f;

            float[] throat = AudioSynth.Buzz(groan, time => 150f - 70f * time / groan + 4f * Mathf.Sin(time * 37f));
            float[] voice = throat.Formants((600f, 3f, 1f), (1000f, 4f, 0.7f), (2500f, 5f, 0.2f)).Normalize(1f)
                .Add(AudioSynth.Noise(groan, 430).BandPass(1200f, 0.8f), 0.15f).Shape(time => Mathf.Pow(Mathf.Sin(time / groan * Mathf.PI), 0.6f));
            float[] fall = AudioSynth.Noise(0.3f, 431).LowPass(220f).Decay(18f);
            float[] weight = AudioSynth.Tone(0.3f, time => 45f + 30f * Mathf.Exp(-time * 20f), time => Mathf.Exp(-time * 16f));
            float[] sound = AudioSynth.Silence(1.6f).Add(voice, 0.6f).Add(fall, 1.2f, 0.62f).Add(weight, 0.9f, 0.62f);

            for (int i = 0; i < 3; i++)
                sound.Add(AudioSynth.Strike(0.2f, 432 + i, (1500f + i * 420f, 30f, 1f), (2900f + i * 300f, 45f, 0.5f)), 0.22f, 0.7f + i * 0.13f);

            return sound.Normalize(0.8f);
        }

        private static float[] CreateExtract()
        {
            string[] notes = { "G4", "B4", "D5", "G5" };
            float[] sound = AudioSynth.Silence(2.2f);

            for (int i = 0; i < notes.Length; i++)
                sound.Add(AudioSynth.Pluck(Note(notes[i]), 1.6f, 1.6f, 0.6f, 440 + i), 0.6f, i * 0.28f);

            sound.Add(CreateBell(Note("G5"), 1.2f, 445), 0.4f, 0.84f);
            sound.Add(AudioSynth.Tone(2.2f, _ => Note("G3"), time => Mathf.Sin(time / 2.2f * Mathf.PI) * 0.5f, 0.3f), 0.25f);

            return sound.Reverb(1.2f, 0.35f).Normalize(0.75f);
        }

        /// A shriek climbing in pitch, torn by a fast rasp.
        private static float[] CreateScreech()
        {
            const float duration = 0.75f;

            float[] throat = AudioSynth.Buzz(duration, time => Mathf.Lerp(650f, 1350f, time / duration) + 80f * Mathf.Sin(time * 2f * Mathf.PI * 31f))
                .Shape(time => 0.6f + 0.4f * Mathf.Sin(time * 2f * Mathf.PI * 173f));
            float[] voice = throat.Formants((1300f, 3f, 1f), (2900f, 4f, 0.7f), (4200f, 5f, 0.4f)).Normalize(1f)
                .Add(AudioSynth.Noise(duration, 450).BandPass(3500f, 1f), 0.2f);

            return voice.Shape(time => Mathf.Sin(Mathf.Sqrt(time / duration) * Mathf.PI)).Drive(3f).Normalize(0.6f);
        }

        /// A hoarse human wail through a crushed throat: an open "aah" that climbs, cracks and sinks, choked by the rope.
        private static float[] CreateScream(int take)
        {
            float duration = 1.5f + take * 0.35f;
            float peak = 430f + take * 70f;

            float[] throat = AudioSynth.Buzz(duration, time =>
                {
                    float t = time / duration;

                    return Mathf.Lerp(240f, peak, Mathf.Sin(Mathf.Min(1f, t * 1.6f) * Mathf.PI * 0.5f)) * (1f - 0.35f * t * t) + 25f * Mathf.Sin(time * 2f * Mathf.PI * (6f + take));
                })
                .Shape(time => 0.55f + 0.45f * Mathf.Sin(time * 2f * Mathf.PI * (37f + take * 9f)));
            float[] voice = throat.Formants((780f, 4f, 1f), (1180f, 5f, 0.8f), (2600f, 6f, 0.45f)).Normalize(1f)
                .Add(AudioSynth.Noise(duration, 460 + take).BandPass(1800f, 0.8f), 0.3f);

            return voice.Shape(time => Mathf.Pow(Mathf.Sin(Mathf.Min(1f, time / duration * 1.15f) * Mathf.PI), 0.4f)).Drive(2.6f).Reverb(0.9f, 0.25f).Normalize(0.75f);
        }

        /// Cave air for one ear: wind breathing through the passages, a whistling draught, drips and far-off rumbles
        /// placed across the stereo field. Loops without a seam.
        private static float[] CreateAmbient(int channel)
        {
            const float loop = 24f;
            const float overlap = 3f;
            float shift = channel * 1.7f;

            float[] wind = AudioSynth.Brown(loop + overlap, 500 + channel).BandPass(time => 260f + 140f * Mathf.Sin(time * 0.23f + shift), 1.2f)
                .Shape(time => 0.55f + 0.45f * Mathf.Sin(time * 0.17f + shift));
            float[] rumble = AudioSynth.Brown(loop + overlap, 502 + channel).LowPass(110f);
            float[] draught = AudioSynth.Noise(loop + overlap, 504 + channel).BandPass(time => 520f + 90f * Mathf.Sin(time * 0.31f + shift), 14f)
                .Shape(time => Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * 0.19f + 1f + shift)), 3f));
            float[] sound = wind.Add(rumble, 0.7f).Add(draught, 0.5f).Normalize(0.3f).Loop(loop);

            // The events are the same for both ears, only louder on their own side.
            System.Random random = new System.Random(510);

            for (int i = 0; i < 9; i++)
            {
                float pitch = 1000f + (float)random.NextDouble() * 700f;
                float pan = (float)random.NextDouble();
                float[] drip = AudioSynth.Tone(0.09f, time => pitch * (1f + 13f * time), time => Mathf.Exp(-time * 55f)).Reverb(1.4f, 0.5f);
                sound.Add(drip, 0.1f * (channel == 0 ? 1f - pan * 0.8f : 0.2f + pan * 0.8f), (float)random.NextDouble() * loop, true);
            }

            for (int i = 0; i < 2; i++)
            {
                float pan = (float)random.NextDouble();
                float[] thud = AudioSynth.Brown(1.6f, 520 + i).LowPass(85f).Shape(time => Mathf.Pow(Mathf.Sin(time / 1.6f * Mathf.PI), 2f)).Reverb(1.5f, 0.4f);
                sound.Add(thud, 0.3f * (channel == 0 ? 1f - pan * 0.6f : 0.4f + pan * 0.6f), 5f + i * 11f + (float)random.NextDouble() * 3f, true);
            }

            float[] chain = AudioSynth.Silence(0.8f);

            for (int i = 0; i < 6; i++)
                chain.Add(AudioSynth.Strike(0.15f, 530 + i, (2100f + i * 310f, 40f, 1f), (3900f + i * 270f, 55f, 0.5f)), 0.5f + (float)random.NextDouble() * 0.5f, i * 0.11f);

            return sound.Add(chain.Reverb(1.8f, 0.6f), channel == 0 ? 0.035f : 0.06f, 15.5f, true);
        }

        /// Tavern tune in D dorian for one ear: a plucked lute over bass and strummed chords, a frame drum and tambourine,
        /// and a flute holding long notes through the second half. Tails wrap around, so the loop has no seam.
        private static float[] CreateMenu(int channel)
        {
            float length = s_melody.Length * Bar;
            float[] dry = AudioSynth.Silence(length);
            bool isLeft = channel == 0;

            for (int bar = 0; bar < s_melody.Length; bar++)
            {
                float start = bar * Bar;
                string[] melody = s_melody[bar].Split(' ');
                string[] chord = s_chords[bar].Split(' ');

                for (int i = 0; i < melody.Length; i++)
                {
                    if (melody[i] is "-" or ".")
                        continue;

                    int held = 1;

                    while (i + held < melody.Length && melody[i + held] == "-")
                        held++;

                    dry.Add(AudioSynth.Pluck(Note(melody[i]), 0.5f + held * 0.4f, 0.9f + held * 0.3f, 0.4f, bar * 16 + i),
                        isLeft ? 0.34f : 0.24f, start + i * Beat * 0.5f, true);
                }

                // Bass on the first and third beat, the chord strummed on the second and fourth.
                for (int beat = 0; beat < 4; beat++)
                {
                    float at = start + beat * Beat;

                    if (beat % 2 == 0)
                    {
                        dry.Add(AudioSynth.Pluck(Note(chord[beat / 2]), 1.2f, 1.4f, 0.3f, 900 + bar * 4 + beat).LowPass(900f), 0.42f, at, true);
                        continue;
                    }

                    for (int i = 2; i < chord.Length; i++)
                        dry.Add(AudioSynth.Pluck(Note(chord[i]), 0.7f, 0.7f, 0.45f, 1200 + bar * 16 + beat * 4 + i), isLeft ? 0.09f : 0.14f, at + (i - 2) * 0.018f, true);
                }

                // Frame drum: deep on one, lighter on the "and" of three; the tambourine answers on two and four.
                foreach ((float beat, float gain) in new[] { (0f, 0.5f), (2.5f, 0.3f) })
                {
                    float[] skin = AudioSynth.Tone(0.3f, time => 58f + 45f * Mathf.Exp(-time * 35f), time => Mathf.Exp(-time * 14f));
                    dry.Add(skin.Add(AudioSynth.Noise(0.05f, 1500 + bar).LowPass(900f).Decay(90f), 0.4f), gain, start + beat * Beat, true);
                }

                for (int i = 0; i < 8; i++)
                {
                    float[] jingle = AudioSynth.Noise(0.12f, 1600 + bar * 8 + i).HighPass(6000f).Decay(i % 4 == 2 ? 28f : 70f, 0.001f);
                    dry.Add(jingle, (i % 4 == 2 ? 0.05f : 0.012f) * (isLeft ? 0.7f : 1f), start + i * Beat * 0.5f, true);
                }

                if (bar < s_melody.Length - s_flute.Length)
                    continue;

                float pitch = Note(s_flute[bar - (s_melody.Length - s_flute.Length)]);
                float[] flute = AudioSynth.Tone(Bar, time => pitch * (1f + 0.006f * Mathf.Sin(time * 2f * Mathf.PI * 5f) * Mathf.Clamp01(time)),
                    time => Mathf.Clamp01(time / 0.12f) * Mathf.Clamp01((Bar - time) / 0.25f), 0.25f);
                float[] breath = AudioSynth.Noise(Bar, 1700 + bar).BandPass(pitch * 2f, 8f).Fade(0.1f, 0.25f);
                dry.Add(flute.Add(breath, 0.5f), isLeft ? 0.075f : 0.05f, start, true);
            }

            // The room is rendered over two rounds of the tune; the second round carries the tail of the first.
            float[] twice = new float[dry.Length * 2];
            dry.CopyTo(twice, 0);
            dry.CopyTo(twice, dry.Length);
            float[] wet = twice.Reverb(1.1f, 0.22f, 0.4f, channel * 23);
            float[] sound = new float[dry.Length];
            Array.Copy(wet, dry.Length, sound, 0, dry.Length);

            return sound.Drive(1.2f).Normalize(0.7f);
        }
    }
}
