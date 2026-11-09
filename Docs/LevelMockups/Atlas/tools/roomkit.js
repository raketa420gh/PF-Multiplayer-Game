// Конструктор макетов катакомб. Комната описывается в tools/rooms/<Name>.js, tools/build.js пишет data/<Name>.js и проверяет её.
// Координаты Unity: x восток, z север, метры от центра клетки, y вверх; pivot снизу по центру; ry по часовой.
// Ярус (level) — это скала всей клетки, из которой вырезаны пустоты (carve); скала становится блоками Wall, пол кладётся
// только под пустотами (иначе NavMesh получает острова внутри стен).
const INNER = 16.8, EDGE = 17.1, DOOR_OFFSET = 8.4, OPENING = 2, DOOR_W = 1.1, DOOR_H = 2.4, TH = 0.6, MIN_Y = -10, MAX_Y = 24;
const round = v => Math.round(v * 1000) / 1000;
const rect = (x1, z1, x2, z2) => ({ x1: Math.min(x1, x2), z1: Math.min(z1, z2), x2: Math.max(x1, x2), z2: Math.max(z1, z2) });

/// Прямоугольники области outer минус cut и только внутри keep (если задан), жадно слитые.
function subtract(outer, cut, keep) {
  const pts = (a, b) => [...new Set([a, b].flat().map(round))].sort((p, q) => p - q);
  const all = [...cut, ...(keep || [])];
  const xs = pts([outer.x1, outer.x2], all.flatMap(r => [r.x1, r.x2]).filter(v => v > outer.x1 && v < outer.x2));
  const zs = pts([outer.z1, outer.z2], all.flatMap(r => [r.z1, r.z2]).filter(v => v > outer.z1 && v < outer.z2));
  const inR = (r, x, z) => x > r.x1 && x < r.x2 && z > r.z1 && z < r.z2;
  const on = xs.slice(0, -1).map((x, i) => zs.slice(0, -1).map((z, j) => {
    const cx = (x + xs[i + 1]) / 2, cz = (z + zs[j + 1]) / 2;
    return !cut.some(r => inR(r, cx, cz)) && (!keep || keep.some(r => inR(r, cx, cz)));
  }));
  const out = [];
  for (let i = 0; i < on.length; i++) for (let j = 0; j < on[i].length; j++) {
    if (!on[i][j]) continue;
    let j2 = j; while (j2 + 1 < on[i].length && on[i][j2 + 1]) j2++;
    let i2 = i; while (i2 + 1 < on.length) { let ok = true; for (let k = j; k <= j2; k++) ok = ok && on[i2 + 1][k]; if (!ok) break; i2++; }
    for (let a = i; a <= i2; a++) for (let b = j; b <= j2; b++) on[a][b] = false;
    out.push(rect(xs[i], zs[j], xs[i2 + 1], zs[j2 + 1]));
  }
  return out;
}

class Level {
  constructor(y, h) { this.y = y; this.h = h; this.carved = []; this.holes = []; }
  /// Вырезать пустоту.
  carve(x1, z1, x2, z2) { this.carved.push(rect(x1, z1, x2, z2)); return this; }
  /// Дыра в полу этого яруса: лестничный пролёт, шахта лифта, провал. Под ней нужна пустота нижнего яруса.
  hole(x1, z1, x2, z2) { this.holes.push(rect(x1, z1, x2, z2)); return this; }
}

class Room {
  constructor(name, opts = {}) {
    this.name = name; this.corner = !!opts.corner; this.items = []; this.levels = []; this.counters = {}; this.text = null;
    this.expected = opts.expect || {};
  }
  desc(text) { this.text = text; return this; }

  /// Ярус с полом на высоте y. h по умолчанию — до пола следующего яруса (минус плита 0,6), у верхнего 6 м.
  level(y, h) { const l = new Level(y, h); this.levels.push(l); return l; }

  add(cat, name, p, size, extra = {}) {
    const key = cat[0].toUpperCase() + cat[1];
    this.counters[key] = (this.counters[key] || 0) + 1;
    const o = { id: `${key}${this.counters[key]}`, cat, name, p: p.map(round) };
    if (extra.q === undefined) o.ry = extra.ry || 0;
    if (size) o.s = size.map(round);
    for (const k of Object.keys(extra)) if (k !== 'ry' && extra[k] !== undefined) o[k] = extra[k];
    this.items.push(o);
    return o;
  }

