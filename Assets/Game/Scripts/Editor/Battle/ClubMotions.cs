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
# reach <share>: the fist that share of the arm's full length from the shoulder (1 = the elbow all but straight), the grip only says which way
# tw <-1..1>: the arm's turn about itself, so the palm rolls round the haft through the swing (palm up in a forehand, over as the club runs out)
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

# Attack 1 (the hand as the first-person footage has it, the club as the hitbox of the third-person one): the head tips left
# and lies back over the head while the fist waits by the right temple; the fist goes out ahead at eye height with the club left
# behind it (laid back at the first active frame), the elbow opens through the swing and the club whips round from the right through
# the crosshair; the arm is at full length as the club runs out to the left, the palm rolls over, the club settles by the left side.
motion attack1 thrust frames 100 windup 40 active 13 peak 48 title Attack_1
idle 0
key 5   0.27 1.28 0.38  -0.45 0.85 0.28   to 0 20 0   lh -0.18 1.22 0.34
key 10  0.38 1.46 0.31  -0.72 0.62 0.30   to 0 26 0   lh -0.20 1.25 0.36
key 16  0.44 1.62 0.30  -0.90 0.38 0.20   to 0 32 0   lh -0.22 1.28 0.38
key 24  0.45 1.70 0.34  -0.93 0.30 -0.20  to 0 38 0   lh -0.22 1.30 0.38
key 30  0.45 1.71 0.37  -0.82 0.48 -0.30  to 0 40 0   lh -0.22 1.30 0.38
key 34  0.44 1.70 0.41  -0.30 0.80 -0.52  to 1 40 0   lh -0.22 1.30 0.38  reach 0.84
key 37  0.42 1.70 0.46   0.20 0.50 -0.84  to 2 36 0   lh -0.22 1.29 0.37  reach 0.86
key 39  0.36 1.70 0.58   0.30 0.40 -0.87  to 4 30 0   lh -0.23 1.27 0.36  reach 0.9 tw 0.6
key 40  0.30 1.705 0.615 0.27 0.35 -0.89  to 5 26 0   lh -0.24 1.26 0.35  reach 0.92 tw 0.65
key 42  0.205 1.70 0.685 0.74 0.29 -0.61  to 7 14 0   lh -0.26 1.23 0.31  reach 0.94 tw 0.75
key 44  0.13 1.68 0.695  0.94 0.31 0.11   to 9 2 0    lh -0.28 1.20 0.26  reach 0.96 tw 0.8
key 46 -0.06 1.65 0.635  0.72 0.22 0.66   to 10 -14 0 lh -0.29 1.18 0.22  reach 0.98 tw 0.75
aim 48 -0.22 1.64 0.555                   to 10 -28 0 lh -0.30 1.16 0.19  reach 1 tw 0.6
key 50 -0.30 1.60 0.42  -0.36 0.08 0.93   to 9 -40 0  lh -0.30 1.14 0.16  reach 1 tw 0.35
key 52 -0.34 1.55 0.38  -0.82 0.06 0.57   to 8 -48 0  lh -0.30 1.13 0.14  reach 1 tw 0.1
key 55 -0.42 1.47 0.40  -0.91 -0.03 0.41  to 7 -52 0  lh -0.30 1.12 0.13  reach 0.97 tw -0.15
key 57 -0.39 1.41 0.40  -0.97 -0.07 0.24  to 7 -52 0  lh -0.30 1.12 0.13  reach 0.9 tw -0.28
key 59 -0.34 1.37 0.39  -0.99 -0.10 -0.03 to 6 -52 0  lh -0.30 1.12 0.12  tw -0.35  el -0.10 1.20 0.28
key 66 -0.31 1.26 0.36  -0.95 -0.18 -0.25 to 5 -50 0  lh -0.30 1.12 0.12  tw -0.37  el -0.06 1.12 0.24
key 76 -0.22 1.17 0.34  -0.93 -0.10 0.30  to 4 -40 0  lh -0.28 1.13 0.14  tw -0.2  el 0.02 1.06 0.20
key 88  0.02 1.15 0.40  -0.72 0.48 0.50   to 2 -8 0   lh -0.22 1.17 0.24  tw 0.1
idle 100
body 12 h 0 -0.02 -0.01 25 0 rf 0.02 -0.06 30
body 32 h 0 -0.04 -0.03 48 0 lf 0 0.04 15 rf 0.03 -0.10 50
body 41 h 0 -0.06 0.02 30 4 rf 0.03 -0.10 30 15
body 47 h 0 -0.09 0.14 -10 8 lf 0 0.10 -5 rf 0.03 -0.10 10 35
body 56 h -0.02 -0.09 0.14 -36 4
body 74 h -0.02 -0.08 0.12 -40 3
body 88 h 0 -0.03 0.04 -12 1 rf 0 0 0 10

