"""Turns the Dark and Darker crypt map screenshot into the layout of floor 1.

    python Tools/CryptLayout/build_layout.py            writes the layout and the markers
    python Tools/CryptLayout/build_layout.py preview    also writes Temp/crypt_preview.png

Needs opencv-python-headless and numpy. Output (read by DungeonLayoutBuilder):
  Crypt.png  one pixel per 0.25 m cell: R = 0 rock / 255 floor, G = 128 + floor height * 50
  Crypt.txt  markers 'Kind;Name;x;y;z;yaw' in floor-local metres
"""
import math
import os
import random
import sys

import cv2
import numpy as np

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
SOURCE = os.path.join(ROOT, 'Assets', 'SpecialFolder', 'Screenshots References', 'dad_crypt_map.png')
TARGET = os.path.join(ROOT, 'Assets', 'Game', 'Configs', 'Dungeon', 'Layouts')

# The map square inside the screenshot: 5 x 5 modules of 30 m.
X0, Y0, X1, Y1 = 38, 43, 928, 933
WORLD = 150.0
SIZE = 600
CELL = WORLD / SIZE
ROCK, LIGHT, DARK = 0, 1, 2

SUNKEN = 1.6          # the darker floors of the map lie this much lower
RAMP = 2.8            # and are reached over stairs this long
PASSAGE = 7           # cells: the narrowest passage left as drawn
PYRAMID = (300, 660, 10.0, 1.6, 0.2)   # centre (screenshot px), half size, height, slope

# Hand corrections in screenshot pixels: (x0, y0, x1, y1, class) or (x0, y0, x1, y1, from, to).
FIXES = [
    # Unhatched insides of rock blocks.
    (184, 544, 249, 601, LIGHT, ROCK), (205, 270, 232, 348, LIGHT, ROCK), (384, 98, 404, 168, LIGHT, ROCK),
    (183, 363, 250, 433, LIGHT, ROCK), (360, 365, 424, 430, LIGHT, ROCK), (360, 188, 399, 254, LIGHT, ROCK),
    (88, 205, 165, 256, LIGHT, ROCK), (541, 572, 606, 611, LIGHT, ROCK), (541, 631, 581, 698, LIGHT, ROCK),
    (717, 545, 783, 611, LIGHT, ROCK), (742, 630, 783, 698, LIGHT, ROCK), (752, 612, 783, 630, LIGHT, ROCK),
    # The alcove of the stairs down.
    (228, 370, 252, 396, LIGHT),
    # The cave under the stairs and the passage to its lower half.
    (288, 255, 328, 292, ROCK, DARK), (303, 290, 316, 325, ROCK, DARK), (290, 246, 322, 256, DARK),
    # The sunken corridor in the south-west.
    (77, 718, 90, 826, DARK),
]

ANKHS = [(570, 63), (795, 257), (883, 365), (913, 486), (167, 491), (103, 667), (303, 740), (337, 805), (303, 913)]
SHRINES = ['ShrineHealth', 'ShrineProtection', 'ShrinePower', 'ShrineSpeed']
DESCEND = (240, 383)
ESCAPE = (590, 512)
PLAYERS = [(130, 78), (700, 880)]
# Rows of coffins as drawn: (first x, first y, columns, column step, rows, row step).
COFFINS = [(783, 279, 2, 20, 8, 9.2), (875, 279, 2, 18, 8, 9.2)]
CONTAINERS = [('SmallOakChest', 34), ('Barrel', 20), ('Crate', 20), ('LargeOakChest', 10), ('Coffin', 10), ('Bookshelf', 6)]
PROPS = [('Put', 'Rubble'), ('Put', 'SkullPile'), ('Put', 'CandleCluster'), ('Kit', 'Barrel'), ('Kit', 'Crate_Wooden'), ('Kit', 'Vase_2'),
         ('Kit', 'Vase_4'), ('Kit', 'Vase_Rubble_Medium'), ('Kit', 'Bag'), ('Kit', 'Bucket_Wooden_1'), ('Kit', 'CandleStick_Stand')]


