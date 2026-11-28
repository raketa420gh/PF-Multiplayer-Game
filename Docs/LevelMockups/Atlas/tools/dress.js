// Одевание блокаута: по готовой геометрии комнаты ставит материалы и детали (категория decor), только вид.
// Комната задаёт тему и свои сцены в exports.dress = S => { S.theme({…}); S.auto(); … }, см. README, раздел «Одевание».
// Общие проходы: кладка и скала на открытых гранях, венцы стен, пол, осыпь у подошвы, колонны, марши, двери, проёмы клетки, свет, пропсы.
const detail = require('./detail');
const kits = require('./kits');
const DEG = Math.PI / 180, INNER = 16.8, EDGE = 17.1, DOOR = 8.4;
const SOLID = new Set(['wall', 'column', 'floor', 'platform', 'pit', 'upper', 'ceiling', 'shell', 'stairs']);
const WALK = new Set(['floor', 'platform', 'stairs']);
const SKIN = new Set(['wall', 'column', 'pit', 'upper', 'shell', 'platform', 'floor']);
const DIRS = { N: [0, 1], E: [1, 0], S: [0, -1], W: [-1, 0] }, BACK = { N: 'S', E: 'W', S: 'N', W: 'E' }, FACE_RY = { N: 0, E: 90, S: 180, W: 270 };
/// Как материал стены превращается в рельеф: rough — рваный камень, course — ряды блоков по швам текстуры, boards — доски, bones — костяная кладка.
const STYLES = {
  rock: { kind: 'rough', name: 'Камень кладки' }, slab: { kind: 'rough', name: 'Камень кладки', big: 0.8 }, pebble: { kind: 'rough', name: 'Камень кладки', big: 0.7 },
  dirt: { kind: 'rough', name: 'Ком земли', stone: 'rock', density: 0.5 }, sand: { kind: 'rough', name: 'Камень', stone: 'rock', density: 0.3 }, ash: { kind: 'rough', name: 'Спёкшийся шлак', density: 0.6 },
  stone: { kind: 'course', bw: 0.8, bh: 0.4, p: 0.2, depth: [0.03, 0.09], name: 'Блок кладки' },
  marble: { kind: 'course', bw: 0.8, bh: 0.4, p: 0.1, depth: [0.02, 0.05], name: 'Мраморный блок' },
  plaster: { kind: 'course', bw: 0.8, bh: 0.4, p: 0.07, depth: [0.02, 0.05], name: 'Блок под штукатуркой', stone: 'stone' },
  brick: { kind: 'course', bw: 0.3, bh: 0.12, p: 0.045, depth: [0.02, 0.06], name: 'Кирпич' },
  planks: { kind: 'boards' }, studs: { kind: 'boards' }, wood: { kind: 'boards' },
  bone: { kind: 'bones' }
};
const PAVED = new Set(['stone', 'marble', 'brick', 'plaster']);
const PROP_MATS = { 'Парапет': 'stone', 'Обломки': 'rock', 'Саркофаг': 'stone', 'Алтарь': 'stone', 'Кости': 'bone', 'Черепа': 'bone', 'Очаг': 'stone', 'Котёл': 'iron', 'Рудная вагонетка': 'iron', 'Наковальня': 'iron', 'Горн': 'brick', 'Гриб': 'fungus', 'Золото': 'gold', 'Колодец': 'slab', 'Упавшая колонна': 'stone', 'Барабан колонны': 'stone', 'Статуя': 'stone', 'Цепь': 'iron', 'Колокол': 'bronze', 'Купель': 'stone', 'Мешки': 'cloth', 'Знамя': 'clothRed' };
const hashName = t => { let h = 2166136261; for (let i = 0; i < t.length; i++) h = Math.imul(h ^ t.charCodeAt(i), 16777619); return h >>> 0; };