  /// Глухая стена от точки к точке вдоль X или Z. th толщина, y низ, h высота.
  wall(x1, z1, x2, z2, { th = TH, y = 0, h = 6 } = {}) {
    if (x1 === x2) return this.add('wall', 'Wall', [x1, y, (z1 + z2) / 2], [th, h, Math.abs(z2 - z1)]);
    if (z1 !== z2) throw new Error(`${this.name}: wall must be axis-aligned`);
    return this.add('wall', 'Wall', [(x1 + x2) / 2, y, z1], [Math.abs(x2 - x1), h, th]);
  }

  /// Стена с проёмами 2 × 2,4 м в точках at (координата вдоль стены); в каждом проёме дверь, door:false — пустой проём,
  /// double:true — двойная. Над проёмом перемычка.
  doorWall(x1, z1, x2, z2, at, { th = TH, y = 0, h = 6, door = true, double = false } = {}) {
    const alongX = z1 === z2, from = alongX ? Math.min(x1, x2) : Math.min(z1, z2), to = alongX ? Math.max(x1, x2) : Math.max(z1, z2);
    const seg = (a, b, yy, hh) => { if (b - a > 0.01) alongX ? this.wall(a, z1, b, z1, { th, y: yy, h: hh }) : this.wall(x1, a, x1, b, { th, y: yy, h: hh }); };
    let cur = from;
    for (const t of [...at].sort((a, b) => a - b)) {
      seg(cur, t - OPENING / 2, y, h);
      seg(t - OPENING / 2, t + OPENING / 2, y + DOOR_H, h - DOOR_H);
      if (door) { const px = alongX ? t : x1, pz = alongX ? z1 : t; double ? this.doubleDoor(px, pz, alongX ? 0 : 90, { y }) : this.door(px, pz, alongX ? 0 : 90, { y }); }
      cur = t + OPENING / 2;
    }
    seg(cur, to, y, h);
  }

  column(x, z, { s = 1.2, y = 0, h = 6, round: isRound = false } = {}) { return this.add('column', 'Wall Column', [x, y, z], [s, h, s], { round: isRound || undefined }); }
  /// Произвольный глухой блок (может висеть над полом).
  block(x, z, w, d, { y = 0, h = 6, ry = 0 } = {}) { return this.add('wall', 'Wall', [x, y, z], [w, h, d], { ry }); }
  /// Марш: центр (x, z), ширина w, длина len, подъём rise вдоль локальной +Z (ry 0 — на север, 90 — на восток, 180 — на юг, 270 — на запад) от y0.
  stairs(x, z, w, len, rise, ry = 0, y0 = 0) { return this.add('stairs', 'Stairs', [x, y0, z], [w, rise, len], { ry }); }

  /// Приподнятый участок с верхом top, стопка слоёв ≤ 1,5 м от y0 (NavMesh видит только грани коллайдеров).
  platform(x1, z1, x2, z2, top, y0 = 0) {
    const r = rect(x1, z1, x2, z2), total = top - (y0 - TH), layers = Math.ceil(total / 1.5), t = total / layers;
    for (let i = 0; i < layers; i++) this.add('platform', 'Platform', [(r.x1 + r.x2) / 2, y0 - TH + i * t, (r.z1 + r.z2) / 2], [r.x2 - r.x1, t, r.z2 - r.z1]);
  }

  /// Плита пола без опор: мостки, галерея, балкон.
  slab(x1, z1, x2, z2, y) { const r = rect(x1, z1, x2, z2); return this.add('floor', 'Floor', [(r.x1 + r.x2) / 2, y - TH, (r.z1 + r.z2) / 2], [r.x2 - r.x1, TH, r.z2 - r.z1]); }
  /// Мелкая вода (замедляет): пол на y, поверхность на y + 0,12; мостки над ней — platform с верхом y + 0,25.
  water(x1, z1, x2, z2, y = 0) { const r = rect(x1, z1, x2, z2); return this.add('water', 'Water', [(r.x1 + r.x2) / 2, y + 0.12, (r.z1 + r.z2) / 2], [r.x2 - r.x1, 0, r.z2 - r.z1]); }
  bars(x1, z1, x2, z2, y = 0) { const alongX = z1 === z2; return this.add('bars', 'Bars', [(x1 + x2) / 2, y, (z1 + z2) / 2], alongX ? [Math.abs(x2 - x1), 3, 0.12] : [0.12, 3, Math.abs(z2 - z1)]); }