# Attack 2: from the first cut's end the fist comes up to the left shoulder, the elbow ahead and the club back over the shoulder;
# a level backhand, the arm straightening ahead and round to the right, where it stays at full length with the club in its line
# and sinks.
motion attack2 after attack1 65 thrust frames 100 windup 35 active 14 peak 42 title Attack_2
idle 0
key 8  -0.26 1.38 0.30  -0.75 0.45 -0.48  to 6 -50 0  lh -0.30 1.10 0.10
key 14 -0.21 1.52 0.27  -0.45 0.50 -0.74  to 4 -52 0  lh -0.30 1.08 0.08  el 0.02 1.44 0.36
key 24 -0.20 1.55 0.29  -0.35 0.35 -0.87  to 2 -52 0  lh -0.30 1.08 0.08  el 0.02 1.47 0.38  tw 0.45
key 30 -0.23 1.56 0.38  -0.42 0.15 -0.90  to 3 -48 0  lh -0.30 1.08 0.10  el 0.03 1.47 0.40  tw 0.45
key 33 -0.28 1.56 0.52  -0.50 -0.02 -0.87 to 5 -40 0  lh -0.30 1.09 0.11  tw 0.45
key 35 -0.30 1.57 0.62  -0.59 -0.11 -0.80 to 6 -30 0  lh -0.30 1.10 0.12  reach 0.9 tw 0.45
key 37 -0.16 1.57 0.68  -0.89 0.01 -0.46  to 8 -18 0  lh -0.31 1.12 0.10  reach 0.94 tw 0.45
key 39  0.00 1.57 0.70  -0.94 0.15 0.31   to 9 -6 0   lh -0.33 1.14 0.06  reach 0.97 tw 0.45
key 41  0.14 1.56 0.69  -0.58 0.09 0.81   to 9 6 0    lh -0.34 1.15 0.03  reach 1 tw 0.45
aim 42  0.20 1.56 0.68                    to 9 12 0   lh -0.35 1.15 0.02  reach 1 tw 0.45
key 44  0.34 1.55 0.62   0.10 0.03 0.99   to 8 24 0   lh -0.36 1.16 -0.02 reach 1 tw 0.45
key 46  0.48 1.53 0.50   0.64 0.00 0.77   to 7 36 0   lh -0.36 1.16 -0.04 reach 1 tw 0.45
key 48  0.58 1.50 0.28   0.86 -0.02 0.51  to 6 46 0   lh -0.36 1.16 -0.05 reach 1 tw 0.45
key 52  0.70 1.42 0.08   0.93 -0.14 0.34  to 4 54 0   lh -0.35 1.16 -0.04 reach 1 tw 0.45
key 58  0.70 1.30 -0.04  0.93 -0.33 0.16  to 2 56 0   lh -0.33 1.16 0.00  reach 0.97 tw 0.45
key 66  0.62 1.18 -0.02  0.86 -0.48 0.17  to 1 54 0   lh -0.30 1.17 0.08  reach 0.9 tw 0.45
key 76  0.48 1.14 0.14   0.62 -0.62 0.48  to 0 40 0   lh -0.24 1.18 0.16  tw 0.45
key 88  0.28 1.20 0.38   0.20 0.85 0.10   to 0 22 0   lh -0.17 1.20 0.30  tw 0.3
idle 100
body 16 h -0.02 -0.08 0.12 -44 6
body 26 h -0.02 -0.07 0.12 -46 5
body 36 h 0 -0.08 0.13 -24 8
body 42 h 0.01 -0.09 0.15 6 9       rf 0.03 -0.12 25 25
body 49 h 0.03 -0.08 0.14 40 5      rf 0.03 -0.14 50 10
body 62 h 0.03 -0.07 0.10 56 2      rf 0.03 -0.14 60 0
body 80 h 0.02 -0.03 0.04 30 0

