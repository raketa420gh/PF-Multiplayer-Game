# Brief for the footage reader

Fill in the angle-bracket parts and send the whole brief as the agent's prompt. Send the review request later with
`SendMessage` to the same agent.

---

You are the eyes of an animation job. A reference video shows motions that are being rebuilt as generated
animations in a Unity game; someone else writes the poses from your description and cannot see the video. They need
numbers and plain statements they can turn into coordinates, not impressions.

Video: `<path>` — <what the user said it shows, e.g. "all Battle Axe attacks from several sides, captions say which attack and the speed">
Work folder: `<Temp/footage/name>`
Tool: `python <skill>/scripts/footage.py` (`--help` lists the commands). It makes contact sheets; you read them
with the Read tool. A sheet costs about width x height / 750 tokens, so prefer a few well-aimed sheets to many.

What the scan found (video frames; "lit" is where the weapon's hitbox overlay is drawn, i.e. the active phase):

```
<output of footage.py scan>
```

## Do this

1. **Name the takes.** One overview sheet of the whole video (a frame every 20-30) is enough to read the captions
   and tell which take is which motion from which side, and which are slow motion. Write
   `<work folder>/takes.json`:

   ```json
   {
     "video": "<path>",
     "scale": 0.75,
     "crops": {"side": [x0, y0, x1, y1], "top": [...], "front": [...]},
     "takes": {"0": {"side": 572, "top": 720, "front": 425, "length": 147, "skip": 0}, "1": {...}, "r": {...}}
   }
   ```

   Take names: `0`, `1`, `2`... for the swings of a series in order, `r` for a riposte, `block`, `impact`,
   `deflect`, otherwise a short word. Values per view are the first video frame of that take. `crops` are fractions
   of the frame that keep the figure and the whole sweep of the weapon in every frame of the take, roughly square.
   `skip`: when a take starts with the motion already under way (the next swing of a series, cut in right after the
   previous hit), how many video frames of it are missing — compare where the weapon is in the take's first frame
   with where the previous swing's active phase ended. `scale` is 0.75 unless told otherwise.

2. **Describe each motion once**, from its real-time takes; use the slow-motion pass only to settle the strike. For
   one motion read the top view and the side view over the whole take at a step of 4-6 frames (a sheet each, cropped),
   then closer (every 1-2 frames) only around the strike. The front view is a tie-breaker.

## The frame of reference

The character stands at the origin facing +Z; +X is **the character's own right**, +Y up. Work out from the feet
and head which way the figure faces in each view before reading any angle (in a front view its right is on the
left of the picture).

- **Blade yaw**, from the top view: the direction from the grip to the weapon's head. 0 = straight ahead of the
  character, +90 = to its right, -90 = to its left, 180 = straight back.
- **Blade elevation**, from the side view: 0 = level, +90 = straight up, -90 = straight down.
- **Grip** = the main (right) hand: left/centre/right of the body midline with an estimate in centimetres, height
  against the body (knee, hip, belly, chest, shoulder, eye, above the head), and depth (behind the body, at it,
  one forearm ahead, arm's length ahead). Give heights in metres where you can (the figure is about 1.8 m:
  shoulder 1.45, chest 1.30, belly 1.10, waist 1.00) and side and depth in centimetres.
- **Two hands on one haft** keep their order on it unless you actually see a hand let go and slide. Give the off
  hand as a distance along the haft from the grip (toward the head or the butt), not as a free position: positions
  read off a small figure put the lower hand on the head's side of the upper one (impossible) in half the rows of
  the bardiche spec. If the hands seem to swap, say so under "unsure".
- **Elbows** the same way, both of them, read mostly from the front view. For raised arms say only whether the
  elbow is below or above the hand and in front of or behind it — the heights are guesses at that size. **Palm** = where the palm faces (down,
  up, in, out, forward, back). **Torso** = the turn left/right in degrees and the bow forward.

## Report this, per motion

```
### <take name>: <one line — what the weapon does>
timing (video frames from the take's start, plus skip): motion begins N, windup hangs N-N, active N-N (scan says N-N), strike crosses the centre line N, weapon stops N, back at rest N, total N
| frame | phase | blade yaw | blade elev | grip side/height/depth | R elbow side/height/depth | R palm faces | off hand (on the haft: cm from the grip; free: where) | L elbow side/height/depth | torso turn / bow |
|---|---|---|---|---|---|---|---|---|---|
| 0 | start | +65 | +55 | +10 / 1.25 / +20 | +20 / 1.00 / at | in | 40 toward the butt | -20 / 1.05 / at | 0 / 0 |
...
path: which way round the weapon goes between the rows where it is not obvious (over the head / in front of the face / behind the back), where it turns over in the hands, where it lingers and where it whips
arms: what the arms do between the rows that a straight interpolation would miss (the elbow stays low while the hand rises, a forearm rolls palm-up, the arms straighten down to the hip)
unsure: what you could not read, and why (blur, weapon hidden behind the body, view missing)
```

Give 8-14 rows per motion: every moment the weapon changes direction or pace, the start and end of the active
phase, the furthest point of the follow-through, the way back. Angles to the nearest 10-15 degrees are what is
achievable; say so rather than inventing precision, and put real doubt under "unsure". Leave out the scenery, the
character model, the legs unless the user asked for footwork, and anything about how it feels.

End with one paragraph: what is the same across the motions (rest pose, how the off hand sits), since that is
authored once.

---

## First-person footage (replace "The frame of reference" and "Report this" with this)

The footage is the player's own view, so report in **screen fractions** (u right, v down, 0..1, read off the
`footage.py fp VIDEO OUT ROW... --top 0` 10% grid), not in body coordinates — the author converts them with the
game's field of view. 3D guesses from DaD's 90-degree view come out wrong in ours; leave them out. Per key moment:

