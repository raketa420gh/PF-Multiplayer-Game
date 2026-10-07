"""Reference footage tools of the video-to-animation skill. Needs opencv-python-headless and numpy.

  scan    VIDEO DIR                      cuts, takes and lit-hitbox runs as numbers -> DIR/scan.json (no pictures)
  sheet   VIDEO START END STEP OUT       contact sheet of video frames, numbered with the video frame
  compare TAKES.json TAKE VIEWS [FRAMES] video above game at matching moments -> cmp_TAKE.jpg beside TAKES.json
  strip   DIR TAKE VIEW [FRAMES]         game renders alone (first-person checks) -> DIR/strip_TAKE_VIEW.jpg
  fp      VIDEO OUT ROW [ROW...]         first-person footage with a 10% screen grid; ROW = video frame, or
                                         frame:render.png (ClipPreview shot) to set the game beside it

FRAMES are clip frames, comma separated; left out = every g_TAKE_VIEW_*.png that SwingPreview rendered.
A picture costs roughly width*height/750 tokens, so the defaults keep sheets small; raise --cell only to settle a doubt.
"""
import argparse
import glob
import json
import os
import re

import cv2
import numpy as np


def label(img, text):
    cv2.putText(img, text, (4, 16), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 0, 0), 3)
    cv2.putText(img, text, (4, 16), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 255), 1)

    return img


def grid(tiles, cols):
    rows = []

    for i in range(0, len(tiles), cols):
        row = tiles[i:i + cols]
        row += [np.zeros_like(tiles[0])] * (cols - len(row))
        rows.append(np.hstack(row))

    return np.vstack(rows)


def cut(img, box):
    h, w = img.shape[:2]

    return img[int(box[1] * h):int(box[3] * h), int(box[0] * w):int(box[2] * w)] if box else img


def fit(img, width, height=None):
    return cv2.resize(img, (width, height or int(img.shape[0] * width / img.shape[1])), interpolation=cv2.INTER_AREA)


def lift(img, gamma):
    lut = np.array([((i / 255.0) ** (1.0 / gamma)) * 255 for i in range(256)]).astype('uint8')

    return cv2.LUT(img, lut)


def grab(cap, frame):
    cap.set(cv2.CAP_PROP_POS_FRAMES, frame)
    ok, img = cap.read()

    if not ok:
        raise SystemExit('no video frame %d' % frame)

    return img


