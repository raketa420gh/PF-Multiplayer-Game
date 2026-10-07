---
name: video-to-animation
description: Turn reference video footage (usually Dark and Darker, first person) into this project's generated animations — weapon swings, riposte, block, idle, and first-person action clips (interacting/searching, drinking, bandaging, casting, picking up) — so that they match the footage in timing, weapon path, screen placement of the hands, finger shape and the arms and torso that carry it. Use whenever the user gives a video file (mp4 and the like) and asks to make, redo or match an animation from it — "сделай анимацию как в видео", "реализуй анимации из видео", "сделай идентичные", a path like D:\something_animations.mp4 next to a weapon or action name — even if they do not say "skill". Also use when asked to compare an existing animation with footage.
---

# Video to animation

The user gives a video and says what it is for ("Battle Axe attacks", "block with a longsword"). The outcome is
re-authored key poses and timings in the animation library, rebuilt clips and configs, a play test, and a short
report in Russian. Animations here are generated from code, never keyed by hand in the clips.

The weapon is half of it. A swing whose blade follows the footage but whose elbows the solver picks on its own
reads as wrong: raised "chicken wing" elbows, hands tucked to the chest, forearms wrung. Grips, elbows, the off arm
and the torso turn come from the footage too, in the same spec and the same review.

## Which job

- **A first-person action** (no weapon swing: interaction, search, drink, bandage, cast, pick up...) → follow
  `references/fp-actions.md` alone, then step 7 here. Screen placement and finger shape are the whole job; the
  footage-reader agent, `takes.json` and the full rebuild are not needed.
- **Weapon motions** (attacks, riposte, block, idle of a weapon) → the steps below.

The user records first-person footage only. For weapon motions that means the swing's 3D path is inferred from
the screen, and the screen is the spec:

- Brief the reader with the first-person variant in `references/footage-reader.md` (screen fractions, fingers).
- Convert screen fractions to root space with the formula in `references/fp-actions.md` (our 75-degree vertical
  view, not DaD's 90 horizontal); an object of known size (a book 0.17 m wide) gives the depth.
- Compare with `footage.py fp VIDEO OUT f:render... --top 0 --mirror` (the mirror for weapons the game plays
  mirrored, `--top 0` or v is off), renders from `SwingPreview` view `fp`. Those renders have flat fingers and no
  HUD: hand shape and the final placement are judged on one play-test screenshot (step 6), never on the rig.
- Elbows: on screen the forearms rising from the bottom corners mean elbows *behind* the hands along the knuckles,
  not out to the sides — out to the sides reads as raised "chicken wings" and bends the wrists past 85.
- When the user describes a body pose for a first-person motion (an idle "arm reached forward, other hand low"),
  the pose that reads right from outside can leave the view; ask the trade-off once, with options, before authoring.

## Why the steps are shaped the way they are

The cost of this job is (number of turns) x (size of the context), plus the pictures. The first run of it took
roughly 200 turns, 45 contact sheets and 5 000 lines of pipeline code read just to learn the API. So:

- **Numbers before pictures.** Timings come from `footage.py scan`, pose problems from `SwingPreview` — both are
  text. A picture is for what numbers cannot say: the shape of the path.
- **The pictures stay out of this context.** A footage-reader agent looks at the video and at the comparisons and
  answers in text. Read a sheet yourself only for the final sign-off, one per swing.
- **Do not read the pipeline code whole.** `references/pipeline.md` is the API on one page. Read the weapon's own
  `Create<Weapon>()` and nothing else unless something there contradicts the page.
- **One round = four calls.** Edit poses, compile, `SwingPreview.Run` for every swing at once, `footage.py compare`.
  Batch every swing into each call; do not go swing by swing.
- **One full rebuild, at the end.** It takes 5-8 minutes. Poses are iterated on the rig without building anything.
- **One weapon per session.** A second weapon in the same session pays for the first one's context on every turn.

## Steps

Work folder: `Temp/footage/<name>/` (Unity wipes `Temp` when the editor closes — nothing there is meant to last).
`$SKILL` below is this skill's folder, `.claude/skills/video-to-animation`.

### 1. Scope, before touching the video

Settle from the request, asking only if it truly cannot be inferred: which weapon (its prefix in
`DungeonWeaponLibrary`), which motions, and whether a motion in the footage has no counterpart in the game (a new
mechanic is the user's call on rules and numbers — build it with a sensible default and flag the default in the
report). Then two checks that are cheap now and expensive later:

- `python $SKILL/scripts/edits.py <Prefix>` lists the clips of that weapon the user has edited by hand in the
  animation editor. A rebuild bakes such an edit back onto the new clip and wrecks it, so an edit of a clip you
  are about to re-author is in the way — and it is the user's work: name it in the report whatever you do with
  it. See "Pitfalls".
- `git status --short` — know what was dirty before you started, so the report can say what is yours.

### 2. Scan the footage (no pictures)

```bash
python $SKILL/scripts/footage.py scan "<video>" Temp/footage/<name>
```

It prints the takes (cut to cut), where the weapon's debug hitbox is lit in each (= the active phase, when the
footage has that overlay), the frame of the fastest motion, and which runs look like slow motion. From this alone
you usually have windup and active length per take in video frames.

