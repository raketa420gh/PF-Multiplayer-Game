namespace Game.Scripts.Editor.Battle
{
    /// The one-handed sword of a skeleton, authored from footage of the Skeleton Footman of Dark and Darker
    /// (D:\skeleton_footman.mp4): the hunched rest with the point dragging on the floor, a cut down the slant from over
    /// the right shoulder and a cut straight down from over the head. Both cuts start from the rest and end bowed to the
    /// floor; a monster's weapon has no taking out, block or riposte.
    internal static class SkeletonSwordMotions
    {
        public static SwordSpec Spec => s_spec;

        /// Seconds of clip per second of the footage: the clips play at the base action speed of the fighters, so the
        /// monster swings as long as the one in the footage does.
        private const float Pace = 0.525f;

        /// Clip frames (60 Hz), model space metres: +Z ahead, +X right. See SwordFootage.Parse for the words.
        private const string Keys = @"# Skeleton sword keys. Footage frames (60 Hz), model space metres: +Z ahead, +X right.
# motion <id> [thrust] frames <n> [windup <f> active <f> peak <f>] title <Name>
# key <frame> grip(x y z) blade(x y z) | aim <frame> grip(x y z) -> strike point on the crosshair | idle <frame>
# to <bow> <turn right> <lean right> | el <right elbow x y z> | ed <where the edge faces x y z> | lh <left hand x y z> | stop
# The spine gives about 0.74 of the bow keyed and no more than 70 degrees.
# Shoulders ~ (+-0.17, 1.46, 0.0) upright, an arm reaches 0.63 m. A hanging arm with the sword in its line leaves the fist
# free to turn about the blade: the first keys of both attacks pin the edge (read off the solved clip) so that it turns over
# frames, not in one.

# Rest (footage frame 0): bowed low, both arms hanging, the point on the floor ahead to the right.
motion idle thrust frames 1 title Idle
key 0  0.27 0.68 0.46   0.25 -0.86 0.44   to 85 0 0   lh -0.26 0.66 0.46
key 1  0.27 0.68 0.46   0.25 -0.86 0.44   to 85 0 0   lh -0.26 0.66 0.46

# Attack 1 (footage 120-276): straightens with the arms thrown wide, the sword goes up the right side point down and ahead,
# is turned over the head (up, left, back) to lie back over the right shoulder, then cuts down the slant to the left shin;
# the body stays bowed over the left knee with the left arm thrown back, then rises dragging the point across the floor.
motion attack1 thrust frames 156 windup 58 active 10 peak 63 title Attack_1
idle 0
key 3    0.33 0.77 0.42   0.23 -0.86 0.45    to 72 2 0     lh -0.31 0.74 0.42   el 0.30 1.06 0.22   ed -0.25 -0.52 -0.82
key 6    0.44 0.92 0.36   0.20 -0.86 0.47    to 55 4 0     lh -0.38 0.84 0.36   el 0.38 1.12 0.12   ed 0.15 -0.48 -0.86
key 12   0.70 1.36 0.12   0.10 -0.80 0.59    to 10 8 0     lh -0.52 1.02 0.16   el 0.46 1.36 0.00   ed 0.75 -0.40 -0.52
key 18   0.58 1.60 0.12  -0.05 -0.72 0.69    to 0 14 0     lh -0.55 1.05 0.12   el 0.44 1.48 -0.04   ed 0.98 -0.10 0.00
key 23   0.48 1.80 0.08  -0.25 -0.60 0.76    to -3 17 0    lh -0.55 1.05 0.12   el 0.46 1.56 -0.08
key 29   0.40 1.90 0.03  -0.40 0.70 0.59     to -5 20 0    lh -0.54 1.05 0.12
key 36   0.36 1.92 -0.02 -0.96 0.15 -0.22    to -6 22 0    lh -0.53 1.05 0.12
key 44   0.36 1.92 -0.04 -0.80 0.10 -0.59    to -7 24 0    lh -0.52 1.05 0.12
key 52   0.40 1.90 -0.06  0.25 0.55 -0.80    to -8 26 0    lh -0.50 1.06 0.14  stop
key 57   0.42 1.88 -0.04  0.35 0.60 -0.72    to -6 24 0    lh -0.50 1.06 0.14  ed -0.3 0.3 0.9
key 60   0.34 1.72 0.30   0.30 0.55 0.78     to 8 6 0      lh -0.48 1.10 0.08   ed -0.3 -0.5 0.8
aim 63   0.12 1.42 0.52                      to 28 -12 0   lh -0.44 1.22 -0.04  ed -0.5 -0.8 0.3
key 66  -0.12 1.00 0.52  -0.45 -0.50 0.74    to 67 -30 0   lh -0.40 1.36 0.00   ed -0.6 -0.6 -0.5
key 69  -0.20 0.70 0.54  -0.76 -0.56 0.33    to 93 -44 0   lh -0.37 1.44 0.04
key 74  -0.22 0.52 0.55  -0.80 -0.58 0.15    to 104 -50 0   lh -0.36 1.46 0.06  stop
key 100 -0.22 0.52 0.55  -0.80 -0.58 0.15    to 102 -49 0   lh -0.36 1.44 0.06  stop
key 116 -0.08 0.60 0.50  -0.72 -0.68 0.15    to 88 -28 0   lh -0.32 0.95 0.32
key 132  0.08 0.66 0.47  -0.45 -0.84 0.30    to 85 -10 0   lh -0.28 0.72 0.44
key 144  0.20 0.68 0.46  -0.05 -0.86 0.50    to 83 -3 0    lh -0.26 0.67 0.46
idle 156

# Attack 2 (footage 312-488): turns to the right with the sword drawn back low and the left hand held out, comes back round
# raising the sword straight up over the head, cuts straight down to the floor ahead and stays bowed over it, then rises.
motion attack2 frames 176 windup 48 active 8 peak 52 title Attack_2
idle 0
key 8    0.30 0.80 0.22   0.15 -0.92 -0.36   to 61 30 0    lh -0.10 0.86 0.46   ed -0.97 -0.20 0.10
key 16   0.26 0.94 -0.20  0.10 -0.85 -0.52   to 15 55 0    lh 0.25 1.15 0.40   ed -0.20 -0.54 0.82
key 26   0.26 0.96 -0.24  0.10 -0.80 -0.59   to 8 60 0     lh 0.36 1.24 0.38   stop
key 34   0.40 1.50 -0.12  0.30 0.30 -0.90    to 0 35 0     lh -0.30 1.20 0.35
key 40   0.30 1.90 -0.02  0.10 0.80 -0.59    to -6 12 0    lh -0.52 1.15 0.15
key 46   0.22 1.98 0.02   0.00 0.98 -0.20    to -8 4 0     lh -0.55 1.12 0.10  stop
key 49   0.22 1.96 0.16   0.00 0.90 0.44     to 0 0 0      lh -0.54 1.10 0.10
aim 52   0.14 1.50 0.52                      to 30 -4 0    lh -0.48 1.05 0.14
key 55   0.10 1.00 0.60  -0.03 -0.80 0.60    to 76 -6 0    lh -0.38 0.90 0.28
key 58   0.08 0.68 0.64  -0.05 -0.84 0.54    to 100 -8 0    lh -0.30 0.62 0.42
key 64   0.08 0.50 0.66  -0.05 -0.55 0.83    to 105 -8 0    lh -0.24 0.42 0.52  stop
key 118  0.08 0.50 0.66  -0.05 -0.55 0.83    to 105 -8 0    lh -0.24 0.42 0.52  stop
key 138  0.14 0.62 0.52   0.05 -0.78 0.62    to 95 -4 0    lh -0.25 0.60 0.48
key 158  0.24 0.68 0.47   0.20 -0.86 0.46    to 85 0 0     lh -0.26 0.66 0.46
idle 176";

        /// The arming sword in the right fist; the left hand is never on the hilt (OffHand only lets the left arm be keyed).
        /// A point dragged on the floor from a hanging arm lies far over in the palm.
        private static readonly SwordSpec s_spec = new SwordSpec
        {
            Prefix = "SkeletonSword", Base = "ArmingSword", Strike = Dungeon.DungeonWeaponLibrary.ArmingStrike, BladeBase = 0.105f, BladeTip = 0.79f, OffHand = 0.14f, MaxLean = 60f,
            Keys = Keys, Model = "Sword", Pace = Pace
        };

        /// The motions as keyed: the weapon's definition takes its timings and rough poses from them.
        public static SwordMotion[] Read() => SwordFootage.Read(s_spec);

        /// Writes the weapon's game clips from the keys (see SwordFootage.Bake).
        public static string Bake() => SwordFootage.Bake(s_spec, false);

        /// Solves the motions and renders frames of them, see SwordFootage.Run.
        public static string Run(string keys, string folder, string shots, string views, int size) => SwordFootage.Run(s_spec, keys, folder, shots, views, size);
    }
}
