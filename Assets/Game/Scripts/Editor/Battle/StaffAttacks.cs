namespace Game.Scripts.Editor.Battle
{
    /// The magic staff's two attacks and its riposte, authored from footage (D:\magicstaff_refs). The animation build ends by
    /// writing the clips from here; the rest and the block stay as the pose library has them.
    internal static class StaffAttacks
    {
        /// Footage frames (60 Hz, played at the game's pace), model space metres: +Z ahead, +X right. See SwordFootage.Parse for the words.
        private const string Keys = @"# Magic Staff attack keys. Footage frames (60 Hz; the clips play at the game's pace), model space metres: +Z ahead, +X right.
# motion <id> [after <id> <frame>] [from <clip suffix>] thrust [strike <z>] frames <n> windup <f> active <f> peak <f> title <Name>: the timing is
#   the combat model's in footage frames; 'strike' = the point that goes through the crosshair where it is not the head (the butt, behind the fist)
# key <frame> grip(x y z) staff(x y z, butt -> head) | aim <frame> grip(x y z) -> the strike point on the crosshair | idle <frame>
# to <bow> <turn right> <lean right> (chest, degrees) | el <right elbow x y z> | lh <left hand off the staff, x y z> | stop
# reach <share>: the fist that share of the arm's full length from the shoulder (1 = the elbow all but straight), the grip only says which way
# Shoulders ~ (+-0.17, 1.46, 0.0), eye (0, 1.67, 0.115). From the footage: the hitbox capsule (the whole staff) tracked in the front and right-side
# views and triangulated, a line of the staff's length fitted to both views outside the active window. The fighter of the footage bows and
# turns from the feet; here the legs stand, so the fist is keyed a little higher and the chest takes what turn it can.

# Attack 1: the head tips left and the fist crosses the chest to the left ear, the staff lying back over the left shoulder; the fist goes
# ahead first, the staff turns round it and the head comes from the upper left through the crosshair; the arm ends long ahead to the
# right with the staff in its line, and holds.
motion attack1 thrust frames 96 windup 34 active 7 peak 37 title Attack_1
idle 0
key 4   0.14 1.38 0.42   -0.09 1.00 -0.01   to 0 0 0     lh -0.27 0.98 0.08
key 8   -0.03 1.45 0.42  -0.26 0.95 -0.16   to 0 -5 0    lh -0.28 0.99 0.10   el 0.26 1.20 0.25
key 12  -0.16 1.50 0.36  -0.33 0.87 -0.37   to 0 -10 0   lh -0.29 1.00 0.12   el 0.15 1.27 0.32
key 16  -0.24 1.54 0.26  -0.23 0.78 -0.58   to 0 -15 0   lh -0.30 1.00 0.12   el 0.04 1.36 0.34
key 20  -0.27 1.60 0.14  0.07 0.71 -0.70    to 0 -20 0   lh -0.30 1.00 0.10   el -0.04 1.46 0.30
key 24  -0.26 1.63 0.08  0.52 0.60 -0.61    to 0 -25 0   lh -0.30 1.00 0.08   el -0.12 1.52 0.24
key 28  -0.25 1.66 0.08  0.66 0.56 -0.50    to 0 -25 0   lh -0.30 1.00 0.08   el -0.12 1.52 0.24 stop
key 31  -0.27 1.66 0.22  0.15 0.66 -0.74    to 2 -20 0   lh -0.30 1.00 0.06   el -0.06 1.50 0.30
key 33  -0.25 1.69 0.36  -0.45 0.64 -0.62   to 4 -10 0   lh -0.31 1.00 0.02   el 0.02 1.50 0.34
key 34  -0.21 1.71 0.43  -0.70 0.66 -0.28   to 5 -5 0    lh -0.32 1.00 0.00   reach 0.86
key 35  -0.12 1.71 0.50  -0.77 0.61 0.18    to 6 4 0     lh -0.33 1.00 -0.04  reach 0.91
key 36  0.00 1.66 0.57   -0.56 0.50 0.66    to 7 12 0    lh -0.34 1.00 -0.08  reach 0.96
aim 37  0.11 1.58 0.60                      to 8 20 0    lh -0.34 1.00 -0.12  reach 1
key 38  0.18 1.55 0.62   -0.15 0.31 0.94    to 9 25 0    lh -0.35 1.00 -0.15  reach 1.03
key 39  0.23 1.52 0.61   -0.08 0.27 0.96    to 10 28 0   lh -0.35 1.00 -0.17  reach 1.04
key 40  0.27 1.49 0.60   -0.06 0.25 0.97    to 10 30 0   lh -0.35 1.00 -0.18  reach 1.04
key 42  0.31 1.45 0.59   -0.05 0.21 0.98    to 10 30 0   lh -0.35 1.00 -0.18  reach 1.04
key 46  0.36 1.43 0.57   -0.03 0.20 0.98    to 10 30 0   lh -0.35 1.00 -0.18  reach 1.04 stop
key 60  0.38 1.41 0.55   -0.03 0.22 0.97    to 10 30 0   lh -0.35 1.00 -0.16  reach 1.04 stop
key 70  0.38 1.40 0.54   -0.03 0.25 0.97    to 9 28 0    lh -0.34 1.00 -0.12  reach 0.98
key 80  0.38 1.33 0.46   -0.14 0.51 0.85    to 5 15 0    lh -0.31 0.99 0.00
key 90  0.29 1.24 0.42   -0.14 0.89 0.42    to 0 5 0     lh -0.28 0.98 0.06
idle 96

# Attack 2: from the first blow's end the fist rises over the right temple and the staff lies level across over the head, the butt out to
# the right; the butt comes round ahead and drops under the arm as the head goes over the top, and the head chops down ahead through the
# crosshair to chest height, the arm long.
motion attack2 after attack1 56 thrust frames 122 windup 55 active 9 peak 60 title Attack_2
idle 0
key 4   0.42 1.54 0.50   -0.32 0.42 0.85    to 8 25 0    lh -0.34 1.02 -0.10  el 0.40 1.33 0.30
key 10  0.44 1.70 0.44   -0.62 0.34 0.71    to 5 22 0    lh -0.32 1.06 0.05   el 0.45 1.45 0.15
key 16  0.39 1.84 0.35   -0.72 0.09 0.69    to 0 20 0    lh -0.31 1.12 0.20   el 0.45 1.65 0.05
key 22  0.38 1.90 0.32   -0.87 -0.16 0.47   to -4 20 0   lh -0.31 1.18 0.30   el 0.42 1.75 0.00
key 30  0.32 1.98 0.22   -0.88 -0.28 0.39   to -5 20 0   lh -0.31 1.20 0.34   el 0.42 1.77 0.00 stop
key 40  0.28 1.99 0.13   -0.99 -0.11 0.07   to -5 20 0   lh -0.31 1.20 0.34   el 0.42 1.78 0.00
key 45  0.26 1.99 0.06   -0.97 0.00 -0.23   to -5 18 0   lh -0.31 1.20 0.33   el 0.41 1.79 0.02
key 49.5 0.22 2.00 0.10  -0.81 0.07 -0.58   to -5 15 0   lh -0.31 1.19 0.31   el 0.40 1.80 0.05
key 51.5 0.23 2.00 0.18  -0.69 0.10 -0.72   to -3 12 0   lh -0.31 1.18 0.28   el 0.38 1.80 0.08
key 53.5 0.24 1.95 0.24  -0.50 0.24 -0.83   to 0 10 0    lh -0.32 1.16 0.22   el 0.36 1.78 0.12
key 54.5 0.23 1.90 0.29  -0.43 0.40 -0.81   to 2 8 0     lh -0.32 1.15 0.18   el 0.35 1.75 0.16
key 55.5 0.22 1.84 0.36  -0.35 0.54 -0.77   to 3 7 0     lh -0.33 1.13 0.14   el 0.33 1.68 0.24
key 56.5 0.19 1.77 0.44  -0.23 0.87 -0.44   to 5 5 0     lh -0.33 1.11 0.08   reach 0.87
key 57.5 0.15 1.69 0.52  -0.13 0.99 0.02    to 7 3 0     lh -0.34 1.09 0.02   reach 0.89
key 58.5 0.11 1.61 0.58  -0.10 0.91 0.41    to 8 0 0     lh -0.34 1.07 -0.05  reach 0.92
key 59.3 0.07 1.55 0.61  -0.12 0.77 0.63    to 9 -3 0    lh -0.35 1.06 -0.10  reach 0.95
aim 60  0.04 1.48 0.62                      to 10 -6 0   lh -0.35 1.05 -0.14  reach 0.97
key 61  0.01 1.42 0.62   -0.15 0.55 0.82    to 10 -8 0   lh -0.35 1.05 -0.17  reach 0.99
key 62  -0.01 1.38 0.62  -0.17 0.49 0.85    to 10 -9 0   lh -0.35 1.05 -0.19  reach 1.02
key 63.5 -0.03 1.34 0.61 -0.19 0.45 0.87    to 10 -10 0  lh -0.35 1.05 -0.20  reach 1.04
key 66  -0.05 1.31 0.60  -0.20 0.42 0.89    to 10 -10 0  lh -0.35 1.05 -0.20  reach 1.04
key 72  -0.05 1.30 0.60  -0.20 0.40 0.89    to 10 -10 0  lh -0.35 1.05 -0.20  reach 1.04 stop
key 90  -0.05 1.27 0.60  -0.21 0.37 0.90    to 10 -10 0  lh -0.35 1.05 -0.18  reach 1.04 stop
key 96  -0.02 1.20 0.56  -0.05 0.42 0.90    to 7 -6 0    lh -0.33 1.02 -0.08
key 104 0.08 1.12 0.50   -0.12 0.54 0.83    to 4 -3 0    lh -0.30 1.00 0.02
key 114 0.20 1.15 0.42   -0.28 0.83 0.48    to 1 0 0     lh -0.28 0.98 0.07
idle 122

# Riposte: both hands, the butt first. From the guard the staff comes level across at neck height, the right fist pulls back to the right
# side and the left one drives the butt round from the left through the crosshair; the chest turns right with it.
motion riposte from _Block thrust strike -0.9 frames 81 windup 22 active 12 peak 28 title Riposte
idle 0
key 6   0.22 1.66 0.30   0.81 0.59 0.02     to 0 0 0
key 10  0.20 1.64 0.28   0.88 0.40 0.26     to 0 0 0
key 14  0.20 1.58 0.26   0.88 0.28 0.38     to 0 -8 0
key 16  0.21 1.57 0.28   0.95 0.12 0.30     to 0 -8 0
key 18  0.22 1.56 0.33   1.00 -0.02 0.05    to 0 -5 0
key 21  0.23 1.55 0.31   0.99 -0.10 -0.02   to 0 0 0
key 22  0.23 1.52 0.27   0.98 -0.16 -0.09   to 1 5 0
key 23  0.25 1.50 0.24   0.96 -0.20 -0.20   to 2 11 0
key 24  0.27 1.48 0.20   0.91 -0.24 -0.33   to 3 18 0
key 25  0.29 1.46 0.15   0.83 -0.27 -0.48   to 4 26 0
key 26  0.30 1.44 0.10   0.71 -0.30 -0.64   to 5 34 0
key 27  0.31 1.42 0.06   0.55 -0.31 -0.78   to 6 42 0
aim 28  0.32 1.40 0.02                      to 7 48 0
key 29  0.34 1.38 -0.01  0.17 -0.26 -0.95   to 8 52 0
key 30  0.35 1.36 -0.03  0.02 -0.22 -0.98   to 8 55 0
key 31  0.36 1.34 -0.04  -0.09 -0.19 -0.98  to 8 56 0
key 33  0.36 1.32 -0.05  -0.15 -0.15 -0.98  to 8 56 0
key 36  0.36 1.30 -0.06  -0.17 -0.12 -0.98  to 8 56 0 stop
key 50  0.36 1.25 -0.06  -0.17 -0.06 -0.98  to 8 54 0 stop
key 54  0.36 1.28 -0.02  0.03 0.24 -0.97    to 6 42 0
key 58  0.35 1.32 0.02   0.16 0.42 -0.89    to 4 28 0    lh 0.05 1.10 0.36
key 62  0.34 1.34 0.10   0.21 0.63 -0.75    to 2 14 0    lh -0.16 1.02 0.24
key 66  0.31 1.33 0.22   0.24 0.82 -0.51    to 1 6 0     lh -0.24 0.99 0.14
key 72  0.27 1.26 0.33   0.07 0.94 -0.33    to 0 2 0     lh -0.27 0.98 0.09
idle 81";

        /// The right fist holds the staff 0.4 m under the head's top, 1.2 m of haft behind it. The attacks are the right hand's alone; in
        /// the block and the riposte the left one holds the haft half a metre nearer the butt, and the riposte strikes with the butt.
        /// At full reach the haft lies over in the palm into the forearm's line, and the fist is free to roll and slide a little on it.
        private static readonly SwordSpec s_spec = new SwordSpec
        {
            Prefix = "Staff", Base = "Staff", Strike = 0.3f, BladeBase = -1.2f, BladeTip = 0.4f, OffHand = 0.5f, IsCarriedOneHanded = true,
            MaxLean = 65f, LeanEase = 0.6f, MaxSlide = 0.05f, Keys = Keys, Pace = Dungeon.DungeonWeaponLibrary.FootagePace
        };

        /// Writes Staff_Attack0-1 and Staff_AttackRiposte from the keys (see SwordFootage.Bake).
        public static string Bake() => SwordFootage.Bake(s_spec, false);

        /// Solves the attacks (keys: a text file while tuning, empty = the keys in the code) and writes data.js of a review page.
        public static string Build(string keys, string folder) => SwordFootage.Build(s_spec, keys, folder);

        /// Solves the attacks and renders frames of them, see SwordFootage.Run.
        public static string Run(string keys, string folder, string shots, string views, int size) => SwordFootage.Run(s_spec, keys, folder, shots, views, size);
    }
}
