# Sourcing CC0 layers

Only CC0 recordings go into the game (the user chose real recordings over synthesis; fal.ai audio is not
configured). OpenGameArt is the source: direct downloads, no login, licence on the page.

## Search

```bash
curl -sL "https://opengameart.org/art-search-advanced?keys=<words+joined+by+plus>&field_art_type_tid%5B%5D=13&field_art_licenses_tid%5B%5D=4&sort_by=count&sort_order=DESC&items_per_page=48" \
  | grep -o 'href="/content/[^"]*"' | sort -u
```

`field_art_type_tid[]=13` is Sound Effect, `field_art_licenses_tid[]=4` is CC0. Then per page:

```bash
curl -sL https://opengameart.org/content/<slug> | grep -o 'https://opengameart.org/sites/default/files/[^"]*' \
  | grep -v -E '/(css|js|styles|license_images)/'
```

Check the licence block on the page says CC0 (some pages mix CC0 and OGA-BY files - take only CC0 ones).

## Download and unpack

Each pack goes into its own folder under `D:/DaD_Sounds/_sources/<pack>/`. Archives: `.zip` with Python
`zipfile`, `.7z` with `py7zr` (no 7z/ffmpeg on PATH). Run Python that reads downloaded files with `-I`.

```bash
py -I -c "import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])" pack.zip D:/DaD_Sounds/_sources/pack
py -I -c "import py7zr,sys; py7zr.SevenZipFile(sys.argv[1]).extractall(sys.argv[2])" pack.7z D:/DaD_Sounds/_sources/pack
```

Then `py -I scripts/analyze.py "D:/DaD_Sounds/_sources/<pack>/**/*.wav"` to see which clips fit which layer.

## Packs already in `_sources`

| folder | page | what it is good for |
|---|---|---|
| `fantozzi/` | fantozzis-footsteps-grasssand-stone | footsteps on stone / sand (flac + ogg; use flac). Spectrum peaks 1.5-4 kHz - close to DaD steps |
| `bones_rattle/` | bones-rattle | ten 70-150 ms dry bone clicks, 1.5-10 kHz - skeleton rattles, bony attacks |
| `deep_breaks/` | deep-bone-crackbreak-sfx | 0.6-0.9 s bone cracks, 150-1500 Hz heavy - bone breaks, heavy skeleton hits |
| `footsteps/` | footsteps-leather-cloth-armor | 0.25 s leather / cloth / metal steps, quiet (-26 dB peak) |
| `single/bones-2.wav` | bones-2 | 1.1 s bone rummage |
| `single/skeleton1sfx_0.wav`, `skeleton2sfx_0.wav` | skeletonsfxshortpack | skeleton noises with low growl content |

Good packs used earlier (re-download when needed): Medieval SFX Weapon Textures (192 kHz real swing foley; slice
by band-energy peaks, not silence), Medieval SFX weapon-on-weapon (clashes), qubodup Impact Meat, Wet Squish
Impacts, metal-footsteps-on-concrete, 42-snow-and-gravel-footsteps, hits_and_punches, wood_crack_hit.

## Picking layers from the reference

Read `analyze.py` output of the DaD reference and split it into parts:

- **Attack** (first 10-20 ms): what makes the click / thud - its band profile says which material.
- **Body decay**: how fast the main hit falls (dB per 40 ms) - `snap.tau` copies it.
- **Secondary events**: later onsets that rise out of the floor (rattles, debris, a second footfall) - a `count` layer
  with `at_ms` / `gap_ms` / `until_ms` from the onset times, gains from how far they sit below the main peak.
- **Floor / tail**: the level the envelope settles at between events - `room.db` and `room.decay_ms`.

Pick a source for each part whose band profile is closest; EQ (`hp`, `lp`, `shelf`, `match`) only trims what is
left.