def classify():
    image = cv2.imdecode(np.fromfile(SOURCE, dtype=np.uint8), cv2.IMREAD_COLOR)[Y0:Y1, X0:X1]
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    hsv = cv2.cvtColor(image, cv2.COLOR_BGR2HSV)
    kernel = np.ones((3, 3), np.uint8)

    # Map icons (ankhs, stairs, the player arrow) are the only saturated colours.
    icon = ((hsv[..., 1] > 160) & (hsv[..., 2] > 110)).astype(np.uint8)
    icon = cv2.dilate(cv2.morphologyEx(icon, cv2.MORPH_OPEN, kernel), np.ones((9, 9), np.uint8))

    black = ((gray < 85) & (icon == 0)).astype(np.uint8)
    thick = cv2.dilate(cv2.erode(black, kernel), kernel)
    thin = black & (1 - thick)

    # Hatching: thin strokes packed together.
    hatch = (cv2.boxFilter(thin.astype(np.float32), -1, (11, 11)) > 0.17).astype(np.uint8)
    hatch = cv2.morphologyEx(hatch, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))

    # The walkable network: what is left between the lines and the hatching, in one piece.
    barrier = black | hatch | icon
    barrier[:2, :] = barrier[-2:, :] = 1
    barrier[:, :2] = barrier[:, -2:] = 1
    count, labels, stats, _ = cv2.connectedComponentsWithStats((1 - barrier).astype(np.uint8), connectivity=4)
    main = int(np.argmax(stats[1:, cv2.CC_STAT_AREA])) + 1
    walk = labels == main

    # Pieces enclosed by thin strokes alone are marks on the floor: coffins, steps, tiles.
    rocky = cv2.dilate(thick | hatch, np.ones((5, 5), np.uint8))

    for label in range(1, count):
        piece = labels == label

        if label != main and not rocky[piece].any():
            walk |= piece

    walk = cv2.morphologyEx(walk.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8)) & (1 - (thick | hatch))
    walk = cv2.morphologyEx(walk, cv2.MORPH_OPEN, kernel).astype(bool)

    smooth = cv2.cvtColor(cv2.medianBlur(image, 7), cv2.COLOR_BGR2HSV)
    dark = cv2.medianBlur((smooth[..., 1] > 92).astype(np.uint8), 5) > 0

    classes = np.full(gray.shape, ROCK, np.uint8)
    classes[walk & ~dark] = LIGHT
    classes[walk & dark] = DARK

    # Under an icon the map is whatever lies next to it.
    _, nearest = cv2.distanceTransformWithLabels(icon, cv2.DIST_L2, 3, labelType=cv2.DIST_LABEL_PIXEL)
    classes = np.where(icon > 0, classes[icon == 0][nearest - 1], classes).astype(np.uint8)

    # Specks left of dashed lines are not walls; pillars are bigger.
    count, labels, stats, _ = cv2.connectedComponentsWithStats((classes == ROCK).astype(np.uint8), connectivity=8)

    for label in range(1, count):
        if stats[label, cv2.CC_STAT_AREA] < 30:
            piece = labels == label
            values = classes[(cv2.dilate(piece.astype(np.uint8), kernel) > 0) & ~piece]
            classes[piece] = DARK if (values == DARK).sum() > (values == LIGHT).sum() else LIGHT

    for fix in FIXES:
        part = classes[fix[1] - Y0:fix[3] - Y0, fix[0] - X0:fix[2] - X0]

        if len(fix) == 5:
            part[:] = fix[4]
        else:
            part[part == fix[4]] = fix[5]

    return image, classes


def resample(classes):
    votes = [cv2.resize((classes == c).astype(np.float32), (SIZE, SIZE), interpolation=cv2.INTER_AREA) for c in range(3)]
    # A wall thinner than a cell must not vanish between two floors.
    votes[ROCK] *= 1.6
    layout = np.argmax(np.stack(votes), axis=0).astype(np.uint8)
    layout[:3, :] = layout[-3:, :] = ROCK
    layout[:, :3] = layout[:, -3:] = ROCK

    layout = widen(layout)

    # Only what can be reached is floor.
    count, labels, stats, _ = cv2.connectedComponentsWithStats((layout != ROCK).astype(np.uint8), connectivity=4)
    main = int(np.argmax(stats[1:, cv2.CC_STAT_AREA])) + 1
    layout[labels != main] = ROCK

    return layout


