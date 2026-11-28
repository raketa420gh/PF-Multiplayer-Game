// Вид. Жильное хранилище зодчего Мартина: один ход, свёрнутый улиткой вокруг камеры с жилой. От входов к центру меняется сам камень:
// кирпичная обкладка империи → тёсаный камень → голая скала с жилой. История: стрелки на стенах, половина лжёт (мел жильщиков,
// уголь тех, кто хотел, чтобы другие не вышли); Мартин заблудился на приёмке и восемь дней сидел в тупике со своим мерным шнуром.
exports.dress = S => {
  const { D } = S, { put, tile, beam, blot, R, J, pick, chance } = D, DEG = Math.PI / 180, STEP = 2.8, N = 11, TOP = 6;
  // ---------- решётка хода: клетки 2,8 м, проходы, расстояние до камеры ----------
  const DIR = { N: [0, 1], E: [1, 0], S: [0, -1], W: [-1, 0] }, BACK = { N: 'S', E: 'W', S: 'N', W: 'E' }, c = i => -14 + STEP * i;
  const clear = (x1, z1, x2, z2) => { for (let t = 0; t <= 1.001; t += 0.05) if (!S.air(x1 + (x2 - x1) * t, 1.5, z1 + (z2 - z1) * t)) return false; return true; };
  const cells = [], cell = (i, j) => i < 0 || j < 0 || i >= N || j >= N ? null : cells[j * N + i];
  for (let j = 0; j < N; j++) for (let i = 0; i < N; i++) {
    const q = { i, j, x: c(i), z: c(j), n: {}, walls: [], d: 999 };
    for (const k of 'NESW') { const [dx, dz] = DIR[k], inside = i + dx >= 0 && i + dx < N && j + dz >= 0 && j + dz < N; q.n[k] = clear(q.x, q.z, q.x + dx * (inside ? STEP : 2.7), q.z + dz * (inside ? STEP : 2.7)); if (q.n[k] && !inside) q.door = k; if (!q.n[k]) q.walls.push(k); }
    q.deg = 4 - q.walls.length; q.hall = Math.abs(i - 5) <= 1 && Math.abs(j - 5) <= 1; cells.push(q);
  }
  for (const queue = [Object.assign(cell(5, 5), { d: 0 })]; queue.length;) { const q = queue.shift(); for (const k of 'NESW') { const o = q.n[k] && cell(q.i + DIR[k][0], q.j + DIR[k][1]); if (o && o.d > q.d + 1) { o.d = q.d + 1; queue.push(o); } } }
  const near = (x, z) => cell(Math.round((x + 14) / STEP), Math.round((z + 14) / STEP));
  const zone = d => d <= 22 ? 0 : d <= 58 ? 1 : 2;                                                     // 0 скала с жилой, 1 тёсаный камень, 2 кирпич
  /// Пояс стены: самый глубокий из ходов по обе её стороны.
  const wallZone = o => { const along = o.s[0] >= o.s[2], len = Math.max(o.s[0], o.s[2]); let best = 999; for (const t of [-0.4, 0, 0.4]) for (const s of [-0.9, 0.9]) { const q = near(o.p[0] + (along ? t * len : s), o.p[2] + (along ? s : t * len)); if (q) best = Math.min(best, q.d); } return zone(best); };
  S.theme({ rock: 'brick', wall: 'stone', floor: 'cobble', arch: 'stone', age: 1.05, density: 0.55, damp: (x, y, z) => 0.1 + 0.25 * Math.max(0, 1 - Math.hypot(x - 11, z + 4) / 9), mat: o => o.cat === 'wall' ? ['rock', 'stone', 'brick'][wallZone(o)] : undefined });
  S.keep(-3.9, 0, -6.7, -1.7, 0.6, -4.5).keep(4.5, 0, 7.3, 6.7, 0.6, 9.5).keep(-15.2, 0, -15.2, -12.8, 0.6, -12.8);
  S.auto({ skip: ['debris'] });

  // ---------- помощники ----------
  /// Стена от точки: как S.wall, но плоскость уточнена до полусантиметра (тонкие метки не тонут в камне); null — стены нет.
  const wallAt = (x, z, dir, max = 3) => {
    const w = S.wall(x, z, dir, 1.5, max), [dx, dz] = DIR[dir], s = dx || dz;
    if (!w) return null;
    let p = w[1] - s * 0.12;
    for (let i = 0; i < 60 && S.air(dx ? p : x, 1.5, dx ? z : p); i++) p += s * 0.005;
    return [w[0], Math.round(p * 200) / 200, w[2]];
  };
  const side = (q, k, du = 0) => { const w = wallAt(q.x + (DIR[k][0] ? 0 : du), q.z + (DIR[k][0] ? du : 0), k); if (!w) throw new Error(`Labyrinth: у клетки ${q.x}, ${q.z} нет стены ${k}`); return w; };
  const hang = (name, m, w, y, ww, h, d, o) => tile(name, m, w[0], w[1], w[2], y, ww, h, d, o);
  const off = (w, du) => [w[0], w[1], w[2] + du], out = (w, t, du = 0) => D.at(w[0], w[1], w[2] + du, t);
  const free = (x, z) => S.air(x, 0.3, z) && S.air(x, 1.2, z);
  // Крошка у подошвы стен: в узких ходах только мелочь.
  { let next = 0; for (const ft of S.feet) { if (--next > 0) continue; const [x, z] = S.fpos(ft.f, ft.u, 0.2); if (ft.clear < 1.4 || !free(x, z) || S.skip.some(q => x > q[0] && x < q[3] && z > q[2] && z < q[5])) continue; next = Math.round(R(8, 16)); D.rubble(x, z, R(0.15, 0.28), Math.round(R(2, 6)), { m: ft.m, name: ft.kind === 'course' ? 'Крошка кладки' : 'Камень осыпи', pile: 0.25, big: ft.kind === 'course' ? 0 : 0.08 }); if (chance(S.damp(x, 0, z))) D.patch(x, z, 0.5, { n: 3 }); } }
  const candles = (x, y, z, n, spread = 0.18, lit = 0) => { for (let i = 0; i < n; i++) { const h = R(0.05, 0.22), cx = x + J(spread), cz = z + J(spread); put('Свеча', 'bone', cx, y, cz, 0.05, h, 0.05, { round: true }); if (chance(lit)) put('Огонёк', 'ember', cx, y + h, cz, 0.025, 0.05, 0.025, { round: true }); } put('Наплыв воска', 'bone', x, y, z, spread * 2 + 0.12, 0.012, spread * 1.7 + 0.1, { round: true, ry: R(0, 180) }); };
  const niche = (w, y, ww = 0.7, h = 0.7, m = 'stone') => { hang('Ниша', 'soot', w, y, ww, h, 0.03, { sink: -0.01 }); hang('Полка ниши', m, w, y - 0.1, ww + 0.2, 0.1, 0.26, { sink: 0.02 }); for (const s of [-1, 1]) hang('Откос ниши', m, off(w, s * (ww / 2 + 0.06)), y, 0.12, h, 0.1, { sink: 0.02 }); hang('Перемычка ниши', m, w, y + h, ww + 0.3, 0.14, 0.12, { sink: 0.02, roll: J(1.5) }); return out(w, 0.12); };
  const sconce = (w, y) => { hang('Пустой держатель факела', 'iron', w, y, 0.1, 0.34, 0.3, { sink: 0.02, pitch: -20 }); for (let i = 0; i < 3; i++) blot('Старая копоть', 'soot', w[0], w[1], w[2] + J(0.1), y + 0.6 + i * 0.25, R(0.25, 0.4), R(0.25, 0.4), 0.012, -99); };
  const rune = (w, y, s = 1) => { blot('Руна Спящих', 'rune', w[0], w[1], w[2], y + 0.5 * s, 0.4 * s, 0.4 * s, 0.012, -99); for (const t of [0, 55, -55]) hang('Руна Спящих', 'rune', w, y, 0.035, 0.7 * s, 0.012, { sink: -0.03, roll: t }); };
  const num = (w, n, y = 2) => { hang('Номер входа', 'slab', w, y, 0.22 + n * 0.09, 0.26, 0.06, { sink: 0.015 }); for (let i = 0; i < n; i++) hang('Бронзовая заклёпка', 'bronze', off(w, (i - (n - 1) / 2) * 0.09), y + 0.1, 0.05, 0.05, 0.03, { sink: -0.04 }); };
  /// Стрелка на стене: dir +1 — в сторону роста u; m — мел (bone), уголь (soot), кровь (blood), резьба (plaster); crossed — чем перечёркнута.
  const arrow = (w, y, dir, { m = 'bone', len = 0.55, crossed, name = m === 'bone' ? 'Стрелка мелом' : m === 'soot' ? 'Стрелка углём' : m === 'blood' ? 'Стрелка кровью' : 'Резная стрелка' } = {}) => {
    const sgn = w[0] === 'N' || w[0] === 'W' ? 1 : -1, tip = w[2] + dir * len / 2, a = 38, hl = len * 0.42, o = { sink: -0.035 };
    hang(name, m, w, y, len, 0.035, 0.012, { ...o, roll: J(3) });
    for (const t of [a, -a]) tile(name, m, w[0], w[1], tip - dir * Math.cos(t * DEG) * hl / 2, y - sgn * dir * Math.sin(t * DEG) * hl / 2, hl, 0.035, 0.012, { ...o, roll: t });
    if (crossed) for (const t of [32, -32]) hang('Перечёркнуто', crossed, w, y, len * 1.25, 0.04, 0.014, { sink: -0.05, roll: t });
  };
  /// Стрелка на стороне k клетки q, указывающая на проход g (перпендикулярный стене).
  const point = (q, k, g, y, o, du = 0) => arrow(off(side(q, k), du), y, (DIR[g][0] || DIR[g][1]), o);
  const corpse = (x, z, ry, { sit } = {}) => {
    const cs = Math.cos(ry * DEG), sn = Math.sin(ry * DEG), part = (n, lx, lz, w, h, d, o = {}) => put(n, 'bone', x + lx * cs + lz * sn, o.y || 0, z - lx * sn + lz * cs, w, h, d, { ry: ry + (o.ry || 0), round: o.round, pitch: o.pitch });
    if (sit) { part('Череп', J(0.04), 0.1, 0.2, 0.2, 0.24, { y: 0.62, ry: J(25) }); part('Рёбра', 0, 0.12, 0.36, 0.34, 0.2, { y: 0.26, round: true }); part('Хребет', 0, 0.06, 0.06, 0.56, 0.06, { y: 0.08 }); part('Таз', 0, 0.14, 0.3, 0.1, 0.22); for (const sx of [-1, 1]) { part('Кость руки', sx * 0.26, 0.3, 0.06, 0.05, 0.5, { ry: sx * 20 }); part('Кость ноги', sx * 0.12, 0.65, 0.07, 0.06, 0.8, { ry: sx * R(-6, 12) }); } return; }
    part('Череп', J(0.05), 0.85, 0.2, 0.2, 0.24, { ry: J(30) }); part('Рёбра', 0, 0.48, 0.36, 0.16, 0.34, { round: true }); part('Хребет', 0, 0.22, 0.06, 0.06, 0.42); part('Таз', 0, -0.05, 0.3, 0.1, 0.18);
    for (const sx of [-1, 1]) { part('Кость руки', sx * 0.3, 0.42, 0.06, 0.05, 0.55, { ry: sx * R(8, 35) }); part('Кость ноги', sx * 0.12, -0.52, 0.07, 0.06, 0.8, { ry: sx * R(-6, 12) }); }
  };
  const fire = (x, z) => { for (let i = 0; i < 8; i++) { const a = i / 8 * 2 * Math.PI; put('Камень кострища', 'rock', x + Math.cos(a) * 0.4, 0, z + Math.sin(a) * 0.4, R(0.18, 0.28), R(0.1, 0.18), R(0.15, 0.22), { ry: R(0, 180), roll: J(8) }); } put('Зола', 'ash', x, 0, z, 0.66, 0.04, 0.66, { round: true }); for (let i = 0; i < 3; i++) beam('Головня', 'soot', [x + J(0.25), 0.04, z + J(0.25)], [x + J(0.1), R(0.1, 0.2), z + J(0.1)], 0.07); D.patch(x, z, 0.8, { m: 'soot', name: 'Гарь', n: 3 }); };
  const bedroll = (x, z, ry, m = 'cloth') => { put('Спальник', m, x, 0, z, 0.7, 0.08, 1.8, { ry: ry + J(5) }); put('Скатка под голову', 'leather', x + Math.sin(ry * DEG) * 0.72, 0.08, z + Math.cos(ry * DEG) * 0.72, 0.5, 0.14, 0.22, { ry }); };
  const spikes = (x, z, r = 1.25) => {
    const spot = (k, fn) => { for (let i = 0, n = 0; i < 40 && n < k; i++) { const a = R(0, 360) * DEG, d = r * R(1, 1.25), px = x + Math.cos(a) * d, pz = z + Math.sin(a) * d; if (free(px, pz) && free(px + 0.3, pz) && free(px - 0.3, pz) && free(px, pz + 0.3) && free(px, pz - 0.3)) { fn(px, pz); n++; } } };
    spot(4, (px, pz) => put('Ржавый потёк', 'blood', px, 0.003, pz, R(0.3, 0.6), 0.012, R(0.2, 0.45), { round: true, ry: R(0, 180) }));
    spot(2, (px, pz) => beam('Гнутый прут', 'iron', [px - 0.15, 0.02, pz - 0.1], [px + R(0.1, 0.2), R(0.04, 0.14), pz + R(0.05, 0.2)], 0.03));
    spot(2, (px, pz) => D.bones(px, pz, 4, { spread: 0.22 }));
  };
  /// Прожилка оссарита в стене: ломаная из светящихся сколов, size растёт к камере.
  const vein = (w, y, size = 1) => { let u = 0, yy = y; for (let i = 0, n = Math.round(R(4, 7) * size); i < n; i++) { const l = R(0.2, 0.45) * size, a = R(-70, 70); hang('Прожилка оссарита', 'ossarite', off(w, u), yy, l, R(0.03, 0.06), 0.03, { sink: -0.05, roll: a }); u += Math.cos(a * DEG) * l * 0.8 * (w[0] === 'N' || w[0] === 'W' ? 1 : -1) * pick([1, 1, -1]); yy += Math.abs(Math.sin(a * DEG)) * l * 0.8; if (yy > 4.5 || Math.abs(u) > 0.9) break; } };

  // ---------- общие черты ходов: у каждой клетки своя, по поясам ----------
  const opposite = q => q.walls.includes('N') && q.walls.includes('S') ? 'N' : q.walls.includes('E') && q.walls.includes('W') ? 'E' : null;
  const F = {
    // Распорка между стенами над головой, на ней цепь с крюком или мёртвый фонарь.
    beam(q) { const k = opposite(q); if (!k) return F.sconce(q); const a = side(q, k), b = side(q, BACK[k]), y = R(3, 3.8), o = J(0.7), ns = k === 'N', A = ns ? [q.x + o, y, a[1] + 0.03] : [a[1] + 0.03, y, q.z + o], B = ns ? [q.x + o, y + J(0.08), b[1] - 0.03] : [b[1] - 0.03, y + J(0.08), q.z + o];
      beam('Распорка между стенами', 'timber', A, B, 0.2); const t = R(0.3, 0.7), hx = A[0] + (B[0] - A[0]) * t, hz = A[2] + (B[2] - A[2]) * t, len = R(0.5, 1.1); beam('Цепь', 'iron', [hx, y - len, hz], [hx, y - 0.08, hz], 0.03);
      if (chance(0.5)) put('Крюк', 'iron', hx, y - len - 0.14, hz, 0.07, 0.16, 0.12); else { put('Мёртвый фонарь', 'iron', hx, y - len - 0.28, hz, 0.18, 0.28, 0.18); put('Стекло фонаря', 'glass', hx, y - len - 0.24, hz, 0.2, 0.16, 0.12); } },
    // Ниша: череп, урна или фигурка смотрителя.
    niche(q) { const w = side(q, pick(q.walls)), [px, pz] = niche(w, 1.2, 0.6, 0.6, zone(q.d) === 2 ? 'brick' : 'stone'), kind = D.rnd(); if (kind < 0.4) put('Череп в нише', 'bone', px, 1.2, pz, 0.2, 0.2, 0.24, { ry: R(-30, 30) + D.FACE[w[0]].ry }); else if (kind < 0.7) { put('Урна', 'clay', px, 1.2, pz, 0.22, 0.3, 0.22, { round: true }); put('Крышка урны', 'clay', px, 1.5, pz, 0.15, 0.05, 0.15, { round: true }); } else { put('Фигурка смотрителя', 'stone', px, 1.2, pz, 0.16, 0.36, 0.14); put('Голова фигурки', 'stone', px, 1.56, pz, 0.12, 0.12, 0.12, { round: true }); } },
    // Вывалившиеся из стены камни и трещина над ними.
    fall(q) { const w = side(q, pick(q.walls)), m = ['rock', 'stone', 'brick'][zone(q.d)], [x, z] = out(w, 0.35, J(0.5)); D.rubble(x, z, 0.35, Math.round(R(8, 14)), { m, pile: 0.5, big: 0.2, name: m === 'brick' ? 'Кирпич' : 'Выпавший камень' }); hang('Пролом в кладке', 'soot', off(w, J(0.3)), R(0.5, 1.2), R(0.5, 0.9), R(0.4, 0.7), 0.014, { sink: -0.01, roll: J(8) }); for (let i = 0, u = J(0.2), y = 1.8; i < 4; i++) { hang('Трещина', 'soot', off(w, u), y, 0.03, 0.5, 0.012, { sink: -0.01, roll: J(25) }); u += J(0.15); y += 0.45; } D.patch(x, z, 0.6, { m: 'dirt', name: 'Пыль', n: 3 }); },
    // Паутина над головой и кокон.
    web(q) { const k = opposite(q) || q.walls[0], a = side(q, k), y = R(3, 4.2), ns = k === 'N' || k === 'S', b = opposite(q) ? side(q, BACK[k]) : null, o = J(0.6), A = ns ? [q.x + o, y, a[1]] : [a[1], y, q.z + o], B = b ? (ns ? [q.x + o + J(0.5), y + J(0.3), b[1]] : [b[1], y + J(0.3), q.z + o + J(0.5)]) : (ns ? [q.x + o + 1, y - 0.6, a[1]] : [a[1], y - 0.6, q.z + o + 1]);
      S.web(A, B, { n: 6 }); if (b && chance(0.6)) { const t = R(0.3, 0.7); put('Кокон', 'cloth', A[0] + (B[0] - A[0]) * t, y - 0.75, A[2] + (B[2] - A[2]) * t, 0.26, 0.5, 0.26, { round: true, roll: J(8) }); } },
    // Тот, кто не вышел: сидит у стены, рядом его вещи.
    bones(q) { const k = pick(q.walls), w = side(q, k), [x, z] = out(w, 0.32, J(0.4)); corpse(x, z, D.FACE[w[0]].ry, { sit: true }); const [px, pz] = out(w, 0.3, 0.75); put(pick(['Котомка', 'Пустая фляга', 'Погасший фонарь']), pick(['leather', 'clay', 'iron']), px, 0, pz, 0.3, 0.22, 0.24, { ry: R(0, 90), round: true }); if (chance(0.5)) { D.tally(w[0], w[1] + (DIR[k][0] || DIR[k][1]) * -0.03, w[2] - 0.9, 1.1, Math.round(R(3, 9))); put('Огрызок мела', 'bone', x + J(0.3), 0, z + J(0.3), 0.04, 0.03, 0.09, { ry: R(0, 180) }); } },
    // Капель: лужа, потёк, мох.
    pool(q) { const w = side(q, pick(q.walls)), [x, z] = out(w, 0.6, J(0.4)); S.pool(x, z, R(0.9, 1.4), R(0.6, 0.9)); hang('Потёк воды', 'wet', w, 0.1, 0.25, R(2.4, 4), 0.012, { sink: -0.03 }); D.mossWall(w[0], w[1], w[2], R(0.8, 1.6), 0.8, { base: 0 }); },
    sconce(q) { sconce(side(q, pick(q.walls)), 2.1); },
    // Зарубки дней и следы ногтей.
    scratch(q) { const k = pick(q.walls), w = side(q, k), p = w[1] + (DIR[k][0] || DIR[k][1]) * -0.03; for (let r = 0, n = Math.round(R(1, 3)); r < n; r++) D.tally(w[0], p, w[2] - 0.5, 1.5 - r * 0.2, Math.round(R(6, 16))); for (let i = 0; i < 4; i++) hang('След ногтей', 'plaster', off(w, 0.5 + i * 0.05), R(0.9, 1.1), 0.014, R(0.3, 0.5), 0.012, { sink: -0.035, roll: J(6) }); },
    // Знамя империи, от которого остались полосы.
    banner(q) { S.banner(side(q, pick(q.walls)), R(3.6, 4.2), { w: 0.9, h: 2, torn: 0.9, m: pick(['clothRed', 'cloth']) }); },
    // Корни сквозь свод.
    roots(q) { S.roots(q.x + J(0.5), TOP, q.z + J(0.5), { n: 6, len: R(2, 3.4), spread: 0.5 }); },
    // Грибница: чужой свет.
    fungus(q) { const w = side(q, pick(q.walls)), [x, z] = out(w, 0.3, J(0.4)); D.fungus(x, z, Math.round(R(5, 8)), { spread: 0.3 }); for (let i = 0; i < 4; i++) hang('Трутовик', 'fungus', off(w, J(0.6)), R(0.4, 2.4), R(0.2, 0.36), 0.07, R(0.14, 0.22), { sink: 0.02, round: true }); },
    // Кольца с кандалами: ходы путали и против каторжников.
    chains(q) { const w = side(q, pick(q.walls)); S.shackle(off(w, -0.4), 1.9, R(0.6, 1)); S.shackle(off(w, 0.45), 1.6, R(0.4, 0.8)); },
    // Спящие: руна, свечи, кости рядком.
    offer(q) { const w = side(q, pick(q.walls)), [x, z] = out(w, 0.3); rune(w, 1.2, R(0.7, 1)); candles(x, 0, z, Math.round(R(3, 5)), 0.2, 0.5); for (let i = 0; i < 4; i++) { const [bx, bz] = out(w, 0.55, -0.45 + i * 0.3); put('Кость-подношение', 'bone', bx, 0, bz, 0.06, 0.05, 0.3, { ry: D.FACE[w[0]].ry + J(10) }); } },
    // Жила: чем ближе к камере, тем ярче.
    vein(q) { const s = q.d <= 8 ? 1.5 : q.d <= 15 ? 1.1 : 0.8; for (const k of q.walls.slice(0, q.d <= 8 ? 2 : 1)) vein(off(side(q, k), J(0.5)), R(0.4, 1.6), s); if (q.d <= 10) { const w = side(q, q.walls[0]), [x, z] = out(w, 0.25, J(0.6)); D.ossarite(x, z, { n: 4, size: 0.7 }); } }
  };
  const SEQ = [['vein', 'roots', 'fall', 'vein', 'bones', 'pool', 'vein', 'niche', 'web', 'vein', 'sconce'], ['niche', 'offer', 'beam', 'web', 'fall', 'chains', 'bones', 'sconce', 'fungus', 'pool', 'scratch', 'banner', 'beam'], ['sconce', 'bones', 'beam', 'scratch', 'fall', 'pool', 'banner', 'web', 'niche', 'chains', 'beam']];
  const OWN = new Set(['0,0', '0,1', '4,0', '4,1', '6,1', '7,1', '7,2', '9,3', '9,7', '7,8', '7,9', '6,9', '1,9', '10,6', '4,3']);   // клетки со своими сценами
  const plain = q => !q.hall && q.d < 999 && q.walls.length && !q.door && !OWN.has(`${q.i},${q.j}`);
  for (const q of cells) if (plain(q)) { const seq = SEQ[zone(q.d)]; F[seq[q.d % seq.length]](q); }

  // ---------- стрелки в ходах: мел жильщиков чаще ведёт к жиле, уголь — прочь; часть перечёркнута чужой рукой ----------
  const inward = q => 'NESW'.split('').find(k => q.n[k] && (o => o && o.d < q.d)(cell(q.i + DIR[k][0], q.j + DIR[k][1])));
  for (const q of cells) {
    if (!plain(q) || q.d % 4 !== 1) continue;
    const t = inward(q), k = t && q.walls.find(k => !!DIR[k][0] !== !!DIR[t][0]);
    if (!k) continue;
    const w = off(side(q, k), J(0.5)), dT = DIR[t][0] || DIR[t][1], y = R(1.4, 1.75), mode = D.rnd();
    if (mode < 0.36) arrow(w, y, dT, { m: chance(0.75) ? 'bone' : 'plaster' });                                   // правда
    else if (mode < 0.68) arrow(w, y, -dT, { m: chance(0.7) ? 'soot' : 'bone' });                                 // ложь
    else if (mode < 0.85) { arrow(w, y, -dT, { m: 'soot', crossed: 'bone' }); arrow(w, y - 0.32, dT); }           // ложь разоблачена
    else { arrow(w, y, dT, { crossed: 'soot' }); arrow(w, y - 0.32, -dT, { m: 'soot' }); }                        // правду затёрли
  }
  // Пороги поясов: там, где кирпич сменяется тёсаным камнем, а тот скалой, над ходом перемычка с бронзовой полосой.
  for (const q of cells) if ((q.d === 23 || q.d === 59) && opposite(q) && !OWN.has(`${q.i},${q.j}`)) { const k = opposite(q), a = side(q, k), b = side(q, BACK[k]), ns = k === 'N'; put('Перемычка пояса', 'stone', ns ? q.x : (a[1] + b[1]) / 2, 2.8, ns ? (a[1] + b[1]) / 2 : q.z, ns ? 0.5 : Math.abs(a[1] - b[1]) + 0.06, 0.45, ns ? Math.abs(a[1] - b[1]) + 0.06 : 0.5); put('Бронзовая полоса порога', 'bronze', ns ? q.x : (a[1] + b[1]) / 2, 0.003, ns ? (a[1] + b[1]) / 2 : q.z, ns ? 0.12 : 2.2, 0.012, ns ? 2.2 : 0.12); }
  // Восемь входов: номер на стене прохода и пустой держатель напротив.
  [[-8.4, 14], [8.4, 14], [14, 8.4], [14, -8.4], [8.4, -14], [-8.4, -14], [-14, -8.4], [-14, 8.4]].forEach(([x, z], i) => { const ns = Math.abs(z) > Math.abs(x), px = ns ? x : x + Math.sign(x) * 1.9, pz = ns ? z + Math.sign(z) * 1.9 : z, a = wallAt(px, pz, ns ? 'E' : 'N', 2), b = wallAt(px, pz, ns ? 'W' : 'S', 2); if (a) num(a, i + 1); if (b) sconce(b, 2.1); });

  // ---------- развилки ----------
  // Юг: каменный указатель; резная стрелка империи верна, угольную («выход») кто-то перечеркнул мелом. Здесь же обрезан шнур Мартина.
  { const q = cell(4, 1), w = side(q, 'W'); put('Каменный указатель', 'stone', -3.72, 0, -11.2, 0.4, 1.25, 0.4, { ry: J(3) }); put('Шапка указателя', 'stone', -3.72, 1.25, -11.2, 0.5, 0.12, 0.5, { ry: 4 });
    arrow(off(w, 0.75), 1.75, 1, { m: 'plaster', len: 0.7 }); arrow(off(w, -0.75), 1.6, -1, { m: 'soot', crossed: 'bone' }); arrow(off(w, 0.7), 1.3, 1); put('Обрезанный конец шнура', 'rope', -3.4, 0.02, -10.5, 0.2, 0.03, 0.05, { ry: 30 }); put('Нож', 'iron', -3.2, 0, -10.3, 0.03, 0.02, 0.22, { ry: 60 }); }
  // Юго-восток: черепа на кольях, руна; угольная стрелка зовёт на юг, в тупик Мартина.
  { const q = cell(7, 2), w = side(q, 'E'); for (const dz of [-0.85, 0.85]) { put('Кол', 'timber', 6.5, 0, -8.4 + dz, 0.06, 1.75, 0.06, { roll: J(3), pitch: J(3) }); put('Череп на колу', 'bone', 6.5, 1.72, -8.4 + dz, 0.2, 0.2, 0.24, { ry: 270 + J(20) }); put('Перекладина', 'timber', 6.5, 1.35, -8.4 + dz, 0.05, 0.05, 0.5); for (const s of [-0.2, 0.2]) put('Лоскут', 'clothRed', 6.5, 0.95, -8.4 + dz + s, 0.02, 0.4, 0.08, { roll: J(6) }); }
    rune(w, 1.9, 0.8); arrow(off(w, 0), 1.45, -1, { m: 'soot', len: 0.7 }); arrow(off(w, 0.1), 1.1, 1, { len: 0.45 }); for (let i = 0; i < 5; i++) put('Кость-подношение', 'bone', 6.35, 0, -8.9 + i * 0.25, 0.3, 0.05, 0.06, { ry: J(12) }); }
  // Восток: статуя зодчего с вытянутой рукой — указывает на ближний выход, а не на жилу.
  { const q = cell(10, 6), w = side(q, 'E'), x = w[1] - 0.32; put('Постамент', 'stone', x, 0, 2.8, 0.56, 0.45, 0.56); put('Карниз постамента', 'stone', x, 0.45, 2.8, 0.64, 0.07, 0.64); for (const s of [-1, 1]) put('Нога статуи', 'stone', x, 0.52, 2.8 + s * 0.1, 0.17, 0.62, 0.15); put('Торс статуи зодчего', 'stone', x, 1.14, 2.8, 0.26, 0.56, 0.42); put('Голова статуи', 'stone', x, 1.72, 2.8, 0.2, 0.24, 0.2, { round: true });
    beam('Указующая рука', 'stone', [x, 1.55, 3.02], [x, 1.62, 3.62], 0.1); put('Свиток с планом в руке', 'stone', x - 0.12, 1.2, 2.62, 0.1, 0.34, 0.1, { round: true, roll: 20 }); hang('Табличка: зодчий Мартин', 'bronze', off(w, -0.75), 1.3, 0.4, 0.22, 0.02, { sink: -0.01 });
    arrow(off(w, -0.8), 1.7, -1, { len: 0.5 }); arrow(off(w, 0.85), 1.5, 1, { m: 'soot', crossed: 'bone' }); }
  // Север: красные лоскуты жильщиков вели на запад, к жиле; кто-то сорвал половину и начертил углём восток — там шипы.
  { const q = cell(6, 9), w = side(q, 'N'); for (let i = 0; i < 6; i++) { hang('Гвоздь', 'iron', off(w, -0.9 + i * 0.3), 1.9, 0.03, 0.03, 0.06, { sink: 0 }); if (i < 3) hang('Красный лоскут', 'clothRed', off(w, -0.9 + i * 0.3), 1.42, 0.09, 0.48, 0.012, { sink: -0.03, roll: J(6) }); }
    for (const [x, z] of [[3.1, 11.9], [3.5, 11.6], [2.6, 11.7]]) put('Сорванный лоскут', 'clothRed', x, 0.004, z, 0.1, 0.012, 0.4, { ry: R(0, 180) }); arrow(off(w, -0.5), 1.15, -1, { crossed: 'soot', len: 0.6 }); arrow(off(w, 0.55), 1.15, 1, { m: 'soot', len: 0.7 }); }
  { const q = cell(7, 9), w = side(q, 'E'); arrow(w, 1.55, -1, { m: 'soot', len: 0.7 }); hang('Угольный рисунок: сундук', 'soot', off(w, 0.1), 1.05, 0.34, 0.22, 0.012, { sink: -0.035 }); hang('Угольный рисунок: сундук', 'soot', off(w, 0.1), 1.27, 0.38, 0.06, 0.012, { sink: -0.035 }); }

  // ---------- тупики ----------
  // Тупик с шипами: приманка — монеты и грибной свет у дальней стены; меловое «берегись» затёрто углём.
  spikes(5.6, 8.4, 1.15); { const q = cell(7, 8), s = side(q, 'S'), e = side(q, 'E'); for (let i = 0; i < 7; i++) put('Монета', 'gold', 5.6 + J(0.9), 0.003, 7.45 + J(0.12), 0.05, 0.012, 0.05, { round: true }); put('Пустой кошель', 'leather', 6.4, 0, 7.5, 0.2, 0.06, 0.16, { ry: 30 }); D.fungus(4.75, 7.5, 6, { spread: 0.2 }); for (let i = 0; i < 4; i++) hang('Трутовик', 'fungus', off(s, J(0.9)), R(0.5, 2.2), R(0.2, 0.36), 0.07, R(0.14, 0.22), { sink: 0.02, round: true });
    for (const t of [45, -45]) hang('Мел: берегись', 'bone', off(e, 0.9), 1.55, 0.3, 0.03, 0.012, { sink: -0.035, roll: t }); for (let i = 0; i < 4; i++) hang('Затёрто углём', 'soot', off(e, 0.9), 1.42 + i * 0.09, 0.44, 0.07, 0.014, { sink: -0.05, roll: J(8) }); }
  // Тупик с гробом: заблудшего похоронили там, где нашли.
  { const q = cell(4, 0), s = side(q, 'S'); hang('Табличка заблудшего', 'bronze', s, 1.5, 0.5, 0.26, 0.02, { sink: -0.01 }); for (let i = 0; i < 10; i++) { const a = i / 10 * 2 * Math.PI; hang('Сухой венок', 'straw', off(s, Math.cos(a) * 0.26), 2.2 + Math.sin(a) * 0.26, 0.13, 0.06, 0.04, { sink: -0.01, roll: a / DEG + 90 }); }
    for (const k of ['E', 'W']) { const [px, pz] = niche(side(q, k, -0.4), 1.2, 0.4, 0.4); candles(px, 1.2, pz, 2, 0.05, k === 'E' ? 1 : 0); } D.tally(s[0], s[1] + 0.03, s[2] - 0.9, 1.1, 8); }
  // Тупик Мартина: восемь дней — восемь глубоких зарубок, чертёжная доска, циркуль, отвес; отсюда он тянул мерный шнур и ходил по кругу.
  { const q = cell(6, 1), w = side(q, 'W'), n = side(q, 'N'), s = side(q, 'S');
    for (let i = 0; i < 8; i++) hang('Зарубка дня', 'plaster', off(w, -0.5 + i * 0.14), 1.35, 0.03, 0.3, 0.014, { sink: -0.035, roll: J(4) }); for (let i = 0; i < 3; i++) hang('Нацарапанная строка', 'plaster', off(w, 0), 1.2 - i * 0.1, R(0.5, 0.8), 0.02, 0.012, { sink: -0.035, roll: J(2) });
    hang('Чертёжная доска', 'wood', off(n, -0.4), 0.05, 0.9, 1.1, 0.05, { sink: -0.1, pitch: 9 }); hang('Лист с планом', 'paper', off(n, -0.4), 0.3, 0.7, 0.7, 0.012, { sink: -0.19, pitch: 9 }); for (let i = 0; i < 4; i++) { hang('Линия плана', 'soot', off(n, -0.4), 0.42 + i * 0.14, 0.56, 0.015, 0.012, { sink: -0.2, pitch: 9 }); hang('Линия плана', 'soot', off(n, -0.64 + i * 0.16), 0.36, 0.015, 0.56, 0.012, { sink: -0.2, pitch: 9 }); }
    beam('Циркуль', 'iron', [2.2, 0.02, -11], [2.5, 0.03, -10.8], 0.02); beam('Циркуль', 'iron', [2.2, 0.02, -11], [2.52, 0.03, -11.1], 0.02); hang('Гвоздь', 'iron', off(s, 0.5), 1.9, 0.03, 0.03, 0.08, { sink: 0 }); { const [px, pz] = out(s, 0.06, 0.5); beam('Нить отвеса', 'rope', [px, 0.75, pz], [px, 1.9, pz], 0.012); put('Отвес', 'lead', px, 0.6, pz, 0.07, 0.15, 0.07, { round: true }); }
    put('Плащ зодчего под голову', 'clothBlue', 1.95, 0, -10.6, 0.6, 0.12, 0.4, { ry: 10 }); put('Пустая фляга', 'leather', 2.6, 0, -11.9, 0.14, 0.08, 0.26, { ry: 50, round: true }); candles(3.6, 0, -10.4, 6, 0.25); put('Мерная рейка', 'timber', 3.1, 0.02, -12.1, 1.6, 0.03, 0.05, { ry: 4 });
    hang('Кольцо шнура', 'iron', off(s, -0.7), 0.3, 0.1, 0.1, 0.06, { sink: 0 }); const cord = [[2.1, -12.15], [6.35, -11.95], [6.4, -7.65], [-3.45, -7.7], [-3.4, -10.5]];
    for (let i = 0; i + 1 < cord.length; i++) { beam('Мерный шнур Мартина', 'rope', [cord[i][0], 0.02, cord[i][1]], [cord[i + 1][0], 0.03, cord[i + 1][1]], 0.03); const len = Math.hypot(cord[i + 1][0] - cord[i][0], cord[i + 1][1] - cord[i][1]); for (let t = 1.4; t < len; t += 1.4) put('Узел-мерка', 'rope', cord[i][0] + (cord[i + 1][0] - cord[i][0]) * t / len, 0.01, cord[i][1] + (cord[i + 1][1] - cord[i][1]) * t / len, 0.07, 0.06, 0.07, { round: true }); if (i) put('Колышек шнура', 'iron', cord[i][0], 0, cord[i][1], 0.04, 0.14, 0.04); } }
  { const q = cell(7, 1), s = side(q, 'S'); arrow(s, 1.5, -1, { m: 'plaster', len: 0.5 }); hang('Нацарапанная строка', 'plaster', s, 1.3, 0.6, 0.02, 0.012, { sink: -0.035 }); F.sconce(q); }
  // Грибной тупик на востоке: «другой свет». Двое шли на него и остались.
  { const q = cell(9, 3); for (const k of ['N', 'S', 'W']) { const w = side(q, k), [x, z] = out(w, 0.3, J(0.3)); D.fungus(x, z, 8, { spread: 0.35 }); for (let i = 0; i < 5; i++) hang('Трутовик', 'fungus', off(w, J(0.8)), R(0.3, 2.8), R(0.22, 0.4), 0.08, R(0.16, 0.24), { sink: 0.02, round: true }); }
    corpse(10.6, -5.2, 100); corpse(11.7, -6.35, 180, { sit: true }); for (let i = 0; i < 5; i++) put('Споры', 'fungus', 11.2 + J(0.9), 0.003, -5.6 + J(0.9), R(0.3, 0.6), 0.012, R(0.2, 0.4), { round: true, ry: R(0, 180) }); put('Погасший фонарь', 'iron', 10.5, 0, -6.3, 0.18, 0.28, 0.18); }
  { const q = cell(10, 3), w = side(q, 'N'); arrow(w, 1.5, -1, { m: 'soot', len: 0.7 }); for (let i = 0; i < 6; i++) hang('Угольный рисунок: свет', 'soot', off(w, 0), 1.95, 0.3, 0.025, 0.012, { sink: -0.035, roll: i * 30 }); }
  // Тупик с сундуком: честный мел над ним и лампады в нишах.
  { const q = cell(9, 7); for (const k of ['E', 'W']) { const [px, pz] = niche(side(q, k, -0.3), 1.25, 0.4, 0.4); candles(px, 1.25, pz, 2, 0.05, 1); } const s = side(q, 'S'); for (const [du, t] of [[-0.12, -50], [0.08, 40]]) hang('Мел: здесь', 'bone', off(s, du), 1.6, du < 0 ? 0.2 : 0.42, 0.035, 0.012, { sink: -0.035, roll: t }); corpse(10.5, 6.3, 20, { sit: false }); beam('Ломик', 'iron', [11.9, 0.02, 6.4], [12.2, 0.04, 6], 0.035); }
  // Привал жильщиков в северо-западном тупике: костёр, спальник, карта того, что успели пройти.
  { const q = cell(1, 9), n = side(q, 'N'), e = side(q, 'E'); fire(-11.2, 12); put('Котелок', 'iron', -11.2, 0.12, 12, 0.26, 0.2, 0.26, { round: true }); bedroll(-12, 10.9, 0); put('Котомка', 'leather', -10.4, 0, 10.5, 0.42, 0.28, 0.3, { ry: 20 }); put('Бухта верёвки', 'rope', -10.45, 0, 11.3, 0.4, 0.1, 0.4, { round: true }); put('Кружка', 'clay', -10.6, 0, 11.9, 0.08, 0.1, 0.08, { round: true });
    D.tally(n[0], n[1] - 0.03, n[2] - 0.9, 1.6, 11); for (let i = 0; i < 7; i++) { const a = i * 1.15, r = 0.08 + i * 0.07; hang('Мел: улитка ходов', 'bone', off(e, Math.cos(a) * r), 1.5 + Math.sin(a) * r, 0.2 + i * 0.03, 0.03, 0.012, { sink: -0.035, roll: a / DEG + 90 }); } hang('Мел: вопрос', 'bone', off(e, 0.7), 1.45, 0.03, 0.2, 0.012, { sink: -0.035 }); }
  // Логово того, кто чертил углём: горшки сажи, отнятые котомки, проба стрелок на стене.
  { const q = cell(0, 1), s = side(q, 'S'), w = side(q, 'W'); for (const [x, z] of [[-14.8, -12], [-14.4, -12.1]]) { put('Горшок сажи', 'clay', x, 0, z, 0.24, 0.24, 0.24, { round: true }); put('Сажа', 'soot', x, 0.2, z, 0.2, 0.05, 0.2, { round: true }); } for (let i = 0; i < 5; i++) put('Уголёк', 'soot', -14.2 + J(0.5), 0, -11.8 + J(0.3), 0.05, 0.04, 0.14, { ry: R(0, 180) });
    for (const [x, z, a] of [[-13.3, -12], [-13.2, -11.5], [-13.45, -11.75]]) put('Отнятая котомка', 'leather', x, 0, z, 0.4, 0.24, 0.3, { ry: a || R(0, 90) }); put('Сапоги', 'leather', -13.2, 0, -10.9, 0.3, 0.3, 0.26, { ry: 20 }); bedroll(-14.75, -10.9, 0, 'clothBlue');
    for (const [du, y, dir] of [[-0.6, 1.7, 1], [0.3, 1.75, -1], [-0.2, 1.4, -1], [0.6, 1.35, 1], [0, 1.05, 1]]) arrow(off(s, du), y, dir, { m: 'soot', len: 0.4 }); blot('Отпечаток ладони', 'soot', w[0], w[1] + 0.03, w[2] + 0.3, 1.4, 0.16, 0.2, 0.012, -99); }
  // Юго-западный тупик, где появляются отряды: только стены.
  { const q = cell(0, 0); sconce(side(q, 'S'), 2.1); S.web([-15.1, 4.5, -15.1], [-14, 4.5, -15.1], { n: 6 }); hang('Печать Морнхейма', 'iron', side(q, 'W'), 2.4, 0.4, 0.4, 0.04, { sink: 0 }); }
  // Шипы на подходе к камере.
  spikes(-2.8, -5.6, 1.15); { const q = cell(4, 3), s = side(q, 'S'); for (const t of [45, -45]) hang('Мел: берегись', 'bone', off(s, 0.9), 1.55, 0.3, 0.03, 0.012, { sink: -0.035, roll: t }); vein(off(side(q, 'N'), -0.6), 0.8, 1.3); }

  // ---------- камера жилы ----------
  // План Мартина в бронзе на северной стене: тот, кто дошёл, видит весь ход.
  { const w = wallAt(0, 2.8, 'N'), s = 0.21, y0 = 1.85, strip = (u, y, ww, h) => hang('Бронзовая линия плана', 'bronze', off(w, u), y, ww, h, 0.03, { sink: -0.06 });
    hang('Плита плана Мартина', 'slab', w, y0 - 0.15, 2.7, 2.62, 0.08, { sink: 0.02 }); hang('Карниз плана', 'stone', w, y0 + 2.47, 2.9, 0.14, 0.16, { sink: 0.02 });
    for (const q of cells) { if (!q.n.E && !(q.door === 'E')) strip((q.i - 5 + 0.5) * s, y0 + q.j * s, 0.025, s); if (!q.n.N && !(q.door === 'N')) strip((q.i - 5) * s, y0 + (q.j + 1) * s - 0.012, s, 0.025); if (q.i === 0 && !q.n.W) strip(-5.5 * s, y0 + q.j * s, 0.025, s); if (q.j === 0 && !q.n.S) strip((q.i - 5) * s, y0 - 0.012, s, 0.025); }
    hang('Камень жилы на плане', 'ossarite', w, y0 + 5 * s + 0.05, 0.11, 0.11, 0.04, { sink: -0.06 }); candles(0.5, 0, 3.6, 3, 0.1, 1); candles(-0.5, 0, 3.65, 2, 0.08); }
  // Жила выходит на восточной стене: по ней и шли забойщики.
  { const w = wallAt(2.8, -1.2, 'E'); for (let i = 0; i < 16; i++) { const t = i / 15, u = -1.6 + t * 4 + J(0.25), y = 0.3 + t * 4 + J(0.3); hang('Жила оссарита', 'ossarite', off(w, u), y, R(0.3, 0.6), R(0.06, 0.14), 0.05, { sink: -0.04, roll: 42 + J(30) }); if (i % 3 === 0) hang('Кристалл жилы', 'ossarite', off(w, u + J(0.2)), y + 0.1, 0.08, R(0.2, 0.36), 0.12, { sink: -0.02, roll: J(40) }); }
    D.ossarite(3.6, -2.2, { n: 7, size: 1.2 }); D.ossarite(3.65, -0.4, { n: 5, size: 0.9 });
    for (const dz of [-0.25, 0.25]) beam('Тетива лестницы', 'timber', [3.25, 0, -1.3 + dz], [3.88, 3.2, -1.3 + dz], 0.07); for (let i = 1; i < 8; i++) put('Перекладина лестницы', 'timber', 3.25 + 0.63 * i / 8.2, i * 0.39, -1.3, 0.05, 0.05, 0.56);
    beam('Кирка', 'timber', [3.4, 0, -2.9], [3.85, 1.05, -3], 0.045); put('Клюв кирки', 'iron', 3.83, 1, -3, 0.06, 0.06, 0.46, { pitch: 10 });
    for (const [x, z] of [[3.45, 0.1], [3.5, -3.45]]) { put('Корзина забойщика', 'straw', x, 0, z, 0.46, 0.34, 0.46, { round: true }); for (let i = 0; i < 4; i++) put('Скол оссарита', 'ossarite', x + J(0.12), 0.3, z + J(0.12), 0.07, R(0.08, 0.16), 0.07, { roll: J(30), pitch: J(30) }); } }
  // Западная стена: стеллаж с лотками; южная: конторка приёмщика с весами.
  S.set('Полки с припасами', -3.7, -1.6, 0.5, 1.8, 2); for (const [y, z] of [[0.72, -2.1], [0.72, -1.2], [1.26, -1.7], [1.8, -2.2], [1.8, -1.1]]) { put('Лоток', 'wood', -3.7, y - 0.02, z, 0.4, 0.05, 0.5); for (let i = 0; i < 3; i++) put('Скол оссарита', 'ossarite', -3.7 + J(0.1), y + 0.03, z + J(0.15), 0.06, R(0.06, 0.14), 0.06, { roll: J(30), pitch: J(30) }); }
  S.set('Конторка', -1.2, -3.6, 1.1, 0.6, 1.15); S.set('Табурет', -1.2, -2.9, 0.4, 0.4, 0.45); S.books(-1.2, 1.12, -3.6, 3, { spread: 0.2 }); put('Стойка весов', 'iron', -0.3, 0, -3.7, 0.3, 0.05, 0.3, { round: true }); put('Стойка весов', 'iron', -0.3, 0.05, -3.7, 0.05, 1.3, 0.05); beam('Коромысло весов', 'iron', [-0.75, 1.3, -3.7], [0.15, 1.38, -3.7], 0.035); for (const [x, y] of [[-0.75, 1.3], [0.15, 1.38]]) { beam('Нить чаши', 'iron', [x, y - 0.4, -3.7], [x, y, -3.7], 0.012); put('Чаша весов', 'bronze', x, y - 0.44, -3.7, 0.3, 0.04, 0.3, { round: true }); }
  put('Гиря', 'bronze', -0.75, 0.9, -3.7, 0.14, 0.12, 0.14, { round: true }); { const w = wallAt(-2.4, -2.8, 'S'); S.board(w, { y: 1.55, w: 1.2, h: 0.8, sheets: 3, name: 'Ведомость жилы' }); D.plaque(w[0], w[1], 0.4, 2.3); }
  // Угли жаровни, копоть факела снаружи; тот, кто дошёл и не унёс.
  put('Угли жаровни', 'ember', -3.2, 1.08, -3.2, 0.7, 0.07, 0.7, { round: true }); D.patch(-3.2, -3.2, 0.9, { m: 'soot', name: 'Сажа', n: 4 }); { const w = wallAt(0, -5.6, 'N'); if (w) for (let i = 0; i < 4; i++) blot('Копоть', 'soot', w[0], w[1], w[2] + J(0.12), 3.2 + i * 0.25, R(0.3, 0.5), R(0.3, 0.45), 0.012, -99); }
  corpse(1.7, -1.7, 325); put('Котомка, набитая золотом', 'leather', 2.3, 0, -2.3, 0.44, 0.3, 0.34, { ry: 40 }); for (let i = 0; i < 8; i++) put('Монета', 'gold', 2 + J(0.6), 0.003, -1.9 + J(0.6), 0.05, 0.012, 0.05, { round: true });
  for (const [x, z] of [[-3.7, 3.7], [3.7, 3.7]]) S.web([x, 5.6, z], [x - Math.sign(x) * 1.1, 5.6, z], { n: 6 }); S.banner(wallAt(-2.8, 0, 'W'), 4.6, { w: 1.1, h: 2.2, torn: 0.5 }); for (let i = 0; i < 3; i++) vein(off(wallAt(-2.8, 2 - i * 0.2, 'W'), i * 0.6 - 0.3), 2.2 + i * 0.5, 1.2);
};