# Attack 3: from the second cut's end the club comes up to lie back over the right shoulder, and the first cut is given once more.
motion attack3 after attack2 65 thrust frames 99 windup 42 active 13 peak 50 title Attack_3
idle 0
key 10  0.50 1.48 0.24   0.78 0.25 -0.57  to 0 54 0   lh -0.30 1.18 0.08
key 20  0.47 1.66 0.31   0.50 0.48 -0.72  to 0 48 0   lh -0.26 1.26 0.24
key 30  0.45 1.71 0.37   0.22 0.55 -0.81  to 0 42 0   lh -0.22 1.30 0.38
key 36  0.44 1.70 0.42   0.15 0.55 -0.82  to 1 40 0   lh -0.22 1.30 0.38  reach 0.84
key 39  0.42 1.70 0.47   0.20 0.50 -0.84  to 2 36 0   lh -0.22 1.29 0.37  reach 0.86
key 41  0.36 1.70 0.58   0.30 0.40 -0.87  to 4 30 0   lh -0.23 1.27 0.36  reach 0.9 tw 0.6
key 42  0.30 1.705 0.615 0.27 0.35 -0.89  to 5 26 0   lh -0.24 1.26 0.35  reach 0.92 tw 0.65
key 44  0.205 1.70 0.685 0.74 0.29 -0.61  to 7 14 0   lh -0.26 1.23 0.31  reach 0.94 tw 0.75
key 46  0.13 1.68 0.695  0.94 0.31 0.11   to 9 2 0    lh -0.28 1.20 0.26  reach 0.96 tw 0.8
key 48 -0.06 1.65 0.635  0.72 0.22 0.66   to 10 -14 0 lh -0.29 1.18 0.22  reach 0.98 tw 0.75
aim 50 -0.22 1.64 0.555                   to 10 -28 0 lh -0.30 1.16 0.19  reach 1 tw 0.6
key 52 -0.30 1.60 0.42  -0.36 0.08 0.93   to 9 -40 0  lh -0.30 1.14 0.16  reach 1 tw 0.35
key 54 -0.34 1.55 0.38  -0.82 0.06 0.57   to 8 -48 0  lh -0.30 1.13 0.14  reach 1 tw 0.1
key 57 -0.42 1.47 0.40  -0.91 -0.03 0.41  to 7 -52 0  lh -0.30 1.12 0.13  reach 0.97 tw -0.15
key 59 -0.39 1.41 0.40  -0.97 -0.07 0.24  to 7 -52 0  lh -0.30 1.12 0.13  reach 0.9 tw -0.28
key 61 -0.34 1.37 0.39  -0.99 -0.10 -0.03 to 6 -52 0  lh -0.30 1.12 0.12  tw -0.35  el -0.10 1.20 0.28
key 68 -0.31 1.26 0.36  -0.95 -0.18 -0.25 to 5 -50 0  lh -0.30 1.12 0.12  tw -0.37  el -0.06 1.12 0.24
key 77 -0.22 1.17 0.34  -0.93 -0.10 0.30  to 4 -40 0  lh -0.28 1.13 0.14  tw -0.2  el 0.02 1.06 0.20
key 88  0.02 1.15 0.40  -0.72 0.48 0.50   to 2 -8 0   lh -0.22 1.17 0.24  tw 0.1
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

