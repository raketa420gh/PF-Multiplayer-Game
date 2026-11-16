namespace Game.Scripts.Editor.Battle
{
    /// The arming sword's three attacks and its riposte, authored from footage. The animation build ends by writing the clips from here; the
    /// review page (Docs/Tasks/ArmingSword_Animations/mockup) shows the same poses.
    internal static class ArmingSwordAttacks
    {
        /// Clip frames (60 Hz), model space metres: +Z ahead, +X right. See Parse for the words.
        private const string Keys = @"# Arming Sword attack keys. Clip frames (60 Hz), model space metres: +Z ahead, +X right. The timing is the combat model's.
# motion <id> [after <motion> <frame>] [from <clip suffix>] [thrust] [title <Name>]: chained from a frame of another motion, or from the end
#   of a clip of the weapon that is not authored here (the riposte answers from the block)
# key <frame> grip(x y z) blade(x y z) | aim <frame> grip(x y z) -> strike point on the crosshair | idle <frame>
# to <bow> <turn right> <lean right> (chest, degrees) | el <elbow x y z> | ed <edge x y z> | hard <w> | stop
# tw <-1..1>: the arm's turn about itself held (palm down .. up), so the forearm does not turn over when the cut is done
# reach <share>: the fist that share of the arm's full length from the shoulder (1 = the elbow all but straight), the grip only says which way
# Right shoulder ~ (0.17, 1.46, 0.0), eye (0, 1.67, 0.115); the arm at full length puts the fist ~0.62 m from the shoulder.
# From the footage (Docs/Tasks/ArmingSword_Animations/arming_sword_refs): the hitbox capsule tracked in the front, right-side and top views and
# triangulated gives the blade through the active window; the first-person video gives where the fist is on the screen. In every cut the
# fist leads and the blade is left behind, it whips over through the crosshair between the first active frame and the peak while the elbow
# opens (about 125 deg -> 168 deg), and on the way out the arm stays long with the blade trailing it; the hilt lies over in the palm there.

# Attack 1: the blade tips left and lies level behind the head, the fist by the right temple; the fist goes up and ahead over the right
# shoulder with the blade still pointing back, the blade comes over the top and through the crosshair, and the long arm carries it down
# to the left hip.
motion forehand
idle 0
key 4   0.30 1.44 0.46   -0.10 0.97 -0.22   to 0 8 0
key 8   0.36 1.62 0.34   -0.35 0.88 -0.32   to -2 16 0
key 12  0.34 1.76 0.22   -0.78 0.40 -0.48   to -3 24 0   el 0.46 1.50 0.06
key 15  0.30 1.78 0.24   -0.85 0.12 -0.51   to -3 27 0   el 0.40 1.50 0.10
key 17  0.22 1.78 0.36   -0.45 0.20 -0.87   to -2 24 0   reach 0.86 el 0.27 1.48 0.17
key 18.75 0.15 1.77 0.47  0.20 0.60 -0.77   to 0 18 0    reach 0.9
key 20.25 0.09 1.70 0.55  0.44 0.88 -0.15   to 3 10 0    reach 0.94
key 21.75 0.03 1.61 0.585 0.33 0.70 0.63    to 6 0 0     reach 0.97
aim 23  -0.02 1.50 0.58                     to 8 -8 0    reach 1
key 24.5 -0.12 1.40 0.52  -0.05 -0.21 0.98  to 10 -15 0  reach 1
key 26  -0.24 1.24 0.42   -0.18 -0.47 0.86  to 12 -21 0  reach 1
key 27.5 -0.31 1.12 0.30  -0.30 -0.60 0.74  to 13 -25 0  reach 1
key 30  -0.32 1.02 0.18   -0.50 -0.68 0.54  to 14 -28 0  reach 0.97
key 33  -0.28 0.98 0.10   -0.70 -0.62 0.10  to 14 -29 0
key 37  -0.22 1.00 0.12   -0.62 -0.50 -0.60 to 13 -28 0  stop
key 48  -0.22 1.00 0.12   -0.60 -0.48 -0.64 to 10 -24 0  stop
key 56  -0.08 1.08 0.34   -0.45 0.60 -0.50  to 5 -10 0
key 64  0.18 1.28 0.50   -0.12 0.97 -0.20   to 2 -3 0
idle 72

# Attack 2: from the first cut's end the fist comes up before the left shoulder, the forearm across the chest and the blade level over that
# shoulder pointing back; out ahead and over, through the crosshair, down to the right hip and out to the right.
motion backhand after 0 31
idle 0
key 5   -0.26 1.22 0.22  -0.62 -0.10 -0.78  to 8 -30 0
key 10  -0.26 1.50 0.26  -0.40 0.30 -0.87   to 3 -33 0   el 0.08 1.30 0.34
key 15  -0.24 1.70 0.30  -0.10 0.10 -0.99   to 0 -35 0   el 0.06 1.46 0.40
key 20  -0.24 1.74 0.31   0.05 0.02 -1.00   to 0 -36 0   el 0.05 1.48 0.40 stop
key 21.5 -0.23 1.75 0.33 -0.15 0.15 -0.98   to 0 -34 0
key 22.5 -0.22 1.75 0.36 -0.42 0.40 -0.81   to 1 -32 0   reach 0.88
key 23.5 -0.18 1.74 0.43 -0.68 0.62 -0.39   to 2 -27 0   reach 0.91
key 24.5 -0.12 1.70 0.50 -0.72 0.62 0.31    to 3 -20 0   reach 0.94
key 25.25 -0.04 1.63 0.54 -0.52 0.48 0.71   to 5 -13 0   reach 0.97
aim 26   0.08 1.52 0.57                     to 6 -6 0    reach 0.99
key 27.25 0.34 1.30 0.46  0.14 -0.20 0.97   to 7 8 0     reach 1
key 28.5 0.46 1.14 0.34   0.34 -0.40 0.85   to 7 18 0    reach 1
key 31   0.52 1.02 0.14   0.62 -0.50 0.60   to 6 26 0    reach 1 tw 0
key 35   0.54 0.98 -0.02  0.82 -0.50 0.28   to 5 30 0    reach 0.97 tw 0
key 42   0.52 0.96 -0.08  0.80 -0.52 -0.30  to 4 28 0    tw 0 stop
key 48   0.52 0.96 -0.08  0.80 -0.52 -0.30  to 4 26 0    tw 0 stop
key 56   0.42 1.00 0.18   0.85 0.10 0.50    to 3 15 0    tw 0.3
key 62   0.32 1.12 0.38   0.35 0.60 0.72    to 1 6 0     tw 0.5
key 67   0.28 1.30 0.50   0.05 0.95 0.30    to 0 2 0
idle 71

# Attack 3: the arm lifts out to the right, the blade swings in level to point ahead and hangs aimed beside the right shoulder; the arm
# shoots out straight from the shoulder with the point on the crosshair, holds, and the tip rises back to upright.
motion stab after 1 34 thrust
idle 0
key 6   0.58 1.18 -0.08  0.85 -0.10 0.52    to 3 26 0    el 0.42 1.14 -0.24
key 11  0.60 1.34 -0.12  0.30 0.05 0.95     to 1 24 0    el 0.42 1.32 -0.30
key 15  0.58 1.42 -0.12  -0.24 0.10 0.97    to 0 22 0    el 0.38 1.38 -0.32
key 23  0.57 1.42 -0.12  -0.26 0.10 0.96    to 0 22 0    el 0.38 1.38 -0.32 stop
key 26.25 0.54 1.43 -0.06 -0.27 0.12 0.955  to 1 18 0
key 28  0.44 1.46 0.20   -0.30 0.16 0.94    to 4 4 0
aim 30  0.22 1.50 0.64                      to 8 -12 0   reach 1 hard 1
aim 33  0.21 1.50 0.66                      to 9 -16 0   reach 1 hard 1 stop
aim 38  0.21 1.50 0.65                      to 9 -15 0   reach 1 hard 1 stop
key 44  0.23 1.46 0.58   -0.28 0.30 0.91    to 7 -11 0   reach 0.93
key 50  0.25 1.40 0.52   -0.20 0.50 0.84    to 5 -7 0
key 57  0.26 1.37 0.53   -0.08 0.78 0.62    to 2 -3 0
key 63  0.27 1.36 0.55   0.00 0.95 0.31     to 1 -1 0
idle 69

# Riposte: from the block the fist goes up over the forehead, the blade back over the head; a steep chop ahead through the crosshair and
# down the right side, the long arm ending by the right hip with the blade behind.
motion riposte from _Block title Riposte
idle 0
key 5   0.16 1.66 0.44   -0.80 0.45 -0.40   to 0 4 0
key 10  0.02 1.80 0.36   -0.50 0.40 -0.77   to -1 -8 0    el 0.30 1.52 0.30
key 15  -0.08 1.83 0.38  -0.22 0.30 -0.93   to -1 -14 0   el 0.26 1.56 0.32
key 18.75 -0.10 1.82 0.44 -0.36 0.60 -0.71  to 0 -12 0    reach 0.9
key 20  -0.07 1.78 0.52  -0.50 0.84 -0.20   to 2 -8 0     reach 0.93
key 21  0.00 1.68 0.57   -0.36 0.80 0.48    to 5 -2 0     reach 0.96
aim 22  0.08 1.52 0.58                      to 8 4 0      reach 0.99
key 23.25 0.28 1.24 0.48  0.04 -0.14 0.99   to 11 12 0    reach 1
key 24.75 0.42 1.02 0.24  0.36 -0.45 0.82   to 13 20 0    reach 1
key 27  0.46 0.94 0.00   0.72 -0.44 0.54    to 14 26 0    reach 1
key 30  0.45 0.93 -0.10  0.82 -0.44 0.38    to 13 30 0    reach 0.97
key 36  0.44 0.94 -0.14  0.84 -0.46 0.28    to 11 30 0    stop
key 44  0.44 0.94 -0.14  0.84 -0.46 0.28    to 9 27 0     stop
key 52  0.40 1.00 0.18   0.80 0.05 0.60     to 5 16 0
key 59  0.32 1.16 0.42   0.30 0.65 0.70     to 2 6 0
idle 67";

        /// At arm's length the hilt lies over in the palm toward the forearm's line (a handshake hold); the fist keeps its place on
        /// it and does not turn round it: the edge leads the cut.
        private static readonly SwordSpec s_spec = new SwordSpec
        {
            Prefix = "ArmingSword", Base = "ArmingSword", Strike = 0.627f, BladeBase = 0.105f, BladeTip = 0.79f, MaxLean = 45f, LeanEase = 0.8f, Keys = Keys
        };

        /// Writes ArmingSword_Attack0-2 and ArmingSword_AttackRiposte from the keys; with traces, the weapon config's blade traces are sampled anew.
        public static string Bake(bool withTraces) => SwordFootage.Bake(s_spec, withTraces);

        /// Solves the attacks (keys: a text file while tuning, empty = the keys in the code) and writes data.js of the review page.
        public static string Build(string keys, string folder) => SwordFootage.Build(s_spec, keys, folder);

        /// Solves the attacks and renders frames of them, see SwordFootage.Run.
        public static string Run(string keys, string folder, string shots, string views, int size) => SwordFootage.Run(s_spec, keys, folder, shots, views, size);
    }
}