function scene(room, spec = {}) {
  const base = room.items.filter(o => o.cat !== 'decor' && o.s);
  const isHall = base.some(o => o.cat === 'shell'), corner = !!(spec.options || {}).corner;
  // Свои границы у ключевого зала (две клетки), у обычной клетки стороны ставит генератор: здесь они мнимые тела.
  const ext = base.filter(o => o.cat === 'floor').reduce((b, o) => ({ x0: Math.min(b.x0, o.p[0] - o.s[0] / 2), x1: Math.max(b.x1, o.p[0] + o.s[0] / 2), z0: Math.min(b.z0, o.p[2] - o.s[2] / 2), z1: Math.max(b.z1, o.p[2] + o.s[2] / 2) }), { x0: -INNER, x1: INNER, z0: -INNER, z1: INNER });
  const lim = isHall ? { x: [ext.x0 + 0.4, ext.x1 - 0.4], z: [ext.z0 + 0.4, ext.z1 - 0.4] } : undefined;
  const D = detail(room, (spec.look || {}).seed || hashName(room.name), lim), { R, J, pick, chance, put, rnd } = D;
  const S = { room, D, base, corner, isHall, T: {}, skip: [], feet: [], done: new Set() };

  // ---------- тела ----------
  const mk = (o, cat = o.cat) => {
    const a = (o.ry || 0) * DEG, c = Math.cos(a), s = Math.sin(a), hw = o.s[0] / 2, hd = o.s[2] / 2, ex = Math.abs(hw * c) + Math.abs(hd * s), ez = Math.abs(hw * s) + Math.abs(hd * c);
    const stair = cat === 'stairs';
    return { o, cat, cx: o.p[0], cz: o.p[2], foot: o.p[1], y0: o.p[1] - (stair ? 0.6 : 0), y1: o.p[1] + o.s[1], hw, hd, c, s, ry: o.ry || 0, x0: o.p[0] - ex, x1: o.p[0] + ex, z0: o.p[2] - ez, z1: o.p[2] + ez };
  };
  const boxes = base.filter(o => SOLID.has(o.cat)).map(o => mk(o));
  const doorsOf = k => isHall || (corner && (k === 'S' || k === 'W')) ? [] : [-DOOR, DOOR];
  S.sides = [];
  if (!isHall) for (const k of 'NESW') {
    const [nx, nz] = DIRS[k], o = { id: `Side${k}`, cat: 'shell', name: 'Сторона клетки', p: [nx * EDGE, 0, nz * EDGE], s: nx ? [0.6, 6, 34.8] : [34.8, 6, 0.6], virtual: true, side: k };
    const b = mk(o); b.gap = (lx, lz, y) => y < 2.45 && doorsOf(k).some(t => Math.abs((nx ? lz : lx) - t) < 0.6);
    boxes.push(b); S.sides.push(b);
  }
  const CELL = 2, grid = new Map(), gk = (i, j) => i * 4096 + j, gi = v => Math.floor(v / CELL) + 512;
  for (const b of boxes) for (let i = gi(b.x0); i <= gi(b.x1); i++) for (let j = gi(b.z0); j <= gi(b.z1); j++) { const k = gk(i, j); if (!grid.has(k)) grid.set(k, []); grid.get(k).push(b); }
  const at = (x, z) => grid.get(gk(gi(x), gi(z))) || [];
  const local = (b, x, z) => { const dx = x - b.cx, dz = z - b.cz; return [dx * b.c - dz * b.s, dx * b.s + dz * b.c]; };
  const rampY = (b, lz) => b.foot + (b.y1 - b.foot) * Math.max(0, Math.min(1, (lz + b.hd) / (2 * b.hd)));
  const inBox = (b, x, y, z) => {
    if (y < b.y0 || y > b.y1 || x < b.x0 || x > b.x1 || z < b.z0 || z > b.z1) return false;
    const [lx, lz] = local(b, x, z);
    if (Math.abs(lx) > b.hw || Math.abs(lz) > b.hd) return false;
    if (b.cat === 'stairs') return y <= rampY(b, lz);
    return !(b.gap && b.gap(lx, lz, y));
  };
  /// Тело в точке (кроме except) или null.
  const solid = S.solid = (x, y, z, except) => { for (const b of at(x, z)) if (b !== except && inBox(b, x, y, z)) return b; return null; };
  const inCell = S.inCell = (x, y, z) => isHall ? x > ext.x0 - 0.31 && x < ext.x1 + 0.31 && z > ext.z0 - 0.31 && z < ext.z1 + 0.31 : Math.max(Math.abs(x), Math.abs(z)) < (y < -0.01 ? EDGE : INNER) + 0.001;
  const air = S.air = (x, y, z, except) => inCell(x, y, z) && !solid(x, y, z, except);
  /// Поверхности, по которым ходят, в точке: [{ y, b }], над каждой есть место для тела.
  const tops = S.tops = (x, z) => {
    const out = [];
    for (const b of at(x, z)) {
      if (!WALK.has(b.cat) || x < b.x0 || x > b.x1 || z < b.z0 || z > b.z1) continue;
      const [lx, lz] = local(b, x, z);
      if (Math.abs(lx) > b.hw || Math.abs(lz) > b.hd) continue;
      const y = b.cat === 'stairs' ? rampY(b, lz) : b.y1;
      if (inCell(x, y + 0.5, z) && !solid(x, y + 0.45, z) && !solid(x, y + 1.1, z) && !solid(x, y + 1.7, z)) out.push({ y, b });
    }
    return out;
  };
  /// Высота пола под точкой: ближайшая к near поверхность, undefined — пола нет.
  S.floor = (x, z, near = 0) => { const t = tops(x, z).sort((a, b) => Math.abs(a.y - near) - Math.abs(b.y - near))[0]; return t && t.y; };
  /// Стена в сторону toward (N, E, S, W) от точки: [сторона, куда она смотрит, её плоскость, координата вдоль] — аргументы настенных наборов detail.js.
  S.wall = (x, z, toward, y = 1.5, max = 14) => {
    const [dx, dz] = DIRS[toward];
    for (let t = 0; t < max; t += 0.05) if (!air(x + dx * t, y, z + dz * t)) { const plane = dx ? x + dx * (t - 0.025) : z + dz * (t - 0.025); return [BACK[toward], Math.round(plane * 20) / 20, dx ? z : x]; }
    return null;
  };
  /// Ближайшая стена к точке: то же, что wall, по всем четырём сторонам.
  S.nearWall = (x, z, y = 1.5, max = 6) => {
    let best = null;
    for (const k of 'NESW') { const w = S.wall(x, z, k, y, max); if (w) { const dist = Math.abs(w[1] - (DIRS[k][0] ? x : z)); if (!best || dist < best.dist) best = { w, dist }; } }
    return best && best.w;
  };

  // ---------- тема ----------
  const T = S.T = { rock: 'rock', wall: 'stone', floor: 'cobble', platform: undefined, stairs: undefined, column: 'stone', pit: undefined, slab: undefined, side: undefined, thick: 1.5, damp: 0.25, age: 1, density: 1, soot: 0.6, doors: 'wood', arch: 'slab' };
  /// Тема комнаты: материалы скалы (толстые блоки), стен, пола, платформ, маршей, колонн; damp — сырость 0…1 или функция (x, y, z); age — степень разрушения; mat(o) — свой материал объекта.
  S.theme = t => { Object.assign(T, t); return S; };
  const waters = base.filter(o => o.cat === 'water' || o.cat === 'acid');
  S.damp = (x, y, z) => {
    if (typeof T.damp === 'function') return Math.max(0, Math.min(1, T.damp(x, y, z)));
    let near = 0;
    for (const w of waters) { const dx = Math.max(0, Math.abs(x - w.p[0]) - w.s[0] / 2), dz = Math.max(0, Math.abs(z - w.p[2]) - w.s[2] / 2), dy = Math.abs(y - w.p[1]); near = Math.max(near, 0.6 - (Math.hypot(dx, dz) + dy * 0.5) / 9); }
    return Math.max(0, Math.min(1, T.damp + near + (y < -0.5 ? 0.08 : 0)));
  };
  const materials = () => {
    for (const o of room.items) {
      if (o.m || o.cat === 'decor' || !o.s) continue;
      const own = T.mat && T.mat(o);
      if (own) { o.m = own; continue; }
      if (o.cat === 'wall' || o.cat === 'shell') o.m = Math.min(o.s[0], o.s[2]) >= T.thick ? T.rock : T.wall;
      else if (o.cat === 'upper' || o.cat === 'ceiling') o.m = T.rock;
      else if (o.cat === 'pit') o.m = T.pit || T.rock;
      else if (o.cat === 'column') o.m = T.column;
      else if (o.cat === 'floor') o.m = o.p[1] + o.s[1] > 0.3 ? T.slab || (PAVED.has(T.wall) ? T.wall : T.floor) : T.floor;
      else if (o.cat === 'platform') o.m = T.platform || T.floor;
      else if (o.cat === 'stairs') o.m = T.stairs || T.platform || (PAVED.has(T.wall) ? T.wall : T.floor);
      else if (o.cat === 'prop') o.m = PROP_MATS[o.name] || 'wood';
    }
    for (const b of S.sides) b.o.m = T.side || T.rock;
  };

  // ---------- грани ----------
  /// Грань тела: рамка с осью u вдоль грани и нормалью наружу; у повёрнутых блоков своя.
  const faces = b => 'NESW'.split('').map(k => {
    const ns = k === 'N' || k === 'S', ry = (b.ry + FACE_RY[k]) % 360, a = ry * DEG;
    return { b, k, ry, c: Math.cos(a), s: Math.sin(a), half: ns ? b.hd : b.hw, len: 2 * (ns ? b.hw : b.hd), y0: b.y0, y1: b.y1, axis: b.ry % 90 === 0, cols: new Map() };
  });
  const fpos = (f, u, out) => [u * f.c + (f.half + out) * f.s + f.b.cx, -u * f.s + (f.half + out) * f.c + f.b.cz];
  const open = (f, u, y) => { const [x, z] = fpos(f, u, 0.07); return air(x, y, z, f.b); };
  /// Плитка на грани: центр u, низ y, w вдоль, h вверх, d наружу, sink — насколько утоплена.
  const ftile = S.ftile = (f, name, m, u, y, w, h, d, { sink = 0.02, roll = 0, yaw = 0, pitch = 0, round } = {}) => { const [x, z] = fpos(f, u, d / 2 - sink); return put(name, m, x, y, z, w, h, d, { ry: f.ry + yaw, roll, pitch, round }); };
  /// Столбец грани: подошвы (пол у стены) и свободное место перед ней.
  const column = (f, u) => {
    const i = Math.floor((u + f.len / 2) / 0.5);
    if (!f.cols.has(i)) {
      const uc = Math.max(-f.len / 2 + 0.05, Math.min(f.len / 2 - 0.05, -f.len / 2 + (i + 0.5) * 0.5)), [x, z] = fpos(f, uc, 0.35);
      const feet = tops(x, z).filter(t => t.y > f.y0 - 0.05 && t.y < f.y1 - 0.2).map(t => ({ y: t.y, ramp: t.b.cat === 'stairs', m: t.b.o.m })).sort((a, b) => a.y - b.y);
      f.cols.set(i, { uc, feet, clear: new Map() });
    }
    return f.cols.get(i);
  };
  const footOf = (col, y) => { let best; for (const ft of col.feet) if (ft.y <= y + 0.3) best = ft; return best; };
  const clearance = (f, col, y) => {
    const key = Math.round(y);
    if (!col.clear.has(key)) { let t = 0.15; for (; t < 4; t += 0.25) { const [x, z] = fpos(f, col.uc, t); if (!air(x, y, z, f.b)) break; } col.clear.set(key, t); }
    return col.clear.get(key);
  };
  const skipped = (x, y, z) => S.skip.some(q => x > q[0] && x < q[3] && y > q[1] && y < q[4] && z > q[2] && z < q[5]);
  /// Запретить детали в объёме (проход, механизм): x1, y1, z1, x2, y2, z2.
  S.keep = (x1, y1, z1, x2, y2, z2) => { S.skip.push([Math.min(x1, x2), Math.min(y1, y2), Math.min(z1, z2), Math.max(x1, x2), Math.max(y1, y2), Math.max(z1, z2)]); return S; };

  const mossOn = (f, u, y, w, h, d, o, wet) => {
    if (chance(wet)) ftile(f, 'Мох', 'moss', u, y + h - 0.01, w * R(0.75, 1.05), R(0.04, 0.07), d + 0.03, o);
    if (chance(wet * 0.6)) ftile(f, 'Мох', 'moss', u + J(w * 0.15), y + h * R(0.3, 0.55), w * R(0.45, 0.8), h * R(0.35, 0.6), d + 0.025, o);
  };
  const rough = (f, st, m) => {
    const area = f.len * (f.y1 - f.y0), n = Math.round(area * 1.1 * T.density * (st.density || 1));
    for (let i = 0; i < n; i++) {
      const u0 = R(-f.len / 2, f.len / 2), y0 = R(f.y0, f.y1);
      if (!open(f, u0, y0)) continue;
      const col = column(f, u0), ft = footOf(col, y0), hf = ft ? y0 - ft.y : 3, clear = clearance(f, col, ft ? ft.y + 1 : y0);
      if (clear < 1.25 && chance(0.85)) continue;
      if (hf > 7 && chance(0.5)) continue;
      const big = (st.big || 1) * (hf < 1.3 ? 1.45 : hf > 7 ? 1.7 : 1), w = Math.min(R(0.3, 0.85) * big, f.len * 0.9), h = Math.min(R(0.22, 0.55) * big, (f.y1 - f.y0) * 0.9);
      const d = Math.min(R(0.1, 0.34) * big, clear < 1.25 ? 0.06 : clear < 2.2 ? 0.16 : 1), u = Math.max(-f.len / 2 + w / 2, Math.min(f.len / 2 - w / 2, u0)), y = Math.max(f.y0, Math.min(f.y1 - h, y0 - h / 2));
      if (!open(f, u - w * 0.4, y + h * 0.15) || !open(f, u + w * 0.4, y + h * 0.15) || !open(f, u - w * 0.4, y + h * 0.85) || !open(f, u + w * 0.4, y + h * 0.85)) continue;
      const [x, z] = fpos(f, u, 0.1);
      if (skipped(x, y + h / 2, z)) continue;
      const o = { sink: Math.min(0.14, d * R(0.25, 0.6)), roll: J(5), yaw: J(9), pitch: J(4) };
      ftile(f, st.name, st.stone || m, u, y, w, h, d, o);
      mossOn(f, u, y, w, h, d, o, S.damp(x, y, z) * (hf < 1.5 ? 1.2 : 0.8));
    }
  };
  // Ряды блоков: выступающий блок стоит точно на блоке текстуры (она развёрнута по миру), у повёрнутых стен — по самой стене.
  const course = (f, st, m) => {
    const { bw, bh } = st, k0 = Math.ceil((f.y0 + 0.02) / bh), k1 = Math.floor((f.y1 - bh + 0.001) / bh), alongX = f.k === 'N' || f.k === 'S';
    const origin = f.axis ? (alongX ? f.b.cx : f.b.cz) : 0, flip = f.axis ? (f.c + f.s > 0 ? 1 : -1) * (alongX ? 1 : -1) : 1;
    for (let k = k0; k <= k1; k++) {
      const off = (k % 2 === 0 ? 0.5 : 0) * bw, a0 = origin * flip - f.len / 2, i0 = Math.ceil((a0 - off) / bw), i1 = Math.floor((a0 + f.len - off) / bw) - 1, y = k * bh;
      for (let i = i0; i <= i1; i++) {
        const roll = rnd();
        if (roll > st.p * T.density + 0.05 * T.age) continue;
        const u = ((i + 0.5) * bw + off - origin * flip), col = column(f, u), ft = footOf(col, y), hf = ft ? y - ft.y : 3;
        if (!open(f, u, y + bh / 2) || !open(f, u - bw * 0.42, y + bh / 2) || !open(f, u + bw * 0.42, y + bh / 2)) continue;
        const [x, z] = fpos(f, u, 0.1), clear = clearance(f, col, ft ? ft.y + 1 : y);
        if (skipped(x, y + bh / 2, z) || clear < 1.25) continue;
        const wet = S.damp(x, y, z), stone = st.stone || m;
        if (roll < 0.007 * T.age) {                                                                 // блок выпал: тёмное гнездо, сам лежит у подошвы
          const part = chance(0.5) ? 1 : R(0.45, 0.6);
          ftile(f, 'Гнездо выпавшего блока', 'soot', u + (1 - part) * bw / 2 * pick([-1, 1]), y + 0.01, bw * part - 0.03, bh - 0.02, 0.012, { sink: 0 });
          if (ft && hf < 3.4 && !ft.ramp && clear > 1.8) { const [px, pz] = fpos(f, u + J(0.3), R(0.35, 0.8)); put('Выпавший блок', stone, px, ft.y, pz, bw * R(0.85, 1), bh * R(0.8, 1), Math.min(0.4, bw * 0.6), { ry: f.ry + J(50), roll: chance(0.4) ? J(14) : 0 }); }
        } else if (roll < 0.03 * T.age) {                                                           // треснул: две половины, одна просела
          const cut = R(0.35, 0.65) * bw, d = R(...st.depth);
          ftile(f, st.name, stone, u - bw / 2 + cut / 2, y, cut - 0.02, bh - 0.015, d, { sink: 0.01, roll: J(1.5) });
          ftile(f, st.name, stone, u + cut / 2, y - 0.012, bw - cut - 0.02, bh - 0.015, d * R(0.5, 1.4), { sink: 0.01, roll: J(3) });
        } else if (roll < 0.05 * T.age) {                                                           // сколотый угол
          ftile(f, 'Скол', 'soot', u + pick([-1, 1]) * bw * 0.32, y + pick([0.02, bh * 0.55]), bw * R(0.2, 0.34), bh * R(0.3, 0.42), 0.012, { sink: 0, roll: J(14) });
        } else {
          const d = R(...st.depth) * (chance(0.08) ? 1.8 : 1), o = { sink: 0.012, roll: J(0.9), yaw: J(1.6), pitch: J(1.2) };
          ftile(f, st.name, stone, u, y + 0.006, bw - R(0.015, 0.035), bh - R(0.012, 0.03), d, o);
          mossOn(f, u, y, bw, bh, d, o, wet * (hf < 1.5 ? 1 : 0.6) * (bw < 0.5 ? 0.5 : 1));
        }
      }
    }
    // Трещины осадки: ломаная из тёмных штрихов сверху вниз.
    for (let i = 0, n = Math.round(f.len * (f.y1 - f.y0) / 26 * T.age + rnd() * 0.6); i < n; i++) {
      let u = R(-f.len / 2 + 0.3, f.len / 2 - 0.3), y = R(f.y0 + 1, f.y1 - 0.2);
      for (let j = 0, c = Math.round(R(3, 8)); j < c && y > f.y0 + 0.3; j++) {
        const h = R(0.25, 0.6), lean = J(28), [x, z] = fpos(f, u, 0.1);
        if (!open(f, u, y - h / 2) || skipped(x, y, z)) break;
        ftile(f, 'Трещина', 'soot', u, y - h, R(0.018, 0.04), h + 0.03, 0.012, { sink: 0, roll: lean });
        u -= Math.sin(lean * DEG) * h * -1; y -= h * Math.cos(lean * DEG);
      }
    }
  };
  const boards = (f, m) => {
    if (!f.axis) return;
    const probe = [-0.45, 0, 0.45].map(t => t * f.len).filter(u => open(f, u, (f.y0 + f.y1) / 2));
    if (!probe.length || f.len < 0.5) return;
    const [x, z] = fpos(f, 0, 0), kk = 'NESW'[Math.round(f.ry / 90) % 4], plane = kk === 'N' || kk === 'S' ? z : x, c = kk === 'N' || kk === 'S' ? f.b.cx : f.b.cz;
    D.boards(kk, plane, c - f.len / 2, c + f.len / 2, f.y0 + 0.02, f.y1, { wear: 0.6 + T.age * 0.5 });
    for (const sgn of [-1, 1]) ftile(f, 'Стойка каркаса', 'timber', sgn * (f.len / 2 - 0.1), f.y0, 0.18, f.y1 - f.y0, 0.1, { sink: 0.02, roll: J(0.5) });
  };
  const bones = f => {
    const n = Math.round(f.len * (f.y1 - f.y0) * 5.5 * T.density);
    for (let i = 0; i < n; i++) {
      const u = R(-f.len / 2 + 0.12, f.len / 2 - 0.12), y = R(f.y0, f.y1 - 0.2);
      if (!open(f, u, y + 0.1)) continue;
      if (chance(0.3)) { const s = R(0.17, 0.22); ftile(f, 'Череп в стене', 'bone', u, y, s, s, s * 1.1, { sink: s * 0.45, roll: J(12), yaw: J(14), round: true }); }
      else { const s = R(0.06, 0.1); ftile(f, 'Торец кости', 'bone', u, y, s, s, 0.2, { sink: 0.1, roll: R(0, 90) }); }
    }
  };
  // Скальные рёбра и наплывы мха на рваной стене.
  const roughExtras = (f, m) => {
    for (let u = -f.len / 2 + R(0.6, 2.4); u < f.len / 2 - 0.6; u += R(2.6, 5.2)) {
      const col = column(f, u), ft = col.feet[0];
      if (!ft || ft.ramp) continue;
      const clear = clearance(f, col, ft.y + 1), [x, z] = fpos(f, u, 0.2), wet = S.damp(x, ft.y, z);
      if (skipped(x, ft.y + 1, z) || !open(f, u, ft.y + 0.6)) continue;
      if (clear > 3.4 && chance(0.55)) {                                                             // ребро: несколько глыб друг на друге
        let y = ft.y, w = R(0.9, 1.7), d = R(0.3, 0.48);
        for (let j = 0, c = Math.round(R(2, 4)); j < c && y < f.y1 - 0.8 && open(f, u, y + 0.5); j++) { const h = R(0.7, 1.5); ftile(f, 'Скальное ребро', m, u + J(0.15), y - 0.02, w, h, d, { sink: 0.06, roll: J(5), yaw: J(12), pitch: J(4) }); y += h * R(0.8, 0.95); w *= R(0.7, 0.9); d *= R(0.7, 0.9); }
      }
      if (chance(wet + 0.1) && f.axis) { const [kk, plane, uu] = toKP(f, u); D.mossWall(kk, plane, uu, ft.y + R(0.4, 3), 0.7 + wet, { base: ft.y }); }
    }
  };
  const toKP = (f, u) => { const [x, z] = fpos(f, u, 0), kk = 'NESW'[Math.round(f.ry / 90) % 4]; return kk === 'N' || kk === 'S' ? [kk, z, x] : [kk, x, z]; };

  /// Рельеф на всех открытых гранях стен, колонн, платформ и плит.
  const skins = () => {
    for (const b of boxes) {
      if (!SKIN.has(b.cat) || b.o.bare) continue;
      const m = b.o.m, st = STYLES[m], first = room.items.length;
      if (!st) continue;
      for (const f of faces(b)) {
        if (f.len < 0.25 || f.y1 - f.y0 < 0.2) continue;
        if (st.kind === 'rough') { rough(f, st, m); if (b.cat !== 'floor' && b.cat !== 'platform' && f.y1 - f.y0 > 1.6) roughExtras(f, m); }
        else if (st.kind === 'course') course(f, st, m);
        else if (st.kind === 'boards') { if (b.cat === 'wall') boards(f, m); }
        else if (st.kind === 'bones') bones(f);
        if (b.cat === 'floor' || b.cat === 'stairs') continue;
        for (const col of f.cols.values()) for (const ft of col.feet) { const [x, z] = fpos(f, col.uc, 0.45); S.feet.push({ x, z, y: ft.y, ramp: ft.ramp, m, kind: st.kind, clear: clearance(f, col, ft.y + 1), f, u: col.uc }); }
        if (st.kind === 'course' && f.axis) plinth(f, st, m);
      }
      // Детали на верхе стен клетки уходят в его слой: он скрыт, пока смотрят внутрь комнаты.
      if (b.cat === 'upper') for (let i = first; i < room.items.length; i++) room.items[i].up = true;
    }
  };
  // Цоколь: ряд крупных блоков у подошвы рукотворной стены.
  const plinth = (f, st, m) => {
    if (st.bw < 0.5) return;
    for (const col of f.cols.values()) {
      const ft = col.feet[0];
      if (!ft || ft.ramp || clearance(f, col, ft.y + 1) < 1.25 || !open(f, col.uc, ft.y + 0.2)) continue;
      const [x, z] = fpos(f, col.uc, 0.1);
      if (skipped(x, ft.y + 0.2, z) || chance(0.1 * T.age)) continue;
      ftile(f, 'Блок цоколя', st.stone || m, col.uc + J(0.02), ft.y, 0.5 - R(0.012, 0.03), R(0.36, 0.46), R(0.1, 0.15), { sink: 0.03, roll: J(0.8), yaw: J(1.5) });
    }
  };

  /// Верх стен, под которым воздух: у скалы камни, у кладки венец из плит, местами сбитых.
  const crowns = () => {
    for (const b of boxes) {
      if (b.cat !== 'wall' && b.cat !== 'column') continue;
      const m = b.o.m, st = STYLES[m];
      if (!st || !air(b.cx, b.y1 + 0.3, b.cz, b)) continue;
      const lw = b.hw * 2, ld = b.hd * 2, long = Math.max(lw, ld), short = Math.min(lw, ld), alongX = lw >= ld;
      const P = (lu, lv) => alongX ? [lu * b.c + lv * b.s + b.cx, -lu * b.s + lv * b.c + b.cz] : [lv * b.c + lu * b.s + b.cx, -lv * b.s + lu * b.c + b.cz];
      if (st.kind === 'rough') {
        for (let i = 0, n = Math.round(lw * ld * 0.55); i < n; i++) { const [x, z] = P(J(long / 2 - 0.2), J(short / 2 - 0.15)), w = R(0.35, 0.9), h = R(0.2, 0.5); if (air(x, b.y1 + 0.2, z, b)) { put('Камень на глыбе', m, x, b.y1 - h * 0.3, z, w, h, R(0.3, 0.7), { ry: R(0, 180), roll: J(10), pitch: J(8) }); if (chance(S.damp(x, b.y1, z))) put('Мох', 'moss', x, b.y1 + h * 0.68, z, w * 0.9, 0.05, w * 0.6, { ry: R(0, 180), round: true }); } }
      } else if (st.kind === 'course' && short <= 1.6) {
        for (let u = -long / 2; u < long / 2 - 0.2;) {
          const len = Math.min(R(0.55, 1), long / 2 - u), [x, z] = P(u + len / 2, 0);
          if (air(x, b.y1 + 0.2, z, b) && !chance(0.14 * T.age)) put('Плита венца', st.stone || m, x, b.y1 - 0.01, z, alongX ? len - 0.025 : short + 0.07, R(0.1, 0.15), alongX ? short + 0.07 : len - 0.025, { ry: b.ry + J(1.5), roll: chance(0.12 * T.age) ? J(4) : 0, pitch: chance(0.12 * T.age) ? J(4) : 0 });
          u += len;
        }
      }
    }
  };

  /// Пол: булыжники, приподнятые и выбитые плиты, грязь, пыль и мох.
  const floors = () => {
    const X0 = isHall ? ext.x0 : -EDGE, X1 = isHall ? ext.x1 : EDGE, Z0 = isHall ? ext.z0 : -EDGE, Z1 = isHall ? ext.z1 : EDGE;
    for (let gx = X0 + 0.25; gx < X1; gx += 0.5) for (let gz = Z0 + 0.25; gz < Z1; gz += 0.5) {
      const x = gx + J(0.24), z = gz + J(0.24);
      for (const t of tops(x, z)) {
        const m = t.b.o.m, y = t.y, wet = S.damp(x, y, z);
        if (t.b.o.bare || skipped(x, y + 0.02, z) || t.b.cat === 'stairs' || m === 'planks' || m === 'wood') continue;
        if (PAVED.has(m)) {
          const st = STYLES[m], roll = rnd(), bw = st.bw, bh = st.bh;
          if (roll < 0.05 * T.age && bw > 0.5) {                                                     // приподнятая, просевшая или выбитая плита на шве текстуры
            const k = Math.floor(-z / bh), off = (k % 2 === 0 ? 0.5 : 0) * bw, i = Math.floor((x - off) / bw), px = (i + 0.5) * bw + off, pz = -(k + 0.5) * bh;
            if (tops(px - bw * 0.4, pz).some(q => q.b === t.b) && tops(px + bw * 0.4, pz).some(q => q.b === t.b)) {
              if (roll < 0.008 * T.age) put('Гнездо выбитой плиты', 'dirt', px, y, pz, bw - 0.03, 0.012, bh - 0.02);
              else put('Плита пола', m, px, y - 0.01, pz, bw - R(0.02, 0.04), R(0.025, 0.05), bh - R(0.015, 0.03), { ry: J(1.5), roll: J(2), pitch: J(2.5) });
            }
          } else if (roll < 0.05 * T.age + 0.02) D.patch(x, z, R(0.4, 0.9), { m: 'dirt', name: 'Пыль', y, n: 3 });
          else if (roll < 0.05 * T.age + 0.02 + wet * 0.05) D.patch(x, z, R(0.4, 0.9), { y, n: 4 });
          else if (roll > 0.992 - 0.004 * T.age) for (let j = 0, c = Math.round(R(2, 5)), cx = x, cz = z, a = R(0, 360); j < c; j++) { const len = R(0.3, 0.7); put('Трещина пола', 'soot', cx, y, cz, 0.03, 0.012, len, { ry: a }); cx += Math.sin(a * DEG) * len * 0.9; cz += Math.cos(a * DEG) * len * 0.9; a += J(40); if (!tops(cx, cz).some(q => Math.abs(q.y - y) < 0.01)) break; }
        } else {
          const roll = rnd(), dens = m === 'cobble' || m === 'rock' ? 0.085 : m === 'dirt' || m === 'sand' || m === 'ash' ? 0.035 : 0.06;
          if (roll < dens) put(m === 'sand' ? 'Камень в песке' : 'Булыжник', m === 'cobble' || m === 'dirt' || m === 'sand' || m === 'ash' ? 'pebble' : m, x, y - 0.01, z, R(0.2, 0.46), R(0.035, 0.08), R(0.18, 0.4), { ry: R(0, 180), round: chance(0.5), roll: J(5), pitch: J(5) });
          else if (roll < dens + 0.012) D.patch(x, z, R(0.5, 1.2), { m: m === 'dirt' ? 'pebble' : 'dirt', name: m === 'dirt' ? 'Щебень' : 'Грязь', y, n: 4 });
          else if (roll < dens + 0.012 + wet * 0.045) D.patch(x, z, R(0.5, 1.1), { y, n: 5 });
        }
      }
    }
  };

  /// Осыпь и обломки у подошвы стен: у скалы кучи камней, у кладки редкие выпавшие блоки и крошка.
  const debris = () => {
    let next = 0;
    for (const ft of S.feet) {
      if (--next > 0) continue;
      if (ft.ramp || ft.clear < 2 || skipped(ft.x, ft.y + 0.3, ft.z) || !tops(ft.x, ft.z).some(t => Math.abs(t.y - ft.y) < 0.05)) continue;
      const wet = S.damp(ft.x, ft.y, ft.z), rad = Math.min(0.9, ft.clear * 0.22);
      if (ft.kind === 'rough') {
        next = Math.round(R(3, 6));
        D.rubble(ft.x, ft.z, R(0.5, 1) * rad / 0.9, Math.round(R(5, 11)), { m: STYLES[ft.m].stone || ft.m, name: 'Камень осыпи', pile: 0.3, big: 0.35, y: ft.y });
        if (chance(wet + 0.1)) D.patch(ft.x, ft.z, 0.8, { n: 4, y: ft.y });
      } else if (ft.kind === 'course') {
        next = Math.round(R(7, 16) / Math.max(0.4, T.age));
        D.rubble(ft.x, ft.z, R(0.4, 0.8) * rad / 0.9, Math.round(R(3, 8)), { m: STYLES[ft.m].stone || ft.m, name: 'Крошка кладки', pile: 0.2, big: 0.12, y: ft.y });
      } else next = 4;
    }
  };

  /// Колонны: база, пояса барабанов и капитель; у оборванной колонны рваный верх.
  const columns = () => {
    for (const b of boxes) {
      if (b.cat !== 'column' || b.o.bare) continue;
      const o = b.o, m = o.m, s = o.s[0], h = o.s[1], round = o.round;
      if (!STYLES[m] || STYLES[m].kind === 'boards' || s < 0.5 || h < 1.5) continue;
      const stone = STYLES[m].stone || m, grounded = solid(b.cx, b.y0 - 0.1, b.cz, b), capped = solid(b.cx, b.y1 + 0.1, b.cz, b);
      if (grounded) { put('База колонны', stone, b.cx, b.y0, b.cz, s + 0.26, 0.28, s + 0.26, { ry: b.ry, round }); put('Вал базы', stone, b.cx, b.y0 + 0.28, b.cz, s + 0.13, 0.12, s + 0.13, { ry: b.ry, round }); }
      if (capped) { put('Капитель', stone, b.cx, b.y1 - 0.34, b.cz, s + 0.3, 0.34, s + 0.3, { ry: b.ry, round }); put('Шейка капители', stone, b.cx, b.y1 - 0.46, b.cz, s + 0.12, 0.12, s + 0.12, { ry: b.ry, round }); }
      else for (let i = 0; i < 5; i++) put('Скол колонны', stone, b.cx + J(s * 0.3), b.y1 - 0.08, b.cz + J(s * 0.3), R(0.2, 0.45), R(0.12, 0.35), R(0.2, 0.4), { ry: R(0, 180), roll: J(18), pitch: J(18) });
      for (let y = b.y0 + R(1.2, 1.6); y < b.y1 - 0.9; y += R(1.1, 1.5)) put('Шов барабана', stone, b.cx + J(0.012), y, b.cz + J(0.012), s + 0.045, 0.05, s + 0.045, { ry: b.ry + J(2), round });
    }
  };

  /// Марши: сбитые кромки ступеней, камешки и мох по краям.
  const stairs = () => {
    for (const b of boxes) {
      if (b.cat !== 'stairs' || b.o.bare) continue;
      const rise = b.y1 - b.foot, steps = Math.max(2, Math.round(rise / 0.25)), run = 2 * b.hd / steps, m = b.o.m, wood = m === 'planks' || m === 'wood';
      for (let i = 0; i < steps; i++) {
        const y = b.foot + rise * (i + 1) / steps, lz = run * (i + 0.5) - b.hd;
        for (const sgn of [-1, 1]) {
          if (!chance(0.3 * T.age)) continue;
          const lx = sgn * (b.hw - R(0.12, 0.4)), x = lx * b.c + lz * b.s + b.cx, z = -lx * b.s + lz * b.c + b.cz, wet = S.damp(x, y, z);
          if (wood) put('Щепа', 'wood', x, y, z, R(0.12, 0.3), 0.02, R(0.04, 0.08), { ry: R(0, 180) });
          else if (chance(wet)) put('Мох', 'moss', x, y, z, R(0.25, 0.5), 0.025, R(0.15, 0.28), { ry: b.ry + J(15), round: true });
          else put('Скол ступени', STYLES[m] && STYLES[m].stone || m, x, y, z, R(0.1, 0.24), R(0.04, 0.09), R(0.08, 0.2), { ry: R(0, 180), roll: J(12) });
        }
        if (!wood && chance(0.12 * T.age)) { const lx = J(b.hw - 0.4), x = lx * b.c + (lz + run * 0.45) * b.s + b.cx, z = -lx * b.s + (lz + run * 0.45) * b.c + b.cz; put('Выщербина', 'soot', x, y - 0.02, z, R(0.2, 0.45), 0.025, 0.05, { ry: b.ry + J(6) }); }
      }
    }
  };

  /// Двери: полотно из досок с коваными полосами, каменный или деревянный портал с обеих сторон.
  const doors = () => {
    for (const o of room.items.filter(o => o.name === 'Door')) {
      const [x, y, z] = o.p, alongX = (o.ry || 0) % 180 === 0, side = alongX ? [1, 0] : [0, 1], jamb = solid(x + side[0] * 0.8, y + 1, z + side[1] * 0.8) || solid(x - side[0] * 0.8, y + 1, z - side[1] * 0.8);
      const th = jamb ? Math.min(1.2, 2 * (alongX ? Math.abs(jamb.hw * jamb.s) + Math.abs(jamb.hd * jamb.c) : Math.abs(jamb.hw * jamb.c) + Math.abs(jamb.hd * jamb.s))) : 0.6, mid = jamb ? (alongX ? jamb.cz : jamb.cx) : (alongX ? z : x);
      const stone = T.doors !== 'wood' ? T.doors : null, jm = jamb && STYLES[jamb.o.m] && STYLES[jamb.o.m].kind !== 'boards' ? (STYLES[jamb.o.m].stone || jamb.o.m) : null;
      for (const sgn of [-1, 1]) {
        const k = alongX ? (sgn > 0 ? 'N' : 'S') : (sgn > 0 ? 'E' : 'W'), u = alongX ? x : z, plane = mid + sgn * th / 2, leaf = (alongX ? z : x) + sgn * 0.06;
        for (let i = 0; i < 5; i++) D.tile('Доска двери', 'wood', k, leaf, u - 0.44 + i * 0.22, y + 0.03, 0.21, 2.34, 0.03, { sink: 0, roll: J(0.4) });
        for (const hy of [0.45, 1.85]) D.tile('Кованая полоса', 'iron', k, leaf, u, y + hy, 1, 0.08, 0.02, { sink: -0.03 });
        D.tile('Кольцо двери', 'iron', k, leaf, u + 0.36, y + 1.05, 0.12, 0.12, 0.04, { sink: -0.03, round: false });
        if (jm || stone) {
          const pm = stone || (jm === 'rock' ? 'slab' : jm);
          for (const s2 of [-1, 1]) for (let j = 0, yy = y; yy < y + 2.38; j++) { const hh = Math.min(R(0.38, 0.6), y + 2.4 - yy); D.tile('Камень портала', pm, k, plane, u + s2 * (0.55 + (j % 2 ? 0.24 : 0.17)), yy, j % 2 ? 0.46 : 0.32, hh - 0.015, R(0.07, 0.12), { sink: 0.02, roll: J(0.8) }); yy += hh; }
          D.tile('Перемычка портала', pm, k, plane, u, y + 2.4, 1.9, 0.34, 0.13, { sink: 0.02, roll: J(0.6) });
        } else D.doorFrame(k, plane, u, { y });
        D.tile('Порог', 'slab', k, plane, u, y, 1.4, 0.05, 0.22, { sink: 0.02 });
      }
      S.keep(x - (alongX ? 1 : th / 2 + 1.1), y, z - (alongX ? th / 2 + 1.1 : 1), x + (alongX ? 1 : th / 2 + 1.1), y + 0.6, z + (alongX ? th / 2 + 1.1 : 1));
    }
  };

  /// Проёмы клетки: блоки арки, замковый камень, кольцо с цепью. Деталей в самом проходе нет.
  const gates = () => {
    for (const b of S.sides) for (const t of doorsOf(b.o.side)) {
      const k = BACK[b.o.side], plane = (DIRS[b.o.side][0] || DIRS[b.o.side][1]) * INNER, m = T.arch;
      const here = S.wall(...D.at(k, plane, t, 0.4), b.o.side, 3.2, 1);                                // арка стоит на стороне клетки, если та открыта в комнату
      if (here && Math.abs(here[1] - plane) < 0.1) {
        D.tile('Замковый камень', m, k, plane, t, 2.75, 0.5, 0.6, 0.3, { sink: 0.02, roll: J(3) });
        for (const sgn of [-1, 1]) { D.tile('Блок арки', m, k, plane, t + sgn * 1.25, 0, 0.5, R(1.5, 2.4), 0.26, { sink: 0.05, roll: J(2) }); D.tile('Блок арки', m, k, plane, t + sgn * 0.75, 2.6 + R(0, 0.2), 0.55, 0.42, 0.24, { sink: 0.04, roll: sgn * R(12, 24) }); }
        if (T.rings !== false) { const side = pick([-1, 1]), [cx, cz] = D.at(k, plane, t + side * 1.7, 0.08); D.tile('Кольцо в стене', 'iron', k, plane, t + side * 1.7, 2.3, 0.16, 0.16, 0.07, { sink: 0 }); D.chain(cx, 2.3, cz, R(0.6, 1.5)); }
      }
      const [x, z] = D.at(k, plane, t, 0);
      S.keep(x - 1.3, 0, z - 1.3, x + 1.3, 2.95, z + 1.3);
    }
  };

  /// Свет: держатель факела и копоть над ним, огарки свечей, чаша жаровни, костёр, обод люстры на цепи.
  const lights = () => {
    for (const o of room.items.filter(o => o.cat === 'light')) {
      const [x, y, z] = o.p;
      if (o.name === 'Torch') {
        const k = 'NESW'[Math.round((o.ry || 0) / 90) % 4], w = S.wall(x + DIRS[k][0] * 0.3, z + DIRS[k][1] * 0.3, BACK[k], y - 0.2, 1.2);
        if (!w) continue;
        D.tile('Держатель факела', 'iron', w[0], w[1], w[2], y - 0.45, 0.1, 0.34, 0.3, { sink: 0.02, pitch: -20 });
        const [tx, tz] = D.at(w[0], w[1], w[2], 0.24); put('Факел', 'timber', tx, y - 0.4, tz, 0.07, 0.5, 0.07, { round: true }); put('Пламя факела', 'ember', tx, y + 0.08, tz, 0.12, 0.2, 0.12, { round: true });
        for (let i = 0; i < 5; i++) D.blot('Копоть', 'soot', w[0], w[1], w[2] + J(0.12), y + 0.35 + i * 0.22, R(0.3, 0.5) - i * 0.04, R(0.3, 0.45), 0.012, -99);
      } else if (o.name === 'Candle') {
        const fy = S.floor(x, z, y - 0.3), by = fy === undefined || Math.abs(fy - (y - 0.3)) > 0.6 ? y - 0.3 : fy;
        for (let i = 0, n = Math.round(R(3, 6)); i < n; i++) { const h = R(0.06, 0.24), cx = x + J(0.16), cz = z + J(0.16); put('Свеча', 'bone', cx, by, cz, 0.05, h, 0.05, { round: true }); put('Огонёк', 'ember', cx, by + h, cz, 0.025, 0.05, 0.025, { round: true }); }
        put('Наплыв воска', 'bone', x, by, z, 0.42, 0.015, 0.36, { round: true, ry: R(0, 180) });
      } else if (o.name === 'Brazier') {
        const fy = S.floor(x, z, y - 0.9), by = fy === undefined || Math.abs(fy - (y - 0.9)) > 1.2 ? y - 0.9 : fy;
        for (let i = 0; i < 3; i++) { const a = (i * 120 + 20) * DEG; D.beam('Нога жаровни', 'iron', [x + Math.cos(a) * 0.3, by, z + Math.sin(a) * 0.3], [x + Math.cos(a) * 0.14, by + 0.66, z + Math.sin(a) * 0.14], 0.045); }
        put('Чаша жаровни', 'iron', x, by + 0.62, z, 0.7, 0.22, 0.7, { round: true }); put('Угли', 'ember', x, by + 0.8, z, 0.56, 0.08, 0.56, { round: true });
        D.patch(x, z, 0.7, { m: 'soot', name: 'Сажа', y: by, n: 4 });
      } else if (o.name === 'Fire') {
        const fy = S.floor(x, z, y - 0.5), by = fy === undefined ? y - 0.5 : fy;
        for (let i = 0; i < 9; i++) { const a = i / 9 * 2 * Math.PI; put('Камень кострища', 'rock', x + Math.cos(a) * 0.55, by, z + Math.sin(a) * 0.55, R(0.22, 0.34), R(0.14, 0.22), R(0.18, 0.26), { ry: R(0, 180), roll: J(8) }); }
        for (let i = 0; i < 4; i++) D.beam('Полено', 'wood', [x + J(0.3), by + 0.05, z + J(0.3)], [x + J(0.1), by + R(0.3, 0.45), z + J(0.1)], 0.11);
        put('Угли', 'ember', x, by + 0.02, z, 0.6, 0.1, 0.6, { round: true }); D.patch(x, z, 1, { m: 'soot', name: 'Зола', y: by, n: 5 });
      } else if (o.name === 'Chandelier') {
        put('Обод люстры', 'iron', x, y, z, 1.7, 0.09, 1.7, { round: true }); put('Ступица люстры', 'iron', x, y + 0.02, z, 0.3, 0.3, 0.3, { round: true });
        for (let i = 0; i < 8; i++) { const a = i / 8 * 2 * Math.PI, cx = x + Math.cos(a) * 0.8, cz = z + Math.sin(a) * 0.8; put('Свеча', 'bone', cx, y + 0.09, cz, 0.06, R(0.1, 0.22), 0.06, { round: true }); put('Огонёк', 'ember', cx, y + 0.3, cz, 0.03, 0.06, 0.03, { round: true }); if (i % 2 === 0) D.beam('Цепь люстры', 'iron', [cx, y + 0.08, cz], [x, y + 1.6, z], 0.03); }
        let top = y + 1.6; while (top < y + 30 && air(x, top + 0.2, z)) top += 0.2;
        D.beam('Цепь люстры', 'iron', [x, y + 1.6, z], [x, top + 0.2, z], 0.05);
      }
    }
  };

  /// Пропсы: вид из частей по имени (kits.js, наборы detail.js и свои наборы комнаты S.kit), тело становится невидимым.
  S.kit = {};
  const props = () => {
    for (const o of room.items.filter(o => o.cat === 'prop' && !o.hide && !S.done.has(o))) {
      const fn = S.kit[o.name] || kits[o.name] || D.kit[o.name];
      if (!fn) continue;
      const args = [o.p[0], o.p[1], o.p[2], o.s[0], o.s[2], o.s[1], o.ry || 0, o];
      if (!(fn === D.kit[o.name] ? fn(...args) : fn(S, ...args))) o.hide = true;
      S.done.add(o);
    }
  };
  /// Имена пропсов, для которых нет набора частей: их тело остаётся цветным блоком.
  S.plain = () => [...new Set(room.items.filter(o => o.cat === 'prop' && !o.hide).map(o => o.name))];

  // ---------- помощники для сцен комнаты ----------
  /// Вещь из набора частей без тела в блокауте (только вид): имя набора, центр, размеры w × d × h. Крупное ставить у стен и в углах, не в проходах.
  S.set = (name, x, z, w, d, h, { y, ry = 0, m } = {}) => {
    const fn = S.kit[name] || kits[name] || D.kit[name], fy = y === undefined ? S.floor(x, z, 0) || 0 : y;
    if (!fn) throw new Error(`${room.name}: нет набора «${name}»`);
    fn === D.kit[name] ? fn(x, fy, z, w, d, h, ry) : fn(S, x, fy, z, w, d, h, ry, { m, name });
    return S;
  };
  /// Знамя на стене: древко, полотнище, рваный низ. wall — результат S.wall(), y — верх.
  S.banner = (wall, y, { w = 0.9, h = 2, m = 'clothRed', torn = 0.5, sign = 'iron' } = {}) => {
    const [k, plane, u] = wall;
    D.tile('Древко знамени', 'timber', k, plane, u, y, w + 0.3, 0.06, 0.06, { sink: -0.04 });
    const strips = 4;
    for (let i = 0; i < strips; i++) { const len = h * (chance(torn) ? R(0.55, 0.9) : 1); D.tile('Полотнище', m, k, plane, u - w / 2 + (i + 0.5) * w / strips, y - len, w / strips - 0.01, len, 0.02, { sink: -0.03, roll: J(1.2) }); }
    if (sign) D.blot('Знак на знамени', sign, k, plane, u, y - h * 0.38, w * 0.42, w * 0.42, 0.075, -99);
    return S;
  };
  /// Кольцо в стене с цепью и кандалами.
  S.shackle = (wall, y = 1.9, len = 0.9) => {
    const [k, plane, u] = wall, [x, z] = D.at(k, plane, u, 0.07);
    D.tile('Кольцо в стене', 'iron', k, plane, u, y, 0.16, 0.16, 0.07, { sink: 0 }); D.chain(x, y, z, len);
    put('Кандалы', 'iron', x, y - len - 0.12, z, 0.14, 0.05, 0.14, { round: true });
    return S;
  };
  /// Вешалка: планка с колышками, на них плащи и ремни.
  S.pegs = (wall, { y = 1.7, n = 3, w = 1.2 } = {}) => {
    const [k, plane, u] = wall;
    D.tile('Планка вешалки', 'timber', k, plane, u, y, w, 0.08, 0.04, { sink: 0 });
    for (let i = 0; i < n; i++) {
      const t = u - w / 2 + (i + 0.5) * w / n;
      D.tile('Колышек', 'timber', k, plane, t, y + 0.01, 0.04, 0.04, 0.14, { sink: 0 });
      if (chance(0.7)) D.tile(pick(['Плащ', 'Плащ', 'Ремень', 'Сума']), pick(['cloth', 'clothBlue', 'leather', 'clothRed']), k, plane, t + J(0.03), y - R(0.7, 1.1), R(0.22, 0.4), R(0.7, 1.1), 0.07, { sink: -0.05, roll: J(3) });
    }
    return S;
  };
  /// Доска на стене с листами (расписание, указ, ведомость) и счётом зарубок.
  S.board = (wall, { y = 1.3, w = 1.2, h = 0.8, sheets = 3, name = 'Доска' } = {}) => {
    const [k, plane, u] = wall;
    D.tile(name, 'wood', k, plane, u, y, w, h, 0.04, { sink: 0, roll: J(1.5) });
    for (let i = 0; i < sheets; i++) D.tile('Лист', 'paper', k, plane, u - w / 2 + 0.2 + (w - 0.4) * (sheets > 1 ? i / (sheets - 1) : 0.5) + J(0.04), y + R(0.12, h - 0.45), R(0.2, 0.28), R(0.26, 0.36), 0.012, { sink: -0.04, roll: J(7) });
    return S;
  };
  /// Половик, шкура, расстеленная ткань.
  S.rug = (x, z, w, d, { m = 'cloth', y, ry = 0, name = 'Половик' } = {}) => { put(name, m, x, (y === undefined ? S.floor(x, z, 0) || 0 : y) + 0.012, z, w, 0.02, d, { ry: ry + J(4) }); return S; };
  /// Лужа или пятно на полу: m wet — вода, blood — кровь, soot — гарь.
  S.pool = (x, z, w, d = w, { m = 'wet', y, name = 'Лужа' } = {}) => { put(name, m, x, (y === undefined ? S.floor(x, z, 0) || 0 : y) + 0.005, z, w, 0.012, d, { round: true, ry: R(0, 180) }); return S; };
  /// Паутина в углу под потолком или между двумя точками: провисшие нити.
  S.web = (a, b, { n = 5 } = {}) => {
    for (let i = 0; i < n; i++) { const t = i / Math.max(1, n - 1), p = [a[0] + J(0.05), a[1] - t * R(0.5, 0.9), a[2] + J(0.05)], q = [b[0] + J(0.05), b[1] - t * R(0.5, 0.9), b[2] + J(0.05)]; D.beam('Паутина', 'cloth', p, q, 0.012); }
    return S;
  };
  /// Свисающие корни или лоза: от точки под потолком вниз.
  S.roots = (x, y, z, { n = 5, len = 1.4, spread = 0.5, m = 'root' } = {}) => {
    for (let i = 0; i < n; i++) { const px = x + J(spread), pz = z + J(spread), l = len * R(0.4, 1); D.beam('Корень', m, [px + J(0.15), y - l, pz + J(0.15)], [px, y, pz], R(0.025, 0.06)); }
    return S;
  };
  /// Стопка книг, свитков или бумаг на поверхности.
  S.books = (x, y, z, n = 4, { spread = 0.3 } = {}) => {
    for (let i = 0, top = y; i < n; i++) { if (chance(0.6)) { const h = R(0.04, 0.08); put('Книга', pick(['leather', 'clothRed', 'clothBlue', 'leather']), x + J(0.03), top, z + J(0.03), R(0.2, 0.28), h, R(0.26, 0.34), { ry: R(-25, 25) }); top += h; } else put(pick(['Свиток', 'Лист']), 'paper', x + J(spread), y, z + J(spread), R(0.05, 0.2), 0.03, R(0.24, 0.34), { ry: R(0, 180) }); }
    return S;
  };
  /// Склянки и бутыли на поверхности.
  S.bottles = (x, y, z, n = 4, { spread = 0.25, m = 'glass' } = {}) => { for (let i = 0; i < n; i++) put(pick(['Склянка', 'Бутыль', 'Колба']), chance(0.25) ? 'clay' : m, x + J(spread), y, z + J(spread), R(0.06, 0.12), R(0.1, 0.26), R(0.06, 0.12), { round: true }); return S; };
  /// Оружие и доспех, брошенные на полу или лежащие на поверхности.
  S.arms = (x, z, n = 3, { y, spread = 0.5 } = {}) => {
    const fy = y === undefined ? S.floor(x, z, 0) || 0 : y;
    for (let i = 0; i < n; i++) {
      const px = x + J(spread), pz = z + J(spread), kind = rnd(), a = R(0, 180);
      if (kind < 0.4) { put('Клинок', 'iron', px, fy, pz, 0.06, 0.02, R(0.7, 0.95), { ry: a }); put('Гарда', 'iron', px, fy, pz, 0.2, 0.03, 0.04, { ry: a }); }
      else if (kind < 0.6) put('Шлем', 'iron', px, fy, pz, 0.24, 0.2, 0.26, { round: true, roll: J(30) });
      else if (kind < 0.8) put('Щит', 'wood', px, fy, pz, 0.6, 0.05, 0.6, { round: true, roll: J(6) });
      else put('Древко', 'timber', px, fy, pz, 0.04, 0.04, R(1.4, 2), { ry: a });
    }
    return S;
  };

  const PASSES = { doors, gates, skins, crowns, columns, stairs, floors, debris, lights, props };
  /// Все общие проходы; skip — имена тех, что комната делает сама: doors, gates, skins, crowns, columns, stairs, floors, debris, lights, props.
  S.auto = ({ skip = [] } = {}) => { materials(); for (const [k, fn] of Object.entries(PASSES)) if (!skip.includes(k)) fn(); return S; };
  S.boxes = boxes; S.local = local; S.faces = faces; S.fpos = fpos; S.open = open; S.materials = materials;
  return S;
}