  /// Проп по размерам: Стол, Лавка, Табурет, Койка, Скамья, Саркофаг, Гроб, Алтарь, Обломки, Штабель ящиков, Бочки, Мешки, Парапет, Стойка с оружием, Полки с припасами, Кости, Черепа, Очаг, Котёл.
  prop(name, x, z, w, d, h, { y = 0, ry = 0, round: isRound } = {}) { return this.add('prop', name, [x, y, z], [w, h, d], { ry, round: isRound }); }
  /// Контейнер: GoldenChest, LargeOakChest, SmallOakChest, Coffin, Bookshelf, Barrel, Crate, Urn, GoldPile.
  chest(name, x, z, { y = 0, ry = 0 } = {}) { return this.add('chest', name, [x, y, z], [0.9, 0.8, 0.9], { ry }); }
  /// Sword, Archer, Crossbow, Warrior, Mage, Skull.
  monster(name, x, z, { y = 0, ry = 0 } = {}) { return this.add('monster', name, [x, y, z], [0.7, 1.8, 0.7], { ry, round: true }); }
  /// Spike, Blade (y — пол под маятником, ry — плоскость качания), Vent, Collapse (y — верх плиты).
  trap(name, x, z, { y = 0, ry = 0 } = {}) { return this.add('trap', name, [x, y, z], [0.8, 0.8, 0.8], { ry }); }
  start(x, z, ry = 0) { return this.add('start', 'Start', [x, 0, z], [1, 0.1, 1], { ry, round: true }); }
  /// Факел на грани стены: y — пол, ry — куда смотрит.
  torch(x, z, ry, y = 0) { return this.add('light', 'Torch', [x, y + 3.2, z], null, { ry, color: '#FFC280', i: 9, r: 12 }); }
  candle(x, z, y = 0) { return this.add('light', 'Candle', [x, y + 0.3, z], null, { color: '#FFB259', i: 1.1, r: 4 }); }
  brazier(x, z, y = 0) { return this.add('light', 'Brazier', [x, y + 0.9, z], null, { color: '#FF9A4D', i: 6, r: 9 }); }
  /// Люстра под потолком, всегда запечена: y — где свет (корпус и цепь на 1,6 м выше), i и r — сила и дальность на весь зал.
  chandelier(x, z, y, { i = 90, r = 30 } = {}) { return this.add('light', 'Chandelier', [x, y, z], null, { color: '#FFC280', i, r }); }

  // ---------- механизмы: один на комнату, спавнится хостом, см. CatacombRoomBuilder.Apply ----------

  /// Деревянная дверь 1,1 × 2,4 в проёме 2 × 2,4: finish ставит по бокам каменные косяки. ry 0 — полотно вдоль X, 90 — вдоль Z.
  door(x, z, ry = 0, { y = 0 } = {}) { return this.add('trap', 'Door', [x, y, z], [DOOR_W, DOOR_H, 0.12], { ry }); }
  /// Двойных дверей больше нет (одна створка модели): то же, что door.
  doubleDoor(x, z, ry = 0, opts = {}) { return this.door(x, z, ry, opts); }
  /// Рычаг на стене: p на уровне пола, ry — куда смотрит. order > 0 — номер в правильной последовательности. action:'lift' — управляет лифтом.
  lever(x, y, z, ry, { order, action } = {}) { return this.add('trap', 'SecretLever', [x, y, z], [0.3, 1.2, 0.2], { ry, order, action }); }
  /// Подвижная плита секрета: x, y (низ), z — закрытое положение, open — смещение при открытии.
  secretDoor(x, y, z, w, h, d, open, { ry = 0 } = {}) { return this.add('trap', 'SecretDoor', [x, y, z], [w, h, d], { ry, open }); }
  /// Нажимная плита на полу; order — номер в правильной последовательности.
  plate(x, z, order, { y = 0, w = 1.6, d = 1.6 } = {}) { return this.add('trap', 'FloorPlate', [x, y, z], [w, 0.1, d], { order }); }
  /// Лифт: пол кабины w × d на высоте y (верхняя остановка), едет на travel вниз (< 0).
  lift(x, y, z, w, d, travel) { return this.add('trap', 'Lift', [x, y, z], [w, 0.2, d], { travel }); }
  /// Точка, откуда за ошибку в последовательности падает монстр (люк в потолке).
  drop(x, y, z) { return this.add('trap', 'MonsterDrop', [x, y, z], [1.4, 0.3, 1.4]); }

