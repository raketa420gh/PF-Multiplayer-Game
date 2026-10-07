# First-person action clips (no weapon swing)

Interacting, searching, drinking, bandaging, casting, picking up: the hands in the player's own view. What the user
checks is the screen: where the hands are, how big they are, how the fingers close, that no joint is wrung. The
reference is first-person footage, so everything is measured in screen fractions and nothing has to be guessed in 3D.
Proven on `InteractFp` (searching a container, `D:\fp_interaction_animation.mp4`): two rounds of poses after the
first draft.

## 1. Read the footage (you, not a reader agent)

The hands are big and screen-locked; one look at a gridded frame is worth more than a reader's prose here.

```bash
python $SKILL/scripts/footage.py sheet "<video>" 0 <last> 12 Temp/footage/<name>/s.jpg --cols 4 --cell 480   # the whole clip
python $SKILL/scripts/footage.py fp "<video>" Temp/footage/<name>/g.jpg <f1> <f2> <f3> <f4>                    # key moments, 10% grid
```

Write down, per key moment and per hand: the screen box of the hand (u left→right, v top→bottom, 0..1), what the
fingers do (fist, claw, open, together), which way the knuckles and the palm face, where the forearm leaves the
screen. Then the rhythm: frames between repeats (60 fps footage, busy clips play at video frames x 1). A loop with
hands taking turns is one phase function, not a key list.

Dark footage: `--gamma 1.8` (default of `fp`). A spectated player (DaD death cam) is fine — the HUD is not ours.

## 2. Screen → root space

FP camera: `BattleAnimationLibrary.Eye` = (0, 1.731, 0.115), looking +Z, vertical FOV 75 (tan 0.767, horizontal tan
1.364 at 16:9). DaD footage is 90 horizontal; matching the screen *fractions* is the requirement, so convert with ours:

```
x = (2u - 1) * 1.364 * d        y = 1.731 - (2v - 1) * 0.767 * d        z = 0.115 + d
```

Start with d = 0.32 for a hand that fills ~12-15% of the screen width (DaD gloves are bulky; our bare hand reads the
same at that depth). `HandPose.Position` is the palm, so use the centre of the glove, not the cuff.

## 3. Author

A phase function in `BattleAnimationLibrary` beside `Bandage` / `Interact`, recorded by a line in
`BattleAnimationBuilder.BuildFirstPerson` (add the state name to `s_firstPersonStates` too), a `*FirstPersonState`
const in `FighterAnimComponent` and its slot in `s_busyFirstPerson` (indexed by busy kind), the state in
`AnimationTestView`'s list. A key list (`UseKeys`) is fine for one-shots.

Bare hands, as `Interact` does it:
- `Main = new HandPose(position, thumb)` + `pose.Edge = fingers`; `Off = new HandPose(position, thumb, fingers)` with
  `OffSocket = LeftHand`. `thumb` = the fist's axis (where the thumb points), `fingers` = where straightened fingers
  would point (knuckle direction). Keep them roughly perpendicular. Right hand palm down: thumb -x; left: +x.
- Fingers: `MainOpen` / `OffOpen` 0 = tight fist, 0.3 = closed claw, 0.55 = around a bottle. "Кисти сомкнуты" = ≤ 0.3.
- Wrist: point `fingers` along the forearm (elbow → hand is steeply up-forward from below the view); fingers flat
  forward on a low hand bent the wrist 56 degrees, along the forearm 34.
- Measured hand positions per side, not mirrored — footage is rarely symmetric.
- `Upper(yaw, pitch)` small; the upper body is screen-locked to the look anyway.

## 4. One round = compile + one execute_code + one sheet

Add a case to `SwingPreview.Sample` for the numbers (`"interact"` samples the phase function into linear keys; prefix
`Fists`). Then:

```csharp
var F = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string report = (string)System.Type.GetType("Game.Scripts.Editor.Battle.SwingPreview, Assembly-CSharp-Editor").GetMethod("Run", F)
    .Invoke(null, new object[] { "Fists", "<case>", "", "", "Temp/footage/<name>", 540 });
System.Type.GetType("Game.Scripts.Editor.Battle.BattleAnimationBuilder, Assembly-CSharp-Editor").GetMethod("RebuildFirstPerson", F).Invoke(null, null);
return report + System.Type.GetType("Game.Scripts.Editor.Battle.ClipPreview, Assembly-CSharp-Editor").GetMethod("Run", F)
    .Invoke(null, new object[] { "<State>", "0,0.25,0.5,0.75", "Temp/footage/<name>" });
```

- `SwingPreview` numbers: wrist ≤ ~60 is relaxed (over 90 broken), twist muscles < 1 (0.8 is the solver's comfort
  limit), reach miss 0.00. Fix FAILs before looking at pictures.
- `RebuildFirstPerson` (menu `Tools/Game/Battle/Rebuild First Person Actions`) re-records only the generated FP states
  on the existing controller in seconds. It re-records all of them: diff the other FP clips afterwards; they should
  come out identical (a stale `Hold.anim` in git was the one exception seen — it then matches `UseFp` frame 0).
- `ClipPreview` renders the *built clip* on the character from the eye, fingers included (`SwingPreview` fp renders
  leave fingers flat — useless for hand shape). Phases are fractions of the clip.

```bash
python $SKILL/scripts/footage.py fp "<video>" Temp/footage/<name>/c.jpg <f1>:Temp/footage/<name>/c_<State>_0.png <f2>:Temp/footage/<name>/c_<State>_2.png
```

Read the sheet yourself: compare the boxes on the grid, move positions by the measured difference (Δu 0.06 at
d 0.32 ≈ 5 cm in x; Δv 0.07 ≈ 3.4 cm in y). Match phases by hand pose (which hand is up), not by frame number.
Done when boxes agree within ~3% of the screen and the finger shapes read the same.

## 5. Finish

`RebuildVerify.Run("Verify")` — compile ok, 0 errors. No full rebuild needed for FP states; the next full rebuild
produces the same clips. Then the report and the commit (SKILL.md step 7).
