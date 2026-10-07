# The animation pipeline on one page

Everything is generated: key poses and timings live in code, `Tools/Game/Battle/Rebuild Animations And Content`
turns them into humanoid clips, the animator controller, weapon configs (with hit traces baked from the clips) and
prefabs. Hand edits to generated assets are overwritten.

## Where things are

| What | Where |
|---|---|
| A weapon's poses and timings | `Assets/Game/Scripts/Editor/Dungeon/DungeonWeaponLibrary.cs`, `Create<Weapon>()` (sword & shield, greatsword, bow: `BattleAnimationLibrary.Create...`) |
| Pose helpers, how keys become motion | `Assets/Game/Scripts/Editor/Battle/BattleAnimationLibrary.cs` |
| Clip recording, controller states | `.../Editor/Battle/BattleAnimationBuilder.cs` (`BuildWeapon`, `AddKeyed`) |
| Config baking and the checks | `.../Editor/Battle/BattleContentBuilder.cs` (`CheckPeak`, `CheckSwing`) |
| Preview without building | `.../Editor/Battle/SwingPreview.cs` |
| Runtime: states, series, riposte | `Assets/Game/Scripts/Battle/Core/CombatComponent.cs`, `.../Battle/View/FighterAnimComponent.cs` |
| Hand edits of clips (the user's) | `Assets/Game/Configs/Battle/AnimationEdits.asset` |

Variants (`s_variants` in `DungeonWeaponLibrary`: Felling Axe on Battle Axe, Halberd on Spear, ...) play the source
weapon's clips with their own model and damage. Changing the source changes them; their blade must still contain
the source's strike point, or the peak check fails for the variant.

## Space and body

Root space in metres: the character stands at the origin facing **+Z**, **+X is its right**, +Y up.
Blade yaw = `atan2(x, z)` (0 forward, +90 to the right, 180 straight back), elevation = `asin(y)`.

- Eye (the crosshair is the ray from it along +Z): `(0, 1.755, 0.115)`.
- Right shoulder about `(0.17, 1.47, -0.06)`; the wrist reaches 0.59 m from it, the grip is 0.14 m past the wrist.
- The main hand is always the right one; a left-handed weapon or an off-hand punch is the same clip mirrored.

## Poses

A pose is the grip position, the direction of the blade, and the torso turn:

```csharp
OneHanded(grip, blade, yaw, pitch)                 // off hand rests by the hip
TwoHanded(grip, blade, offHand = -0.14f, yaw, pitch)   // off hand that far along the blade from the main one (negative = toward the butt)
SwordShield(grip, blade, [shield, shieldNormal,] yaw, pitch)
Guard(hand, thumb, fingers, open, pitch)           // bare hands, mirrored on both sides
Punch(fist, off, offFingers, offOpen, yaw, pitch)
Axe(grip, blade, yaw, pitch, offHand = AxeGrip)    // DungeonWeaponLibrary: TwoHanded with the butt grip at -0.42
```

`yaw`/`pitch` turn the spine in degrees (yaw + = to the right, pitch + = bowing forward); the head counter-turns.

```csharp
PoseAt(grip, yaw, elevation, torso, pitch, offHand, elbow, offElbow)  // DungeonWeaponLibrary: the head direction as angles; offHand 0 = one-handed
BattleAnimationLibrary.Elbows(pose, elbow, offElbow)                 // root-space elbows the solver pulls toward (zero = its own choice)
```

Elbows: the solver picks each elbow on its swivel circle by wrist comfort, the twist muscles and, when given, the
authored elbow (`HandPose.Elbow`/`ElbowWeight`); only the side of the arm the authored point is on matters, and an
authored point near the shoulder-to-wrist line counts for little.
A wrist bent past 85 degrees costs more than any authored elbow (`BattlePoseRig.MaxWristBend`/`BrokenWrist`): when
the footage's elbow would break the wrist, the solver leaves it, and "elbow off the authored one" in the preview is
then expected. The off hand on a shared haft is not tied to the edge: it wraps the haft with its knuckles away from
its shoulder and only its elbow is searched — its roll used to follow the edge and bent that wrist back by 140-178
degrees whenever a cut turned the edge over.
`pose.Lean` (degrees, 0 = the haft square to the forearm): how far the weapon is laid over in the main hand toward
the line of the forearm. Footage with the haft along a long arm (the stop of a one-handed chop, a hand folded across
the chest) cannot be held by the wrist alone — without the lean the fist hangs 70-90 degrees off the forearm and the
elbow wings out. Lean ≈ 90 minus the angle between forearm and haft, 60-70 on a straight arm; it is interpolated
between keys and goes into the clip as a rotation curve of both hand sockets (`CreateMorningStar` is the example).
The reports do not flag a twist muscle that flips sign for a frame or two (a visible twitch): after tuning leans or
raised elbows, look for a frame-to-frame jump in the `muscles` columns of `report_<swing>.txt`.
The roll of the weapon is worked out for you: in a cut the leading edge follows the path of the strike point. Give
`pose.Edge` by hand only for a rest pose that needs a particular face of the weapon toward the viewer.

## A swing

```csharp
new AttackDefinition
{
    Windup = 60 * Footage, Active = 14 * Footage, Recovery = 73 * Footage,   // seconds of clip; Footage = one video frame
    Damage, MoveMultiplier, Stagger,
    Raise  = new() { Via(18, pose, 0.4f), Via(50, pose, 0.5f) },             // passed on the way up
    WindupPose = ...,   // where the active phase begins
    MidPose    = ...,   // the peak; its blade direction is replaced, see below
    EndPose    = ...,   // where the active phase ends
    Return = new() { Via(80, pose, 0.5f), Via(114, pose, 0.6f) },            // passed on the way back; the first is the follow-through
    Launch = 1f,        // the weapon is already moving at WindupPose (0 = it settles there first)
    After = previous    // chained: the clip starts from that swing's EndPose instead of the idle pose
}
```

- Keys in order: start (idle, or `After`'s end pose, or the block pose for `WeaponDefinition.Riposte`) → `Raise` →
  `WindupPose` at `Windup` → peak at the middle of the active phase → `EndPose` → `Return` → idle at the end.
- `Via(videoFrame, pose, slope)`: slope 1 passes the pose at the average pace of its neighbours, below 1 lingers
  there (0.4-0.5 for the pose a windup hangs in), above 1 whips through. Frames are counted from the start of the
  swing in video frames.
- **The peak is not what you author.** `Peak()` turns the weapon so that its strike point lies on the crosshair
  ray, and pulls the grip toward the aim line if it is further than 0.75 of the strike distance. Author `MidPose`
  with the grip where the hand should be and `Vector3.forward` as the blade; read the result in the report.
- Without `Return` the follow-through is extrapolated 45% past `EndPose` — leave room for it.
- `ComboStart`/`ComboEnd`: by convention `Windup + Active * 0.5` and `Windup + Active + Recovery * 0.65`.
- `Strike` on the attack overrides the weapon's strike point; negative = the butt (behind the grip) meets the
  crosshair. `WeaponDefinition.IsRound` (a staff): no edge leads the cut and the edge check is off.

Block: `Block`, `BlockHit`, `BlockLowered` poses with `BlockRaise`, `BlockImpact`, `BlockRecovery` seconds.
Deflect: `DeflectPose`, `DeflectDuration`. Idle: `Idle`. Ranged: `DrawPose`, `ReleasePose`.

## Timing

Clips are authored at **clip frames = video frames x 0.75** (the `Footage` constant in `DungeonWeaponLibrary`, keep
it). The game plays attack, block and draw clips at `CombatComponent._baseActionSpeed` = 0.525 times the fighter's
Action Speed stat, so with Action Speed 100% the motion runs 30% slower than the footage — the user's chosen tempo.
Change the tempo via `_baseActionSpeed` only, never via `Footage`. Busy clips (drinking, bandaging, casting) play at
the stat alone: video frames x 1. Key times need not be whole frames; the peak is rounded to the 60 Hz grid by itself.

## Authoring rules that hold

- At the peak the grip is on the side the cut is heading to: the blade trails the hand.
- At every key keep the blade 15-45 degrees behind the forearm in the direction of travel, never ahead of it.
- Peak grip around y 1.42-1.46, z 0.48-0.54 keeps the hand at the bottom edge of the first-person view.
- A two-handed pose far to the off side, or behind the head, needs the off hand slid up the haft (`offHand` -0.3
  to -0.25): with the butt grip the main hand cannot reach.
- Behind-the-head poses: keep the off hand's fist out of the camera (grip x away from 0, z below -0.1), or the
  first-person view is a palm.
- A free off hand (one-handed weapon, nothing in the left hand) is placed with an explicit `Up` = its forearm
  (elbow → hand, from a two-bone estimate off the turned shoulder) and no authored elbow, as `CreateMorningStar`
  does. An auto-rolled free hand, or the footage reader's off elbow as a hint, wrings the arm (twist near 2) and
  throws the elbow about.
- The reader gives depth from the chest, and the chest itself goes forward as the torso bows: add about 5 cm to the
  grip's depth per 10-15 degrees of pitch, or the arm comes out bent where the footage has it long. Stop a few
  centimetres short of straight: on a fully straight arm the twist muscles flicker between upper arm and forearm.
- When the weapon must turn over between a swing's end and the next windup, give that a key or two (upright is a
  natural one) rather than one long segment: the edge turns at most ~12 degrees a frame.

## Limits the build enforces

`CheckPeak`: the blade passes within 1 cm of the crosshair ray at the peak. `CheckSwing`: the weapon rolls less than
25 degrees about its axis in a frame, and during the active phase of a cut the edge is within 15 degrees of the
travel. `SwingPreview` reports the same numbers from the rig in a second, plus wrist bend (over 90 looks broken),
hands that fall short of their targets (over 2 cm means the pose is out of reach), elbows off the authored side
(degrees round the swivel circle) and the arm's twist muscles as the clip stores them (past 1 the avatar clamps the
roll: a wrung forearm in the game that the rig render does not show). `report_<swing>.txt` has the elbows and the
four twist muscles per frame. `scripts/rigstats.py` sums those tables up (broken wrists, wrung forearms, elbow jerk) and compares two
runs. `SwingPreview` deletes its earlier renders of the swing first, so `footage.py
compare` never mixes in frames of an old key set.

## Other keyed clips

First-person busy clips are plain key lists in `BattleAnimationLibrary`: `CastKeys`, `CastReleaseKeys`, `UseKeys`
(with `DrinkTime` as the gameplay duration), `Hold`, `Bandage`. A changed keyed upper-body state does not need the
full rebuild: load `Fighter.controller`, `RemoveState` the old one on `layers[1].stateMachine`, and call
`BattleAnimationBuilder.AddKeyed(rig, upper, name, duration, SettleRoll(rig, keys))` by reflection on a fresh
`BattlePoseRig`. `SwingPreview` does not know these; give it a case in `Sample` if one needs iterating.

## Unity MCP

- `execute_code` compiles as C# 6 against `Assembly-CSharp` only: reach editor types with
  `System.Type.GetType("<Full.Name>, Assembly-CSharp-Editor")` and reflection; every optional parameter must be passed.
- After `refresh_unity` with a compile request, `execute_code` returning "compiling" means wait and call again.
- Bash heredocs with C# or quotes in them fail; write patch scripts with the Write tool and run them.
- `ScreenCapture.CaptureScreenshot("Temp/...png")` is the screenshot that includes the HUD.
