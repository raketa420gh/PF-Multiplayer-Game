using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// The Zweihander's motions, authored from footage (Docs/Tasks/Zweihander_Animations): taking it out, the rest pose, the
    /// three attacks of the series, the block up and down and the riposte; under the attacks the body turns, lunges and
    /// steps as the third-person footage has it. The review page shows these poses.
    internal static class ZweihanderMotions
    {
        public static SwordSpec Spec => s_spec;

        private const float Guard = 0.07f;
        private const float Tip = 1.43f;

        /// Clip frames (60 Hz), model space metres: +Z ahead, +X right. See SwordFootage.Parse for the words.
        private const string Keys = @"# Zweihander keys. Clip frames (60 Hz), model space metres: +Z ahead, +X right.
# motion <id> [after <id> <frame|end>] [thrust] [back] frames <n> [windup <f> active <f> peak <f>] [shown <f>] title <Name>
# key <frame> grip(x y z) blade(x y z) | aim <frame> grip(x y z) -> strike point on the crosshair | idle <frame>
# to <bow> <turn right> <lean right> | el/le <right/left elbow x y z> | ed <edge x y z> | lh <left hand off the hilt, x y z> | hard <w> | stop
# reach <share>: the right fist that share of the arm's full length from the shoulder (1 = the elbow all but straight), the grip only says which way
# body <frame> [h dx dy dz yaw [pitch]] [lf dx dz [yaw [heel]]] [rf dx dz [yaw [heel]]]: the hips off the stance (metres) and the pelvis
#   turned right / pitched forward (degrees), each foot off its stance, turned right, its heel raised. From the third-person footage;
#   the keys above stay as the eyes see them, the eyes ride with the body.
# Shoulders ~ (+-0.17, 1.46, 0.0), eye (0, 1.67, 0.115). The right fist is under the guard, the left one 22 cm behind it.

# Rest: the fists before the chin, the blade laid back over the right shoulder, its flat to the view.
motion idle thrust frames 1 title Idle
key 0  0.055 1.485 0.43   0.105 0.86 -0.495   to 0 22 0   ed -1 0.12 0  el 0.36 1.28 0.20  le -0.30 1.15 0.25
key 1  0.055 1.485 0.43   0.105 0.86 -0.495   to 0 22 0   ed -1 0.12 0  el 0.36 1.28 0.20  le -0.30 1.15 0.25

# Taking it out: the right hand goes over the right shoulder for the hilt, draws the sword forward, the left hand meets it.
motion equip thrust frames 54 shown 13 title Equip
key 0   0.26 1.10 0.22   0.20 0.90 0.30    to 0 0 0     lh -0.24 1.08 0.22
key 6   0.34 1.45 0.10   0.30 0.80 -0.50   to -2 12 0   lh -0.24 1.08 0.24
key 12  0.28 1.72 -0.08  0.10 0.50 -0.86   to -4 22 0   lh -0.24 1.10 0.26
key 18  0.20 1.75 0.33   0.25 0.55 -0.80   to -2 22 0   lh -0.20 1.15 0.30
key 22  0.19 1.66 0.36   0.35 0.65 -0.68   to 0 22 0    lh -0.10 1.22 0.36
key 26  0.12 1.60 0.38   0.50 0.75 -0.40   to 0 22 0    lh 0.02 1.34 0.44
key 31  0.08 1.52 0.41   0.40 0.85 -0.35   to 0 22 0
key 38  0.06 1.49 0.43   0.25 0.90 -0.36   to 0 22 0
idle 54

# Attack 1: the blade laid level back over the right shoulder, then a level cut round from the right to the left at the neck: the hilt
# goes out on a wide arc with the right arm long, the blade turns evenly through the crosshair and runs out round the left side.
motion attack1 frames 120 windup 44 active 15 peak 52 title Attack_1
idle 0
key 8   0.14 1.54 0.30   0.15 0.60 -0.80   to 0 36 0
key 18  0.24 1.62 0.08   0.15 0.05 -0.99   to 0 74 0
key 31  0.25 1.62 0.07   0.20 0.03 -0.98   to 0 84 0   stop
key 38  0.30 1.59 0.12   0.26 0.02 -0.96   to 0 78 0
key 40  0.37 1.58 0.16   0.40 0.02 -0.92   to 0 70 0
key 42  0.45 1.57 0.21   0.64 0.03 -0.77   to 1 56 0   reach 0.9
key 44  0.50 1.56 0.27   0.87 0.05 -0.50   to 2 42 0   reach 0.95
key 46  0.40 1.53 0.40   0.99 0.08 0.00    to 2 26 0   reach 0.96
key 48  0.26 1.51 0.50   0.86 0.11 0.50    to 3 12 0   reach 0.97
key 50  0.08 1.48 0.56   0.50 0.13 0.86    to 4 0 0    reach 0.96
aim 52 -0.10 1.45 0.56                     to 4 -10 0  reach 0.94
key 54 -0.16 1.45 0.55  -0.34 0.13 0.93    to 4 -22 0  reach 0.93
key 56 -0.24 1.45 0.50  -0.61 0.11 0.78    to 4 -34 0  reach 0.9
key 58 -0.30 1.45 0.43  -0.80 0.09 0.59    to 4 -44 0  reach 0.86
key 60 -0.33 1.44 0.36  -0.92 0.06 0.37    to 4 -50 0  reach 0.8
key 62 -0.34 1.44 0.31  -0.98 0.03 0.19    to 4 -54 0  reach 0.75
key 65 -0.35 1.42 0.26  -1.00 0.00 -0.05   to 4 -58 0  reach 0.7
key 69 -0.34 1.40 0.24  -0.94 -0.02 -0.34  to 4 -62 0
key 80 -0.30 1.34 0.24  -0.72 0.02 -0.69   to 3 -60 0
key 92 -0.27 1.31 0.26  -0.64 0.10 -0.76   to 3 -56 0  stop
key 103 -0.12 1.38 0.34 -0.35 0.75 -0.56   to 1 -22 0
key 112 0.03 1.47 0.42   0.05 0.88 -0.47   to 0 12 0
idle 120
body 14 h 0.00 -0.05 -0.02 68 0    lf 0 0.05 25    rf 0.03 -0.12 70
body 37 h 0.00 -0.06 -0.03 74 0
body 45 h 0.00 -0.08 0.06 30 18    rf 0.03 -0.12 40 15
body 53 h 0.00 -0.09 0.15 -15 24   lf 0 0.10 -5    rf 0.03 -0.12 10 35
body 61 h -0.02 -0.08 0.14 -40 6
body 85 h -0.02 -0.07 0.12 -46 5
body 100 h 0.00 -0.03 0.05 -15 2   rf 0 0 0 10

# Attack 2: from the first cut's end the blade goes up the left side and behind the head, then in one even run-up a level cut from the
# left to the right, the right arm opening to its full length at the crosshair and staying long as the blade runs out to the right.
motion attack2 after attack1 82 back frames 116 windup 46 active 14 peak 53 title Attack_2
idle 0
key 14 -0.20 1.52 0.20  -0.15 0.70 -0.70   to 2 -56 0
key 22 -0.22 1.60 0.14   0.50 0.30 -0.80   to 0 -62 0
key 30 -0.22 1.61 0.12   0.69 0.10 -0.72   to 0 -64 0  stop
key 33 -0.22 1.61 0.12   0.56 0.08 -0.83   to 0 -64 0
key 36 -0.22 1.60 0.12   0.37 0.06 -0.93   to 0 -62 0
key 39 -0.24 1.59 0.14   0.10 0.05 -0.99   to 0 -58 0
key 41 -0.28 1.58 0.18  -0.09 0.04 -1.00   to 0 -52 0
key 43 -0.34 1.56 0.24  -0.47 0.03 -0.88   to 1 -44 0  reach 0.72
key 45 -0.40 1.54 0.34  -0.83 0.03 -0.56   to 2 -34 0  reach 0.76
key 47 -0.30 1.52 0.43  -1.00 0.05 -0.09   to 2 -22 0  reach 0.82
key 49 -0.16 1.50 0.51  -0.90 0.08 0.44    to 3 -10 0  reach 0.88
key 51 -0.03 1.49 0.57  -0.53 0.11 0.84    to 4 0 0    reach 0.93
aim 53  0.10 1.48 0.55                     to 4 6 0    reach 0.96
key 55  0.25 1.47 0.44   0.34 0.13 0.93    to 4 20 0   reach 0.96
key 57  0.39 1.46 0.30   0.64 0.11 0.76    to 4 34 0   reach 0.95
key 59  0.50 1.46 0.20   0.86 0.08 0.50    to 4 46 0   reach 0.94
key 62  0.54 1.45 0.14   0.98 0.04 0.17    to 4 58 0   reach 0.92
key 65  0.54 1.45 0.12   1.00 0.02 -0.03   to 3 66 0   reach 0.88
key 70  0.50 1.44 0.10   0.96 0.00 -0.29   to 3 72 0   reach 0.82
key 76  0.46 1.43 0.10   0.86 0.02 -0.51   to 2 76 0   reach 0.76
key 84  0.40 1.42 0.14   0.74 0.10 -0.66   to 2 76 0
key 94  0.34 1.42 0.20   0.66 0.25 -0.71   to 1 70 0   stop
key 104 0.22 1.45 0.32   0.42 0.62 -0.66   to 0 48 0
idle 116
body 14 h -0.02 -0.07 0.12 -50 5
body 30 h -0.02 -0.07 0.12 -52 5
body 41 h 0.00 -0.08 0.13 -30 9
body 46 h 0.00 -0.08 0.15 -5 8
body 53 h 0.02 -0.09 0.17 20 9     rf 0.03 -0.14 30 25
body 61 h 0.03 -0.08 0.14 50 5     rf 0.03 -0.16 55 10
body 74 h 0.03 -0.07 0.10 62 2     rf 0.03 -0.16 65 0
body 92 h 0.02 -0.04 0.05 45 0

# Attack 3: the sword comes back level by the right ribs, its point ahead, and is driven up into the crosshair: fast at first, slowing
# to a stop with both arms long.
motion attack3 after attack2 104 thrust frames 116 windup 50 active 15 peak 58 title Attack_3
idle 0
key 12  0.22 1.30 0.26   0.00 0.70 0.70    to 3 50 0
key 22  0.28 1.28 0.06  -0.05 0.10 0.99    to 5 74 0
key 37  0.30 1.30 -0.06 -0.06 0.08 0.99    to 5 80 0   stop
key 45  0.30 1.31 -0.06 -0.06 0.08 0.99    to 5 80 0   stop
key 48  0.31 1.34 -0.02 -0.08 0.09 0.99    to 5 70 0
key 50  0.30 1.38 0.10  -0.09 0.10 0.99    to 6 54 0
key 52  0.26 1.43 0.33  -0.10 0.11 0.99    to 6 30 0
key 54  0.21 1.50 0.54  -0.10 0.12 0.99    to 6 6 0
key 56  0.15 1.55 0.66  -0.10 0.12 0.99    to 6 -14 0
aim 58  0.10 1.60 0.73                     to 6 -26 0  hard 1
aim 62  0.10 1.60 0.74                     to 6 -28 0  hard 1 stop
aim 66  0.11 1.60 0.71                     to 6 -24 0  hard 1
key 74  0.20 1.44 0.36  -0.08 0.08 1.00    to 3 20 0
key 84  0.20 1.42 0.34  -0.05 0.12 0.99    to 2 60 0   stop
key 96  0.14 1.44 0.40   0.00 0.60 0.80    to 1 50 0
key 108 0.08 1.47 0.43   0.08 0.90 -0.30   to 0 28 0
idle 116
body 12 h 0.02 -0.05 0.00 55 0     lf 0 0.05 20    rf 0.03 -0.12 60
body 37 h 0.02 -0.07 -0.03 66 0
body 46 h 0.02 -0.07 -0.03 66 0
body 50 h 0.01 -0.08 0.03 40 8     rf 0.03 -0.12 40 15
body 58 h 0.00 -0.09 0.18 -22 12   lf 0 0.12 -5    rf 0.03 -0.12 5 40
body 67 h 0.00 -0.09 0.17 -20 10
body 76 h 0.01 -0.07 0.08 25 5     rf 0.03 -0.12 40 10
body 86 h 0.02 -0.05 0.02 55 2     rf 0.03 -0.12 60 0
body 100 h 0.01 -0.03 0.00 40 0

# Block: the hilt goes up by the right temple, the point ahead and a little left and down across the view.
motion block thrust frames 14 title Block
idle 0
key 4   0.08 1.50 0.42  -0.25 0.95 -0.10   to 0 18 0
key 8   0.12 1.60 0.40  -0.75 0.50 0.35    to 0 12 0
key 11  0.15 1.73 0.38  -0.60 0.00 0.80    to 0 8 0
key 14  0.15 1.78 0.37  -0.25 -0.16 0.955  to 0 6 0    ed 0.2 0.95 0.2

motion blockLower after block end thrust frames 20 title Block_Lower
idle 0
key 6   0.16 1.66 0.39  -0.90 0.00 0.40    to 0 10 0
key 10  0.12 1.53 0.41  -0.80 0.50 0.20    to 0 15 0
key 14  0.08 1.49 0.42  -0.30 0.93 -0.10   to 0 19 0
idle 20

# Riposte: from the block the right hand takes the sword back over the head, the left hand lets go and comes back to the
# hilt, then a cut from the upper right down to the lower left with the arms long from the crosshair on.
motion riposte after block end frames 94 windup 24 active 7 peak 27 title Riposte
idle 0
key 6   0.14 1.82 0.26  -0.95 0.05 0.30    to -2 12 0   lh -0.14 1.52 0.38
key 12  0.10 1.84 0.18   0.10 0.10 -0.99   to -4 16 0   lh -0.02 1.70 0.34
key 16  0.08 1.84 0.20   0.15 0.50 -0.85   to -4 16 0
key 19  0.08 1.82 0.24   0.20 0.78 -0.59   to -3 14 0
key 22  0.06 1.74 0.36   0.26 0.96 0.10    to 0 8 0
key 24  0.02 1.60 0.50   0.30 0.83 0.47    to 4 2 0     reach 0.9
key 26 -0.10 1.48 0.55   0.24 0.50 0.83    to 8 -6 0    reach 0.97
aim 27 -0.18 1.40 0.56                     to 10 -12 0  reach 1
key 28 -0.22 1.31 0.52   0.03 -0.05 1.00   to 12 -18 0  reach 1
key 29 -0.28 1.21 0.45  -0.13 -0.28 0.95   to 13 -24 0  reach 1
key 30 -0.33 1.11 0.37  -0.28 -0.45 0.85   to 14 -28 0  reach 1
key 32 -0.36 1.02 0.30  -0.41 -0.71 0.58   to 15 -36 0  reach 0.98 tw 0.5
key 35 -0.33 0.99 0.25  -0.72 -0.62 0.30   to 15 -44 0  reach 0.98 tw 0.5
key 38 -0.31 0.99 0.24  -0.86 -0.50 0.10   to 15 -48 0 tw 0.5
key 42 -0.30 1.00 0.24  -0.88 -0.44 -0.16  to 14 -50 0 tw 0.5  stop
key 62 -0.30 1.00 0.24  -0.88 -0.42 -0.18  to 12 -48 0 tw 0.5  stop
key 74 -0.14 1.20 0.32  -0.60 0.60 -0.40   to 6 -22 0
key 86  0.02 1.42 0.42  -0.05 0.90 -0.42   to 1 10 0
idle 94
body 6  h 0 -0.01 -0.02 12 0
body 16 h 0 -0.02 -0.03 22 0       rf 0.02 -0.04 25
body 24 h 0 -0.07 0.08 5 12        rf 0.02 -0.06 15 20
body 28 h 0 -0.09 0.15 -15 13      lf 0 0.10 -5    rf 0.02 -0.06 5 35
body 35 h -0.02 -0.09 0.15 -40 7
body 62 h -0.02 -0.08 0.13 -46 5
body 76 h 0 -0.04 0.06 -20 3       rf 0 0 0 10";

        private static readonly Color s_steel = new Color(0.62f, 0.64f, 0.68f);
        private static readonly Color s_iron = new Color(0.3f, 0.3f, 0.32f);
        private static readonly Color s_leather = new Color(0.2f, 0.13f, 0.09f);

        /// 1.8 m from the pommel to the point, 1.36 m of it the blade; the parrying lugs end the ricasso.
        private static readonly SwordSpec s_spec = new SwordSpec
        {
            Prefix = "Zweihander", Base = "ArmingSword", Strike = 1.15f, BladeBase = Guard + 0.02f, BladeTip = Tip, OffHand = 0.22f, MaxLean = 42f, Keys = Keys, Pace = Dungeon.DungeonWeaponLibrary.FootagePace, Model = "Zweihander",
            Mock = new[]
            {
                (new Vector3(0f, 0f, -0.13f), new Vector3(0.03f, 0.034f, 0.4f), s_leather),
                (new Vector3(0f, 0f, -0.35f), new Vector3(0.05f, 0.05f, 0.05f), s_iron),
                (new Vector3(0f, 0f, Guard), new Vector3(0.014f, 0.42f, 0.014f), s_iron),
                (new Vector3(0f, 0f, Guard + 0.005f), new Vector3(0.1f, 0.04f, 0.01f), s_iron),
                (new Vector3(0f, 0f, Guard + 0.085f), new Vector3(0.008f, 0.032f, 0.17f), s_steel),
                (new Vector3(0f, 0f, Guard + 0.17f), new Vector3(0.008f, 0.15f, 0.025f), s_steel),
                (new Vector3(0f, 0f, (Guard + 0.17f + Tip - 0.06f) * 0.5f), new Vector3(0.006f, 0.044f, Tip - 0.06f - Guard - 0.17f), s_steel),
                (new Vector3(0f, 0f, Tip - 0.03f), new Vector3(0.005f, 0.026f, 0.06f), s_steel)
            }
        };

        /// The motions as keyed: the weapon's definition takes its timings and rough poses from them.
        public static SwordMotion[] Read() => SwordFootage.Read(s_spec);

        /// Writes the weapon's game clips from the keys (see SwordFootage.Bake).
        public static string Bake() => SwordFootage.Bake(s_spec, false);

        /// Solves the motions (keys: a text file while tuning, empty = the keys in the code) and writes data.js of the review page.
        public static string Build(string keys, string folder) => SwordFootage.Build(s_spec, keys, folder);

        /// Solves the motions and renders frames of them, see SwordFootage.Run.
        public static string Run(string keys, string folder, string shots, string views, int size) => SwordFootage.Run(s_spec, keys, folder, shots, views, size);
    }
}
