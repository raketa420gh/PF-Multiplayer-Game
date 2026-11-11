using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// The Club's motions, authored from footage (Docs/Tasks/Club_Animations): taking it out, the rest pose, the three attacks
    /// of the series, the block up and down and the riposte. The right hand swings it alone; the left one is free and braces
    /// the head in the block. The review page shows these poses.
    internal static class ClubMotions
    {
        public static SwordSpec Spec => s_spec;

        public const float Back = -0.1f;
        public const float Tip = 0.88f;

        /// Clip frames (60 Hz), model space metres: +Z ahead, +X right. See SwordFootage.Parse for the words.
        private const string Keys = @"# Club keys. Clip frames (60 Hz), model space metres: +Z ahead, +X right.
# motion <id> [after <id> <frame|end>] [thrust] frames <n> [windup <f> active <f> peak <f>] [shown <f>] title <Name>
# key <frame> grip(x y z) club(x y z) | aim <frame> grip(x y z) -> strike point on the crosshair | idle <frame>
# to <bow> <turn right> <lean right> | el <right elbow x y z> | ed <where the knuckles face x y z> | lh <left hand, off the club, x y z> | hard <w> | stop
# body <frame> [h dx dy dz yaw [pitch]] [lf dx dz [yaw [heel]]] [rf dx dz [yaw [heel]]]: the hips off the stance (metres) and the pelvis
#   turned right / pitched forward (degrees), each foot off its stance, turned right, its heel raised. From the third-person footage;
#   the keys above stay as the eyes see them, the eyes ride with the body.
# Shoulders ~ (+-0.17, 1.46, 0.0), eye (0, 1.67, 0.115). The right fist holds the grip; every motion is a 'thrust': a round club has no edge to lead.

# Rest: the club upright before the right side of the chest, leaning ahead; the left hand open by the ribs.
motion idle thrust frames 1 title Idle
key 0  0.21 1.20 0.42  -0.07 0.90 0.43   to 0 16 0   lh -0.16 1.20 0.32
key 1  0.21 1.20 0.42  -0.07 0.90 0.43   to 0 16 0   lh -0.16 1.20 0.32

# Taking it out: from the right hip the head comes up ahead and over to the rest.
motion equip thrust frames 28 shown 6 title Equip
key 0   0.30 0.96 0.16   0.15 -0.75 0.64   to 0 4 0    lh -0.22 1.06 0.22
key 7   0.33 1.02 0.30   0.05 0.10 0.99    to 0 8 0    lh -0.22 1.08 0.24
key 13  0.34 1.14 0.40  -0.32 0.66 0.68    to 0 12 0   lh -0.20 1.14 0.28
key 20  0.26 1.19 0.42  -0.16 0.86 0.48    to 0 15 0   lh -0.17 1.19 0.31
idle 28

# Attack 1: the head tips left and goes round over the head to lie back over the right shoulder, then a cut round from the
# right to the left at the neck, running out low by the left hip.
motion attack1 thrust frames 100 windup 40 active 13 peak 48 title Attack_1
idle 0
key 6   0.24 1.30 0.42  -0.40 0.82 0.41   to 0 20 0   lh -0.18 1.24 0.36
key 12  0.38 1.60 0.40  -0.72 0.48 0.50   to 0 28 0   lh -0.20 1.28 0.40
key 16  0.40 1.72 0.30  -0.45 0.85 0.27   to 0 34 0   lh -0.22 1.32 0.42
key 22  0.34 1.74 0.18   0.05 0.60 -0.80  to 0 42 0   lh -0.22 1.34 0.42
key 30  0.27 1.70 0.10   0.48 0.08 -0.87  to 0 50 0   lh -0.22 1.34 0.42  stop
key 38  0.30 1.70 0.16   0.50 0.30 -0.81  to 0 46 0   lh -0.22 1.32 0.40
key 42  0.20 1.60 0.40   0.60 0.55 -0.58  to 1 34 0   lh -0.24 1.28 0.36
key 44  0.10 1.56 0.46   0.80 0.50 0.33   to 2 22 0   lh -0.26 1.24 0.30
key 46  0.00 1.52 0.46   0.55 0.35 0.76   to 3 8 0    lh -0.28 1.20 0.25
aim 48 -0.08 1.50 0.44                    to 4 -6 0   lh -0.29 1.18 0.22
key 50 -0.12 1.44 0.40  -0.60 -0.05 0.80  to 5 -22 0  lh -0.30 1.15 0.18
key 52 -0.14 1.36 0.34  -0.88 -0.15 0.45  to 6 -32 0  lh -0.30 1.13 0.15  el 0.16 1.30 0.24
key 55 -0.14 1.24 0.30  -0.96 -0.18 0.20  to 6 -40 0  lh -0.30 1.12 0.13  el 0.20 1.24 0.22
key 58 -0.08 1.08 0.26  -0.96 -0.15 0.22  to 6 -44 0  lh -0.30 1.12 0.12  el 0.24 1.16 0.26  stop
key 72 -0.08 1.06 0.25  -0.98 -0.12 0.05  to 5 -42 0  lh -0.30 1.12 0.12  el 0.24 1.14 0.25  stop
key 86  0.04 1.16 0.40  -0.45 0.75 0.20   to 2 -10 0  lh -0.22 1.17 0.24
idle 100
body 12 h 0 -0.02 -0.01 25 0 rf 0.02 -0.06 30
body 32 h 0 -0.04 -0.03 48 0 lf 0 0.04 15 rf 0.03 -0.10 50
body 41 h 0 -0.06 0.02 30 4 rf 0.03 -0.10 30 15
body 47 h 0 -0.09 0.14 -10 8 lf 0 0.10 -5 rf 0.03 -0.10 10 35
body 56 h -0.02 -0.09 0.14 -36 4
body 74 h -0.02 -0.08 0.12 -40 3
body 88 h 0 -0.03 0.04 -12 1 rf 0 0 0 10

# Attack 2: from the first cut's end the club goes up the left side to lie back over the left shoulder, then a level
# backhand cut from the left to the right.
motion attack2 after attack1 65 thrust frames 100 windup 35 active 14 peak 42 title Attack_2
idle 0
key 8  -0.12 1.16 0.24  -0.72 0.38 -0.58  to 4 -44 0  lh -0.30 1.10 0.10
key 16 -0.16 1.40 0.20  -0.42 0.45 -0.79  to 2 -48 0  lh -0.30 1.08 0.08
key 24 -0.18 1.52 0.16  -0.18 0.20 -0.96  to 0 -50 0  lh -0.30 1.08 0.08  stop
key 30 -0.18 1.52 0.18  -0.50 0.10 -0.86  to 0 -48 0  lh -0.30 1.08 0.10
key 35 -0.18 1.50 0.26  -0.90 0.05 -0.43  to 1 -36 0  lh -0.30 1.10 0.12
key 38 -0.14 1.48 0.38  -0.98 0.02 0.20   to 2 -22 0  lh -0.31 1.12 0.10
key 40 -0.06 1.47 0.46  -0.75 0.03 0.66   to 3 -8 0   lh -0.33 1.14 0.06
aim 42  0.04 1.46 0.50                    to 3 4 0    lh -0.35 1.15 0.02
key 44  0.16 1.46 0.48   0.60 0.04 0.80   to 3 18 0   lh -0.36 1.16 -0.02
key 46  0.26 1.45 0.42   0.90 0.02 0.44   to 3 32 0   lh -0.36 1.16 -0.04
key 49  0.32 1.43 0.32   1.00 0.00 -0.05  to 2 46 0   lh -0.36 1.16 -0.05
key 54  0.34 1.40 0.24   0.85 0.05 -0.52  to 2 56 0   lh -0.35 1.16 -0.04
key 62  0.32 1.38 0.20   0.70 0.20 -0.68  to 1 58 0   lh -0.33 1.16 0.00  stop
key 74  0.28 1.30 0.30   0.35 0.75 -0.40  to 0 40 0   lh -0.24 1.18 0.16
key 88  0.22 1.22 0.40   0.00 0.93 0.30   to 0 20 0   lh -0.17 1.20 0.30
idle 100
body 16 h -0.02 -0.08 0.12 -44 6
body 26 h -0.02 -0.07 0.12 -46 5
body 36 h 0 -0.08 0.13 -24 8
body 42 h 0.01 -0.09 0.15 6 9       rf 0.03 -0.12 25 25
body 49 h 0.03 -0.08 0.14 40 5      rf 0.03 -0.14 50 10
body 62 h 0.03 -0.07 0.10 56 2      rf 0.03 -0.14 60 0
body 80 h 0.02 -0.03 0.04 30 0

# Attack 3: from the second cut's end the club comes up to lie back over the right shoulder again, and the first cut is given once more.
motion attack3 after attack2 65 thrust frames 99 windup 42 active 13 peak 50 title Attack_3
idle 0
key 10  0.32 1.50 0.22   0.65 0.45 -0.60  to 0 56 0   lh -0.30 1.18 0.06
key 20  0.29 1.66 0.14   0.48 0.15 -0.86  to 0 54 0   lh -0.26 1.26 0.24
key 34  0.25 1.68 0.10   0.50 0.06 -0.86  to 0 50 0   lh -0.22 1.34 0.42  stop
key 40  0.30 1.70 0.16   0.50 0.30 -0.81  to 0 46 0   lh -0.22 1.32 0.40
key 44  0.20 1.60 0.40   0.60 0.55 -0.58  to 1 34 0   lh -0.24 1.28 0.36
key 46  0.10 1.56 0.46   0.80 0.50 0.33   to 2 22 0   lh -0.26 1.24 0.30
key 48  0.00 1.52 0.46   0.55 0.35 0.76   to 3 8 0    lh -0.28 1.20 0.25
aim 50 -0.08 1.50 0.44                    to 4 -6 0   lh -0.29 1.18 0.22
key 52 -0.12 1.44 0.40  -0.60 -0.05 0.80  to 5 -22 0  lh -0.30 1.15 0.18
key 54 -0.14 1.36 0.34  -0.88 -0.15 0.45  to 6 -32 0  lh -0.30 1.13 0.15  el 0.16 1.30 0.24
key 57 -0.14 1.24 0.30  -0.96 -0.18 0.20  to 6 -40 0  lh -0.30 1.12 0.13  el 0.20 1.24 0.22
key 60 -0.08 1.08 0.26  -0.96 -0.15 0.22  to 6 -44 0  lh -0.30 1.12 0.12  el 0.24 1.16 0.26  stop
key 72 -0.08 1.06 0.25  -0.98 -0.12 0.05  to 5 -42 0  lh -0.30 1.12 0.12  el 0.24 1.14 0.25  stop
key 86  0.04 1.16 0.40  -0.45 0.75 0.20   to 2 -10 0  lh -0.22 1.17 0.24
idle 99
body 20 h 0.02 -0.05 0.04 54 0 rf 0.03 -0.12 56
body 34 h 0.00 -0.04 -0.02 50 0 lf 0 0.04 15 rf 0.03 -0.10 50
body 43 h 0 -0.06 0.02 30 4 rf 0.03 -0.10 30 15
body 49 h 0 -0.09 0.14 -10 8 lf 0 0.10 -5 rf 0.03 -0.10 10 35
body 58 h -0.02 -0.09 0.14 -36 4
body 74 h -0.02 -0.08 0.12 -40 3
body 88 h 0 -0.03 0.04 -12 1 rf 0 0 0 10

# Block: the right fist goes up by the right temple, the head comes down ahead to the left onto the open left palm.
motion block thrust frames 12 title Block
idle 0
key 4   0.22 1.42 0.42  -0.50 0.80 0.33   to 0 14 0   lh -0.18 1.32 0.40
key 8   0.20 1.62 0.39  -0.85 0.20 0.49   to 0 12 0   lh -0.30 1.44 0.50
key 12  0.19 1.74 0.37  -0.75 -0.30 0.59  to 0 10 0   lh -0.24 1.53 0.62

motion blockLower after block end thrust frames 12 title Block_Lower
idle 0
key 4   0.20 1.62 0.39  -0.85 0.20 0.49   to 0 12 0   lh -0.30 1.42 0.48
key 8   0.22 1.42 0.42  -0.50 0.80 0.33   to 0 14 0   lh -0.20 1.30 0.40
idle 12

# Riposte: from the block the club goes back over the right shoulder and the first cut is given, faster.
motion riposte after block end thrust frames 84 windup 23 active 9 peak 29 title Riposte
idle 0
key 8   0.24 1.76 0.20   0.05 0.45 -0.89  to 0 22 0   lh -0.26 1.40 0.46
key 16  0.27 1.70 0.12   0.45 0.10 -0.89  to 0 40 0   lh -0.22 1.34 0.42  stop
key 23  0.20 1.60 0.40   0.60 0.55 -0.58  to 1 34 0   lh -0.24 1.28 0.36
key 25  0.10 1.56 0.46   0.80 0.50 0.33   to 2 22 0   lh -0.26 1.24 0.30
key 27  0.00 1.52 0.46   0.55 0.35 0.76   to 3 8 0    lh -0.28 1.20 0.25
aim 29 -0.08 1.50 0.44                    to 4 -6 0   lh -0.29 1.18 0.22
key 31 -0.12 1.44 0.40  -0.60 -0.05 0.80  to 5 -22 0  lh -0.30 1.15 0.18
key 33 -0.14 1.36 0.34  -0.88 -0.15 0.45  to 6 -32 0  lh -0.30 1.13 0.15  el 0.16 1.30 0.24
key 36 -0.14 1.24 0.30  -0.96 -0.18 0.20  to 6 -40 0  lh -0.30 1.12 0.13  el 0.20 1.24 0.22
key 40 -0.08 1.08 0.26  -0.96 -0.15 0.22  to 6 -44 0  lh -0.30 1.12 0.12  el 0.24 1.16 0.26  stop
key 56 -0.08 1.06 0.25  -0.98 -0.12 0.05  to 5 -42 0  lh -0.30 1.12 0.12  el 0.24 1.14 0.25  stop
key 70  0.04 1.16 0.40  -0.45 0.75 0.20   to 2 -10 0  lh -0.22 1.17 0.24
idle 84
body 16 h 0 -0.03 -0.03 36 0 rf 0.03 -0.10 40
body 23 h 0 -0.06 0.03 24 4 rf 0.03 -0.10 25 15
body 28 h 0 -0.09 0.14 -10 8 lf 0 0.10 -5 rf 0.03 -0.10 10 35
body 36 h -0.02 -0.09 0.14 -36 4
body 56 h -0.02 -0.08 0.12 -40 3
body 72 h 0 -0.03 0.04 -12 1 rf 0 0 0 10";

        private static readonly Color s_wood = new Color(0.3f, 0.2f, 0.14f);
        private static readonly Color s_leather = new Color(0.22f, 0.1f, 0.08f);

        /// 0.98 m from the butt to the head's end, 88 cm of it ahead of the fist; the head is 10 cm thick. At full reach the haft lies over in the palm, in line with the forearm. The left hand is
        /// never on the grip: OffHand only lets the left arm be keyed.
        private static readonly SwordSpec s_spec = new SwordSpec
        {
            Prefix = "Club", Base = "ArmingSword", Strike = 0.62f, BladeBase = 0.12f, BladeTip = Tip, OffHand = 0.14f, MaxLean = 55f, Keys = Keys, Model = "Club", Pace = Dungeon.DungeonWeaponLibrary.FootagePace,
            Mock = new[]
            {
                (new Vector3(0f, 0f, (Back + 0.1f) * 0.5f), new Vector3(0.04f, 0.04f, 0.1f - Back), s_leather),
                (new Vector3(0f, 0f, 0.3f), new Vector3(0.055f, 0.055f, 0.4f), s_wood),
                (new Vector3(0f, 0f, (0.5f + Tip) * 0.5f), new Vector3(0.09f, 0.09f, Tip - 0.5f), s_wood)
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
