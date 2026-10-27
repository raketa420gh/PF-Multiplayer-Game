# Recurve bow animations from DaD footage (pending)

Videos: `D:\recurvebow_equip.mp4`, `D:\recurvebow_block.mp4`, `D:\recurvebow_shot.mp4`; the reload video comes later.
Skill: `video-to-animation`. Prefix `Bow` (`BattleAnimationLibrary.CreateBow`, bow in the left hand socket).
The block of ranged weapons, the `_Reload` state and `ReloadVia`/`ReleaseVia` were added with the crossbow (2026-10-27).

Footage reader's spec (screen fractions, u right, v down; "lower wrap" = just above the grip, "upper wrap" = 2/3 up
the upper limb; the lower limb is always off screen; roll + = top leaning right).

## Idle (shared)
Bow low in the left hand only, full fist, knuckles at u .46 v .86 (only the top of the fist shows, no forearm).
Roll +47. Lower wrap .53-.54,.70-.72; upper wrap .74-.76,.25-.39; upper limb leaves the top edge at u .83-.93.
String almost parallel to the upper limb, a little right of and below it. Right hand off screen. No idle sway.

## Equip (188 frames)
Empty hands 0-66, bow appears 68-70 from the lower right (hip/quiver side, never over the shoulder), fast swing
70-84, small overshoot 84-90, settled ~100. Only the left hand carries it.
| frame | lower wrap / upper wrap / tip | roll | left hand |
|---|---|---|---|
| 70 | upper limb only: tip .81,.77 → .66,.90, off the bottom | +55 | below |
| 76 | tip .94,.41, limb to .51,.89, exits bottom at u .47 | +58 | below |
| 80 | .53,.82 / .77,.49 / exits right edge v .37 | +50 | knuckles .46,.96 |
| 84 | .54,.74 / .76,.38 / exits top-right, right edge v .15 | +48 | .46,.87 |
| 90 | .53,.71 / .77,.38 / right edge v .10 | +47 | .46,.84 |
| 184 | idle | +47 | .46,.86 |

## Block (354 frames)
Idle to 66, raise 68-84 (~16), hold 86-288, lower 290-310 (~20), settled ~316. Bow nearly flat across the chest,
both hands; string side toward the camera and down.
| frame | bow | roll | left hand | right hand | forearms |
|---|---|---|---|---|---|
| 64 | .53,.72 / .73,.35 | +47 | .42,.89 | – | – |
| 72 | .51,.60 / .74,.22 | +47 | .41,.93 (dips) | – | – |
| 78 | grip .29,.75; upper limb through .56,.48 to .98,.25 | ~+70 | fist .29,.75 knuckles up | thumb grabs the upper limb at .76,.46 | L from bottom-left; R enters from right edge v .25-.6 |
| 86 | grip .25,.65; upper wrap .58,.49; tip .76,.31 in the right glove | +70 | .25,.65 | wraps the limb near the tip .76-.84,.27-.33 | L from bottom-left ~35°; R horizontal from right edge v .25-.45 |
| 150-288 | hold, same | +70 | same | same | same |
| 294 | grip .34,.83; lower wrap .46,.54; upper wrap .75,.13 | +50 | .34,.83 | lets go, open glove .86,.45 falling down-right | – |
| 300 | overshoot .49,.73 / .62,.22, grip below screen | +25 | below | gone | – |
| 308 | .53,.70 / .75,.31 | +45 | .43,.90 | – | – |
72-78: the bow pivots round the left fist, rolls ~25° in 6 frames while the fist goes left-up .41,.93 → .29,.75.

## Shot (1067 frames, two shots, walking during the first hold)
Shot 1: draw starts ~40, full draw ~90, hold 90-510, release ~512, idle by ~556. Shot 2: draw 666, full 728, hold
730-900, release 905-910, idle ~940 (draw ~60 frames, right hand enters higher).
| frame | phase | bow | roll | left hand | right hand | arrow |
|---|---|---|---|---|---|---|
| 44 | bow drops, flattens | limb .61,.48 → top edge .79 | flatter | low | coming in | – |
| 52 | nock | low flat: limb .56,.83 → .76,.68 | ~+70 | out of view | glove .64,.41 pinches the nock, tip up-left .54,.32; forearm from right edge | yes |
| 60 | nocking | upper limb .71,.80 → right edge v .40 | ~+65 | .51,.81 | .59,.58 carries the arrow down to the bow; forearm from lower right | tip .51,.45 |
| 70 | draw | limb .60,.65 → right edge v .21 | +60 | .49,.69 | leaving lower right | tip .53,.59 |
| 80 | draw | limb .59,.64 → .94,.08 | +48 | .51,.65 | off screen | tip .54,.58 |
| 96 | full draw | limb .56,.64 → top edge .88; string .59,.97 → right edge v .29 | +41 | .51,.62 | anchor off screen | tip .54,.57 |
| 262-498 | hold/walk | limb .51,.56 → top edge ~.74 | +38 | .51,.55; L forearm enters bottom at u .22, ~45° | off screen (u .62-.65, v ≥ 1.05) | tip on the knuckles .51,.56, shaft exits bottom u .62 |
| 516 | release | slightly more upright | ~+35 | .50,.56 | open glove drops in at ~.7,.9 | gone |
| 526 | lower | lower wrap .56,.50 | +40 | dropping | – | – |
| 540 | lowering | lower wrap .56,.65; tip .81,.06 | +45 | .49,.81 | – | – |
| 556 | idle | as idle | +47 | .46,.86 | – | – |
Release: no visible kick (< .01); bow hand still 6-10 frames, then 30-40 frames down to idle.
Sway while walking at full draw: riser nearly screen-locked (u .51 ± .005, v .54-.56) — no authored sway needed.
Fingers: left hand full fist round the grip, knuckles up toward the camera; right hand pinches the nock with thumb
and index while nocking. String fingers never seen.