### 3. Hand the video to a footage reader

Spawn one agent (general-purpose, in the foreground — the next step needs its answer) with the brief in
`references/footage-reader.md`, filled in with the video path, the work folder, the scan output and what the user
said the footage shows. It names the takes, writes `takes.json`, and returns a motion spec per take: a table of key
moments with the weapon's direction in degrees, where the grip is, where both elbows are, which way the palms face,
where the off hand is and how far the torso turns and bows. Keep the agent's id: the same
agent reviews the comparisons later through `SendMessage`, with the footage already in its context.

If the spec comes back vague where it matters (no angles, "swings to the left"), ask the reader for that take again
with a narrower question rather than opening the video yourself.

### 4. Author

Read `references/pipeline.md`, then the weapon's `Create<Weapon>()`. Convert timings (video frames x 0.75 for
attacks and blocks, x 1 for busy clips), write the key poses from the spec, and keep what the footage does not
contradict (damage, block numbers, the peak-on-crosshair rule).

- Take the active phase from where the hitbox is lit, across all views of the take (the scan prints it); a windup
  that ends 5-10 frames late reads as a lagging strike even when every pose is right.
- Write every key with `PoseAt(grip, yaw, elevation, torso, pitch, offHand, elbow, offElbow)`: the angles come
  straight from the spec, the grip from its side/height/depth (z = 0.1 + depth ahead of the body), the elbows the
  same way. An elbow only has to be on the right side of the arm, not at arm's length.
- Hands far from the body in the footage are far from the body in the keys: an arm "nearly straight forward" is a
  grip about 0.65 m from the shoulder, not the 0.45 m that reads as tucked.
- A strike with the butt or the pommel gets `AttackDefinition.Strike` negative; a staff or another round haft gets
  `WeaponDefinition.IsRound` (no edge to lead with).
- Keep every grip within reach before anything else: the palm reaches about 0.73 m from the shoulder
  `(0.17, 1.47, -0.06)`; a peak grip at 0.79 m left the hand 15 cm short and the swing spinning. "Main hand short"
  in the preview is this.
- A thrust: put the windup, the peak and the end grip on one line along the direction the strike point is aimed at
  the peak (elevation = asin((1.731 - grip y) / strike), about 20 degrees for a chest-high grip and a 1.45 m point)
  and give the windup and end that elevation. Otherwise the point travels across the blade, the swing counts as a
  cut and its edge spins to follow it.