def widen(layout):
    """Passages narrower than PASSAGE are cut wider: doorways drawn as a gap in a line would not let a fighter through."""
    walk = (layout != ROCK).astype(np.uint8)
    disc = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (PASSAGE, PASSAGE))
    wide = cv2.morphologyEx(walk, cv2.MORPH_OPEN, disc)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(walk & (1 - wide), connectivity=8)
    carve = np.zeros(walk.shape, np.uint8)
    square = np.ones((3, 3), np.uint8)

    for label in range(1, count):
        x, y, w, h = stats[label, :4]
        x0, y0, x1, y1 = max(x - 3, 0), max(y - 3, 0), min(x + w + 3, SIZE), min(y + h + 3, SIZE)
        piece = (labels[y0:y1, x0:x1] == label).astype(np.uint8)
        ends = cv2.dilate(piece, square) & wide[y0:y1, x0:x1]

        # A passage joins two wide places; a sliver in a corner touches one.
        if cv2.connectedComponents(ends, connectivity=8)[0] > 2:
            carve[y0:y1, x0:x1] |= cv2.dilate(piece, np.ones((5, 5), np.uint8))

    carve &= 1 - walk
    _, nearest = cv2.distanceTransformWithLabels(1 - walk, cv2.DIST_L2, 3, labelType=cv2.DIST_LABEL_PIXEL)

    return np.where(carve > 0, layout[walk > 0][nearest - 1], layout).astype(np.uint8)


def cell_of(point):
    """Screenshot pixel -> (column, row) of the layout."""
    return (int((point[0] - X0) * SIZE / (X1 - X0)), int((point[1] - Y0) * SIZE / (Y1 - Y0)))


def world_of(column, row):
    return ((column + 0.5) * CELL - WORLD * 0.5, WORLD * 0.5 - (row + 0.5) * CELL)


def heights(layout):
    light = layout == LIGHT
    dark = layout == DARK
    distance = np.zeros(layout.shape, np.float32)
    reached = light.copy()
    step = 0
    cross = cv2.getStructuringElement(cv2.MORPH_CROSS, (3, 3))
    square = np.ones((3, 3), np.uint8)

    # Distance from the upper floor through the lower one, around the walls.
    while True:
        step += 1
        grown = (cv2.dilate(reached.astype(np.uint8), cross if step % 2 else square) > 0) & dark & ~reached

        if not grown.any():
            break

        distance[grown] = step
        reached |= grown

    height = -np.minimum(SUNKEN, distance * CELL * SUNKEN / RAMP)
    height[dark & ~reached] = -SUNKEN

    column, row = cell_of(PYRAMID[:2])
    rows, columns = np.mgrid[0:SIZE, 0:SIZE]
    inside = PYRAMID[2] - np.maximum(np.abs(columns - column), np.abs(rows - row)) * CELL
    pyramid = np.clip(inside * PYRAMID[4], 0.0, PYRAMID[3])
    height = np.where(light & (inside > 0), pyramid, height)
    height[layout == ROCK] = 0.0

    return height.astype(np.float32)


