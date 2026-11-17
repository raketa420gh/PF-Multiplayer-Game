namespace Game.Scripts.Editor.Battle
{
    /// The morning star's three attacks and its riposte, authored from footage (D:\morningstar_refs). The animation build ends by
    /// writing the clips from here; the rest, the block and taking it out stay as the pose library has them.
    internal static class MorningStarAttacks
    {
        /// Footage frames (60 Hz, played at the game's pace), model space metres: +Z ahead, +X right. See SwordFootage.Parse for the words.
        private const string Keys = @"# Morning Star attack keys. Footage frames (60 Hz; the clips play at the game's pace), model space metres: +Z ahead, +X right.
# motion <id> [after <id> <frame>] [from <clip suffix>] thrust frames <n> windup <f> active <f> peak <f> title <Name>: the timing is the
#   combat model's in footage frames; a round weapon has no edge to lead, so every motion is a 'thrust'
# key <frame> grip(x y z) haft(x y z) | aim <frame> grip(x y z) -> the ball on the crosshair | idle <frame>
# to <bow> <turn right> <lean right> (chest, degrees) | el <right elbow x y z> | lh <left hand x y z> | stop
# reach <share>: the fist that share of the arm's full length from the shoulder (1 = the elbow all but straight), the grip only says which way
# Shoulders ~ (+-0.17, 1.46, 0.0), eye (0, 1.67, 0.115). From the footage: the hitbox (the ball and the haft's line) tracked in the front and
# right-side views and triangulated through the active window, the fist and the ball read off both views in the windup and the run-out.
# In the footage the ball crosses the eye line 1-3 frames off the middle of the active window: the swing is shifted so that it does at the peak.

# Attack 1: the ball tips left and goes round level over and behind the head, the fist over the right temple; the fist comes ahead on the
# right of the crosshair with the haft still pointing back, the ball whips over the top and through the crosshair as the elbow opens, and
# the long arm carries it down ahead to chest height, where it holds.
motion attack1 thrust frames 90 windup 43 active 10 peak 48 title Attack_1
idle 0
key 6   0.28 1.42 0.42  -0.50 0.86 0.05   to 0 4 0    lh -0.30 1.00 0.22
key 12  0.30 1.66 0.38  -0.87 0.49 -0.03  to 0 8 0    lh -0.34 0.98 0.16
key 18  0.29 1.84 0.24  -0.98 0.15 -0.12  to -2 12 0  lh -0.38 0.96 0.12  el 0.44 1.50 0.04
key 24  0.24 1.92 0.17  -0.86 -0.12 -0.50 to -3 15 0  lh -0.40 0.95 0.10  el 0.46 1.56 0.02
key 30  0.24 1.93 0.16  -0.55 -0.28 -0.79 to -3 16 0  lh -0.40 0.95 0.08  el 0.46 1.60 0.00
key 35  0.22 1.93 0.18  -0.20 -0.28 -0.94 to -3 15 0  lh -0.39 0.95 0.04  el 0.44 1.62 0.02
key 38  0.21 1.92 0.25  0.02 0.10 -0.99   to -2 12 0  lh -0.38 0.94 -0.02 el 0.42 1.62 0.08
key 40.5 0.20 1.86 0.34 0.16 0.54 -0.83   to 0 8 0    lh -0.36 0.93 -0.08 reach 0.86
key 42  0.19 1.82 0.40  0.24 0.80 -0.55   to 2 3 0    lh -0.35 0.93 -0.12 reach 0.89
key 43.5 0.17 1.76 0.46 0.26 0.96 -0.06   to 4 -2 0   lh -0.34 0.92 -0.16 reach 0.92
key 45  0.13 1.70 0.53  0.20 0.88 0.43    to 6 -8 0   lh -0.34 0.92 -0.20 reach 0.95
key 46.5 0.07 1.61 0.60 0.08 0.60 0.80    to 8 -14 0  lh -0.34 0.91 -0.24 reach 0.98
aim 48  -0.02 1.48 0.62                   to 10 -20 0 lh -0.35 0.90 -0.28 reach 1
key 49   -0.05 1.42 0.62  -0.03 0.15 0.99  to 11 -24 0 lh -0.35 0.90 -0.30 reach 1
key 50   -0.07 1.35 0.62  -0.05 0.08 1.00  to 12 -27 0 lh -0.35 0.90 -0.31 reach 1
key 51   -0.08 1.30 0.61  -0.07 0.04 1.00  to 13 -30 0 lh -0.35 0.90 -0.32 reach 1
key 52.5 -0.08 1.26 0.60  -0.08 0.03 1.00  to 14 -32 0 lh -0.35 0.90 -0.32 reach 1
key 55   -0.07 1.25 0.59  -0.08 0.07 1.00  to 14 -34 0 lh -0.35 0.90 -0.32 reach 1
key 59   -0.05 1.25 0.58  -0.07 0.14 0.99  to 14 -34 0 lh -0.36 0.92 -0.28 reach 0.99 stop
key 64   -0.03 1.25 0.57  -0.07 0.22 0.97  to 12 -30 0 lh -0.37 0.94 -0.20 reach 0.96
key 72  0.10 1.20 0.48   -0.30 0.42 0.86  to 7 -16 0  lh -0.37 0.98 -0.04
key 82  0.20 1.19 0.45   -0.18 0.84 0.51  to 2 -5 0   lh -0.33 1.00 0.14
idle 90

# Attack 2: from the first blow's end the forearm folds across the chest and the fist climbs to the left ear, the ball behind the left
# shoulder; the fist goes ahead first, the ball comes round the left side and across through the crosshair, and the arm ends long ahead
# to the right with the haft in its line.
motion attack2 after attack1 62 thrust frames 90 windup 48 active 7 peak 52 title Attack_2
idle 0
key 4   0.02 1.24 0.56   -0.25 0.06 0.97   to 10 -24 0  lh -0.38 0.98 -0.18
key 10  -0.02 1.32 0.48  -0.58 0.24 0.78   to 8 -18 0   lh -0.41 1.08 -0.06  el 0.26 1.36 0.32
key 16  -0.09 1.39 0.41  -0.97 0.23 -0.11  to 5 -22 0   lh -0.43 1.13 0.00   el 0.24 1.43 0.34
key 22  -0.20 1.47 0.40  -0.68 -0.05 -0.73 to 3 -30 0   lh -0.45 1.10 0.00   el 0.16 1.53 0.36
key 28  -0.19 1.59 0.38  -0.48 -0.27 -0.83 to 2 -36 0   lh -0.45 1.10 -0.05  el 0.12 1.60 0.37
key 34  -0.20 1.67 0.28  -0.29 -0.36 -0.89 to 2 -40 0   lh -0.45 1.10 -0.10  el 0.09 1.62 0.41
key 40  -0.20 1.71 0.28  -0.21 -0.24 -0.95 to 2 -40 0   lh -0.45 1.10 -0.10  el 0.09 1.56 0.45 stop
key 44  -0.19 1.71 0.34  -0.42 0.08 -0.90  to 3 -36 0   lh -0.42 1.08 -0.10  el 0.13 1.52 0.47
key 46  -0.17 1.72 0.43  -0.62 0.26 -0.74  to 4 -31 0   lh -0.41 1.06 -0.13  el 0.14 1.51 0.48
key 47.5 -0.14 1.72 0.50 -0.78 0.42 -0.46  to 4 -26 0   lh -0.40 1.05 -0.16  reach 0.86
key 48.5 -0.12 1.71 0.53 -0.85 0.50 -0.16  to 5 -22 0   lh -0.39 1.04 -0.18  reach 0.88
key 49.3 -0.09 1.69 0.55 -0.84 0.52 0.16   to 6 -17 0   lh -0.38 1.03 -0.21  reach 0.90
key 50  -0.06 1.67 0.57  -0.75 0.48 0.46   to 6 -13 0   lh -0.38 1.03 -0.23  reach 0.92
key 50.7 -0.02 1.64 0.59 -0.56 0.42 0.71   to 7 -8 0    lh -0.37 1.02 -0.25  reach 0.94
key 51.4 0.04 1.59 0.61  -0.34 0.33 0.88   to 8 -3 0    lh -0.36 1.01 -0.27  reach 0.97
aim 52  0.09 1.53 0.62                     to 9 2 0     lh -0.36 1.00 -0.28  reach 0.99
key 52.8 0.16 1.46 0.62  0.03 0.17 0.98   to 9 8 0     lh -0.35 1.00 -0.29  reach 1
key 54  0.23 1.38 0.60   0.10 0.08 0.99    to 10 18 0   lh -0.35 1.00 -0.30  reach 1
key 55.5 0.27 1.35 0.57  0.10 0.05 0.99    to 10 26 0   lh -0.35 1.00 -0.30  reach 1
key 58  0.29 1.33 0.55   0.03 0.08 1.00    to 10 30 0   lh -0.35 1.00 -0.30  reach 1 stop
key 62  0.29 1.32 0.54   -0.04 0.10 0.99   to 9 30 0    lh -0.35 1.00 -0.28  reach 0.98
key 66  0.29 1.30 0.58   -0.04 0.14 0.99   to 8 26 0    lh -0.35 1.00 -0.24
key 72  0.28 1.27 0.52   -0.13 0.30 0.94   to 5 18 0    lh -0.33 1.00 -0.10
key 80  0.25 1.22 0.47   -0.18 0.72 0.67   to 2 8 0     lh -0.31 1.00 0.08
idle 90

# Attack 3: the first blow again out of the backhand's end, coming in wider from the right and running out to the left.
motion attack3 after attack2 62 thrust frames 90 windup 46 active 10 peak 51 title Attack_3
idle 0
key 6   0.30 1.42 0.56   -0.60 0.50 0.62   to 6 24 0    lh -0.34 1.04 -0.10
key 12  0.27 1.66 0.40   -0.90 0.36 0.23   to 2 18 0    lh -0.32 1.08 0.12
key 18  0.25 1.84 0.25   -1.00 0.04 -0.04  to -2 16 0   lh -0.30 1.10 0.24   el 0.47 1.60 0.06
key 24  0.25 1.90 0.18   -0.87 -0.15 -0.47 to -3 16 0   lh -0.27 1.12 0.29   el 0.44 1.64 0.12
key 30  0.24 1.91 0.13   -0.56 -0.16 -0.81 to -3 16 0   lh -0.28 1.12 0.28   el 0.45 1.65 0.14
key 36  0.24 1.90 0.14   -0.18 -0.03 -0.98 to -3 15 0   lh -0.30 1.12 0.25   el 0.45 1.66 0.14
key 40  0.26 1.89 0.21   0.08 0.23 -0.97   to -2 12 0   lh -0.35 1.12 0.20   el 0.43 1.62 0.22
key 43.5 0.24 1.87 0.38  0.36 0.62 -0.70   to 0 8 0     lh -0.36 1.10 0.05   reach 0.86
key 45  0.22 1.84 0.46   0.48 0.80 -0.36   to 2 4 0     lh -0.37 1.10 -0.04  reach 0.89
key 46.3 0.19 1.80 0.52  0.52 0.85 0.05    to 4 0 0     lh -0.38 1.10 -0.10  reach 0.92
key 47.6 0.15 1.75 0.57  0.46 0.76 0.46    to 5 -5 0    lh -0.38 1.11 -0.15  reach 0.95
key 49  0.09 1.68 0.61   0.34 0.60 0.72    to 7 -11 0   lh -0.39 1.12 -0.20  reach 0.97
key 50  0.03 1.61 0.63   0.20 0.42 0.88    to 8 -15 0   lh -0.39 1.13 -0.24  reach 0.99
aim 51  -0.04 1.53 0.63                    to 10 -20 0  lh -0.40 1.14 -0.27  reach 1
key 52  -0.11 1.45 0.62  0.03 0.10 0.99    to 11 -25 0  lh -0.40 1.15 -0.29  reach 1
key 53  -0.17 1.38 0.61  -0.04 0.00 1.00   to 12 -29 0  lh -0.40 1.15 -0.30  reach 1
key 54.5 -0.21 1.32 0.60 -0.12 -0.04 0.99  to 13 -33 0  lh -0.40 1.15 -0.30  reach 1
key 57  -0.20 1.28 0.59  -0.20 -0.02 0.98  to 14 -36 0  lh -0.40 1.14 -0.30  reach 1
key 61  -0.16 1.27 0.58  -0.22 0.04 0.97   to 14 -36 0  lh -0.40 1.12 -0.30  reach 0.99 stop
key 66  -0.08 1.26 0.54  -0.26 0.12 0.96   to 11 -30 0  lh -0.38 1.06 -0.22
key 72  0.04 1.24 0.46   -0.30 0.36 0.88   to 7 -18 0   lh -0.35 1.02 -0.08
key 80  0.17 1.22 0.44   -0.26 0.78 0.57   to 3 -7 0    lh -0.32 1.00 0.08
idle 90

# Riposte: from the guard the ball goes level to the left, back round behind the head without a pause and comes over on the widest slant,
# from the upper right through the crosshair down to the left.
motion riposte from _Block thrust frames 72 windup 30 active 10 peak 35 title Riposte
idle 0
key 3   0.17 1.74 0.30   -0.86 -0.32 0.36  to 0 2 0     lh -0.28 1.20 0.18
key 7   0.16 1.80 0.18   -0.92 -0.33 0.10  to 0 6 0     lh -0.33 1.18 0.14   el 0.43 1.62 0.00
key 11  0.17 1.86 0.15   -0.95 -0.27 -0.17 to -1 10 0   lh -0.35 1.14 0.10   el 0.46 1.59 0.08
key 15.5 0.18 1.86 0.20  -0.90 -0.18 -0.40 to -2 15 0   lh -0.37 1.06 0.05   el 0.42 1.57 0.20
key 19  0.20 1.86 0.24   -0.49 -0.02 -0.87 to -2 19 0   lh -0.38 1.05 0.00   el 0.36 1.56 0.28
key 22.5 0.23 1.85 0.33  -0.06 0.13 -0.99  to -2 20 0   lh -0.38 1.05 -0.05  el 0.32 1.53 0.36
key 25.5 0.24 1.83 0.42  0.36 0.37 -0.86   to 0 16 0    lh -0.38 1.05 -0.12  reach 0.86
key 27.5 0.24 1.81 0.48  0.56 0.57 -0.60   to 1 12 0    lh -0.38 1.06 -0.17  reach 0.88
key 29  0.23 1.79 0.52   0.74 0.62 -0.26   to 3 8 0     lh -0.39 1.07 -0.21  reach 0.9
key 30.5 0.21 1.77 0.56  0.77 0.60 0.22    to 4 3 0     lh -0.39 1.08 -0.24  reach 0.92
key 32  0.17 1.74 0.60   0.62 0.52 0.59    to 6 -3 0    lh -0.40 1.10 -0.27  reach 0.95
key 33  0.12 1.69 0.62   0.46 0.47 0.75    to 7 -8 0    lh -0.40 1.11 -0.28  reach 0.97
key 34  0.06 1.62 0.63   0.25 0.40 0.88    to 8 -13 0   lh -0.40 1.12 -0.29  reach 0.99
aim 35  -0.02 1.53 0.63                    to 10 -19 0  lh -0.40 1.12 -0.30  reach 1
key 36  -0.10 1.44 0.62  0.02 0.08 1.00    to 11 -25 0  lh -0.40 1.12 -0.30  reach 1
key 37  -0.16 1.37 0.61  -0.08 -0.02 1.00  to 12 -30 0  lh -0.40 1.12 -0.30  reach 1
key 38.5 -0.19 1.31 0.60 -0.18 -0.06 0.98  to 13 -34 0  lh -0.40 1.12 -0.30  reach 1
key 41  -0.17 1.28 0.59  -0.23 -0.04 0.97  to 14 -37 0  lh -0.40 1.12 -0.30  reach 1
key 45  -0.14 1.27 0.58  -0.22 0.03 0.98   to 14 -37 0  lh -0.40 1.10 -0.30  reach 0.99 stop
key 50  -0.07 1.25 0.53  -0.22 0.12 0.97   to 11 -30 0  lh -0.38 1.05 -0.20
key 57  0.08 1.23 0.44   -0.32 0.45 0.83   to 6 -14 0   lh -0.34 1.00 0.00
key 65  0.18 1.21 0.43   -0.20 0.78 0.59   to 2 -4 0    lh -0.31 1.00 0.12
idle 72";

        /// The ball's middle is 0.55 m ahead of the fist. The left hand is never on the haft: OffHand only lets the left arm be keyed.
        /// In a swing the fist is free on the round haft: it lies over toward the forearm's line at full reach, slides a little and rolls.
        private static readonly SwordSpec s_spec = new SwordSpec
        {
            Prefix = "MorningStar", Base = "MorningStar", Strike = 0.55f, BladeBase = 0.1f, BladeTip = 0.65f, OffHand = 0.14f, IsCarriedOneHanded = true,
            MaxLean = 55f, LeanEase = 0.6f, MaxSlide = 0.03f, Keys = Keys, Pace = Dungeon.DungeonWeaponLibrary.FootagePace
        };

        /// Writes MorningStar_Attack0-2 and MorningStar_AttackRiposte from the keys (see SwordFootage.Bake).
        public static string Bake() => SwordFootage.Bake(s_spec, false);

        /// Solves the attacks (keys: a text file while tuning, empty = the keys in the code) and writes data.js of a review page.
        public static string Build(string keys, string folder) => SwordFootage.Build(s_spec, keys, folder);

        /// Solves the attacks and renders frames of them, see SwordFootage.Run.
        public static string Run(string keys, string folder, string shots, string views, int size) => SwordFootage.Run(s_spec, keys, folder, shots, views, size);
    }
}