- Around the peak of a cut keep the neighbouring keys at the peak's re-aimed elevation (read it in the report): a
  flat sweep whose peak is lifted 15-25 degrees onto the crosshair gets a hump, the edge turns with the path and the
  weapon spins past the 25-degree limit. Do not let the end pose run back against the swing either (a yaw that
  swings -25 then returns to -12 at the follow-through spun the riposte 33 degrees a frame).
- Overhead, the elbow goes below and behind the hand: an elbow level with or ahead of a raised hand makes the
  forearm point back against the leading edge — a wrist bent 150 degrees. The footage reader's elbow heights for
  raised arms are the least reliable numbers of the spec.
- A new weapon is wired in `DungeonWeaponLibrary` (const, `CatalogOrder` at the end, `CreateAll`, `s_force`),
  a `DungeonWeaponPrefabBuilder.Build<Weapon>()` (`Pole`, `WeaponParts.Grip/Band`, `mesh.Plate` in the YZ plane
  with the edge on +Y), the attachment in `BattleContentBuilder`, the item at the end of `DungeonItemLibrary`
  (item ids are list positions) and the loot/table lists. `SwingPreview` gives numbers before the first rebuild,
  renders the weapon only after it.

### 5. Iterate on the rig

Compile (`refresh_unity`, compile requested, wait), then in one `execute_code`:

```csharp
var t = System.Type.GetType("Game.Scripts.Editor.Battle.SwingPreview, Assembly-CSharp-Editor");
return t.GetMethod("Run").Invoke(null, new object[] { "<Prefix>", "0,1,2,r", "top,side", "", "Temp/footage/<name>", 256 });
```

Arguments: prefix; swings (`0..n`, `r`, `block`, `impact`, `deflect`); views (`fp,front,side,top`, or `""` for
numbers only); clip frames (`""` = the frames of the keys); folder; tile size. The return value lists per swing
what is within limits and what fails: peak off the crosshair, edge off the cut, wrists past 90 degrees, weapon
spinning within a frame, a hand that cannot reach its target, an elbow off the authored side, and the arm's twist
muscles. Fix every FAIL of the first five before spending a picture on the swing — a failing pose is going to change
anyway, and the build enforces those (peak, edge, roll). The twist muscles (`Arm/Forearm Twist In-Out`, past 1 the
avatar clamps the roll and the forearm reads as wrung) are what makes arms look twisted in the game, not in the rig
render: bring them down where the spec allows, accept what a pose of the footage needs, and say where in the
report. `report_<swing>.txt` in the folder has the per-frame table
(grip position, blade yaw/elevation) for when a number in the spec needs checking against the pose.

Whenever the rig or a shared pose changes, run `SwingPreview` for the affected weapons into a `base` tree before
and a `new` tree after (`<folder>/<Prefix>`), then `python $SKILL/scripts/rigstats.py base new`: frames with a
broken wrist, with a wrung forearm and the worst elbow jerk per swing, base against new. A change to the rig that
wins one of these and loses another is a trade, not a fix — say which in the report.

Then compare, all views of a swing in one sheet:

```bash
python $SKILL/scripts/footage.py compare Temp/footage/<name>/takes.json <take> front,side,top
```

The front view is where elbows and palms show; leave it out only while the weapon path alone is being settled.

Send the reader the sheet paths with `SendMessage` and ask for deviations only — of the weapon, the arms and the
torso together (the review request in `references/footage-reader.md`). Sheet tiles are about 190 px: when the reader
reports an arm the numbers in `report_<swing>.txt` contradict (elbow columns), ask it to check the full-size
`g_<swing>_<view>_<frame>.png` before changing anything. Apply, repeat. Two or three rounds
is normal; if a fourth is needed for the same swing, the keys are probably fighting the interpolation — add a key
at the moment that keeps drifting instead of pushing its neighbours further.

Before the rebuild: one `fp` strip per swing (`SwingPreview` with view `fp`, then `footage.py strip`) to see what
the player sees, and one comparison sheet per swing read by yourself as the sign-off.