class Placer:
    def __init__(self, layout, height):
        self.layout = layout
        self.height = height
        self.walk = layout != ROCK
        self.clearance = cv2.distanceTransform(self.walk.astype(np.uint8), cv2.DIST_L2, 5) * CELL
        # How open the place is: the widest spot within 3 m.
        self.room = cv2.dilate(self.clearance, np.ones((25, 25), np.uint8))
        slope = cv2.dilate(height, np.ones((9, 9), np.uint8)) - cv2.erode(height, np.ones((9, 9), np.uint8))
        self.flat = slope < 0.05
        self.gy, self.gx = np.gradient(cv2.GaussianBlur(self.clearance, (0, 0), 2))
        self.lines = []
        self.taken = []
        self.random = random.Random(2024)

    def add(self, kind, name, column, row, yaw, lift=0.0, reserve=0.0):
        x, z = world_of(column, row)
        y = float(self.height[int(row), int(column)]) + lift
        self.lines.append('%s;%s;%.2f;%.2f;%.2f;%.0f' % (kind, name, x, y, z, yaw))

        if reserve > 0:
            self.taken.append((column, row, reserve / CELL))

    def is_free(self, column, row, margin=0.0):
        return all((column - c) ** 2 + (row - r) ** 2 > (radius + margin / CELL) ** 2 for c, r, radius in self.taken)

    def into_room(self, column, row):
        """Yaw that turns local +Z away from the nearest wall."""
        return math.degrees(math.atan2(self.gx[row, column], -self.gy[row, column]))

    def nearest(self, point, clearance):
        column, row = cell_of(point)
        rows, columns = np.nonzero((self.clearance >= clearance) & self.flat)
        best = np.argmin((columns - column) ** 2 + (rows - row) ** 2)

        return int(columns[best]), int(rows[best])

    def scatter(self, mask, spacing, limit=10 ** 6):
        """Picks cells of the mask no closer to each other than the spacing, in a shuffled order."""
        rows, columns = np.nonzero(mask)
        order = list(range(len(rows)))
        self.random.shuffle(order)
        picked = []
        grid = {}
        size = spacing / CELL

        for index in order:
            column, row = int(columns[index]), int(rows[index])
            key = (int(column // size), int(row // size))
            near = [p for dx in (-1, 0, 1) for dy in (-1, 0, 1) for p in grid.get((key[0] + dx, key[1] + dy), [])]

            if any((column - c) ** 2 + (row - r) ** 2 < size * size for c, r in near) or not self.is_free(column, row):
                continue

            grid.setdefault(key, []).append((column, row))
            picked.append((column, row))

            if len(picked) >= limit:
                break

        return picked


def place(layout, height):
    placer = Placer(layout, height)
    rnd = placer.random

    for index, (px, py) in enumerate(PLAYERS):
        column, row = placer.nearest((px, py), 1.75)

        for dx, dy in ((-3, -3), (3, -3), (-3, 3), (3, 3)):
            placer.add('Player', 'Player', column + dx, row + dy, 0)

        placer.taken.append((column, row, 16.0 / CELL))

    monsters = placer.scatter((placer.clearance >= 1.0) & placer.flat, 13.5)
    placer.taken = [(c, r, 2.5 / CELL) for c, r, _ in placer.taken]

    for column, row in monsters:
        placer.add('Monster', 'Monster', column, row, rnd.uniform(0, 360))

    column, row = placer.nearest(DESCEND, 0.9)
    placer.add('Descend', 'DescendPortal', column, row, placer.into_room(column, row), reserve=2.5)
    column, row = placer.nearest(ESCAPE, 0.9)
    placer.add('Escape', 'EscapePortal', column, row, placer.into_room(column, row), reserve=2.5)

    # More ways out in the far corners of the map.
    for point in ((75, 100), (900, 110), (110, 880)):
        column, row = placer.nearest(point, 1.2)
        placer.add('Escape', 'EscapePortal', column, row, placer.into_room(column, row), reserve=2.5)

    for index, point in enumerate(ANKHS):
        column, row = placer.nearest(point, 0.9)
        placer.add('Dynamic', SHRINES[index % len(SHRINES)], column, row, placer.into_room(column, row), reserve=2.0)

    column, row = cell_of(PYRAMID[:2])
    placer.add('Loot', 'GoldenChest', column, row, 180, reserve=2.0)
    placer.add('Put', 'Brazier', column - 6, row - 6, 0, reserve=1.0)
    placer.add('Put', 'Brazier', column + 6, row + 6, 0, reserve=1.0)

    for first_x, first_y, columns, column_step, rows, row_step in COFFINS:
        for i in range(columns):
            for j in range(rows):
                column, row = cell_of((first_x + i * column_step, first_y + j * row_step))

                if placer.walk[row, column]:
                    kind = 'Loot' if (i + j) % 4 == 0 else 'Put'
                    placer.add(kind, 'Coffin' if kind == 'Loot' else 'Sarcophagus', column, row, 90, reserve=1.3)

    # Chandeliers over the widest places, braziers in the middle of the largest.
    peaks = (placer.clearance >= 2.6) & (placer.clearance >= placer.room - 0.01)

    for column, row in placer.scatter(peaks, 13):
        placer.lines.append('Chandelier;Chandelier;%.2f;0;%.2f;0' % world_of(column, row))

        if placer.clearance[row, column] >= 4.5 and placer.flat[row, column] and placer.is_free(column, row, 1.0):
            placer.add('Put', 'Brazier', column, row, 0, reserve=1.2)

    # Torches on straight walls.
    rock = ~placer.walk
    sides = ((0, -1, 180), (1, 0, -90), (0, 1, 0), (-1, 0, 90))   # rock to the north, east, south, west
    torches = np.zeros(layout.shape, np.uint8)
    facing = np.zeros(layout.shape, np.int16)

    for index, (dx, dy, yaw) in enumerate(sides):
        wall = placer.walk & np.roll(rock, (-dy, -dx), (0, 1))
        along = np.ones((1, 9), np.uint8) if dx == 0 else np.ones((9, 1), np.uint8)
        straight = cv2.erode(wall.astype(np.uint8), along) > 0
        torches[straight] = index + 1

    for column, row in placer.scatter((torches > 0) & (placer.room >= 1.1), 11):
        dx, dy, yaw = sides[torches[row, column] - 1]
        x, z = world_of(column, row)
        y = float(height[row, column]) + 2.6
        placer.lines.append('Put;WallTorch;%.2f;%.2f;%.2f;%.0f' % (x + dx * (CELL * 0.5 - 0.02), y, z - dy * (CELL * 0.5 - 0.02), yaw))

    # Loot and clutter stand by the walls of rooms, never in a corridor.
    by_wall = (placer.clearance >= 0.7) & (placer.clearance <= 0.9) & (placer.room >= 2.0) & placer.flat
    names = [name for name, weight in CONTAINERS for _ in range(weight)]

    for column, row in placer.scatter(by_wall, 11):
        placer.add('Loot', rnd.choice(names), column, row, placer.into_room(column, row), reserve=1.4)

    for column, row in placer.scatter(by_wall, 4.5):
        kind, name = rnd.choice(PROPS)
        placer.add(kind, name, column, row, rnd.uniform(0, 360), reserve=1.0)

    return placer


def main():
    image, classes = classify()
    layout = resample(classes)
    height = heights(layout)
    placer = place(layout, height)

    os.makedirs(TARGET, exist_ok=True)
    picture = np.zeros((SIZE, SIZE, 3), np.uint8)
    picture[..., 2] = np.where(layout != ROCK, 255, 0)                       # R
    picture[..., 1] = np.clip(np.round(128 + height * 50), 0, 255)           # G
    cv2.imencode('.png', picture)[1].tofile(os.path.join(TARGET, 'Crypt.png'))

    with open(os.path.join(TARGET, 'Crypt.txt'), 'w', encoding='utf-8', newline='\n') as file:
        file.write('\n'.join(placer.lines) + '\n')

    kinds = {}

    for line in placer.lines:
        key = line.split(';')[0] + ' ' + (line.split(';')[1] if line.split(';')[0] in ('Loot', 'Dynamic') else '')
        kinds[key] = kinds.get(key, 0) + 1

    print('floor %.0f m2, sunken %.0f m2' % ((layout != ROCK).sum() * CELL * CELL, (layout == DARK).sum() * CELL * CELL))
    print(', '.join('%s: %d' % item for item in sorted(kinds.items())))

    if len(sys.argv) > 1 and sys.argv[1] == 'preview':
        shade = np.clip(200 + height * 60, 60, 255).astype(np.uint8)
        view = cv2.cvtColor(np.where(layout != ROCK, shade, 30).astype(np.uint8), cv2.COLOR_GRAY2BGR)
        view = cv2.resize(view, None, fx=3, fy=3, interpolation=cv2.INTER_NEAREST)
        colors = {'Player': (0, 255, 0), 'Monster': (0, 0, 255), 'Loot': (0, 200, 255), 'Put': (255, 160, 0), 'Kit': (200, 120, 60),
                  'Dynamic': (255, 0, 255), 'Escape': (255, 80, 0), 'Descend': (0, 0, 160), 'Chandelier': (0, 255, 255)}

        for line in placer.lines:
            kind, name, x, y, z, yaw = line.split(';')
            center = (int((float(x) + WORLD * 0.5) / CELL * 3), int((WORLD * 0.5 - float(z)) / CELL * 3))
            cv2.circle(view, center, 3 if name == 'WallTorch' else 5, colors[kind] if name != 'WallTorch' else (0, 220, 255), -1 if kind != 'Kit' else 1)

        cv2.imwrite(os.path.join(ROOT, 'Temp', 'crypt_preview.png'), view)


if __name__ == '__main__':
    main()
