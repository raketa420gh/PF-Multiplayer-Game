"""Lists the clips that carry the user's hand edits (Configs/Battle/AnimationEdits.asset): a rebuild bakes them back
onto whatever the generator makes, so an edit of a clip that is being re-authored is in the way.
Run from the project root; an argument narrows the list to clip names containing it."""
import glob
import io
import math
import re
import sys

BONES = {13: 'left upper arm', 14: 'right upper arm', 15: 'left forearm', 16: 'right forearm', 17: 'left hand', 18: 'right hand'}
names = {}

for meta in glob.glob('Assets/Game/Animations/**/*.anim.meta', recursive=True):
    names[re.search(r'guid: (\w+)', open(meta).read()).group(1)] = meta.replace('\\', '/')[:-5].split('/')[-1]

text = io.open('Assets/Game/Configs/Battle/AnimationEdits.asset', encoding='utf-8').read()
wanted = sys.argv[1] if len(sys.argv) > 1 else ''

for part in re.split(r'\n  - _clip: ', text.split('\n  _weapons:')[0])[1:]:
    clip = names.get(re.match(r'\{fileID: \d+, guid: (\w+)', part).group(1), 'unknown clip')

    if wanted not in clip:
        continue

    tracks = []

    for track in re.split(r'\n    - _bone: ', part)[1:]:
        keys = re.findall(r'_frame: (\d+)\n\s+_rotation: \{x: [-\de.]+, y: [-\de.]+, z: [-\de.]+, w: ([-\de.]+)\}', track)
        turn = max((2 * math.degrees(math.acos(min(1.0, abs(float(w))))) for _, w in keys), default=0.0)

        if turn > 5:
            bone = int(track.split('\n')[0])
            tracks.append('%s%s %d keys up to %.0f deg' % (BONES.get(bone, 'bone %d' % bone), ' socket' if '_isSocket: 1' in track else '', len(keys), turn))

    grips = len(re.findall(r'\n    - _hand: ', part))
    print('%s: %s%s' % (clip, '; '.join(tracks) or 'no turn above 5 deg', ', %d grip(s)' % grips if grips else ''))
