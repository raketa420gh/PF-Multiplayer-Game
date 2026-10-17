---
name: dad-sound-alike
description: Make original game sounds that sound like a given Dark and Darker sound — pull the reference out of the user's DaD install (Wwise event → .wav), measure it, then layer CC0 real recordings to a JSON recipe until the numbers match, and hand over 10-12 takes. Use whenever the user names a DaD sound or event (e.g. "Monsters_Skeleton_footsteps", "звук двери из Dark and Darker") and wants it extracted, analysed, or a similar one generated — "вытащи звук из игры", "сделай похожий звук", "сгенерируй как в DaD" — even if they do not say "skill". Also use to extract a DaD sound only for listening.
---

# DaD sound-alike

DaD audio is reference only: never ship extracted files. The deliverable is new takes built from CC0 recordings
whose length, envelope and spectrum match the reference, and a short report in Russian.

Everything lives under `D:/DaD_Sounds/`: `<Event>/` references, `_sources/<pack>/` CC0 recordings,
`Generated/<name>/` takes, `_tools/` (Oodle, vgmstream, pak indexes - downloaded/built on first use).
Scripts are in `scripts/`; run them with `py -I` (system Python 3.13 has numpy, scipy, soundfile, py7zr).

## 1. Find and extract the reference

```bash
py -I scripts/dad_audio.py search <part of name>     # e.g. skeleton_foot, door, zombie
py -I scripts/dad_audio.py extract <EventName>       # -> D:/DaD_Sounds/<EventName>/<Event>_<n>_<mediaId>.wav
```

The user's name may be the Unreal asset name or the Wwise event name; `search` shows the asset names `extract`
takes. What the script does and where it can fail:

- The event uasset (IoStore, Oodle) lists its bank and `Media/<id>.wem` ids. `_<n>` is the order of that list,
  not the original `<name>_<n>.wav` numbering.
- The `.pak` index is AES-encrypted, so media are matched by the size the bank's HIRC records. Same-size
  candidates are ranked by the pak's lexical file order (ids are hashes, so position interpolates); losers go to
  `_alternates/`. Say so in the report when alternates exist - the user may need to listen to them.
- `MISSING <id>`: no bank describes that media (a stale reference) - ignore unless it was the only one.
- After a game update the cached pak indexes rebuild themselves (keyed on pak mtime).

## 2. Measure it

```bash
py -I scripts/analyze.py D:/DaD_Sounds/<EventName>
```

Per file: length, peak, six band energies, onsets, 5 ms envelope every 20 ms; the group's mean band profile is
the `target`. Describe the sound to yourself in parts (attack material, body decay speed, secondary events and
their timing/level, floor/tail) - see "Picking layers" in `references/sourcing.md`.

## 3. Get sources

Look in `D:/DaD_Sounds/_sources/` first (table in `references/sourcing.md`); fetch more CC0 packs from
OpenGameArt the way that file shows. `analyze.py` on candidate clips tells which band profile fits which part.

## 4. Write the recipe and build

Copy `recipes/skeleton_step.json` (the worked example: stone heel + bone click + 1-3 rattles + room tail) to
`recipes/<name>.json` and edit. Fields:

| field | meaning |
|---|---|
| `length_ms`, `variants`, `seed` | clip length; take count; variant v uses seed+v (same recipe → same files) |
| `target` / `reference` | band profile to EQ toward; `reference` dir computes it when `target` is absent |
| `layers[].src` | glob under `_sources` (or recipe `sources`); one file drawn per hit |
| `at_ms`, `gain_db`, `pitch` (semitones) | `[lo, hi]` ranges; equal ends = fixed |
| `hp`, `lp` | Hz, 2nd order |
| `snap.hold`, `snap.tau` | ms: keep the attack, then exponential decay - shortens long bodies to the reference's decay |
| `max_ms` | crop the layer |
| `count`, `gap_ms`, `until_ms` | repeated hits (rattles, debris): count range, spacing, last start time |
| `chance` | probability the layer appears in a take |
| `shelf` {hz, db}, `room` {db, decay_ms, len_ms, band} | high shelf; noise-convolution room tail |
| `match` | 0..1 strength of the FFT EQ toward `target` (0.6 keeps takes distinct) |
| `fade_ms`, `peak_db` | tail fade, final peak |

```bash
py -I scripts/layer_build.py recipes/<name>.json            # -> D:/DaD_Sounds/Generated/<name>/<name>_NN.wav
py -I scripts/analyze.py D:/DaD_Sounds/<EventName> D:/DaD_Sounds/Generated/<name>
```

Iterate on the recipe until, versus the reference: bands within ~3-4 dB on average, the main hit decays at the same
dB per 40 ms, secondary onsets land in the same window at a similar level below the peak, and the floor/tail sits
at the same level. Fix the source choice first, then timing/gains, then EQ - EQ cannot turn wood into bone. You
cannot listen: report the numbers, and let the user's ears decide.

## 5. Hand over

Report in Russian: where the reference and takes are, layer breakdown, the comparison numbers, which takes deviate
(thinner / heavier) and any `_alternates`. Ask (AskUserQuestion) what next: import, brighter/heavier, more takes.

On "import": copy takes into `Assets/Game/Audio/...` next to related sounds (e.g. monster sounds in
`Assets/Game/Audio/Dungeon/Monsters/<Monster>/`), PascalCase names (`Step_01.wav`). Unity makes the `.meta` on
refresh with the default importer. Do not wire them into configs unless asked; when asked, the existing wiring
lives in `Assets/Game/Scripts/Editor/Battle/CombatSoundBuilder.cs` (weapon sets, footsteps by footwear).