def save(path, img):
    cv2.imwrite(path, img, [cv2.IMWRITE_JPEG_QUALITY, 86])
    print('%s %dx%d ~%d tokens' % (path, img.shape[1], img.shape[0], img.shape[1] * img.shape[0] // 750))


def runs(frames, gap=2):
    out = []

    for f in frames:
        if out and f - out[-1][1] <= gap:
            out[-1][1] = f
        else:
            out.append([f, f])

    return out


def scan(a):
    """A take runs from one cut to the next. Its lit runs are where the debug overlay of the weapon's hitbox is drawn
    (bright green), which is the active phase; 'fastest' is the frame of the most motion, a hint at the peak when
    there is no overlay. A run several times longer than the rest is a slow-motion pass."""
    cap = cv2.VideoCapture(a.video)
    fps = cap.get(cv2.CAP_PROP_FPS)
    ignore = [float(v) for v in a.ignore.split(',')] if a.ignore else None
    prev, diffs, lit = None, [], []

    while True:
        ok, img = cap.read()

        if not ok:
            break

        small = cv2.resize(img, (320, 180), interpolation=cv2.INTER_AREA).astype(np.int16)
        diffs.append(0.0 if prev is None else float(np.abs(small - prev).mean()))
        prev = small
        b, g, r = (img[:, :, i].astype(np.int16) for i in range(3))
        mask = (g > 170) & (r < 140) & (b < 140) & (g - r > 80)

        if ignore:
            h, w = mask.shape
            mask[int(ignore[1] * h):int(ignore[3] * h), int(ignore[0] * w):int(ignore[2] * w)] = False

        lit.append(int(mask.sum()))

    count = len(diffs)
    cuts = [0] + [f for f in range(1, count) if diffs[f] > a.cut] + [count]
    takes = []

    for start, end in zip(cuts, cuts[1:]):
        if end - start < a.shortest:
            continue

        # Captions and HUD of the same green are there on every frame of the take: only what comes on top counts.
        still = float(np.percentile(lit[start:end], 20))
        inside = runs([f for f in range(start, end) if lit[f] - still > a.lit])
        fastest = max(range(min(start + 3, end - 1), end), key=lambda f: diffs[f])
        takes.append({'start': start, 'end': end - 1, 'length': end - start, 'fastest': fastest - start,
                      'lit': [[run[0] - start, run[1] - start] for run in inside]})

    lengths = sorted(run[1] - run[0] + 1 for t in takes for run in t['lit'])
    usual = lengths[len(lengths) // 4] if lengths else 0
    print('%d frames at %.0f fps, %d takes (frames below count from the start of a take)' % (count, fps, len(takes)))

    for i, t in enumerate(takes):
        spans = ['%d-%d (%d%s)' % (run[0], run[1], run[1] - run[0] + 1, ', slow motion?' if run[1] - run[0] + 1 > usual * 2.5 else '')
                 for run in t['lit']]
        print('take %2d: video %4d-%4d (%3d) | fastest %3d | lit %s' % (i, t['start'], t['end'], t['length'], t['fastest'], ', '.join(spans) or 'none'))

    os.makedirs(a.dir, exist_ok=True)
    json.dump({'video': a.video, 'fps': fps, 'frames': count, 'takes': takes}, open(os.path.join(a.dir, 'scan.json'), 'w'), indent=1)


def sheet(a):
    cap = cv2.VideoCapture(a.video)
    box = [float(v) for v in a.crop.split(',')] if a.crop else None
    tiles = [label(lift(fit(cut(grab(cap, f), box), a.cell), a.gamma), str(f)) for f in range(a.start, a.end + 1, a.step)]
    save(a.out, grid(tiles, a.cols))


def rendered(folder, take, view):
    found = [int(re.search(r'_(\d+)\.png$', f).group(1)) for f in glob.glob(os.path.join(folder, 'g_%s_%s_*.png' % (take, view)))]

    return sorted(found)


def compare(a):
    """TAKES.json: {"video": path, "scale": 0.75, "crops": {view: [x0, y0, x1, y1]},
    "takes": {take: {view: first video frame of the take, ..., "skip": n, "length": n}}}.
    Clip frame c is video frame start + c / scale - skip: scale is the pace the game plays clips at, skip is how
    many video frames of the motion had gone by before the take was cut in (a swing chained to the one before).
    Length keeps a clip that runs longer than its take from showing the next one."""
    spec = json.load(open(a.takes))
    folder = os.path.dirname(os.path.abspath(a.takes))
    take = spec['takes'][a.take]
    cap = cv2.VideoCapture(spec['video'])
    blocks = []

    for view in a.views.split(','):
        frames = [int(f) for f in a.frames.split(',')] if a.frames else rendered(folder, a.take, view)
        above, below = [], []

        for c in frames:
            v = max(int(round(take[view] + c / spec.get('scale', 1.0) - take.get('skip', 0))), take[view])
            v = min(v, take[view] + take.get('length', 10 ** 9) - 1)
            img = fit(cut(grab(cap, v), spec.get('crops', {}).get(view)), a.cell, a.cell)
            above.append(label(lift(img, a.gamma), '%s v%d' % (view, v - take[view])))
            game = cv2.imread(os.path.join(folder, 'g_%s_%s_%d.png' % (a.take, view, c)))
            below.append(label(fit(game, a.cell, a.cell), 'c%d' % c))

        for i in range(0, len(frames), a.cols):
            blocks += [grid(above[i:i + a.cols], a.cols), grid(below[i:i + a.cols], a.cols)]

    save(os.path.join(folder, 'cmp_%s.jpg' % a.take), np.vstack(blocks))


def strip(a):
    frames = [int(f) for f in a.frames.split(',')] if a.frames else rendered(a.dir, a.take, a.view)
    tiles = []

    for c in frames:
        img = cv2.imread(os.path.join(a.dir, 'g_%s_%s_%d.png' % (a.take, a.view, c)))
        img = fit(img, a.cell * img.shape[1] // img.shape[0], a.cell)
        cv2.drawMarker(img, (img.shape[1] // 2, img.shape[0] // 2), (0, 0, 255), cv2.MARKER_CROSS, 8, 1)
        tiles.append(label(img, 'c%d' % c))

    save(os.path.join(a.dir, 'strip_%s_%s.jpg' % (a.take, a.view)), grid(tiles, a.cols))


def fp(a):
    """Screen placement of first-person hands is read off the 10% grid in screen fractions (u right, v down)."""
    cap = cv2.VideoCapture(a.video)
    rows = []

    for row in a.rows:
        frame, _, render = row.partition(':')
        tiles = [label(lift(fit(grab(cap, int(frame)), a.cell, a.cell * 9 // 16), a.gamma), frame)]

        if render:
            img = cv2.imread(render)
            # A weapon the game plays mirrored is rendered as authored, right-handed: flip it to face the footage.
            tiles.append(label(fit(cv2.flip(img, 1) if a.mirror else img, a.cell, a.cell * 9 // 16), os.path.basename(render)))

        for img in tiles:
            h, w = img.shape[:2]

            for i in range(1, 10):
                cv2.line(img, (w * i // 10, 0), (w * i // 10, h), (0, 255, 0), 1)
                cv2.line(img, (0, h * i // 10), (w, h * i // 10), (0, 255, 0), 1)

        rows.append(np.hstack(tiles)[int(a.top * a.cell * 9 // 16):])

    width = max(r.shape[1] for r in rows)
    save(a.out, np.vstack([np.hstack([r, np.zeros((r.shape[0], width - r.shape[1], 3), np.uint8)]) for r in rows]))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest='command', required=True)

    p = sub.add_parser('scan')
    p.add_argument('video')
    p.add_argument('dir')
    p.add_argument('--cut', type=float, default=25, help='mean frame difference that counts as a cut')
    p.add_argument('--lit', type=int, default=400, help='green pixels that count as a drawn hitbox')
    p.add_argument('--shortest', type=int, default=20, help='takes shorter than this are dropped')
    p.add_argument('--ignore', help='x0,y0,x1,y1 (0..1) kept out of the green count: a caption, a HUD')
    p.set_defaults(run=scan)

    p = sub.add_parser('sheet')
    p.add_argument('video')
    p.add_argument('start', type=int)
    p.add_argument('end', type=int)
    p.add_argument('step', type=int)
    p.add_argument('out')
    p.add_argument('--cols', type=int, default=6)
    p.add_argument('--cell', type=int, default=250)
    p.add_argument('--crop', help='x0,y0,x1,y1 (0..1)')
    p.add_argument('--gamma', type=float, default=1.5, help='lifts dark footage')
    p.set_defaults(run=sheet)

    p = sub.add_parser('compare')
    p.add_argument('takes')
    p.add_argument('take')
    p.add_argument('views')
    p.add_argument('frames', nargs='?')
    p.add_argument('--cols', type=int, default=8)
    p.add_argument('--cell', type=int, default=190)
    p.add_argument('--gamma', type=float, default=1.5)
    p.set_defaults(run=compare)

    p = sub.add_parser('strip')
    p.add_argument('dir')
    p.add_argument('take')
    p.add_argument('view')
    p.add_argument('frames', nargs='?')
    p.add_argument('--cols', type=int, default=6)
    p.add_argument('--cell', type=int, default=140)
    p.set_defaults(run=strip)

    p = sub.add_parser('fp')
    p.add_argument('video')
    p.add_argument('out')
    p.add_argument('rows', nargs='+')
    p.add_argument('--cell', type=int, default=960)
    p.add_argument('--top', type=float, default=0.35, help='screen fraction cut off the top: hands live low; 0 for weapons'
                                                          ' (v is then read off the whole screen)')
    p.add_argument('--mirror', action='store_true', help='flip the renders left-right (weapons the game plays mirrored)')
    p.add_argument('--gamma', type=float, default=1.8)
    p.set_defaults(run=fp)

    args = parser.parse_args()
    args.run(args)


if __name__ == '__main__':
    main()
