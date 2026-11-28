// Утварь выработок для сцен рудничных комнат (StopeWorkings, HaulageSpiral, SunkenPit, Chasm, CollapsedHall, FungusTerraces):
// const M = require('../sets/mine')(S). Настенное ставится по грани M.face, напольное — от низа y.
module.exports = S => {
  const { D } = S, { put, beam, R, J, pick, chance } = D, DEG = Math.PI / 180, M = {};
  const DIR = { N: [0, 1], E: [1, 0], S: [0, -1], W: [-1, 0] };
  /// Точка от (x, z): f вперёд по курсу ry, r вправо.
  const off = M.off = (x, z, ry, f, r = 0) => { const s = Math.sin(ry * DEG), c = Math.cos(ry * DEG); return [x + s * f + c * r, z + c * f - s * r]; };
  const floorAt = (x, z, y) => y === undefined ? S.floor(x, z, 0) || 0 : y;

  // ---------- настенное ----------
  /// Грань тела в сторону toward от точки на высоте y (пол + 1,5): её оси держат вещь и на повёрнутом целике.
  /// tile(имя, материал, du, низ, w, h, d, o), disc — овал лицом наружу (центр yy), at(du, out) → [x, z], kp — [сторона, плоскость, u] для наборов detail.js (у прямых граней).
  M.face = (x, z, toward, y = 1.5, max = 8) => {
    const [dx, dz] = DIR[toward];
    for (let t = 0; t < max; t += 0.05) {
      const px = x + dx * t, pz = z + dz * t, b = S.solid(px, y, pz);
      if (!b) { if (S.inCell(px, y, pz)) continue; return null; }
      let f = null, near = 9;
      for (const q of S.faces(b)) { if (-(q.s * dx + q.c * dz) < 0.3) continue; const dist = Math.abs((px - b.cx) * q.s + (pz - b.cz) * q.c - q.half); if (dist < near) { near = dist; f = q; } }
      if (!f) return null;
      const u = (px - b.cx) * f.c - (pz - b.cz) * f.s, at = (du = 0, out = 0) => S.fpos(f, u + du, out), [wx, wz] = at(), k = 'NESW'[Math.round(f.ry / 90) % 4];
      return {
        f, u, y, ry: f.ry, at, kp: f.axis ? (k === 'N' || k === 'S' ? [k, wz, wx] : [k, wx, wz]) : null,
        tile: (name, m, du, yy, w, h, d, o) => S.ftile(f, name, m, u + du, yy, w, h, d, o),
        disc: (name, m, du, yy, w, h, th = 0.02, roll = J(25)) => { const [ax, az] = at(du, -0.005); return put(name, m, ax, yy, az, w, th, h, { round: true, pitch: 90, ry: f.ry, roll }); }
      };
    }
    return null;
  };
  /// Чистый участок стены под настенное: вызывать до S.auto(), иначе лист закроют камни кладки.
  M.clear = (W, w, y0, y1) => { if (!W) return W; const [ax, az] = W.at(-w / 2 - 0.4, -0.2), [bx, bz] = W.at(w / 2 + 0.4, 0.5); S.keep(ax, y0 - 0.25, az, bx, y1 + 0.25, bz); return W; };
  /// Ниша с масляной лампой на уровне глаз и копоть над ней.
  M.lamp = (W, y = W && W.y + 0.05, lit) => {
    if (!W) return;
    W.tile('Ниша лампы', 'soot', 0, y - 0.04, 0.36, 0.34, 0.012, { sink: -0.1 }); W.tile('Полка ниши', 'slab', 0, y - 0.1, 0.42, 0.06, 0.3, { sink: 0.02, roll: J(2) });
    const [x, z] = W.at(J(0.05), 0.2); put('Масляная лампа', 'clay', x, y - 0.04, z, 0.13, 0.07, 0.13, { round: true });
    if (lit) put('Огонёк', 'ember', x, y + 0.03, z, 0.03, 0.06, 0.03, { round: true });
    for (let i = 0; i < 3; i++) W.tile('Копоть', 'soot', J(0.05), y + 0.32 + i * 0.2, R(0.22, 0.3) - i * 0.05, 0.22, 0.012, { sink: -0.14 + i * 0.03, roll: J(12) });
  };
  /// Лист на стене: строки, печать (seal — материал), подпись; out — насколько отстоит (поверх доски или другого листа).
  M.sign = (W, y, { w = 0.42, h = 0.58, lines = 6, seal, sig, name = 'Лист', m = 'paper', du = 0, roll = J(3), out = 0 } = {}) => {
    if (!W) return;
    W.tile(name, m, du, y, w, h, 0.012, { sink: -out, roll });
    for (let i = 0; i < lines; i++) W.tile('Строка', 'soot', du + J(0.02), y + h - 0.1 - i * (h - 0.24) / lines, w * R(0.55, 0.8), 0.018, 0.008, { sink: -out - 0.012, roll });
    if (sig) W.tile('Подпись', 'soot', du + w * 0.18, y + 0.05, w * 0.4, 0.022, 0.008, { sink: -out - 0.012, roll: roll + 14 });
    if (seal) W.tile('Печать', seal, du - w * 0.25, y + 0.03, 0.1, 0.1, 0.02, { sink: -out - 0.012 });
  };
  /// Номерная доска: гвозди рядами, на них бирки тех, кто в забое; taken — доля пустых гвоздей.
  M.tags = (W, y, { cols = 8, rows = 3, taken = 0.2, w = 1.2, name = 'Номерная доска' } = {}) => {
    if (!W) return;
    W.tile(name, 'wood', 0, y, w, rows * 0.16 + 0.1, 0.04, { sink: 0, roll: J(1.5) });
    for (let r = 0; r < rows; r++) for (let c = 0; c < cols; c++) { const du = -w / 2 + (c + 0.5) * w / cols, yy = y + 0.1 + r * 0.16; W.tile('Гвоздь', 'iron', du, yy + 0.06, 0.02, 0.02, 0.05, { sink: -0.04 }); if (!chance(taken)) W.tile('Бирка', 'bronze', du + J(0.01), yy, 0.05, 0.07, 0.012, { sink: -0.06, roll: J(8) }); }
  };
  /// Меловая метка жильщиков: arrow — стрелка (dir ±1 вдоль стены), cross — крест, count — счёт.
  M.chalk = (W, y, kind = 'cross', { du = 0, dir = 1, n = 7, m = 'bone', name = 'Меловая метка', k = 1, out = 0.01, slab } = {}) => {
    if (!W) return;
    if (slab) { W.tile('Гладкий камень', 'rock', du + (kind === 'count' ? n * 0.03 * k : 0), y - 0.16 * k, (kind === 'cross' ? 0.6 : 0.75) * k, 0.52 * k, 0.36, { sink: 0.03, roll: J(4) }); out = 0.335; }
    const st = (u, yy, w, h, roll = 0) => W.tile(name, m, du + u * k, yy, w * k, h * k, 0.01, { sink: -out, roll });
    if (kind === 'arrow') { st(0, y, 0.5, 0.035); st(dir * 0.18, y + 0.07 * k, 0.2, 0.035, -45 * dir); st(dir * 0.18, y - 0.07 * k, 0.2, 0.035, 45 * dir); }
    else if (kind === 'cross') { st(-0.12, y, 0.035, 0.34, -45); st(0.12, y, 0.035, 0.34, 45); }
    else for (let i = 0; i < n; i++) i % 5 === 4 ? st((i - 2) * 0.06, y + 0.02 * k, 0.28, 0.02, 25) : st(i * 0.06, y, 0.02, R(0.12, 0.16), J(5));
  };
  /// Сочащаяся вода: потёки по стене, натёк, мох и лужа у подошвы.
  M.seep = (W, y, { len = 1.6, moss = 0.7, pool = true } = {}) => {
    if (!W) return;
    const [fx, fz] = W.at(0, 0.45), fy = S.floor(fx, fz, y - len), base = fy === undefined ? y - len : fy;
    for (let i = 0; i < 4; i++) { const h = Math.min(y - base, len * R(0.5, 1)); W.tile('Потёк', 'wet', J(0.3), y - h, R(0.06, 0.16), h, 0.16, { sink: 0.02, roll: J(3) }); }
    W.tile('Натёк', 'slab', J(0.1), y - 0.1, R(0.3, 0.5), 0.16, 0.3, { sink: 0.03, roll: J(6) });
    for (let i = 0; i < 4; i++) if (chance(moss)) W.tile('Мох', 'moss', J(0.4), base + R(0.05, 0.9), R(0.3, 0.6), R(0.2, 0.4), 0.22, { sink: 0.02, roll: J(20) });
    if (pool && fy !== undefined) { S.pool(fx + J(0.1), fz + J(0.1), R(0.7, 1.2), R(0.5, 0.8), { y: fy }); D.patch(fx, fz, 0.6, { y: fy, n: 3 }); }
  };
  /// Жила в стене: ломаная из прожилок вдоль стены под уклон slope.
  M.vein = (W, y, { len = 2.4, slope = 0.3, m = 'ossarite', du = 0 } = {}) => {
    if (!W) return;
    for (let t = -len / 2; t < len / 2; t += R(0.22, 0.4)) if (chance(0.65) && S.open(W.f, W.u + du + t, y + t * slope)) W.tile('Прожилка', m, du + t, y + t * slope + J(0.05), R(0.08, 0.18), R(0.02, 0.035), 0.1, { sink: 0.03, roll: J(25) });
  };
  /// Крепь у стены от пола y: стойки, верхняк, подкосы, затяжка из досок за стойками.
  M.timbering = (W, y, { w = 2.2, h = 2.8, lag = 4, du = 0 } = {}) => {
    if (!W) return;
    for (const s of [-1, 1]) {
      W.tile('Стойка крепи', 'timber', du + s * w / 2, y, 0.24, h, 0.24, { sink: -0.03, roll: s * R(0.5, 2.5), pitch: J(1.5), round: true });
      const [x1, z1] = W.at(du + s * (w / 2 - 0.1), 0.17), [x2, z2] = W.at(du + s * (w / 2 - 0.8), 0.17); beam('Подкос крепи', 'timber', [x1, y + h - 0.9, z1], [x2, y + h - 0.08, z2], 0.13);
    }
    W.tile('Верхняк крепи', 'timber', du, y + h, w + 0.7, 0.26, 0.3, { sink: -0.03, roll: J(1.8) }); W.tile('Клин', 'wood', du + J(w * 0.3), y + h + 0.26, 0.3, 0.1, 0.2, { sink: -0.05, roll: J(8) });
    for (let i = 0; i < lag; i++) if (chance(0.8)) W.tile('Затяжка', 'wood', du + J(0.06), y + 0.35 + i * (h - 0.6) / lag, w - 0.3, R(0.14, 0.2), 0.04, { sink: -0.01, roll: J(2.5) });
  };
  /// Инструмент, прислонённый к стене: pick, shovel, hammer, bar.
  M.lean = (W, kind = 'pick', { du = 0, len = R(0.95, 1.25) } = {}) => {
    if (!W) return;
    const [bx, bz] = W.at(du, R(0.28, 0.4)), [tx, tz] = W.at(du + J(0.08), 0.06), fy = S.floor(bx, bz, W.y - 1.5);
    if (fy === undefined) return;
    beam(kind === 'bar' ? 'Лом' : 'Рукоять', kind === 'bar' ? 'iron' : 'timber', [bx, fy, bz], [tx, fy + len, tz], 0.04);
    if (kind === 'pick') W.tile('Кайло', 'iron', du, fy + len - 0.04, 0.5, 0.05, 0.06, { sink: -0.03, roll: J(12) });
    else if (kind === 'hammer') W.tile('Кувалда', 'iron', du, fy + len - 0.06, 0.22, 0.11, 0.11, { sink: -0.02, roll: J(8) });
    else if (kind === 'shovel') put('Штык лопаты', 'iron', bx, fy, bz, 0.2, 0.28, 0.03, { ry: W.ry, pitch: -14 });
  };
  /// Крюк в стене и то, что на нём висит: coil — бухта каната, bag — сума, lantern — фонарь, chain — цепь.
  M.hook = (W, y, kind = 'coil', { du = 0 } = {}) => {
    if (!W) return;
    W.tile('Крюк', 'iron', du, y, 0.05, 0.05, 0.16, { sink: 0.02 });
    const [x, z] = W.at(du, 0.12);
    if (kind === 'coil') { W.disc('Бухта каната', 'rope', du, y - 0.22, 0.46, 0.5, 0.1); W.disc('Бухта каната', 'rope', du, y - 0.24, 0.3, 0.34, 0.14); }
    else if (kind === 'bag') W.tile('Сума', 'leather', du, y - 0.5, 0.34, 0.44, 0.14, { sink: -0.02, roll: J(5) });
    else if (kind === 'lantern') { put('Фонарь', 'iron', x, y - 0.3, z, 0.14, 0.24, 0.14); put('Стекло фонаря', 'glass', x, y - 0.26, z, 0.15, 0.14, 0.1); }
    else D.chain(x, y, z, R(0.5, 1.1));
  };

  // ---------- напольное ----------
  /// Бревно лёжа: центр, длина вдоль курса ry.
  M.log = (x, y, z, len, dia, ry, { m = 'timber', name = 'Бревно' } = {}) => { const [sx, sz] = off(x, z, ry, -len / 2); return put(name, m, sx, y + dia / 2, sz, dia * R(0.9, 1), len, dia * R(0.9, 1), { round: true, pitch: 90, ry }); };
  M.plank = (x, y, z, len, ry, { w = 0.2, m = 'wood', name = 'Доска', tilt = 0 } = {}) => put(name, m, x, y, z, w, 0.035, len, { ry, pitch: tilt, roll: J(2) });
  /// Рельсы от a к b [x, y, z]: шпалы и две нитки кусками; miss — доля сорванных шпал и кусков.
  M.rail = (a, b, { gauge = 0.62, miss = 0.08, ties = true } = {}) => {
    const d = [b[0] - a[0], b[1] - a[1], b[2] - a[2]], len = Math.hypot(d[0], d[2]), ry = Math.atan2(d[0], d[2]) / DEG, n = Math.max(1, Math.round(len / 0.65)), k = Math.max(1, Math.round(len / 1.8)), rx = d[2] / len, rz = -d[0] / len;
    const P = (t, s, up) => [a[0] + d[0] * t + rx * s, a[1] + d[1] * t + up, a[2] + d[2] * t + rz * s];
    for (let i = 0; i < n; i++) if (ties && !chance(miss)) put('Шпала', 'timber', ...P((i + 0.5) / n, J(0.03), 0), gauge + R(0.4, 0.55), 0.07, R(0.14, 0.18), { ry: ry + J(5) });
    for (const s of [-1, 1]) for (let i = 0; i < k; i++) if (!chance(miss)) beam('Рельс', 'iron', P(i / k + 0.004, s * gauge / 2, 0.095), P((i + 1) / k - 0.004, s * gauge / 2 + J(0.012), 0.095), 0.05, 0.05);
  };
  /// Куча руды; glow — доля светящихся осколков.
  M.ore = (x, z, r = 0.5, n = 10, { y, glow = 0 } = {}) => {
    const fy = floorAt(x, z, y);
    D.rubble(x, z, r, n, { m: 'pebble', name: 'Руда', y: fy, pile: 0.45, big: 0.12 });
    for (let i = 0, c = Math.round(n * glow); i < c; i++) put('Осколок оссарита', 'ossarite', x + J(r * 0.6), fy + R(0, 0.04), z + J(r * 0.6), R(0.04, 0.08), R(0.06, 0.16), R(0.04, 0.07), { ry: R(0, 90), pitch: J(30), roll: J(30) });
  };
  M.basket = (x, z, { y, full = true } = {}) => {
    const fy = floorAt(x, z, y);
    put('Корзина', 'straw', x, fy, z, 0.44, 0.3, 0.44, { round: true }); put('Обод корзины', 'rope', x, fy + 0.28, z, 0.47, 0.04, 0.47, { round: true });
    if (full) for (let i = 0; i < 5; i++) put('Кусок руды', 'pebble', x + J(0.11), fy + 0.3, z + J(0.11), R(0.1, 0.17), R(0.07, 0.12), R(0.1, 0.15), { ry: R(0, 180), roll: J(20) });
  };
  M.bucket = (x, z, { y, tipped } = {}) => { const fy = floorAt(x, z, y); put('Ведро', 'wood', x, fy + (tipped ? 0.15 : 0), z, 0.3, 0.32, 0.3, { round: true, pitch: tipped ? 90 : 0, ry: R(0, 360) }); if (!tipped) put('Обруч ведра', 'iron', x, fy + 0.24, z, 0.32, 0.03, 0.32, { round: true }); };
  M.coil = (x, z, { y, dia = 0.5 } = {}) => { const fy = floorAt(x, z, y); put('Бухта каната', 'rope', x, fy, z, dia, 0.1, dia, { round: true }); put('Бухта каната', 'rope', x + J(0.03), fy + 0.1, z + J(0.03), dia * 0.8, 0.08, dia * 0.8, { round: true }); };
  /// Инструмент на полу: pick — кайло, shovel — лопата, hammer — кувалда, bar — лом, wedge — клинья.
  M.tool = (kind, x, z, { y, ry = R(0, 360) } = {}) => {
    const fy = floorAt(x, z, y), [hx, hz] = off(x, z, ry, 0.36);
    if (kind === 'bar') return put('Лом', 'iron', x, fy, z, 0.035, 0.035, 1.3, { ry });
    if (kind === 'wedge') { for (let i = 0; i < 4; i++) put('Клин', 'iron', x + J(0.2), fy, z + J(0.2), 0.05, 0.04, 0.16, { ry: R(0, 180) }); return; }
    put('Рукоять', 'timber', x, fy, z, 0.04, 0.04, kind === 'shovel' ? 1.1 : 0.8, { ry });
    if (kind === 'pick') put('Кайло', 'iron', hx, fy, hz, 0.52, 0.045, 0.06, { ry: ry + J(4) });
    else if (kind === 'hammer') put('Кувалда', 'iron', hx, fy, hz, 0.22, 0.1, 0.1, { ry });
    else { const [bx, bz] = off(x, z, ry, 0.66); put('Штык лопаты', 'iron', bx, fy, bz, 0.2, 0.025, 0.28, { ry }); }
  };
  /// Стойка крепи с подхватом и клином под кровлю; snapped — сломана: пень и вершина рядом на полу.
  M.post = (x, y, z, h, { dia = 0.22, snapped, ry = R(0, 360) } = {}) => {
    if (!snapped) { put('Стойка крепи', 'timber', x, y, z, dia, h - 0.1, dia, { round: true, roll: J(2), pitch: J(2) }); put('Подхват', 'wood', x, y + h - 0.1, z, 0.7, 0.07, 0.26, { ry }); put('Клин', 'wood', x + J(0.1), y + h - 0.03, z + J(0.1), 0.24, 0.03, 0.12, { ry: ry + 90 }); return; }
    put('Сломанная стойка', 'timber', x, y, z, dia, h * R(0.3, 0.45), dia, { round: true, roll: J(9), pitch: J(9) });
    const [lx, lz] = off(x, z, ry, 0.9); M.log(lx, y, lz, h * 0.5, dia, ry + J(10), { name: 'Вершина стойки' });
    for (let i = 0; i < 5; i++) put('Щепа', 'wood', x + J(0.5), y, z + J(0.5), R(0.15, 0.4), 0.02, R(0.04, 0.08), { ry: R(0, 180) });
  };
  /// Костровая крепь: клеть из брёвен крест-накрест.
  M.crib = (x, y, z, w, h, { ry = 0, dia = 0.2 } = {}) => { for (let j = 0, n = Math.floor(h / dia); j < n; j++) for (const s of [-1, 1]) { const [lx, lz] = off(x, z, ry + (j % 2) * 90, 0, s * (w / 2 - dia / 2)); M.log(lx, y + j * dia * 0.97, lz, w + J(0.08), dia, ry + (j % 2) * 90 + J(2)); } };
  /// Дверной оклад поперёк хода: две стойки и верхняк между точками a и b [x, z] на полу y; sag — провис сломанного конца.
  M.frame = (a, b, y, h, { th = 0.24, sag = 0 } = {}) => {
    for (const [x, z] of [a, b]) put('Стойка крепи', 'timber', x, y, z, th, h, th, { round: true, roll: J(2.5), pitch: J(2.5) });
    const d = [b[0] - a[0], b[1] - a[1]], len = Math.hypot(...d), e = 0.3 / len;
    beam('Верхняк', 'timber', [a[0] - d[0] * e, y + h + th / 2, a[1] - d[1] * e], [b[0] + d[0] * e, y + h + th / 2 - sag, b[1] + d[1] * e], th + 0.04, th);
    const t = sag ? R(0.6, 0.85) : R(0.15, 0.4); put('Клин', 'wood', a[0] + d[0] * t, y + h + th - sag * t - 0.02, a[1] + d[1] * t, 0.26, 0.08, 0.16, { ry: R(0, 180) });
  };
  /// Приставная лестница от a к b [x, y, z]; ry — куда она смотрит, если стоит отвесно.
  M.ladder = (a, b, { w = 0.46, ry, miss = 0.1 } = {}) => {
    const hx = b[0] - a[0], hz = b[2] - a[2], yaw = ry !== undefined ? ry : Math.atan2(hx, hz) / DEG, rx = Math.cos(yaw * DEG) * w / 2, rz = -Math.sin(yaw * DEG) * w / 2, len = Math.hypot(hx, b[1] - a[1], hz);
    for (const s of [-1, 1]) beam('Тетива лестницы', 'timber', [a[0] + s * rx, a[1], a[2] + s * rz], [b[0] + s * rx, b[1], b[2] + s * rz], 0.06);
    for (let t = 0.3; t < len - 0.1; t += 0.32) if (!chance(miss)) { const k = t / len, p = [a[0] + hx * k, a[1] + (b[1] - a[1]) * k, a[2] + hz * k]; beam('Перекладина', 'wood', [p[0] - rx, p[1], p[2] - rz], [p[0] + rx, p[1] + 0.001, p[2] + rz], 0.035); }
  };
  M.rope = (a, b, th = 0.035, name = 'Канат') => beam(name, 'rope', a, b, th);
  /// Стоянка жильщиков: спальники, мешок, кружка, остывший костёр с котелком.
  M.camp = (x, z, { y, ry = 0, fire = [-0.2, -1.35], beds = 1 } = {}) => {
    const fy = floorAt(x, z, y);
    for (let i = 0; i < beds; i++) { const [bx, bz] = off(x, z, ry, 0, i * 0.95); put('Спальник', pick(['cloth', 'clothBlue', 'leather']), bx, fy, bz, 0.7, 0.07, 1.8, { ry: ry + J(8) }); const [px, pz] = off(bx, bz, ry, 0.72, J(0.1)); put('Скатка под голову', 'cloth', px, fy + 0.07, pz, 0.45, 0.12, 0.22, { ry: ry + J(15) }); }
    const [sx, sz] = off(x, z, ry, 0.9, -0.75); put('Заплечный мешок', 'leather', sx, fy, sz, 0.38, 0.4, 0.28, { ry: ry + J(30), roll: J(6) }); put('Клапан мешка', 'cloth', sx, fy + 0.4, sz, 0.4, 0.05, 0.3, { ry: ry + J(30) }); put('Кружка', 'clay', sx + 0.32, fy, sz + J(0.15), 0.08, 0.1, 0.08, { round: true });
    if (!fire) return;
    const [cx, cz] = off(x, z, ry, fire[0], fire[1]);
    for (let i = 0; i < 8; i++) { const a = i / 8 * 2 * Math.PI; put('Камень кострища', 'rock', cx + Math.cos(a) * 0.42, fy, cz + Math.sin(a) * 0.42, R(0.18, 0.28), R(0.1, 0.18), R(0.15, 0.22), { ry: R(0, 180), roll: J(8) }); }
    put('Зола', 'ash', cx, fy, cz, 0.6, 0.04, 0.6, { round: true }); for (let i = 0; i < 3; i++) put('Головня', 'soot', cx + J(0.18), fy + 0.04, cz + J(0.18), 0.07, 0.06, R(0.25, 0.45), { ry: R(0, 180) });
    D.patch(cx, cz, 0.8, { m: 'soot', name: 'Сажа', y: fy, n: 4 }); put('Котелок', 'iron', cx + 0.5, fy, cz + 0.3, 0.24, 0.18, 0.24, { round: true });
  };
  /// Приношение Спящих: череп в кругу огарков, кости лучами, руна.
  M.offering = (x, z, { y, n = 6, r = 0.45, rune = true } = {}) => {
    const fy = floorAt(x, z, y);
    put('Череп', 'bone', x, fy, z, 0.2, 0.2, 0.24, { ry: R(0, 360) });
    for (let i = 0; i < n; i++) { const a = i / n * 2 * Math.PI, cx = x + Math.cos(a) * r, cz = z + Math.sin(a) * r; put('Огарок', 'bone', cx, fy, cz, 0.05, R(0.05, 0.2), 0.05, { round: true }); put('Наплыв воска', 'bone', cx, fy, cz, 0.14, 0.012, 0.12, { round: true }); put('Кость', 'bone', x + Math.cos(a + 0.5) * r * 0.55, fy, z + Math.sin(a + 0.5) * r * 0.55, 0.3, 0.05, 0.06, { ry: 90 - (a + 0.5) / DEG }); }
    if (rune) { put('Руна', 'rune', x, fy + 0.002, z, r * 2.6, 0.01, 0.04, { ry: R(0, 180) }); put('Руна', 'rune', x, fy + 0.002, z, 0.04, 0.01, r * 2.6, { ry: R(0, 30) }); }
  };
  /// Останки лёжа, голова по курсу ry; helmet — рядом каска рудокопа.
  M.body = (x, z, { y, ry = R(0, 360), helmet } = {}) => {
    const fy = floorAt(x, z, y), P = (f, r) => { const [px, pz] = off(x, z, ry, f, r); return [px, fy, pz]; };
    put('Череп', 'bone', ...P(0.78, J(0.06)), 0.2, 0.2, 0.24, { ry: ry + J(50), roll: J(25) }); put('Рёбра', 'bone', ...P(0.4, 0), 0.36, 0.16, 0.42, { ry, round: true }); put('Хребет', 'bone', ...P(0.12, J(0.03)), 0.06, 0.05, 0.46, { ry: ry + J(8) }); put('Таз', 'bone', ...P(-0.08, 0), 0.3, 0.1, 0.18, { ry, round: true });
    for (const s of [-1, 1]) { put('Кость', 'bone', ...P(0.45 + J(0.1), s * 0.32), 0.05, 0.05, 0.34, { ry: ry + s * R(10, 60) }); put('Кость', 'bone', ...P(-0.35, s * 0.13), 0.06, 0.06, 0.44, { ry: ry + s * R(0, 14) }); put('Кость', 'bone', ...P(-0.8, s * R(0.12, 0.25)), 0.05, 0.05, 0.4, { ry: ry + J(25) }); }
    if (helmet) put('Каска рудокопа', 'leather', ...P(0.95, 0.32), 0.26, 0.16, 0.28, { round: true, roll: J(30) });
  };
  /// Руда горкой в вагонетках комнаты: fill — доля гружёных, glow — доля светящихся кусков.
  M.loads = (fill = 0.7, glow = 0) => {
    for (const o of S.base.filter(o => o.name === 'Рудная вагонетка')) if (chance(fill)) {
      const [x, y, z] = o.p, [w, h, d] = o.s;
      put('Руда в вагонетке', 'pebble', x, y + 0.36, z, w - 0.2, h - 0.5, d - 0.2, { ry: o.ry || 0 });
      for (let i = 0; i < 9; i++) { const [px, pz] = off(x, z, o.ry || 0, J(d / 2 - 0.25), J(w / 2 - 0.25)); put('Кусок руды', chance(glow) ? 'ossarite' : 'pebble', px, y + h - 0.16, pz, R(0.14, 0.3), R(0.1, 0.2), R(0.14, 0.26), { ry: R(0, 180), roll: J(20), pitch: J(20) }); }
    }
  };
  return M;
};