  /// Сборка: скала, полы, потолок, стены верхней части клетки.
  finish() {
    const levels = this.levels.sort((a, b) => a.y - b.y);
    if (!levels.some(l => l.y === 0)) throw new Error(`${this.name}: no level at y=0`);
    levels.forEach((l, i) => {
      const next = levels[i + 1];
      if (l.h === undefined) l.h = next ? next.y - TH - l.y : 6;
      else if (next && Math.abs(l.h - (next.y - TH - l.y)) > 1e-6) throw new Error(`${this.name}: level ${l.y} h must reach the slab of the level above`);
      const bound = l.y < 0 ? EDGE : INNER, outer = rect(-bound, -bound, bound, bound);
      if (l.y === 0) {
        for (const s of [-1, 1]) {
          l.carve(s * DOOR_OFFSET - 1.2, INNER, s * DOOR_OFFSET + 1.2, INNER - 1.8);
          l.carve(INNER, s * DOOR_OFFSET - 1.2, INNER - 1.8, s * DOOR_OFFSET + 1.2);
          if (!this.corner) { l.carve(s * DOOR_OFFSET - 1.2, -INNER, s * DOOR_OFFSET + 1.2, -INNER + 1.8); l.carve(-INNER, s * DOOR_OFFSET - 1.2, -INNER + 1.8, s * DOOR_OFFSET + 1.2); }
        }
      }
      for (const r of subtract(outer, l.carved)) this.add('wall', 'Wall', [(r.x1 + r.x2) / 2, l.y - TH, (r.z1 + r.z2) / 2], [r.x2 - r.x1, l.h + TH, r.z2 - r.z1]);
      const floorArea = rect(-EDGE, -EDGE, EDGE, EDGE), thresholds = [];
      if (l.y === 0) for (const s of [-1, 1]) {
        thresholds.push(rect(s * DOOR_OFFSET - 1.2, INNER, s * DOOR_OFFSET + 1.2, EDGE), rect(INNER, s * DOOR_OFFSET - 1.2, EDGE, s * DOOR_OFFSET + 1.2));
        if (!this.corner) thresholds.push(rect(s * DOOR_OFFSET - 1.2, -EDGE, s * DOOR_OFFSET + 1.2, -INNER), rect(-EDGE, s * DOOR_OFFSET - 1.2, -INNER, s * DOOR_OFFSET + 1.2));
      }
      for (const r of subtract(floorArea, l.holes, [...l.carved, ...thresholds])) this.add('floor', 'Floor', [(r.x1 + r.x2) / 2, l.y - TH, (r.z1 + r.z2) / 2], [r.x2 - r.x1, TH, r.z2 - r.z1]);
    });
    const topLevel = levels[levels.length - 1], top = topLevel.y + topLevel.h;
    this.add('ceiling', 'Ceiling', [0, top, 0], [34.2, TH, 34.2]);
    if (top > 6) for (const [x, z, sx, sz] of [[0, EDGE, 34.78, TH], [0, -EDGE, 34.78, TH], [-EDGE, 0, TH, 33.6], [EDGE, 0, TH, 33.6]])
      this.add('upper', 'Wall Upper', [x, 6, z], [sx, top - 6, sz]);
    this.items.filter(o => o.name === 'Door').forEach(d => this.jambs(d));
    return this;
  }

  /// Полотно уже проёма: два косяка закрывают остаток, толщиной и смещением как стена сбоку (без стены или в скале — TH по оси двери).
  jambs(d) {
    const alongX = d.ry % 180 === 0, w = (OPENING - DOOR_W) / 2, [x, y, z] = d.p;
    const walls = this.items.filter(o => o.cat === 'wall' || o.cat === 'upper').map(o => {
      const [sx, sy, sz] = o.ry % 180 ? [o.s[2], o.s[1], o.s[0]] : o.s;
      return { x1: o.p[0] - sx / 2, x2: o.p[0] + sx / 2, y1: o.p[1], y2: o.p[1] + sy, z1: o.p[2] - sz / 2, z2: o.p[2] + sz / 2 };
    });
    for (const s of [-1, 1]) {
      const px = alongX ? x + s * (OPENING / 2 + 0.1) : x, pz = alongX ? z : z + s * (OPENING / 2 + 0.1);
      const b = walls.find(b => px >= b.x1 && px <= b.x2 && pz >= b.z1 && pz <= b.z2 && y + 1 >= b.y1 && y + 1 <= b.y2);
      const th = b ? (alongX ? b.z2 - b.z1 : b.x2 - b.x1) : TH, isWall = th <= 1.5, t = isWall ? th : TH;
      const off = b && isWall ? (alongX ? (b.z1 + b.z2) / 2 - z : (b.x1 + b.x2) / 2 - x) : 0, along = s * (DOOR_W + w) / 2;
      this.add('wall', 'Wall', alongX ? [x + along, y, z + off] : [x + off, y, z + along], alongX ? [w, DOOR_H, t] : [t, DOOR_H, w]);
    }
  }

  toFile() { return `// "edited":true\nATLAS.room("${this.name}",[\n${this.items.map(o => JSON.stringify(o)).join(',\n')}\n]);\n`; }
}

module.exports = { Room, subtract, rect, INNER, EDGE, DOOR_OFFSET, OPENING, DOOR_W, DOOR_H, TH, MIN_Y, MAX_Y };
