using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// The Flanged Mace's motions, authored from footage (D:\GameDevelopment\flanged_mace_ref): the rest pose, the three attacks
    /// of the series and the riposte; taking it out and the block up and down are not in the footage and are made to its rest
    /// and block poses. The right hand swings it alone; the left one hangs free.
    internal static class FlangedMaceMotions
    {
        public static SwordSpec Spec => s_spec;

        /// The cut butt behind the fist, the flanges and the finial's end ahead of it.
        public const float Back = -0.13f;
        public const float HeadFrom = 0.57f;
        public const float HeadTo = 0.71f;
        public const float Tip = 0.74f;

        /// Footage frames (60 Hz), model space metres: +Z ahead, +X right. See SwordFootage.Parse for the words.
        private const string Keys = @"# Flanged mace keys. Footage frames (60 Hz), model space metres: +Z ahead, +X right.
# motion <id> [after <id> <frame|end>] [thrust] frames <n> [windup <f> active <f> peak <f>] [shown <f>] title <Name>
# key <frame> grip(x y z) mace(x y z) | aim <frame> grip(x y z) -> strike point on the crosshair | idle <frame>
# to <bow> <turn right> <lean right> | el <right elbow x y z> | lh <left hand, off the mace, x y z> | hard <w> | stop
# reach <share>: the fist that share of the arm's full length from the shoulder (1 = the elbow all but straight), the grip only says which way
# tw <-1..1>: the arm's turn about itself
# Shoulders ~ (+-0.17, 1.46, 0.0), eye (0, 1.67, 0.115). The right fist holds the haft 13 cm from its butt; every motion is a 'thrust': a round
# head has no edge to lead. The footage's fighter leans and pushes the hips into every blow (head 13-15 cm ahead); here the body stands,
# so the keys are what the eyes see and the arm's length is given by 'reach'.

# Rest: the mace upright before the right shoulder, its head by the top right of the view; the left arm hangs.
motion idle thrust frames 1 title Idle
key 0  0.25 1.25 0.50  -0.04 0.98 0.18    to 0 8 0   lh -0.26 0.94 0.08
key 1  0.25 1.25 0.50  -0.04 0.98 0.18    to 0 8 0   lh -0.26 0.94 0.08

# Taking it out: from the right hip the head comes up ahead and over to the rest.
motion equip thrust frames 28 shown 6 title Equip
key 0   0.32 0.98 0.18   0.15 -0.75 0.64   to 0 4 0   lh -0.26 0.94 0.08
key 7   0.34 1.06 0.32   0.05 0.10 0.99    to 0 6 0   lh -0.26 0.94 0.08
key 13  0.34 1.20 0.44  -0.30 0.70 0.65    to 0 8 0   lh -0.26 0.94 0.08
key 20  0.28 1.24 0.49  -0.12 0.94 0.32    to 0 8 0   lh -0.26 0.94 0.08
idle 28

# Attack 1: the fist goes up by the right temple and the mace tips over to the left, level above the head; it goes round behind the
# head and comes up at the back, then straight down the middle with the arm opening; it stops dead level with the chest, the arm
# long, rebounds point up and goes back to the rest in an arc.
motion attack1 thrust frames 100 windup 43 active 9 peak 48 title Attack_1
idle 0
key 8   0.45 1.61 0.16  -0.74 0.67 0.03   to 0 14 0   lh -0.26 0.95 0.08
key 16  0.38 1.75 0.06  -0.95 0.22 0.21   to 0 18 0   lh -0.26 0.96 0.08
key 24  0.22 1.84 0.05  -0.87 -0.09 -0.49 to -2 16 0  lh -0.26 0.97 0.08
key 32  0.18 1.90 0.13  -0.30 -0.26 -0.92 to -3 10 0  lh -0.26 0.98 0.08
key 38  0.16 1.89 0.30   0.04 0.13 -0.99  to -2 2 0   lh -0.27 0.98 0.06
key 41  0.11 1.85 0.44   0.14 0.62 -0.77  to 0 -8 0   lh -0.28 0.98 0.02  reach 0.84
key 43  0.06 1.80 0.55   0.21 0.93 -0.30  to 3 -16 0  lh -0.29 0.98 -0.03 reach 0.88
key 45  0.00 1.68 0.64   0.16 0.93 0.34   to 6 -24 0  lh -0.30 0.98 -0.08 reach 0.91
key 47 -0.04 1.52 0.69   0.09 0.59 0.81   to 9 -30 0  lh -0.30 0.98 -0.12 reach 0.94
aim 48 -0.06 1.44 0.70                    to 10 -33 0 lh -0.30 0.98 -0.13 reach 0.96
key 50 -0.10 1.35 0.69   0.03 0.14 0.99   to 12 -38 0 lh -0.30 0.98 -0.14 reach 0.98
key 53 -0.10 1.33 0.68   0.03 0.12 0.99   to 12 -40 0 lh -0.30 0.98 -0.14 reach 0.98 stop
key 57 -0.06 1.36 0.67   0.02 0.30 0.95   to 11 -38 0 lh -0.30 0.98 -0.12 reach 0.95
key 62  0.02 1.42 0.63   0.00 0.56 0.83   to 9 -32 0  lh -0.29 0.97 -0.08
key 72  0.12 1.49 0.58  -0.01 0.88 0.47   to 5 -18 0  lh -0.28 0.96 0.00
key 84  0.20 1.36 0.53  -0.05 0.96 0.28   to 2 -4 0   lh -0.27 0.95 0.05
idle 100

# Attack 2: from the first blow's rebound the mace stands up, falls over to the right and goes down behind the right shoulder, straight
# back and up behind; down the middle again, lower than the first, the arm at its longest.
motion attack2 after attack1 67 thrust frames 100 windup 42 active 10 peak 47 title Attack_2
idle 0
key 4   0.06 1.47 0.50   0.14 0.82 0.56   to 6 -26 0  lh -0.29 0.97 -0.06
key 12  0.12 1.74 0.32   0.70 0.70 -0.10  to 2 -14 0  lh -0.28 0.97 0.00
key 20  0.15 1.82 0.14   0.92 0.09 -0.39  to 0 -4 0   lh -0.27 0.97 0.04
key 28  0.08 1.87 0.12   0.56 -0.41 -0.72 to -2 -6 0  lh -0.27 0.98 0.05 el 0.42 1.62 0.06
key 36  0.05 1.88 0.24   0.13 -0.13 -0.98 to -2 -12 0 lh -0.27 0.98 0.04 el 0.40 1.64 0.10
key 40  0.04 1.85 0.36  -0.06 0.31 -0.95  to 0 -18 0  lh -0.28 0.98 0.00  reach 0.8
key 42  0.04 1.78 0.50  -0.12 0.72 -0.68  to 3 -24 0  lh -0.29 0.98 -0.05 reach 0.85
key 44  0.04 1.65 0.60  -0.10 0.99 -0.08  to 6 -30 0  lh -0.30 0.98 -0.10 reach 0.9
key 46  0.03 1.48 0.67  -0.06 0.74 0.67   to 9 -35 0  lh -0.30 0.98 -0.13 reach 0.95
aim 47  0.03 1.40 0.69                    to 10 -37 0 lh -0.30 0.98 -0.14 reach 0.97
key 49  0.03 1.29 0.68  -0.02 0.02 1.00   to 12 -40 0 lh -0.30 0.98 -0.15 reach 0.99
key 53  0.03 1.29 0.68  -0.02 0.03 1.00   to 12 -40 0 lh -0.30 0.98 -0.15 reach 0.99 stop
key 58  0.03 1.34 0.67  -0.06 0.14 0.99   to 11 -39 0 lh -0.30 0.98 -0.13 reach 0.97
key 64  0.02 1.44 0.64  -0.22 0.30 0.93   to 9 -36 0  lh -0.30 0.98 -0.10
key 71  0.02 1.54 0.58  -0.40 0.41 0.82   to 7 -32 0  lh -0.29 0.97 -0.06
key 82  0.12 1.50 0.52  -0.30 0.85 0.43   to 4 -18 0  lh -0.28 0.96 0.00
key 92  0.22 1.32 0.51  -0.08 0.97 0.22   to 1 0 0    lh -0.27 0.95 0.06
idle 100

# Attack 3: from the second blow's rebound the fist goes to the left temple, the elbow ahead, and the head drops behind the left
# shoulder; it comes up behind on the left and down a steep slant from the upper left to the middle.
motion attack3 after attack2 71 thrust frames 112 windup 45 active 10 peak 50 title Attack_3
idle 0
key 8  -0.04 1.72 0.38  -0.98 -0.04 0.19  to 4 -34 0  lh -0.29 0.97 -0.04
key 16 -0.16 1.76 0.28  -0.87 -0.39 -0.29 to 2 -38 0  lh -0.29 0.97 -0.02 el 0.12 1.50 0.40
key 24 -0.20 1.85 0.23  -0.36 -0.58 -0.73 to 0 -40 0  lh -0.29 0.97 0.00 el 0.10 1.52 0.40
key 32 -0.21 1.90 0.23  -0.02 -0.56 -0.83 to 0 -40 0  lh -0.29 0.97 0.00 el 0.10 1.54 0.40
key 40 -0.20 1.90 0.32  -0.15 0.11 -0.98  to 0 -40 0  lh -0.29 0.97 -0.02
key 43 -0.17 1.83 0.42  -0.48 0.58 -0.66  to 2 -41 0  lh -0.30 0.98 -0.06 reach 0.84 tw 0.2
key 45 -0.16 1.77 0.53  -0.60 0.77 -0.22  to 4 -42 0  lh -0.30 0.98 -0.09 reach 0.9 tw 0.35
key 47 -0.07 1.69 0.61  -0.55 0.79 0.28   to 7 -44 0  lh -0.30 0.98 -0.12 reach 0.92 tw 0.5
key 49  0.04 1.55 0.64  -0.30 0.53 0.80   to 10 -47 0 lh -0.30 0.98 -0.14 reach 0.93 tw 0.58
aim 50  0.07 1.45 0.64                    to 11 -48 0 lh -0.30 0.98 -0.15 reach 0.94 tw 0.62
key 52  0.11 1.30 0.62  -0.08 0.12 0.99   to 13 -50 0 lh -0.30 0.98 -0.16 reach 0.95 tw 0.65
key 56  0.11 1.30 0.62  -0.08 0.13 0.99   to 13 -50 0 lh -0.30 0.98 -0.16 reach 0.95 tw 0.65 stop
key 62  0.07 1.36 0.61  -0.08 0.26 0.96   to 11 -46 0 lh -0.30 0.98 -0.13 tw 0.6
key 74  0.10 1.48 0.57  -0.05 0.70 0.71   to 7 -30 0  lh -0.29 0.97 -0.06
key 88  0.20 1.44 0.52  -0.10 0.95 0.28   to 3 -12 0  lh -0.28 0.96 0.02
key 100 0.23 1.30 0.51  -0.06 0.98 0.20   to 1 0 0    lh -0.27 0.95 0.06
idle 112

# Block: the fist by the right temple, the forearm across the brow, the mace across before the face with its head ahead to the left and down.
motion block thrust frames 12 title Block
idle 0
key 4   0.30 1.46 0.36  -0.45 0.80 0.40   to 0 8 0    lh -0.26 0.95 0.08
key 8   0.28 1.60 0.22  -0.72 0.30 0.62   to 0 10 0   lh -0.26 0.96 0.08
key 12  0.25 1.68 0.12  -0.77 -0.33 0.55  to 0 11 0   lh -0.26 0.97 0.08

motion blockLower after block end thrust frames 12 title Block_Lower
idle 0
key 4   0.28 1.60 0.22  -0.72 0.30 0.62   to 0 10 0   lh -0.26 0.96 0.08
key 8   0.30 1.46 0.36  -0.45 0.80 0.40   to 0 8 0    lh -0.26 0.95 0.08
idle 12

# Riposte: from the block the head drops down to the left hip and goes round behind the left side while the fist crosses the brow
# to the left; up behind the left shoulder and down the slant of the third blow, steeper and faster.
motion riposte after block end thrust frames 87 windup 27 active 9 peak 33 title Riposte
idle 0
key 4   0.16 1.64 0.10  -0.74 -0.54 0.40  to 0 6 0    lh -0.26 0.97 0.08
key 8   0.03 1.65 0.10  -0.69 -0.70 -0.18 to 0 0 0    lh -0.26 0.97 0.07
key 16 -0.06 1.82 0.16  -0.78 -0.26 -0.57 to -2 -8 0  lh -0.27 0.97 0.05
key 22 -0.08 1.88 0.28  -0.71 0.33 -0.62  to -2 -16 0 lh -0.28 0.98 0.02
key 25 -0.12 1.87 0.40  -0.53 0.58 -0.62  to 0 -22 0  lh -0.29 0.98 -0.03 reach 0.84
key 27 -0.16 1.84 0.50  -0.48 0.72 -0.51  to 2 -26 0  lh -0.29 0.98 -0.06 reach 0.9
key 29 -0.11 1.77 0.60  -0.43 0.90 -0.09  to 5 -32 0  lh -0.30 0.98 -0.10 reach 0.93
key 31 -0.04 1.60 0.68  -0.31 0.74 0.60   to 8 -37 0  lh -0.30 0.98 -0.13 reach 0.96
aim 33  0.05 1.40 0.68                    to 11 -40 0 lh -0.30 0.98 -0.15 reach 0.97
key 34  0.07 1.33 0.67  -0.13 0.32 0.94   to 12 -41 0 lh -0.30 0.98 -0.16 reach 0.97
key 36  0.09 1.29 0.67  -0.10 0.22 0.97   to 12 -41 0 lh -0.30 0.98 -0.16 reach 0.97
key 40  0.09 1.29 0.67  -0.10 0.21 0.97   to 12 -41 0 lh -0.30 0.98 -0.16 reach 0.97 stop
key 44  0.08 1.34 0.65  -0.08 0.34 0.94   to 10 -38 0 lh -0.30 0.98 -0.13
key 56  0.12 1.48 0.58  -0.04 0.80 0.60   to 6 -22 0  lh -0.29 0.97 -0.05
key 70  0.20 1.34 0.52  -0.06 0.96 0.26   to 2 -4 0   lh -0.27 0.95 0.05
idle 87";

        private static readonly Color s_iron = new Color(0.3f, 0.3f, 0.32f);
        private static readonly Color s_wood = new Color(0.3f, 0.2f, 0.14f);

        /// 0.87 m from the butt to the finial, 74 cm of it ahead of the fist; the head is 14 cm long and 13 cm across at its widest,
        /// 66 cm from the fist. At full reach the haft lies over in the palm, in line with the forearm, and in a swing the fist is free
        /// on the round haft: it lies over early, slides a little and rolls round it. The left hand is never on the haft: OffHand only
        /// lets the left arm be keyed.
        private static readonly SwordSpec s_spec = new SwordSpec
        {
            Prefix = "FlangedMace", Base = "ArmingSword", Strike = 0.66f, BladeBase = 0.12f, BladeTip = Tip, OffHand = 0.14f, MaxLean = 55f, LeanEase = 0.6f, MaxSlide = 0.05f, Keys = Keys,
            Model = "FlangedMace", Pace = Dungeon.DungeonWeaponLibrary.FootagePace,
            Mock = new[]
            {
                (new Vector3(0f, 0f, (Back + HeadFrom) * 0.5f), new Vector3(0.036f, 0.036f, HeadFrom - Back), s_wood),
                (new Vector3(0f, 0f, (HeadFrom + HeadTo) * 0.5f), new Vector3(0.12f, 0.12f, HeadTo - HeadFrom), s_iron)
            }
        };

        /// The motions as keyed: the weapon's definition takes its timings and rough poses from them.
        public static SwordMotion[] Read() => SwordFootage.Read(s_spec);

        /// Writes the weapon's game clips from the keys (see SwordFootage.Bake).
        public static string Bake() => SwordFootage.Bake(s_spec, false);

        /// Solves the motions (keys: a text file while tuning, empty = the keys in the code) and writes data.js of a review page.
        public static string Build(string keys, string folder) => SwordFootage.Build(s_spec, keys, folder);

        /// Solves the motions and renders frames of them, see SwordFootage.Run.
        public static string Run(string keys, string folder, string shots, string views, int size) => SwordFootage.Run(s_spec, keys, folder, shots, views, size);
    }
}