function dress(room, spec) {
  const S = scene(room, spec);
  spec.dress(S);
  room.scene = S;
  if ((spec.look || {}).side !== false) room.side = S.sides.length ? S.sides[0].o.m : undefined;
  return S;
}

/// Детали, которые ничего не касаются: ни тел, ни пропсов, ни других стоящих деталей. Возвращает «имя ×N».
function floaters(room) {
  const S = room.scene || scene(room), PAD = 0.06, cell = 1.5, key = (i, j, k) => `${i}|${j}|${k}`;
  const aabb = o => {
    const [w, h, d] = o.s, tilt = o.q ? Math.hypot(w, h, d) / 2 : 0, a = (o.ry || 0) * DEG, ex = o.q ? tilt : Math.abs(w / 2 * Math.cos(a)) + Math.abs(d / 2 * Math.sin(a)), ez = o.q ? tilt : Math.abs(w / 2 * Math.sin(a)) + Math.abs(d / 2 * Math.cos(a));
    const cy = o.p[1] + h / 2, ey = o.q ? tilt : h / 2;
    return [o.p[0] - ex - PAD, (o.q ? o.p[1] + Math.min(0, h / 2 - tilt) : cy - ey) - PAD, o.p[2] - ez - PAD, o.p[0] + ex + PAD, (o.q ? o.p[1] + tilt + h / 2 : cy + ey) + PAD, o.p[2] + ez + PAD];
  };
  const decor = room.items.filter(o => o.cat === 'decor').map(o => ({ o, q: aabb(o), ok: false }));
  const solids = [...S.boxes.map(b => [b.x0, b.y0, b.z0, b.x1, b.y1, b.z1]), ...room.items.filter(o => o.s && ['prop', 'chest', 'water', 'acid', 'bars', 'machine', 'trap'].includes(o.cat)).map(o => aabb(o))];
  const hit = (a, b) => a[0] < b[3] && a[3] > b[0] && a[1] < b[4] && a[4] > b[1] && a[2] < b[5] && a[5] > b[2];
  const grid = new Map(), cells = q => { const out = []; for (let i = Math.floor(q[0] / cell); i <= Math.floor(q[3] / cell); i++) for (let j = Math.floor(q[1] / cell); j <= Math.floor(q[4] / cell); j++) for (let k = Math.floor(q[2] / cell); k <= Math.floor(q[5] / cell); k++) out.push(key(i, j, k)); return out; };
  decor.forEach(d => { for (const c of cells(d.q)) { if (!grid.has(c)) grid.set(c, []); grid.get(c).push(d); } });
  const queue = [];
  const touch = q => { for (const c of cells(q)) for (const d of grid.get(c) || []) if (!d.ok && hit(q, d.q)) { d.ok = true; queue.push(d); } };
  solids.forEach(touch);
  while (queue.length) touch(queue.pop().q);
  const loose = {};
  decor.filter(d => !d.ok).forEach(d => { (loose[d.o.name] = loose[d.o.name] || []).push(d.o); });
  return Object.entries(loose).sort((a, b) => b[1].length - a[1].length).map(([n, l]) => `${n} ×${l.length} (${l[0].p.join(', ')})`);
}

module.exports = { dress, floaters, scene, STYLES };