```
| frame | phase | weapon centre u,v | weapon size w,h | weapon: which face/edge to the camera, roll on screen (deg, 0 = upright), tilt toward/away | book/main hand u,v + what it holds, thumb, fingers, palm | other hand u,v + the same | forearms: from which screen edge, angle |
```

- Fingers per hand per distinct phase: where the thumb lies (on the near face / round the edge / behind), whether
  the fingers are together, spread, curled, wrapped round an edge; which way the palm faces.
- Say when a cut is not a cut: arms filling the screen read as one to the scan.
- Note camera motion (pitch, lunge) separately so it is not taken for hand motion.
- Strikes with the hands (punches, claws): every frame of the active phase, at 1-frame steps. Per frame give:
  - which face of the fist we see and the thumb-to-little-finger line on screen (0 = vertical fist, 90 =
    horizontal, palm down);
  - the forearm (glove cuff) screen angle and length;
  - whether the elbow is above, level with or below the fist;
  - the u range where the sleeve meets the bottom edge (that is the shoulder's sweep);
  - where the fist turns over and how it leaves the view.
- The same strike repeated: describe the cleanest one, give the start frames of the others.
- The rest pose seen at the start of every video: describe it once.

## Review request (send later, same agent)

> The poses are in. `<work folder>/cmp_<take>.jpg` puts video frames (top row of each pair, `v` = video frame) above
> the game's render of the same moment from the same side (`c` = clip frame). For each of: <sheets>, list only
> where the weapon differs from the video by more than about 15 degrees, or a hand, an elbow or the torso by more
> than about 10 cm / 15 degrees, or a forearm looks wrung, as `c<frame>: video <what>, game <what> → <which way to
> move it>`. At most 8 lines per sheet, worst first; "matches" if nothing qualifies; end each sheet with one line:
> do the arms read natural or twisted next to the video. Check an arm on the full-size `g_<take>_<view>_<frame>.png`
> before reporting it: the tiles are small. The mannequin, the lighting, the floor and the legs are not part of
> the comparison.

For hand strikes add: "The game renders have flat fingers and no sleeves: an open-looking hand is the fist. Judge
it by the back of the hand and the forearm line, not the finger shape." Without this, the reader reports every
fist as an open palm.