### 6. Rebuild and verify

`execute_menu_item` `Tools/Game/Battle/Rebuild Animations And Content` reports "disconnected" after ~30 s and keeps
running; poll the timestamp of `Assets/Game/Prefabs/Battle/ShieldDummy.prefab` (saved last) from a background
shell command, then read the console for `[BattleContentBuilder]` errors — none is the pass mark.

Training table, every time: the weapon's display name must be in `BattleSceneBuilder.s_tableItems` (the items
laid out on the table in `BattleScene`). If it is missing, add it before the last weapon-like entry (`"Round
Shield"`), then `Tools/Game/Dungeon/Build Content` if the item asset is new, then `Tools/Game/Battle/Build Scene`
— do this before the play test, since it rebuilds the scene. Already there: nothing to do, say so in the report.

Play test in `BattleScene` (the sandbox never saves the player's kit): copy `$SKILL/assets/SwingPlayTest.cs` into
`Assets/Game/Scripts/Editor/Battle/`, compile, open the scene, enter play mode, wait ~12 s for the session, call
`SwingPlayTest.Begin("<Display Name>", "combo" | "riposte" | "block", "Temp/footage/<name>")` by reflection, wait for
it to finish (`until [ -f play_<mode>.txt ]` in a shell, not sleeps), read `play_<mode>.txt`. "block" logs how each
weapon part stands in the view — the answer to "it stands crooked" in numbers. Open the scene and enter play mode in
two separate calls; "no local fighter yet" past ~20 s means the session never started: stop, enter again, retry.
Stop play, delete the copied file and its `.meta`, compile again, reopen the scene that was open before.

### 7. Report (Russian, terse)

- A table: motion, what it does in one line, windup / active / total in video frames.
- What you decided yourself (rules, numbers) and want confirmed.
- What does not match the footage or looks wrong (failed-then-accepted limits, first-person visibility).
- Anything of the user's you removed or overwrote, and where the old version is.
- How the user checks it: the clip asset and the state name in `Fighter.controller` to open in the animation editor.

Then commit (the user asked for it): only the files this job changed — library/builder/view code, the new or
re-recorded clips with their `.meta`, `Fighter.controller`, configs a rebuild changed. Leave what was dirty before
step 1 alone (`git status` from the start). Use the `commit-push` skill's message style (`Area: what changed`) but do
not push unless asked.

- Add files by name or by a glob checked against `git status` first: `Animations/Battle/*.anim` swept the user's
  untracked clip into a commit once.
- A file the user had dirty before you (a shared library) gets only your hunks: write `git show HEAD:<file>` to
  `Temp/`, apply your edit there with the same anchored patch, `git hash-object -w --no-filters` it and
  `git update-index --cacheinfo 100644,<hash>,<file>`.
- `Fighter.controller`, `Bow.prefab`, `Fighter.prefab` come out of every rebuild with fileID churn: leave them out
  unless the job changed states.

## What makes the footage cheap to read

Say this to the user when it helps; it is their recording.

- A top view and a side view fix the weapon's direction completely; the front view adds almost nothing.
- The hitbox debug overlay gives the active phase to the frame without anyone looking at it.
- One take per motion, hard cuts between takes, the camera still within a take, 60 fps.
- A slow-motion pass is only worth it for the strike itself.
- Named takes in the request ("0:07 attack 1 front, 0:09 side...") save the overview pass.

## Pitfalls

- **Hand edits.** `BattleAnimationBuilder.BakeEdits` re-applies every entry of `AnimationEdits.asset` after a
  rebuild. Bone keys made for the old clip turned the first re-authored swing into a 103-degree spin with the peak
  0.2 m off the crosshair. An entry for a clip being replaced has to go (remove it from `_clips`, restore the clip
  from its `Animations/BattleSource` copy or rebuild) — with the user told.
- **Chained swings start where the previous one ends.** Changing swing N's end pose changes swing N+1's first
  frames; preview them together.
- **The peak pose is not the one you author.** The blade is re-aimed so the strike point sits on the crosshair and
  the grip is pulled toward the aim line. Read the peak frame in `report_<swing>.txt` instead of assuming.
- **The wrist limit is real.** A blade more than ~45 degrees ahead of the forearm in the direction of travel bends
  the wrist past 90; put the grip on the side the cut is heading to.
- **The editor in the background runs play mode at ~4 fps.** Read timing from `combat.StateTime` in the log, never
  from wall-clock gaps between screenshots.
- **Never edit a script while in play mode** — the reload kills the Fusion session.
- **Never edit a script while the rebuild runs** — the reload lands in the middle of it.
- **Patch scripts must anchor on unique text near the edit.** `s.index('After = chop')` found `After = chopLeft` of
  another weapon earlier in the file and duplicated half of `DungeonWeaponLibrary`. Search from the start of the
  block (`s.index(marker, start)`) and assert the count.
- **Rig constants by reflection.** To tune a cost in `BattlePoseRig`, make it `static` for a moment, set it through
  reflection inside one `execute_code` that runs every weapon into its own folder, compare with `rigstats.py`, and
  make it `const` again. Long runs time out the call but still finish — the folders are the result.
- **Footage specs contradict themselves at times** (a palm "down" on a haft whose head is on the thumb's side, an
  elbow 10 cm from its hand). Where the pose cannot be held, keep the weapon and the elbow's side, and let the
  twist and wrist numbers say how far off it is.
- **A non-blade held across** (a book, a board): set `WeaponDefinition.IsRound` or `LeadWithEdge` rolls it like a
  blade and the peak lies flat; give the main hand `Main.Up` explicitly (as the off hand has) or the solver trades
  the wrist for the roll and the elbow jumps 80 degrees off.
- **A mirrored weapon's mesh axes.** The game hangs the prefab in the left socket with x-scale -1: turning a mesh in
  the builder (`WeaponMesh.Matrix`) by +a shows as -a in the hand. Check the sign on the numbers of one vertex
  transform before the first render; three render rounds went into guessing it.
- **`Renderer.bounds` is not a shape.** It is the box around the turned local box: a straight book read as 45
  degrees off. Measure axes (`SwingPlayTest` "block", or transform the mesh vertices).
- **"It looks crooked" with the numbers straight**: look for geometry the eye reads as a turn — a spine bulging past
  the boards showed its round side in the view and read as a book turned sideways.
- **The animation editor shows exactly what the game shows**, mirrored weapons (Spellbook, SwordShieldLeft) and flipped
  states (off-hand swings, Use) included: it plays them mirrored, baked, read-only ("mirrored in the game"). Unity's
  humanoid mirror is exact for the body (1 mm) and `SocketMirror` for the weapon, but it is a muscle mirror, not a bone
  reflection: edits made on a mirrored view cannot be carried back onto the authored clip, so do not try. If the editor
  and the game still differ, the editor is wrong, not the clip: compare one numeric pose in both before re-authoring.
- **"In the game it plays something else" on a block hit**: check `Impact` against the block's `Stability`
  (`DungeonWeaponLibrary.s_force`). A hit with more Impact breaks the block — Stagger: the idle clip and the stagger
  flinch, never `BlockImpact`. A book with Stability 1 never showed its impact clip against a weapon.
- **Judge a mirrored weapon mirrored.** `SwingPreview`/`ClipPreview` render the authored (right-handed) side: compare with
  `footage.py ... --mirror`; for "which hand, where on the screen" the play-test or animation-editor screenshot is the truth.
- **Shared poses** (rest, the low stop on one side, upright on the other) are reused by several swings. When one
  swing needs it different (the head up at the start of the next swing rather than down at the end of the last),
  give that swing its own key instead of bending the shared pose.