# Riposte: from the block the club lies back over the head and comes straight down ahead, a little to the left, the arm at full
# length; it runs out low by the left knee.
motion riposte after block end thrust frames 84 windup 23 active 9 peak 28 title Riposte
idle 0
key 8   0.20 1.78 0.36  -0.55 0.35 -0.76  to 0 16 0   lh -0.26 1.40 0.46
key 16  0.16 1.80 0.38   0.10 0.30 -0.95  to 0 20 0   lh -0.22 1.34 0.42
key 21  0.10 1.80 0.42   0.30 0.36 -0.88  to 2 18 0   lh -0.23 1.30 0.38
key 23  0.04 1.78 0.50   0.60 0.70 -0.39  to 4 14 0   lh -0.24 1.28 0.36  reach 0.92
key 25 -0.05 1.68 0.57   0.60 0.73 0.33   to 7 6 0    lh -0.26 1.24 0.30  reach 0.95
key 27 -0.13 1.56 0.61   0.36 0.47 0.80   to 10 -4 0  lh -0.28 1.20 0.25  reach 0.98
aim 28 -0.17 1.50 0.62                    to 12 -8 0  lh -0.29 1.18 0.22  reach 1
key 30 -0.24 1.34 0.60  -0.20 -0.20 0.96  to 14 -16 0 lh -0.30 1.15 0.18  reach 1
key 32 -0.29 1.20 0.52  -0.30 -0.36 0.88  to 16 -20 0 lh -0.30 1.13 0.15  reach 0.97
key 35 -0.32 1.06 0.42  -0.38 -0.55 0.74  to 17 -24 0 lh -0.30 1.12 0.13  reach 0.97
key 40 -0.30 0.96 0.34  -0.45 -0.65 0.61  to 18 -26 0 lh -0.30 1.12 0.12  reach 0.97
key 52 -0.28 0.95 0.32  -0.48 -0.62 0.62  to 16 -24 0 lh -0.30 1.12 0.12  reach 0.97 stop
key 68 -0.05 1.08 0.38  -0.45 0.55 0.70   to 6 -8 0   lh -0.22 1.17 0.24
idle 84
body 16 h 0 -0.03 -0.03 36 0 rf 0.03 -0.10 40
body 23 h 0 -0.06 0.03 24 4 rf 0.03 -0.10 25 15
body 28 h 0 -0.09 0.14 -10 8 lf 0 0.10 -5 rf 0.03 -0.10 10 35
body 36 h -0.02 -0.09 0.14 -36 4
body 56 h -0.02 -0.08 0.12 -40 3
body 72 h 0 -0.03 0.04 -12 1 rf 0 0 0 10";

        private static readonly Color s_wood = new Color(0.3f, 0.2f, 0.14f);
        private static readonly Color s_leather = new Color(0.22f, 0.1f, 0.08f);

        /// 0.98 m from the butt to the head's end, 88 cm of it ahead of the fist; the head is 10 cm thick. At full reach the haft lies over in the palm, in line with the forearm, and in a swing
        /// the fist is free on the round haft: it lies over early, slides a little and rolls round it. The left hand is never on the grip: OffHand only lets the left arm be keyed.
        private static readonly SwordSpec s_spec = new SwordSpec
        {
            Prefix = "Club", Base = "ArmingSword", Strike = 0.62f, BladeBase = 0.12f, BladeTip = Tip, OffHand = 0.14f, MaxLean = 55f, LeanEase = 0.6f, MaxSlide = 0.05f, Keys = Keys, Model = "Club", Pace = Dungeon.DungeonWeaponLibrary.FootagePace,
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
