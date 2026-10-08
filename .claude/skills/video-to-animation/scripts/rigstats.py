"""Arm quality of SwingPreview reports, as numbers: how often and how far the wrists break and the forearms wring,
and how hard the elbows jerk. Text only, no pictures.

  python rigstats.py DIR                 one folder of report_*.txt (one weapon), a line per swing
  python rigstats.py BASE NEW            two trees of <weapon>/report_*.txt, per swing BASE -> NEW and the totals

Run SwingPreview into BASE before a change to the rig or the poses and into NEW after it; a change that trades
broken wrists for wrung forearms shows here at once. Columns:
  wrist   worst bend of the main / off wrist in degrees (over 90 reads broken), n>90 = frames with either over 90
  twist   worst arm/forearm twist muscle of the main / off arm (past 1 the avatar clamps it: a wrung forearm),
          n>1 = frames with either past 1
  jerk    worst second difference of an elbow's position from frame to frame, metres (a jump, not a fast move)
  flips   frames where a twist muscle jumps more than 0.8 away from both neighbours and back: a one or two frame
          twitch of the arm in the game that SwingPreview does not flag (listed as swing:frame after the table)
"""
import glob
import math
import os
import sys


def rows(path):
    result = []

    for line in open(path).read().splitlines()[1:]:
        cells = [cell.split() for cell in line.split('|')]

        if len(cells) < 10:
            continue

        result.append({
            'wrist': [float(v) for v in cells[5]],
            'elbows': [[float(v) for v in cells[7]], [float(v) for v in cells[8]]],
            'twist': [float(v) for v in cells[9]],
            'muscles': [float(v) for v in cells[10]] if len(cells) > 10 else [],
        })

    return result


def flips(frames):
    """Frames whose twist muscle leaves both neighbours by over 0.8 (one frame), or two frames that do it together."""
    found = []

    for i in range(1, len(frames) - 1):
        for j in range(len(frames[i]['muscles'])):
            m = [f['muscles'][j] for f in frames]
            single = abs(m[i] - m[i - 1]) > 0.8 and abs(m[i] - m[i + 1]) > 0.8
            double = i + 2 < len(m) and abs(m[i] - m[i - 1]) > 0.8 and abs(m[i + 1] - m[i + 2]) > 0.8 and abs(m[i] - m[i + 1]) < 0.5

            if single or double:
                found.append(i)
                break

    return found


def stats(path):
    frames = rows(path)
    jerk = [0.0, 0.0]

    for a, b, c in zip(frames, frames[1:], frames[2:]):
        for side in (0, 1):
            jerk[side] = max(jerk[side], math.sqrt(sum(
                (a['elbows'][side][i] - 2 * b['elbows'][side][i] + c['elbows'][side][i]) ** 2 for i in range(3))))

    return [
        max(f['wrist'][0] for f in frames), max(f['wrist'][1] for f in frames),
        sum(f['wrist'][0] > 90 for f in frames) + sum(f['wrist'][1] > 90 for f in frames),
        max(f['twist'][0] for f in frames), max(f['twist'][1] for f in frames),
        sum(f['twist'][0] > 1 for f in frames) + sum(f['twist'][1] > 1 for f in frames),
        jerk[0], jerk[1], len(flips(frames)),
    ]


def line(name, s):
    return f"{name:22s} | {s[0]:4.0f} {s[1]:4.0f} {s[2]:3d} | {s[3]:4.2f} {s[4]:4.2f} {s[5]:3d} | {s[6]:.3f} {s[7]:.3f} | {s[8]:3d}"


def main():
    header = f"{'swing':22s} | wrist M  O n>90 | twist M  O  n>1 | jerk M  O    | flips"

    if len(sys.argv) == 2:
        print(header)

        twitches = []

        for path in sorted(glob.glob(os.path.join(sys.argv[1], 'report_*.txt'))):
            name = os.path.basename(path)[7:-4]
            print(line(name, stats(path)))
            twitches += [f"{name}:{i}" for i in flips(rows(path))]

        if twitches:
            print("twitches at " + " ".join(twitches))

        return

    base, new = sys.argv[1], sys.argv[2]
    totals = [[0.0] * 9, [0.0] * 9]
    print(header + "   (base, then new)")

    for path in sorted(glob.glob(os.path.join(base, '*', 'report_*.txt'))):
        other = os.path.join(new, os.path.relpath(path, base))

        if not os.path.exists(other):
            continue

        name = os.path.basename(os.path.dirname(path)) + ' ' + os.path.basename(path)[7:-4]
        a, b = stats(path), stats(other)
        print(line(name, a))
        print(line('  ->', b))

        for i in range(9):
            totals[0][i] += a[i]
            totals[1][i] += b[i]

    print(f"frames with a broken wrist {totals[0][2]:.0f} -> {totals[1][2]:.0f}, with a wrung forearm {totals[0][5]:.0f} -> {totals[1][5]:.0f}, "
          f"twitches {totals[0][8]:.0f} -> {totals[1][8]:.0f}")


if __name__ == '__main__':
    main()
