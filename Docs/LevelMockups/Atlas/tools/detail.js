// Детали окружения поверх блокаута: только вид (категория decor), одинаковые при каждой сборке (seed).
// Плоскость стены задаётся стороной, куда она смотрит (N, E, S, W), координатой plane и отрезком u1…u2 вдоль неё
// (мировая x для N и S, мировая z для E и W). Всё, что ставится на стену, утоплено в неё задней гранью.
const FACE = { N: { ry: 0, n: [0, 1] }, E: { ry: 90, n: [1, 0] }, S: { ry: 180, n: [0, -1] }, W: { ry: 270, n: [-1, 0] } };
const DEG = Math.PI / 180, INSIDE = 16.4;

/// lim — границы россыпи { x: [от, до], z: [от, до] }, по умолчанию внутренность клетки: россыпь у стены не уходит в саму стену.
module.exports = (r, seed = 1, lim = { x: [-INSIDE, INSIDE], z: [-INSIDE, INSIDE] }) => {
  const inX = v => Math.max(lim.x[0], Math.min(lim.x[1], v)), inZ = v => Math.max(lim.z[0], Math.min(lim.z[1], v)), inU = (k, u) => k === 'N' || k === 'S' ? inX(u) : inZ(u);
  let s = seed >>> 0;
  const rnd = () => { s = s + 0x6D2B79F5 | 0; let t = Math.imul(s ^ s >>> 15, 1 | s); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; };
  const R = (a, b) => a + (b - a) * rnd(), pick = a => a[Math.floor(rnd() * a.length)], chance = p => rnd() < p, J = v => R(-v, v);
  const mul = (a, b) => [a[3] * b[0] + a[0] * b[3] + a[1] * b[2] - a[2] * b[1], a[3] * b[1] - a[0] * b[2] + a[1] * b[3] + a[2] * b[0], a[3] * b[2] + a[0] * b[1] - a[1] * b[0] + a[2] * b[3], a[3] * b[3] - a[0] * b[0] - a[1] * b[1] - a[2] * b[2]];
  const axis = (i, deg) => { const q = [0, 0, 0, Math.cos(deg * DEG / 2)]; q[i] = Math.sin(deg * DEG / 2); return q; };
  const r4 = q => q.map(v => Math.round(v * 1e4) / 1e4);
  /// Поворот Unity: сначала крен (z), затем тангаж (x), затем курс (y).
  const quat = (pitch, yaw, roll) => r4(mul(mul(axis(1, yaw), axis(0, pitch)), axis(2, roll)));
  const put = (name, m, x, y, z, w, h, d, { ry = 0, pitch = 0, roll = 0, round } = {}) =>
    r.decor(name, x, y, z, w, h, d, Math.abs(pitch) > 0.3 || Math.abs(roll) > 0.3 ? { q: quat(pitch, ry, roll), m, round } : { ry, m, round });
  /// Точка на плоскости стены: u вдоль, out от неё.
  const at = (k, plane, u, out = 0) => k === 'N' || k === 'S' ? [u, plane + FACE[k].n[1] * out] : [plane + FACE[k].n[0] * out, u];
  const hits = (skip, u1, u2, y1, y2) => (skip || []).some(([a, b, c, d]) => u2 > a && u1 < b && y2 > c && y1 < d);
  /// Плитка на стене: центр (u, y по низу), w вдоль стены, h вверх, d толщина, sink — насколько утоплена.
  const tile = (name, m, k, plane, u, y, w, h, d, { sink = 0.02, roll = 0, yaw = 0, pitch = 0, round } = {}) => {
    const [x, z] = at(k, plane, u, d / 2 - sink);
    return put(name, m, x, y, z, w, h, d, { ry: FACE[k].ry + yaw, roll, pitch, round });
  };
  /// Брус между двумя точками: ось бруса от a к b.
  const beam = (name, m, a, b, th, th2 = th) => {
    if (b[1] < a[1]) [a, b] = [b, a];
    const d = [b[0] - a[0], b[1] - a[1], b[2] - a[2]], len = Math.hypot(...d), u = d.map(v => v / len);
    const q = [u[2], 0, -u[0], 1 + u[1]], n = Math.hypot(...q) || 1;
    return r.decor(name, a[0], a[1], a[2], th, len, th2, { q: r4(q.map(v => v / n)), m });
  };

  const D = { rnd, R, pick, chance, J, put, tile, beam, at, quat, FACE };

  /// Камни, выступающие из стены на разную глубину: как кладка подземелья, без ровной плоскости.
  D.stones = (k, plane, u1, u2, y1, y2, { density = 1.1, m = 'rock', skip, big = 1, name = 'Камень кладки', moss = 0 } = {}) => {
    const n = Math.round((u2 - u1) * (y2 - y1) * density);
    for (let i = 0; i < n; i++) {
      const w = R(0.3, 0.85) * big, h = R(0.22, 0.55) * big, d = R(0.1, 0.34) * big, u = R(u1 + w / 2, u2 - w / 2), y = R(y1, Math.max(y1, y2 - h));
      if (hits(skip, u - w / 2, u + w / 2, y, y + h)) continue;
      const o = { sink: Math.min(0.14, d * R(0.25, 0.6)), roll: J(5), yaw: J(9), pitch: J(4) };
      tile(name, m, k, plane, u, y, w, h, d, o);
      // Мох лежит на самом камне: шапкой сверху и наплывом на лице.
      if (chance(moss)) tile('Мох', 'moss', k, plane, u, y + h - 0.01, w * R(0.75, 1.05), R(0.04, 0.07), d + 0.03, o);
      if (chance(moss * 0.6)) tile('Мох', 'moss', k, plane, u + J(w * 0.15), y + h * R(0.3, 0.55), w * R(0.45, 0.8), h * R(0.35, 0.6), d + 0.025, o);
    }
  };

  /// Овальная нашлёпка на стене: центр (u, y), w вдоль стены, h по высоте, th наружу.
  const blot = D.blot = (name, m, k, plane, u, y, w, h, th, base = 0) => { const [x, z] = at(k, plane, inU(k, u)); return put(name, m, x, Math.max(base + h / 2, y), z, w, th, h, { round: true, pitch: 90, ry: FACE[k].ry, roll: J(30) }); };
  /// Пятно мха на стене: наплыв из овалов и свисающие пряди. base — высота пола под стеной.
  D.mossWall = (k, plane, u, y, size = 1, { strands = true, base = 0 } = {}) => {
    const n = Math.round(R(5, 9) * size);
    for (let i = 0; i < n; i++) blot('Мох', 'moss', k, plane, u + J(0.6 * size), y + J(0.5 * size), R(0.25, 0.75) * size, R(0.2, 0.6) * size, R(0.03, 0.08), base);
    if (strands) for (let i = 0, c = Math.round(R(2, 5) * size); i < c; i++) {
      const h = R(0.25, 0.8) * size;
      tile('Прядь мха', 'moss', k, plane, inU(k, u + J(0.5 * size)), Math.max(base, y - h * R(0.6, 1)), R(0.04, 0.08), h, 0.03, { sink: 0, roll: J(4) });
    }
  };

  /// Мох и грязь на полу: плоские лоскуты.
  D.patch = (x, z, radius, { m = 'moss', name = 'Мох', y = 0, n = 6 } = {}) => {
    for (let i = 0; i < n; i++) {
      const a = R(0, 360) * DEG, d = radius * Math.sqrt(rnd());
      put(name, m, inX(x + Math.cos(a) * d), y, inZ(z + Math.sin(a) * d), R(0.35, 0.95) * radius, R(0.012, 0.03), R(0.25, 0.7) * radius, { ry: R(0, 180), round: true });
    }
  };

  /// Осыпь: кирпичи и обломки кучей, к середине гуще и выше; каждый лежит на земле или на тех, что упали раньше.
  D.rubble = (x, z, radius, n, { m = 'brick', y = 0, pile = 0.5, name = 'Кирпич', big = 0 } = {}) => {
    const lain = [];
    for (let i = 0; i < n; i++) {
      const a = R(0, 360) * DEG, t = rnd() ** (0.5 + pile), d = radius * t, isBig = chance(big);
      const w = isBig ? R(0.4, 0.8) : R(0.24, 0.32), h = isBig ? R(0.25, 0.5) : R(0.1, 0.16), dp = isBig ? R(0.3, 0.6) : R(0.12, 0.17);
      const px = inX(x + Math.cos(a) * d), pz = inZ(z + Math.sin(a) * d), under = lain.filter(p => Math.hypot(p[0] - px, p[1] - pz) < 0.2).map(p => p[2]), rest = Math.min(pile * 1.6, under.length ? Math.max(...under) : 0);
      lain.push([px, pz, rest + h * 0.85]);
      put(isBig ? 'Обломок' : name, isBig && m === 'brick' ? 'slab' : m, px, y + rest, pz, w, h, dp, { ry: R(0, 360), pitch: chance(0.5) ? J(28) : 0, roll: chance(0.4) ? J(22) : 0 });
    }
  };

  /// Ряд камней вдоль подошвы стены: цоколь самостроя.
  D.footing = (k, plane, u1, u2, { skip, h = 0.42, m = 'slab' } = {}) => {
    for (let u = u1; u < u2 - 0.15;) {
      const w = Math.min(R(0.35, 0.8), u2 - u), hh = h * R(0.75, 1.15);
      if (!hits(skip, u, u + w, 0, hh)) tile('Камень цоколя', m, k, plane, u + w / 2, 0, w - 0.02, hh, R(0.1, 0.16), { sink: 0.03, roll: J(2), yaw: J(3) });
      u += w;
    }
  };

  /// Обшивка стены вертикальными досками: щели, разная длина, треснувшие, отошедшие и выпавшие доски, заплаты.
  D.boards = (k, plane, u1, u2, y1, y2, { skip, bw = 0.26, m = 'wood', wear = 1 } = {}) => {
    const piece = (u, w, a, b, o = {}) => { if (b - a > 0.12) tile('Доска', m, k, plane, u, a, w, b - a, 0.035, { sink: 0, roll: o.roll || J(0.7), yaw: o.yaw || 0 }); };
    for (let u = u1; u < u2 - 0.08; u += bw) {
      const w = Math.min(bw, u2 - u) - 0.018 - R(0, 0.02), c = u + Math.min(bw, u2 - u) / 2, top = y2 - (chance(0.25) ? R(0.03, 0.14) : 0);
      const spans = [[y1, top]];
      for (const [a, b, lo, hi] of skip || []) if (c + w / 2 > a && c - w / 2 < b) {
        for (let i = spans.length - 1; i >= 0; i--) { const [p, q] = spans[i]; if (hi > p && lo < q) spans.splice(i, 1, ...[[p, Math.min(q, lo)], [Math.max(p, hi), q]].filter(([e, f]) => f - e > 0.12)); }
      }
      for (const [a, b] of spans) {
        const roll = rnd();
        if (roll < 0.035 * wear && b - a > 1) continue;                                   // выпала: за ней тёмная стена
        if (roll < 0.075 * wear && b - a > 1.2) { const cut = R(a + 0.4, b - 0.4); piece(c - 0.01, w, a, cut - 0.015, { roll: J(1.5) }); piece(c + 0.012, w, cut + 0.015, b, { roll: J(2.5) }); continue; }  // треснула пополам
        if (roll < 0.11 * wear && b - a > 1.2) { piece(c, w, a, b, { roll: pick([-1, 1]) * R(3.5, 8), yaw: J(3) }); continue; }             // держится на одном гвозде
        piece(c, w, a, b);
      }
      if (chance(0.02 * wear) && !hits(skip, c - 0.5, c + 0.5, y1 + 0.5, y1 + 2)) tile('Заплата', m, k, plane, c, R(y1 + 0.6, Math.max(y1 + 0.7, y2 - 0.8)), R(0.6, 1.1), R(0.16, 0.24), 0.03, { sink: -0.035, roll: J(6) });
    }
  };

  /// Каркас стены: стойки, верхний брус и ригель.
  D.frame = (k, plane, u1, u2, y1, y2, { step = 2.6, rail = true, m = 'timber', th = 0.2 } = {}) => {
    const n = Math.max(1, Math.round((u2 - u1) / step));
    for (let i = 0; i <= n; i++) tile('Стойка каркаса', m, k, plane, u1 + (u2 - u1) * i / n, y1, th, y2 - y1, 0.1, { sink: 0.02, roll: J(0.6) });
    tile('Обвязка', m, k, plane, (u1 + u2) / 2, y2 - th, u2 - u1 + th, th, 0.12, { sink: 0.02 });
    if (rail) tile('Ригель', m, k, plane, (u1 + u2) / 2, y1 + (y2 - y1) * R(0.42, 0.5), u2 - u1, th * 0.7, 0.09, { sink: 0.02, roll: J(0.4) });
  };

  /// Настил из досок поверх плиты: стыки, щели, приподнятые и выпавшие доски.
  D.deck = (x1, z1, x2, z2, y, { along = 'x', bw = 0.3, holes = [], m = 'wood', wear = 1 } = {}) => {
    const [a1, a2, b1, b2] = along === 'x' ? [x1, x2, z1, z2] : [z1, z2, x1, x2];
    for (let b = b1; b < b2 - 0.1; b += bw) {
      const w = Math.min(bw, b2 - b) - 0.015;
      for (let a = a1; a < a2 - 0.1;) {
        const len = Math.min(R(1.8, 4.2), a2 - a), c = a + len / 2, cb = b + w / 2 + 0.007;
        const [x, z, bx1, bx2, bz1, bz2] = along === 'x' ? [c, cb, a, a + len, b, b + w] : [cb, c, b, b + w, a, a + len];
        const blocked = holes.some(q => bx2 > q.x1 && bx1 < q.x2 && bz2 > q.z1 && bz1 < q.z2);
        if (!blocked && !chance(0.025 * wear)) put('Доска настила', m, x, y, z, along === 'x' ? len - 0.02 : w, 0.035, along === 'x' ? w : len - 0.02, { ry: J(0.5), pitch: chance(0.05 * wear) ? J(2.2) : 0 });
        a += len;
      }
    }
  };

  /// Цепь из звеньев от точки вниз.
  D.chain = (x, y, z, len, { name = 'Цепь' } = {}) => {
    for (let t = 0, i = 0; t < len; t += 0.11, i++) put(name, 'iron', x, y - t - 0.11, z, i % 2 ? 0.02 : 0.055, 0.12, i % 2 ? 0.055 : 0.02);
  };

  /// Полка на двух цепях: доска на стене на уровне глаз, цепи идут от её переднего края вверх к стене, на полке посуда.
  D.shelf = (k, plane, u, { y = 1.5, w = 1.2, items = 3 } = {}) => {
    const f = FACE[k], depth = 0.3, ends = [u - w / 2 + 0.08, u + w / 2 - 0.08];
    tile('Полка на цепях', 'wood', k, plane, u, y, w, 0.04, depth, { sink: 0 });
    tile('Планка полки', 'timber', k, plane, u, y - 0.07, w - 0.1, 0.07, 0.04, { sink: 0 });
    for (const e of ends) {
      const [x1, z1] = at(k, plane, e, depth - 0.03), [x2, z2] = at(k, plane, e, 0.02);
      beam('Цепь полки', 'iron', [x1, y + 0.04, z1], [x2, y + 0.55, z2], 0.025);
      tile('Кольцо в стене', 'iron', k, plane, e, y + 0.52, 0.07, 0.07, 0.04, { sink: 0 });
    }
    for (let i = 0; i < items; i++) {
      const [x, z] = at(k, plane, u - w / 2 + 0.2 + (w - 0.4) * (items > 1 ? i / (items - 1) : 0.5) + J(0.05), depth / 2 + J(0.03)), kind = rnd();
      if (kind < 0.45) put('Горшок', 'clay', x, y + 0.04, z, R(0.14, 0.2), R(0.14, 0.24), R(0.14, 0.2), { round: true });
      else if (kind < 0.75) put('Кружка', 'clay', x, y + 0.04, z, 0.09, 0.11, 0.09, { round: true });
      else put('Свёрток', 'cloth', x, y + 0.04, z, 0.22, 0.09, 0.16, { ry: f.ry + J(20) });
    }
  };

  /// Накрытый стол: миски, кружки, кувшин, хлеб.
  D.tableSet = (x, z, top, w, d, { ry = 0, places = 4 } = {}) => {
    const c = Math.cos(ry * DEG), sn = Math.sin(ry * DEG), P = (lx, lz) => [x + lx * c + lz * sn, z - lx * sn + lz * c];
    for (let i = 0; i < places; i++) {
      const lx = (w - 0.5) * ((i >> 1) / Math.max(1, Math.ceil(places / 2) - 1) - 0.5) + J(0.06), side = i % 2 ? 1 : -1, [px, pz] = P(lx, side * (d / 2 - 0.18));
      put('Миска', 'clay', px, top, pz, 0.2, 0.05, 0.2, { round: true });
      if (chance(0.7)) { const [mx, mz] = P(lx + 0.2, side * (d / 2 - 0.2) + J(0.05)); put('Кружка', 'clay', mx, top, mz, 0.08, 0.1, 0.08, { round: true, pitch: chance(0.2) ? 90 : 0 }); }
    }
    const [jx, jz] = P(J(0.2), J(0.05)); put('Кувшин', 'clay', jx, top, jz, 0.16, 0.26, 0.16, { round: true });
    const [bx, bz] = P(0.35 + J(0.1), J(0.08)); put('Хлеб', 'straw', bx, top, bz, 0.26, 0.08, 0.16, { ry: ry + J(40) });
  };

  /// Бельевая верёвка с тряпьём между двумя точками.
  D.laundry = (a, b, { n = 4 } = {}) => {
    beam('Верёвка', 'rope', a, b, 0.025);
    const yaw = Math.atan2(b[0] - a[0], b[2] - a[2]) / DEG + 90;
    for (let i = 0; i < n; i++) {
      const t = (i + 0.5 + J(0.2)) / n, h = R(0.5, 1.1), w = R(0.45, 0.9), p = a.map((v, j) => v + (b[j] - v) * t);
      put(pick(['Рубаха', 'Простыня', 'Тряпьё']), pick(['cloth', 'cloth', 'clothRed', 'clothBlue']), p[0], p[1] - h - 0.02, p[2], w, h, 0.02, { ry: yaw + J(6), roll: J(3), pitch: J(5) });
    }
  };

  /// Рама окна и ставни: открытая, закрытая, висит на одной петле, выломана.
  D.window = (k, plane, u, y0, y1, w, state = 'open') => {
    tile('Наличник', 'timber', k, plane, u, y0 - 0.09, w + 0.24, 0.09, 0.1, { sink: 0.02 });
    tile('Наличник', 'timber', k, plane, u, y1, w + 0.24, 0.09, 0.08, { sink: 0.02 });
    for (const sgn of [-1, 1]) tile('Наличник', 'timber', k, plane, u + sgn * (w / 2 + 0.06), y0, 0.1, y1 - y0, 0.07, { sink: 0.02 });
    if (state === 'frame') return;
    const leaf = (sgn, roll, yaw, drop = 0) => tile('Ставня', 'wood', k, plane, u + sgn * (state === 'shut' ? w / 4 : w * 0.75 + 0.1), y0 - drop, w / 2, y1 - y0, 0.035, { sink: -0.03, roll, yaw });
    if (state === 'shut') { leaf(-1, J(0.8), 0); leaf(1, J(0.8), 0); tile('Засов ставен', 'timber', k, plane, u, (y0 + y1) / 2, w * 0.9, 0.08, 0.04, { sink: -0.07, roll: J(3) }); }
    else if (state === 'hang') { leaf(-1, J(1), 0); leaf(1, R(14, 24), 4, 0.12); }
    else if (state === 'one') leaf(pick([-1, 1]), J(1.5), 0);
    else { leaf(-1, J(1), 0); leaf(1, J(1), 0); }
  };

  /// Наличник и порог двери, кованые петли на полотне рисует сама дверь.
  D.doorFrame = (k, plane, u, { y = 0, w = 1.1, h = 2.4 } = {}) => {
    for (const sgn of [-1, 1]) tile('Косяк', 'timber', k, plane, u + sgn * (w / 2 + 0.09), y, 0.16, h + 0.1, 0.12, { sink: 0.03, roll: J(0.5) });
    tile('Притолока', 'timber', k, plane, u, y + h, w + 0.5, 0.18, 0.14, { sink: 0.03, roll: J(0.8) });
    tile('Порог', 'slab', k, plane, u, y, w + 0.3, 0.06, 0.3, { sink: 0.02 });
  };

  /// Детский рисунок углём: камни, камни и человечки под чертой потолка.
  D.drawing = (k, plane, u, y) => {
    const st = (du, dy, w, h, roll = 0) => tile('Рисунок углём', 'soot', k, plane, u + du, y + dy, w, h, 0.012, { sink: 0, roll });
    st(0, 0.62, 0.9, 0.02, J(2));                                                       // потолок вместо неба
    for (let i = 0; i < 5; i++) { const du = -0.35 + i * 0.17 + J(0.02), w = R(0.1, 0.15), h = R(0.07, 0.11), dy = R(0, 0.06); st(du, dy, w, 0.015); st(du, dy + h, w, 0.015); st(du - w / 2, dy, 0.015, h); st(du + w / 2, dy, 0.015, h); }
    for (const du of [-0.2, 0.05, 0.28]) { const h = R(0.14, 0.22), dy = 0.2; st(du, dy, 0.015, h, J(4)); st(du, dy + h * 0.55, 0.12, 0.015, J(8)); st(du - 0.035, dy - 0.08, 0.015, 0.09, 20); st(du + 0.035, dy - 0.08, 0.015, 0.09, -20); st(du, dy + h, 0.06, 0.06); }
  };

  /// Зарубки счёта на стене.
  D.tally = (k, plane, u, y, n) => { for (let i = 0; i < n; i++) i % 5 === 4 ? tile('Зарубки', 'soot', k, plane, u + (i - 2) * 0.05, y + 0.02, 0.24, 0.015, 0.012, { sink: 0, roll: 25 }) : tile('Зарубки', 'soot', k, plane, u + i * 0.05, y, 0.015, R(0.11, 0.15), 0.012, { sink: 0, roll: J(5) }); };

  /// Друза оссарита: холодные голубые кристаллы, растут из кости.
  D.ossarite = (x, z, { y = 0, n = 5, size = 1 } = {}) => {
    put('Кость', 'bone', x, y, z, 0.5 * size, 0.07, 0.09, { ry: R(0, 180) });
    for (let i = 0; i < n; i++) put('Оссарит', 'ossarite', x + J(0.16 * size), y, z + J(0.16 * size), R(0.05, 0.1) * size, R(0.15, 0.42) * size, R(0.05, 0.1) * size, { ry: R(0, 90), pitch: J(24), roll: J(24) });
  };

  /// Грибы: бледные шляпки кучкой.
  D.fungus = (x, z, n, { y = 0, spread = 0.4 } = {}) => {
    for (let i = 0; i < n; i++) {
      const px = x + J(spread), pz = z + J(spread), h = R(0.06, 0.2), c = R(0.08, 0.22);
      put('Ножка гриба', 'fungus', px, y, pz, c * 0.3, h, c * 0.3, { round: true });
      put('Шляпка гриба', 'fungus', px, y + h, pz, c, c * 0.35, c, { round: true, pitch: J(10) });
    }
  };

  /// Кости на полу.
  D.bones = (x, z, n, { y = 0, spread = 0.5 } = {}) => {
    for (let i = 0; i < n; i++) put('Кость', 'bone', x + J(spread), y, z + J(spread), R(0.25, 0.5), 0.06, 0.07, { ry: R(0, 180) });
    if (n > 3) put('Череп', 'bone', x + J(spread * 0.5), y, z + J(spread * 0.5), 0.2, 0.2, 0.24, { ry: R(0, 360) });
  };

  /// Булыжники, выступающие из мостовой: ниже шага, по ним ходят.
  D.flags = (x1, z1, x2, z2, n, { m = 'pebble', y = 0 } = {}) => {
    for (let i = 0; i < n; i++) put('Булыжник', m, R(x1, x2), y, R(z1, z2), R(0.22, 0.5), R(0.03, 0.08), R(0.2, 0.42), { ry: R(0, 180), round: true });
  };


  // ---------- Мебель и утварь: вид пропа из частей; сам проп остаётся невидимым телом (поле hide) ----------
  /// Оси пропа: части ставятся от его низа, x вдоль ширины, z вдоль глубины.
  const local = (x, y, z, ry = 0) => {
    const c = Math.cos(ry * DEG), sn = Math.sin(ry * DEG), P = (lx, lz) => [x + lx * c + lz * sn, z - lx * sn + lz * c];
    return { P, part: (name, m, lx, ly, lz, w, h, d, o = {}) => { const [px, pz] = P(lx, lz); return put(name, m, px, y + ly, pz, w, h, d, { ...o, ry: ry + (o.ry || 0) }); } };
  };
  /// Диск стоймя (колесо): ось вдоль местной x, центр на высоте ly.
  const wheel = (L, name, m, lx, ly, lz, dia, th) => L.part(name, m, lx - th / 2, ly, lz, dia, th, dia, { round: true, roll: -90 });
  const legs = (L, w, d, h, m = 'timber', th = 0.08) => { for (const sx of [-1, 1]) for (const sz of [-1, 1]) L.part('Ножка', m, sx * (w / 2 - th), 0, sz * (d / 2 - th), th, h, th, { roll: sx * J(2), pitch: J(2) }); };
  const K = D.kit = {};

  K['Стол'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry), n = Math.max(2, Math.round(Math.min(w, d) / 0.3)), wide = w >= d;
    for (let i = 0; i < n; i++) wide ? L.part('Доска столешницы', 'wood', 0, h - 0.05, (i + 0.5) / n * d - d / 2, w + J(0.04), 0.05, d / n - 0.01) : L.part('Доска столешницы', 'wood', (i + 0.5) / n * w - w / 2, h - 0.05, 0, w / n - 0.01, 0.05, d + J(0.04));
    legs(L, w - 0.1, d - 0.1, h - 0.05);
    wide ? L.part('Царга', 'timber', 0, h * 0.35, 0, w - 0.3, 0.06, 0.05) : L.part('Царга', 'timber', 0, h * 0.35, 0, 0.05, 0.06, d - 0.3);
  };
  K['Лавка'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry), wide = w >= d;
    L.part('Сиденье лавки', 'wood', 0, h - 0.05, 0, w, 0.05, d, { roll: wide ? 0 : J(1), pitch: wide ? J(1) : 0 });
    for (const s of [-1, 1]) wide ? L.part('Опора лавки', 'timber', s * (w / 2 - 0.2), 0, 0, 0.05, h - 0.05, d * 0.85) : L.part('Опора лавки', 'timber', 0, 0, s * (d / 2 - 0.2), w * 0.85, h - 0.05, 0.05);
  };
  K['Скамья'] = K['Лавка'];
  K['Табурет'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry + R(0, 60));
    L.part('Сиденье табурета', 'wood', 0, h - 0.05, 0, w, 0.05, d, { round: true });
    for (let i = 0; i < 3; i++) { const a = i * 120 * DEG; L.part('Ножка', 'timber', Math.cos(a) * w * 0.3, 0, Math.sin(a) * d * 0.3, 0.05, h - 0.05, 0.05, { roll: -Math.cos(a) * 7, pitch: Math.sin(a) * 7 }); }
  };
  // Койка: длинная вдоль z, изголовье у +z.
  K['Койка'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry);
    for (const s of [-1, 1]) L.part('Боковина койки', 'timber', s * (w / 2 - 0.03), 0.18, 0, 0.05, 0.16, d);
    L.part('Изголовье', 'wood', 0, 0, d / 2 - 0.03, w, 0.78, 0.05, { pitch: J(1.5) }); L.part('Изножье', 'wood', 0, 0, -d / 2 + 0.03, w, 0.46, 0.05);
    L.part('Соломенный тюфяк', 'straw', 0, 0.3, -0.02, w - 0.12, 0.14, d - 0.14);
    L.part('Одеяло', pick(['cloth', 'clothRed', 'clothBlue']), J(0.03), 0.44, -0.2 + J(0.08), w - 0.04, 0.05, d * R(0.5, 0.72), { ry: J(5) }); L.part('Подушка', 'cloth', J(0.05), 0.44, d / 2 - 0.3, 0.48, 0.09, 0.28, { ry: J(10) });
  };
  K['Колыбель'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry);
    for (const s of [-1, 1]) { L.part('Борт колыбели', 'wood', s * (w / 2 - 0.02), 0.14, 0, 0.03, h - 0.14, d); L.part('Полоз колыбели', 'timber', 0, 0, s * (d / 2 - 0.12), w + 0.2, 0.07, 0.05, { roll: s * 3 }); L.part('Торец колыбели', 'wood', 0, 0.14, s * (d / 2 - 0.02), w, h - 0.08, 0.03); }
    L.part('Одеяльце', 'cloth', 0, 0.2, 0, w - 0.06, 0.1, d - 0.06);
  };
  const counter = (open, top = 'wood') => (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry), wide = w >= d;
    L.part('Столешница', top, 0, h - 0.07, 0, w + 0.08, 0.07, d + 0.08);
    if (open) { legs(L, w, d, h - 0.07, 'timber', 0.09); L.part('Нижняя полка', 'wood', 0, 0.22, 0, w - 0.2, 0.04, d - 0.16); return; }
    for (const s of [-1, 1]) {
      wide ? L.part('Торец', 'wood', s * (w / 2 - 0.025), 0, 0, 0.05, h - 0.07, d) : L.part('Торец', 'wood', 0, 0, s * (d / 2 - 0.025), w, h - 0.07, 0.05);
      for (let i = 0, n = Math.round(Math.max(w, d) / 0.26); i < n; i++) wide ? L.part('Доска', 'wood', (i + 0.5) / n * w - w / 2, 0, s * (d / 2 - 0.02), w / n - 0.012, h - 0.07, 0.035, { roll: J(0.6) }) : L.part('Доска', 'wood', s * (w / 2 - 0.02), 0, (i + 0.5) / n * d - d / 2, 0.035, h - 0.07, d / n - 0.012, { pitch: J(0.6) });
    }
  };
  K['Стойка'] = K['Прилавок'] = counter(false); K['Лоток'] = K['Верстак'] = counter(true);
  K['Конторка'] = (x, y, z, w, d, h, ry) => { counter(false)(x, y, z, w, d, h - 0.12, ry); const L = local(x, y, z, ry); L.part('Наклонная доска', 'wood', 0, h - 0.14, 0, w + 0.06, 0.04, d + 0.06, { roll: 12 }); };
  K['Корыто'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry);
    for (const s of [-1, 1]) { L.part('Борт корыта', 'wood', 0, 0.08, s * (d / 2 - 0.025), w, h - 0.08, 0.05, { pitch: s * 6 }); L.part('Торец корыта', 'wood', s * (w / 2 - 0.025), 0.08, 0, 0.05, h - 0.08, d - 0.06); L.part('Опора корыта', 'timber', s * (w / 2 - 0.2), 0, 0, 0.08, 0.1, d + 0.1); }
    L.part('Дно корыта', 'wood', 0, 0.08, 0, w - 0.06, 0.04, d - 0.08);
  };
  // Стеллаж: стойки, четыре полки, на них припасы.
  K['Полки с припасами'] = (x, y, z, w, d, h, ry) => {
    const turn = d > w, L = local(x, y, z, ry + (turn ? 90 : 0)), len = Math.max(w, d), dep = Math.min(w, d);
    for (const s of [-1, 1]) for (const b of [-1, 1]) L.part('Стойка стеллажа', 'timber', s * (len / 2 - 0.04), 0, b * (dep / 2 - 0.04), 0.07, h, 0.07, { roll: J(0.7) });
    for (const ly of [0.12, 0.66, 1.2, 1.74].filter(v => v < h - 0.1)) {
      L.part('Полка', 'wood', 0, ly, 0, len - 0.04, 0.04, dep - 0.02, { roll: J(0.8) });
      for (let lx = -len / 2 + 0.22; lx < len / 2 - 0.15; lx += R(0.26, 0.5)) {
        const kind = rnd();
        if (kind < 0.3) L.part('Горшок', 'clay', lx, ly + 0.04, J(0.04), R(0.16, 0.24), R(0.16, 0.3), R(0.16, 0.24), { round: true });
        else if (kind < 0.5) L.part('Мешочек', 'cloth', lx, ly + 0.04, J(0.04), 0.24, 0.2, 0.2, { round: true });
        else if (kind < 0.7) L.part('Ящичек', 'wood', lx, ly + 0.04, J(0.03), 0.3, 0.2, 0.26, { ry: J(8) });
        else if (kind < 0.82) L.part('Бутыль', 'clay', lx, ly + 0.04, J(0.04), 0.1, 0.3, 0.1, { round: true });
      }
    }
  };
  const barrel = (L, lx, ly, lz, dia, h, name = 'Бочка') => {
    L.part(name, 'wood', lx, ly, lz, dia, h, dia, { round: true });
    for (const t of [0.14, 0.8]) L.part('Обруч', 'iron', lx, ly + h * t, lz, dia + 0.03, 0.05, dia + 0.03, { round: true });
    L.part('Пузо бочки', 'wood', lx, ly + h * 0.3, lz, dia + 0.05, h * 0.4, dia + 0.05, { round: true });
  };
  D.barrel = (x, y, z, dia = 0.62, h = 0.95) => barrel(local(x, y, z, R(0, 90)), 0, 0, 0, dia, h);
  K['Бочки'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry), nx = Math.max(1, Math.round(w / 0.64)), nz = Math.max(1, Math.round(d / 0.64)), dia = Math.min(w / nx, d / nz) - 0.04;
    for (let i = 0; i < nx; i++) for (let j = 0; j < nz; j++) barrel(L, (i + 0.5) / nx * w - w / 2 + J(0.02), 0, (j + 0.5) / nz * d - d / 2 + J(0.02), dia, h * R(0.76, 0.84));
    if (chance(0.6)) barrel(L, J(0.1), h * 0.8, J(0.1), dia * 0.62, h * 0.38, 'Бочонок');
  };
  const crate = (L, lx, ly, lz, s, yaw) => {
    L.part('Ящик', 'wood', lx, ly, lz, s, s - 0.02, s, { ry: yaw });
    for (const t of [0.12, 0.72]) L.part('Обвязка ящика', 'timber', lx, ly + s * t, lz, s + 0.035, 0.07, s + 0.035, { ry: yaw });
  };
  K['Штабель ящиков'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry), s = Math.min(w, d) / 2 - 0.02, tiers = Math.max(1, Math.round(h / (s + 0.02)));
    for (let t = 0; t < tiers; t++) for (const [sx, sz] of [[-1, -1], [1, -1], [-1, 1], [1, 1]]) if (t === 0 || chance(t === tiers - 1 ? 0.35 : 0.75)) crate(L, sx * (s / 2 + 0.01) + J(0.02) * t, t * s, sz * (s / 2 + 0.01) + J(0.02) * t, s, J(4 + t * 5));
  };
  // Поленница: поленья торцами наружу поперёк короткой стороны.
  K['Поленница'] = (x, y, z, w, d, h, ry) => {
    const turn = w > d, L = local(x, y, z, ry + (turn ? 90 : 0)), len = Math.min(w, d), run = Math.max(w, d), dia = 0.23, rows = Math.floor(run / dia), layers = Math.floor(h / (dia * 0.9));
    for (let j = 0; j < layers; j++) for (let i = 0; i < rows - (j % 2); i++) {
      const [px, pz] = L.P(-len / 2 + J(0.03), (i + 0.5 + (j % 2) * 0.5) * dia - run / 2);
      put('Полено', 'wood', px, y + j * dia * 0.9 + dia * R(0.42, 0.5), pz, dia * R(0.75, 1), len + J(0.05), dia * R(0.75, 1), { round: true, pitch: 90, ry: ry + (turn ? 90 : 0) + 90 + J(3) });
    }
  };
  K['Мешки'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry);
    for (let i = 0, n = Math.max(3, Math.round(w * d * 3)); i < n; i++) { const up = i >= n - 2; L.part('Мешок', 'cloth', J(w / 2 - 0.3) * (up ? 0.4 : 1), up ? h * 0.45 : 0, J(d / 2 - 0.25) * (up ? 0.4 : 1), R(0.42, 0.55), R(0.3, 0.4), R(0.6, 0.75), { round: true, ry: R(0, 180), roll: J(8) }); }
  };
  K['Перила'] = (x, y, z, w, d, h, ry) => {
    const turn = d > w, L = local(x, y, z, ry + (turn ? 90 : 0)), len = Math.max(w, d), n = Math.max(1, Math.round(len / 1.2));
    for (let i = 0; i <= n; i++) L.part('Стойка перил', 'timber', -len / 2 + 0.05 + (len - 0.1) * i / n, 0, 0, 0.09, h, 0.09, { roll: J(1.2) });
    L.part('Поручень', 'timber', 0, h - 0.07, 0, len, 0.07, 0.11); L.part('Жердь перил', 'wood', 0, h * 0.45, 0, len, 0.06, 0.04, { roll: J(1) });
  };
  K['Плетень'] = (x, y, z, w, d, h, ry) => {
    const turn = d > w, L = local(x, y, z, ry + (turn ? 90 : 0)), len = Math.max(w, d);
    for (let lx = -len / 2 + 0.1; lx < len / 2; lx += 0.42) L.part('Кол плетня', 'timber', lx, 0, J(0.02), 0.07, h + R(0.05, 0.3), 0.07, { roll: J(5), pitch: J(4) });
    for (const ly of [0.16, 0.36, 0.56, 0.74]) for (const s of [-1, 1]) L.part('Прут плетня', 'wood', J(0.05), ly + J(0.03), s * 0.045, len - R(0, 0.5), 0.05, 0.03, { roll: J(1.5) });
  };
  K['Грядка'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry);
    for (const s of [-1, 1]) { L.part('Доска грядки', 'wood', 0, 0, s * (d / 2 + 0.02), w + 0.1, h + 0.04, 0.04, { roll: J(1) }); L.part('Доска грядки', 'wood', s * (w / 2 + 0.02), 0, 0, 0.04, h + 0.04, d + 0.02, { pitch: J(1) }); }
    return true;
  };
  K['Наковальня'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry);
    L.part('Колода', 'wood', 0, 0, 0, 0.5, h * 0.55, 0.5, { round: true }); L.part('Подошва наковальни', 'iron', 0, h * 0.55, 0, w * 0.5, h * 0.12, d * 0.9); L.part('Шейка наковальни', 'iron', 0, h * 0.67, 0, w * 0.3, h * 0.13, d * 0.6);
    L.part('Лицо наковальни', 'iron', -w * 0.06, h * 0.8, 0, w * 0.72, h * 0.2, d * 0.8); L.part('Рог наковальни', 'iron', w * 0.38, h * 0.84, 0, w * 0.26, h * 0.1, d * 0.4);
  };
  K['Рудная вагонетка'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry);
    for (const s of [-1, 1]) { L.part('Борт вагонетки', 'iron', s * (w / 2 - 0.03), 0.34, 0, 0.05, h - 0.34, d, { roll: s * -5 }); L.part('Торец вагонетки', 'iron', 0, 0.34, s * (d / 2 - 0.03), w - 0.06, h - 0.34, 0.05); for (const b of [-1, 1]) wheel(L, 'Колесо вагонетки', 'iron', s * (w / 2 - 0.12) + 0.03, 0.2, b * d * 0.3, 0.4, 0.07); }
    L.part('Дно вагонетки', 'iron', 0, 0.3, 0, w - 0.06, 0.06, d - 0.06); L.part('Рама вагонетки', 'timber', 0, 0.2, 0, w * 0.6, 0.1, d + 0.2);
  };
  K['Телега под рогожей'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry);
    L.part('Дно телеги', 'wood', 0, 0.62, 0, w, 0.08, d);
    for (const s of [-1, 1]) { L.part('Борт телеги', 'wood', s * (w / 2 - 0.02), 0.7, 0, 0.04, 0.42, d, { roll: s * -4 }); for (const b of [-1, 1]) wheel(L, 'Колесо телеги', 'timber', s * (w / 2 + 0.1), 0.5, b * d * 0.28, 1, 0.08); L.part('Оглобля', 'timber', s * 0.32, 0.5, d / 2 + 0.75, 0.07, 0.07, 1.6, { pitch: 14 }); }
    L.part('Поклажа под рогожей', pick(['cloth', 'clothBlue']), J(0.03), 0.7, -0.05, w - 0.12, h - 0.72, d - 0.3, { roll: J(2), pitch: J(2) });
    for (const lz of [-d * 0.25, d * 0.2]) L.part('Верёвка', 'rope', 0, 0.7, lz, w - 0.02, h - 0.69, 0.04, { roll: J(1) });
  };
  K['Тачка'] = (x, y, z, w, d, h, ry) => {
    const L = local(x, y, z, ry);
    L.part('Кузов тачки', 'wood', 0, 0.3, -0.1, w - 0.1, h - 0.3, d * 0.55, { pitch: -6 }); wheel(L, 'Колесо тачки', 'timber', 0.04, 0.27, -d / 2 + 0.2, 0.54, 0.08);
    for (const s of [-1, 1]) { L.part('Рукоять тачки', 'timber', s * (w / 2 - 0.08), 0.3, 0.25, 0.05, 0.05, d * 0.7, { pitch: -8 }); L.part('Ножка', 'timber', s * (w / 2 - 0.1), 0, 0.3, 0.05, 0.32, 0.05); }
  };


  /// Скальные выступы на гранях глыбы или ребра: у блока нет ни одной ровной грани.
  D.crag = (x, z, w, d, h, ry, { y = 0, n = 8, m = 'rock', moss = 0 } = {}) => {
    const L = local(x, y, z, ry);
    for (let i = 0; i < n; i++) {
      const side = Math.floor(R(0, 5)), sw = R(0.35, Math.max(0.5, Math.min(1.1, w * 0.5))), sh = R(0.3, 0.75), sd = R(0.2, 0.5), ly = side === 4 ? h - sh * 0.5 : R(0, Math.max(0.1, h - sh));
      const [lx, lz, turn] = side === 0 ? [J(w / 2 - sw / 3), d / 2 - sd * 0.3, 0] : side === 1 ? [J(w / 2 - sw / 3), -d / 2 + sd * 0.3, 0] : side === 2 ? [w / 2 - sd * 0.3, J(d / 2 - sw / 3), 90] : side === 3 ? [-w / 2 + sd * 0.3, J(d / 2 - sw / 3), 90] : [J(w / 2 - sw / 2), J(d / 2 - sd / 2), R(0, 90)];
      L.part('Скальный выступ', m, lx, ly, lz, sw, sh, sd, { ry: turn + J(12), roll: J(8), pitch: J(6) });
      if (chance(moss)) L.part('Мох', 'moss', lx, ly + sh - 0.01, lz, sw * R(0.7, 1), R(0.04, 0.07), sd + 0.04, { ry: turn, round: true });
    }
  };

  /// Крепь выработки у стены: две стойки, верхняк и подкос. Империя ставила её до того, как здесь поселились.
  D.timbering = (k, plane, u, { w = 2.2, h = 3.8 } = {}) => {
    for (const s of [-1, 1]) {
      tile('Стойка крепи', 'timber', k, plane, u + s * w / 2, 0, 0.26, h, 0.26, { sink: -0.04, roll: s * R(0.5, 2.5), pitch: J(1.5) });
      const [x1, z1] = at(k, plane, u + s * (w / 2 - 0.1), 0.17), [x2, z2] = at(k, plane, u + s * (w / 2 - 0.85), 0.17);
      beam('Подкос крепи', 'timber', [x1, h - 0.95, z1], [x2, h - 0.1, z2], 0.14);
    }
    tile('Верхняк крепи', 'timber', k, plane, u, h, w + 0.7, 0.28, 0.3, { sink: -0.03, roll: J(1.8) });
    tile('Клин', 'wood', k, plane, u + J(w * 0.3), h + 0.28, 0.3, 0.12, 0.2, { sink: -0.05, roll: J(8) });
  };

  /// Плита с печатью империи: резьба стёрта, край сколот.
  D.plaque = (k, plane, u, y) => {
    tile('Плита Морнхейма', 'slab', k, plane, u, y, 1.3, 0.95, 0.14, { sink: 0.02, roll: J(2.5) });
    tile('Кромка плиты', 'slab', k, plane, u, y + 0.95, 1.42, 0.12, 0.2, { sink: 0.02, roll: J(2) });
    blot('Печать Морнхейма', 'iron', k, plane, u, y + 0.52, 0.5, 0.5, 0.18);
    for (let i = 0; i < 3; i++) tile('Стёртая строка', 'soot', k, plane, u + J(0.05), y + 0.08 + i * 0.07, R(0.6, 0.95), 0.025, 0.012, { sink: -0.14 });
  };

  return D;
};
